# ObjectStoreE
## Regions
A region is a Type in this project, which can contain SubRegions and Direct values. Subregions are more Regions inside the Region and Direct values are string values inside a Region. These regions are used, because they can be converted into strings and back. You can use the ```new Region("Region name")``` constructor, to create a new Region. Then, you can use ```.SubRegions``` and ```.DirectValues```  to access Subregions and direct Values of the Region. You can also create a region using the ```CreateSingleRegionByString()``` method, which will return a Region, based on a Region save string, which you can generate using ```.RegionSaveString```.
## Automatic Object conversion
IMPORTANT: Automatic Object conversion will save all fields (Public and Private) of an object including other objects. But due to saving and loading all values, IT MIGHT NOT BE MEMORY SAVE. So please be sure to use it with caution. Due to the Apache license, there's no warranty, that saving or loading works, or is memory-safe.
This system even supports self-reference, by saving objects in Regions with pointer values. It could even be used to perfectly copy an Object, but it is not optimized for it. To convert an Object to a string, use the following syntax:
```
using ObjectStoreE;
string[] myObject = new[] {"Hello World!", "ByeWorld"}; //This can be any object, I choose a string array for this example
Region convertedObjectRegion = Automatic.ConvertObjectToRegion(myObject, "Region Name (My object)"); // Use the Automatic class to make a Region out of your object
string convertedObjectString = convertedObjectRegion.RegionSaveString; //Get the region save string
// Or in one line:
string converttedObjectString = Automatic.ConvertObjectToRegion(myObject, "my object region name").RegionSaveString;
```
To convert it back, just use this syntax:
```
using ObjectStoreE;
string regionString = <region save string here>;
Region objectRegion = Region.CreateSingleRegionByString(regionString); //Create a Region out of your region save string
string[] myRecreatedStringArray = Automatic.ConvertRegionToObject(objectRegion) as string[]; //Now just recreate the object with the Region you just created (This will return an object type, so you need to unbox it)
```
That's pretty much it for automatic conversion. Disclaimer again: THIS IS NOT MEMORY SAFE IF SOMEBODY HAS ACCESS TO THE STRING GENERATED BETWEEN CONVERSIONS!!!! The save string even allows people, who have access to it, to create completely new objects to e.g. download malicious programs.
## Manual saving
Manual saving is much safer, as you have control, over what happens with your Regions. It is standard, that every object, you want to save, has a property, which returns its Region and one constructor accepting a region.
Here's an example:
```
public class SaveObject0
{
 string saveString = "Some Value";
 int saveInt = 42;
 SaveObject1 anotherObject;

 public Region RegionSave //Region save property
 {
  get
  {
    Region result = new("SaveObject0"); //Create a new region. The standard is to call it the same name as your class (for this the name is "SaveObject0"
    result.SubRegions.Add(anotherObject.RegionSave); //Save the anotherObject object, by calling its RegionSave property
    result.DirectValues.Add(new("saveString", saveString)); //Add a direct value to this region, to save 
    result.DirectValues.Add(new("saveInt", saveInt.ToString())); //Because Regions need to get converted to strings, every direct value needs to be in string form.
    return result;
  }
 }

 public SaveObject0(Region region) //Region constructor
 {
  saveString = region.FindDirectValue("saveString").value; //Search the direct value with the name "saveString" and get its value
  saveInt = Int.Parse(region.FindDirectValue("saveInt").value); //Same for the int, but because values can only be in string form, you need to parse it before.
  anotherObject = new(region.FindSubregionWithName("SaveObject1"); //Call the constructor of the class of the object with a SubRegion of your Region, with the name "SaveObject1"
 }
}
```
Manual saving might be more annoying to program, but it's often more than twice as efficient and safe.
## End
Good luck! And feel free to contribute to this cheap json fake :P

