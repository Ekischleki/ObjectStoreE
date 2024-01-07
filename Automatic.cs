using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;

namespace ObjectStoreE
{



    public static class Automatic
    {
        public class ConversionContext
        {
            private int nextPointer = 0;
            public string GetNextPointer()
            {
                try
                {
                    return nextPointer.ToString("X");
                }
                finally
                {
                    nextPointer++;
                }
            }
            public readonly Dictionary<object, string> PointerMap = new();
            public readonly Queue<(object obj, string usePointer)> QueuedConversions = new();
            public readonly Region result;
            public ConversionContext()
            {
                result = new();
            }
            /// <summary>
            /// A pointer to an object (Will enqueue if object has not been parsed)
            /// </summary>
            /// <param fieldName="obj"></param>
            /// <returns></returns>
            public string GetReferencePointer(object obj)
            {
                if (PointerMap.TryGetValue(obj, out string? ptrName))
                {
                    return ptrName;
                }
                ptrName = GetNextPointer();
                QueuedConversions.Enqueue((obj, ptrName));
                PointerMap.Add(obj, ptrName);
                return ptrName;
            }
        }

        private class ParsingContext
        {

            public ParsingContext(Region data)
            {
                this.data = data;
            }
            public readonly Region data;
            public Dictionary<string, object> PointerMap = new();
            public readonly Queue<(string pointer, object obj)> QueuedParsings = new();



            public object GetReferenceObj(string pointer, Type objectType)
            {
                if (PointerMap.TryGetValue(pointer, out object? reference))
                {
                    return reference;
                }
                object resultObject;
                if (objectType.IsArray)
                {

                    Region pointerData = data.FindSubregionWithName(pointer);
                    resultObject = Array.CreateInstance(objectType.GetElementType()!, int.Parse(pointerData.FindDirectValue("c") ?? throw new Exception("Value cannot be null")));
                }
                else
                {
                    resultObject = FormatterServices.GetUninitializedObject(objectType);
                }


                QueuedParsings.Enqueue((pointer, resultObject));
                PointerMap.Add(pointer, resultObject);
                return resultObject;
            }
        }

        private static readonly Dictionary<string, Type> typeShortcuts = new()
        {
            { "s",  typeof(string) },
            { "i", typeof(int) },
            { "l", typeof(long) },
            { "f", typeof(float) },
            { "d", typeof(double) },
            { "b", typeof(bool) },
            { "byte", typeof(byte) },

        };

        private static readonly Dictionary<Type, string> typeNames = ReverceDict(typeShortcuts);

        private static Dictionary<U, T> ReverceDict<T, U>(Dictionary<T, U> dict)
            where U : notnull
            where T : notnull
        {
            Dictionary<U, T> result = new();
            foreach (var key in dict)
            {
                result.Add(key.Value, key.Key);
            }
            return result;
        }
        public static ExpectedType? ConvertRegionToObject<ExpectedType>(Region input)
        {

            ParsingContext parser = new(input);

            string basePtr = input.FindDirectValue("base") ?? throw new Exception("Value cannot be null");

            Region baseRegion = input.FindSubregionWithName(basePtr)!;

            object resultObject = parser.GetReferenceObj(basePtr, typeof(ExpectedType));



            while (parser.QueuedParsings.Count > 0)
            {
                var ptr = parser.QueuedParsings.Dequeue().pointer;

                if (ptr == basePtr)
                {
                    ConvertPointer(ptr, parser, typeof(ExpectedType));
                }
                else
                {
                    ConvertPointer(ptr, parser, null);
                }
            }

            return (ExpectedType?)resultObject;
        }



        private static void ConvertPointer(string pointer, ParsingContext parser, Type? expectedType)
        {
            Region thisRegion = parser.data.FindSubregionWithName(pointer);

            string typeName = thisRegion.FindDirectValue("t");

            Type? objType = (typeShortcuts.GetValueOrDefault(typeName) ?? Type.GetType(typeName))
                ?? throw new Exception("Type specefied in file is invalid. It might be due to a version change.");
            if (expectedType != null && !expectedType.IsAssignableFrom(objType))
                throw new Exception("The input ptr did not match the expected objectType");

            List<FieldInfo> fields = objType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList();
            fields.AddRange(objType.BaseType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList());
            object thisObject = parser.GetReferenceObj(pointer, objType);
            if (objType.IsArray)
            {
                Type elementType = objType.GetElementType()!;
                Type listType = typeof(List<>).MakeGenericType(elementType);
                IList list = Activator.CreateInstance(listType)! as IList ?? throw new Exception();

                Region arrayRegion = thisRegion.FindSubregionWithName("a");
                int counter = 0;

                while (true)
                {
                    Region? currentIndexRegion = arrayRegion.FindSubregionWithNameOrDefault(counter.ToString());
                    if (currentIndexRegion == null) //No more indices
                        break;
                    string? currentIndexPointer = currentIndexRegion.FindDirectValueOrDefault("p", null);

                    if (currentIndexPointer == null) //Index is null
                    {

                        list.Add(null);

                    }
                    else
                    {
                        list.Add(parser.GetReferenceObj(currentIndexPointer, elementType));

                    }
                    counter++;
                }

                list.CopyTo((Array)thisObject, 0);
                return;
            }


            if (thisObject is IConvertable convertable) //We can use the implemented parser. This should save a lot of space.
            {
                convertable.LoadByRegion(thisRegion);
                return;
            }
            //Type genericListType = typeof(List<>).MakeGenericType(objType);

            foreach (Region fieldRegion in thisRegion.FindSubregionsWithName("f"))
            {
                string fieldName = fieldRegion.FindDirectValue("n") ?? throw new Exception("Cannot be null");
                string? fieldValue = fieldRegion.FindDirectValue("v");
                FieldInfo? field = fields.FirstOrDefault(x => x.Name == fieldName, null) ?? throw new Exception("Cannot decode field as it doesn't exist anymore.");

                if (fieldValue == null)
                {
                    field.SetValue(thisObject, null);
                    continue;

                }

                if (field.FieldType == typeof(bool))
                {
                    field.SetValue(thisObject, bool.Parse(fieldValue));
                    continue;
                }
                if (field.FieldType == typeof(byte))
                {
                    field.SetValue(thisObject, byte.Parse(fieldValue));
                    continue;

                }
                if (field.FieldType == typeof(sbyte))
                {
                    field.SetValue(thisObject, sbyte.Parse(fieldValue));
                    continue;
                }
                if (field.FieldType == typeof(short))
                {
                    field.SetValue(thisObject, short.Parse(fieldValue));
                    continue;
                }
                if (field.FieldType == typeof(ushort))
                {
                    field.SetValue(thisObject, ushort.Parse(fieldValue));
                    continue;
                }
                if (field.FieldType == typeof(int))
                {
                    field.SetValue(thisObject, int.Parse(fieldValue));
                    continue;
                }
                if (field.FieldType == typeof(uint))
                {
                    field.SetValue(thisObject, uint.Parse(fieldValue));
                    continue;
                }
                if (field.FieldType == typeof(long))
                {
                    field.SetValue(thisObject, long.Parse(fieldValue));
                    continue;

                }
                if (field.FieldType == typeof(ulong))
                {
                    field.SetValue(thisObject, ulong.Parse(fieldValue));
                    continue;
                }
                if (field.FieldType == typeof(float))
                {
                    field.SetValue(thisObject, float.Parse(fieldValue));
                    continue;
                }
                if (field.FieldType == typeof(double))
                {
                    field.SetValue(thisObject, double.Parse(fieldValue));
                    continue;
                }
                if (field.FieldType == typeof(decimal))
                {
                    field.SetValue(thisObject, decimal.Parse(fieldValue));
                    continue;
                }
                //Char types
                if (field.FieldType == typeof(char))
                {
                    field.SetValue(thisObject, char.Parse(fieldValue));
                    continue;
                }
                if (field.FieldType == typeof(string))
                {
                    field.SetValue(thisObject, fieldValue);
                    continue;
                }
                //Mutable objects
                Region pointingFieldRegion = parser.data.FindSubregionWithNameOrDefault(fieldValue)!;
                Type fieldRuntimeType = Type.GetType(pointingFieldRegion.FindDirectValue("t") ?? throw new Exception("Cannot be null")) ?? throw new Exception("Invalid runtime objectType, perhaps the binary changed between versions.");
                
                field.SetValue(thisObject, parser.GetReferenceObj(fieldValue, fieldRuntimeType));
                continue;

            }

            return;
        }
        public static Region ConvertObjectToRegion(object obj)
        {
            ConversionContext conversion = new();

            conversion.result.AddDirectValue("base", conversion.GetReferencePointer(obj));
            while (conversion.QueuedConversions.Count > 0)
            {
                
                var (convertObj, pointer) = conversion.QueuedConversions.Dequeue();
                //Debug.Assert(conversion.result.Subregions.All(x => x.value.Subregions.Any(x => x.name == "f")));
                //Debug.Assert(convertObj.GetType() != typeof(string));
                ConvertObject(convertObj, pointer, conversion);
            }

            return conversion.result;
        }



        private static void ConvertObject(object obj, string pointer, ConversionContext conversion)
        {
            if (obj is IConvertable convertable)
            {
                Region implementedConvert = convertable.ConvertToRegion(conversion);
                if (implementedConvert.FindDirectValueOrDefault("t", defaultAtNullValue: string.Empty) != null)
                {
                    throw new Exception("IConvertables can't contain a direct fieldValue called 't' in automatic conversions");
                }

                implementedConvert.AddDirectValue("t", obj.GetType().AssemblyQualifiedName);
                conversion.result.AddSubRegion(pointer, implementedConvert);
                return;
            }

            //List<Region> ptrName = new List<Region>() { new(pointerCount.ToString()) }; //Result contains all objects that have been converted by this functions (And recursive calls)



            Region currentObject = new();
            conversion.result.AddSubRegion(pointer, currentObject);

            Type objectType = obj.GetType();

            Debug.Assert(objectType != typeof(string));
            var typeName = typeNames.GetValueOrDefault(objectType);
            string assemblyTypeName = typeName ?? obj.GetType().AssemblyQualifiedName ?? throw new Exception("Assembly Qualified name for object objectType is null");

            currentObject.AddDirectValue("t", assemblyTypeName);

            if (objectType.IsArray)
            {
                currentObject.AddDirectValue("c", ((Array)obj).Length.ToString());
                Region arrayRegion = new();
                currentObject.AddSubRegion("a", arrayRegion);
                int count = -1;
                foreach (object itemObject in (IEnumerable)obj)
                {
                    count++;
                    Region item = new();
                    arrayRegion.AddSubRegion(count.ToString(), item);

                    if (itemObject != null)
                    {
                        item.AddDirectValue("p", conversion.GetReferencePointer(itemObject));
                    }


                }
                return;
            }


            List<FieldInfo> fields = objectType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList();
            fields.AddRange(objectType.BaseType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList());

            foreach (FieldInfo field in fields)
            {
                Region currentField = new();
                currentField.AddDirectValue("n", field.Name);
                currentObject.AddSubRegion("f", currentField);

                var fieldValue = field.GetValue(obj);
                if (fieldValue == null)
                {
                    currentField.AddDirectValue("v", null);

                    continue;
                }



                //Numeric
                if (field.FieldType == typeof(bool))
                {
                    currentField.AddDirectValue("t", "bool");

                    currentField.AddDirectValue("v", ((bool)fieldValue).ToString());
                    continue;

                }
                if (field.FieldType == typeof(byte))
                {
                    currentField.AddDirectValue("t", "byte");

                    currentField.AddDirectValue("v", ((byte)fieldValue).ToString());
                    continue;

                }
                if (field.FieldType == typeof(sbyte))
                {
                    currentField.AddDirectValue("t", "sbyte");

                    currentField.AddDirectValue("v", ((sbyte)fieldValue).ToString());
                    continue;

                }
                if (field.FieldType == typeof(short))
                {
                    currentField.AddDirectValue("t", "short");

                    currentField.AddDirectValue("v", ((short)fieldValue).ToString());
                    continue;

                }
                if (field.FieldType == typeof(ushort))
                {
                    currentField.AddDirectValue("t", "ushort");

                    currentField.AddDirectValue("v", ((ushort)fieldValue).ToString());
                    continue;

                }
                if (field.FieldType == typeof(int))
                {
                    currentField.AddDirectValue("t", "int");

                    currentField.AddDirectValue("v", ((int)fieldValue).ToString());
                    continue;

                }
                if (field.FieldType == typeof(uint))
                {
                    currentField.AddDirectValue("t", "uint");

                    currentField.AddDirectValue("v", ((uint)fieldValue).ToString());
                    continue;
                }
                if (field.FieldType == typeof(long))
                {
                    currentField.AddDirectValue("t", "long");

                    currentField.AddDirectValue("v", ((long)fieldValue).ToString());
                    continue;

                }
                if (field.FieldType == typeof(ulong))
                {
                    currentField.AddDirectValue("t", "ulong");

                    currentField.AddDirectValue("v", ((ulong)fieldValue).ToString());
                    continue;

                }
                if (field.FieldType == typeof(float))
                {
                    currentField.AddDirectValue("t", "float");

                    currentField.AddDirectValue("v", ((float)fieldValue).ToString());
                    continue;

                }
                if (field.FieldType == typeof(double))
                {
                    currentField.AddDirectValue("t", "double");

                    currentField.AddDirectValue("v", ((double)fieldValue).ToString());
                    continue;

                }
                if (field.FieldType == typeof(decimal))
                {
                    currentField.AddDirectValue("t", "decimal");

                    currentField.AddDirectValue("v", ((decimal)fieldValue).ToString());
                    continue;

                }
                //Char types
                if (field.FieldType == typeof(char))
                {
                    currentField.AddDirectValue("t", "char");

                    currentField.AddDirectValue("v", ((char)fieldValue).ToString());
                    continue;

                }
                if (field.FieldType == typeof(string))
                {
                    currentField.AddDirectValue("t", "string");

                    currentField.AddDirectValue("v", (string)fieldValue);
                    continue;

                }
                //Custom objects
                currentField.AddDirectValue("t", "p");

                currentField.AddDirectValue("v", conversion.GetReferencePointer(fieldValue));
            }
            return;
        }
    }
}
