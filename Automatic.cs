using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;

namespace ObjectStoreE
{
    internal class IntHolder
    {
        public int value;
        public IntHolder(int value)
        {
            this.value = value;
        }
        public override string ToString()
        {
            return value.ToString();
        }
        public static IntHolder operator +(IntHolder a, int b)
        {
            a.value += b;
            return a;
        }
    }
    public class Automatic
    {

        public static object ConvertRegionToObject(Region input)
        {
            return ConvertPointer(input, 0, new());
        }
        private static object ConvertPointer(Region input, int pointer, Dictionary<int, object> pointers)
        {

            Region pointingRegion = input.FindSubregionWithName(pointer.ToString());

            string typeName = pointingRegion.FindDirectValue("type").value;
            Type? objType = /*(AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.FullName == typeName)) ?? */Type.GetType(typeName); 

            if (objType == null)
                throw new Exception("Type specefied in file is invalid. It might be due to a version change.");
            List<FieldInfo> fields = objType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList();
            fields.AddRange(objType.BaseType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList());
            object thisObject;
            if (objType.IsArray)
            {
                //Create List of array type to add Items and later convert into array
                Type elementType = objType.GetElementType();
                Type listType = typeof(List<>).MakeGenericType(elementType);
                object list = Activator.CreateInstance(listType);

                Region arrayRegion = pointingRegion.FindSubregionWithName("array");
                int counter = 0;

                while (true)
                {
                    Region? currentIndexRegion = arrayRegion.FindSubregionWithName(counter.ToString(), false, true);
                    if (currentIndexRegion == null) //No more indices
                        break;
                    string? currentIndexPointer = currentIndexRegion.FindDirectValue("p").value;

                    if (currentIndexPointer == null) //Index is null
                    {
                        list.GetType().GetMethod("Add").Invoke(list, new object?[] { null });

                    }
                    else
                    {
                        int currentIndexPointerLiteral = int.Parse(currentIndexPointer);
                        if (pointers.TryGetValue(currentIndexPointerLiteral, out object? foundPointer)) //Index has already been parsed
                        {
                            list.GetType().GetMethod("Add").Invoke(list, new object[] { foundPointer });
                        }
                        else
                        {

                            list.GetType().GetMethod("Add").Invoke(list, new object[] { ConvertPointer(input, currentIndexPointerLiteral, pointers) });
                        }
                    }
                    counter++;
                }
                thisObject = list.GetType().GetMethod("ToArray").Invoke(list, null);
                return thisObject;
            }
            if (objType == typeof(string))
            {
                thisObject = pointingRegion.FindDirectValue("v").value;
                return thisObject;
            }


            thisObject = FormatterServices.GetUninitializedObject(objType);

            pointers.Add(pointer, thisObject);
            Type genericListType = typeof(List<>).MakeGenericType(objType);

            foreach (Region fieldRegion in pointingRegion.FindSubregionWithNameArray("field"))
            {
                string name = fieldRegion.FindDirectValue("n").value ?? throw new Exception("Cannot be null");
                string? value = fieldRegion.FindDirectValue("v").value;
                FieldInfo? field = fields.ToList().FirstOrDefault(x => x.Name == name, null);
                if (field == null)
                {
                    throw new Exception("Invalid field name");
                }
                if (value == null)
                {
                    field.SetValue(thisObject, null);
                    continue;

                }

                if (field.FieldType == typeof(bool))
                {
                    field.SetValue(thisObject, bool.Parse(value));
                    continue;
                }
                if (field.FieldType == typeof(byte))
                {
                    field.SetValue(thisObject, byte.Parse(value));
                    continue;

                }
                if (field.FieldType == typeof(sbyte))
                {
                    field.SetValue(thisObject, sbyte.Parse(value));
                    continue;
                }
                if (field.FieldType == typeof(short))
                {
                    field.SetValue(thisObject, short.Parse(value));
                    continue;
                }
                if (field.FieldType == typeof(ushort))
                {
                    field.SetValue(thisObject, ushort.Parse(value));
                    continue;
                }
                if (field.FieldType == typeof(int))
                {
                    field.SetValue(thisObject, int.Parse(value));
                    continue;
                }
                if (field.FieldType == typeof(uint))
                {
                    field.SetValue(thisObject, uint.Parse(value));
                    continue;
                }
                if (field.FieldType == typeof(long))
                {
                    field.SetValue(thisObject, long.Parse(value));
                    continue;

                }
                if (field.FieldType == typeof(ulong))
                {
                    field.SetValue(thisObject, ulong.Parse(value));
                    continue;
                }
                if (field.FieldType == typeof(float))
                {
                    field.SetValue(thisObject, float.Parse(value));
                    continue;
                }
                if (field.FieldType == typeof(double))
                {
                    field.SetValue(thisObject, double.Parse(value));
                    continue;
                }
                if (field.FieldType == typeof(decimal))
                {
                    field.SetValue(thisObject, decimal.Parse(value));
                    continue;
                }
                //Char types
                if (field.FieldType == typeof(char))
                {
                    field.SetValue(thisObject, char.Parse(value));
                    continue;
                }
                if (field.FieldType == typeof(string))
                {
                    field.SetValue(thisObject, value);
                    continue;
                }
                //Custom objects
                if (pointers.TryGetValue(int.Parse(value), out object pointing))
                {
                    field.SetValue(thisObject, pointing);
                    continue;
                }
                field.SetValue(thisObject, ConvertPointer(input, int.Parse(value), pointers));
            }

            return thisObject;
        }

        public static Region ConvertObjectToRegion(object obj, string regionName)
        {
            Region result = new(regionName);
            result.SubRegions.AddRange(ConvertObject(obj, new(0)));
            return result;
        }



        private static List<Region> ConvertObject(object obj, IntHolder pointerCount, Dictionary<object, int>? pointerObjectMap = null)
        {
            List<Region> result = new List<Region>() { new(pointerCount.ToString()) }; //Result contains all objects that have been converted by this functions (And recursive calls)

            Region currentObject = result[0];
            currentObject.DirectValues.Add(new("type", obj.GetType().AssemblyQualifiedName, false));
            if (pointerObjectMap == null)
            {
                pointerObjectMap = new Dictionary<object, int>();
            }
            pointerObjectMap.Add(obj, pointerCount.value);
            pointerCount += 1;

            if (obj.GetType().IsArray)
            {
                Region enumerable = new("array");
                currentObject.SubRegions.Add(enumerable);
                int count = -1;
                foreach (object itemObject in (IEnumerable)obj)
                {
                    count++;
                    Region item = new(count.ToString());
                    enumerable.SubRegions.Add(item);

                    if (itemObject == null)
                    {
                        item.DirectValues.Add(new("p", null, false));

                        continue;
                    }

                    if (pointerObjectMap.TryGetValue(itemObject, out int pointerValue)) // Has already parsed
                    {
                        item.DirectValues.Add(new("p", pointerValue.ToString(), false));

                        continue;
                    }
                    item.DirectValues.Add(new("p", pointerCount.ToString(), false));

                    
                    result.AddRange(ConvertObject(itemObject, pointerCount, pointerObjectMap));
                }
                return result;
            }
            if (obj is string)
            {
                currentObject.DirectValues.Add(new("v", (string)obj, false));
                return result;
            }



            Type type = obj.GetType();
            List<FieldInfo> fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList();
            fields.AddRange(type.BaseType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ToList());

            foreach (FieldInfo field in fields)
            {
                Region currentField = new("field");
                currentField.DirectValues.Add(new("n", field.Name, false));
                
                currentObject.SubRegions.Add(currentField);
                if (field.GetValue(obj) == null)
                {
                    currentField.DirectValues.Add(new("v", null, false));

                    continue;
                }
                //Numeric
                if (field.FieldType == typeof(bool))
                {
                    currentField.DirectValues.Add(new("t", "bool", false));

                    currentField.DirectValues.Add(new("v", ((bool)field.GetValue(obj)).ToString(), false));
                    continue;

                }
                if (field.FieldType == typeof(byte))
                {
                    currentField.DirectValues.Add(new("t", "byte", false));

                    currentField.DirectValues.Add(new("v", ((byte)field.GetValue(obj)).ToString(), false));
                    continue;

                }
                if (field.FieldType == typeof(sbyte))
                {
                    currentField.DirectValues.Add(new("t", "sbyte", false));

                    currentField.DirectValues.Add(new("v", ((sbyte)field.GetValue(obj)).ToString(), false));
                    continue;

                }
                if (field.FieldType == typeof(short))
                {
                    currentField.DirectValues.Add(new("t", "short", false));

                    currentField.DirectValues.Add(new("v", ((short)field.GetValue(obj)).ToString(), false));
                    continue;

                }
                if (field.FieldType == typeof(ushort))
                {
                    currentField.DirectValues.Add(new("t", "ushort", false));

                    currentField.DirectValues.Add(new("v", ((ushort)field.GetValue(obj)).ToString(), false));
                    continue;

                }
                if (field.FieldType == typeof(int))
                {
                    currentField.DirectValues.Add(new("t", "int", false));

                    currentField.DirectValues.Add(new("v", ((int)field.GetValue(obj)).ToString(), false));
                    continue;

                }
                if (field.FieldType == typeof(uint))
                {
                    currentField.DirectValues.Add(new("t", "uint", false));

                    currentField.DirectValues.Add(new("v", ((uint)field.GetValue(obj)).ToString(), false));
                    continue;
                }
                if (field.FieldType == typeof(long))
                {
                    currentField.DirectValues.Add(new("t", "long", false));

                    currentField.DirectValues.Add(new("v", ((long)field.GetValue(obj)).ToString(), false));
                    continue;

                }
                if (field.FieldType == typeof(ulong))
                {
                    currentField.DirectValues.Add(new("t", "ulong", false));

                    currentField.DirectValues.Add(new("v", ((ulong)field.GetValue(obj)).ToString(), false));
                    continue;

                }
                if (field.FieldType == typeof(float))
                {
                    currentField.DirectValues.Add(new("t", "float", false));

                    currentField.DirectValues.Add(new("v", ((float)field.GetValue(obj)).ToString(), false));
                    continue;

                }
                if (field.FieldType == typeof(double))
                {
                    currentField.DirectValues.Add(new("t", "double", false));

                    currentField.DirectValues.Add(new("v", ((double)field.GetValue(obj)).ToString(), false));
                    continue;

                }
                if (field.FieldType == typeof(decimal))
                {
                    currentField.DirectValues.Add(new("t", "decimal", false));

                    currentField.DirectValues.Add(new("v", ((decimal)field.GetValue(obj)).ToString(), false));
                    continue;

                }
                //Char types
                if (field.FieldType == typeof(char))
                {
                    currentField.DirectValues.Add(new("t", "char", false));

                    currentField.DirectValues.Add(new("v", ((char)field.GetValue(obj)).ToString(), false));
                    continue;

                }
                if (field.FieldType == typeof(string))
                {
                    currentField.DirectValues.Add(new("t", "string", false));

                    currentField.DirectValues.Add(new("v", (string)field.GetValue(obj), false));
                    continue;

                }
                //Custom objects
                currentField.DirectValues.Add(new("t", "p", false));
                if (pointerObjectMap.TryGetValue(field.GetValue(obj), out int pointerValue)) // Has already parsed
                {
                    currentField.DirectValues.Add(new("v", pointerValue.ToString(), false));
                    continue;
                }
                currentField.DirectValues.Add(new("v", pointerCount.ToString(), false));

                result.AddRange(ConvertObject(field.GetValue(obj), pointerCount, pointerObjectMap));

            }
            return result;
        }
    }
}
