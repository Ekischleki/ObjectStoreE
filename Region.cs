using System.Text;
using System.Xml.Linq;

namespace ObjectStoreE
{
    public class Region
    {
        public string regionName;
        private List<Region>? subRegions;
        private readonly List<string>? subRegionsUnRead;
        public List<DirectValue> DirectValues { get; set; }

        public override string ToString()
        {
            return RegionSaveString;
        }
        public string RegionSaveString
        {
            get
            {
                return GenerateSaveString();
            }
        }
        public List<Region> SubRegions
        {
            get
            {
                if (subRegions == null)
                    if (subRegionsUnRead == null)
                        subRegions = new List<Region>();
                    else
                        subRegions = new List<Region>(Read.TopLevelRegion(subRegionsUnRead.ToArray()));
                return subRegions;
            }
            set
            {
                if (subRegions == null)
                    if (subRegionsUnRead == null)
                        subRegions = null;
                    else
                        subRegions = new List<Region>(Read.TopLevelRegion(subRegionsUnRead.ToArray()));
                subRegions = value;
            }
        }
        /// <summary>
        /// This will return a region based on the input string you provide it with. This can be used, to convert a RegionSaveString back to a Region.
        /// This method will throw an exception, if there are multible top level regions, or if there are none.
        /// </summary>
        /// <param name="regionData"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>

        public static Region CreateSingleRegionByString(string regionData)
        {
            List<Region> topLevelRegions = Read.TopLevelRegion(regionData.Split(';'));
            if (topLevelRegions.Count == 0)
                throw new Exception("No valid top level region.");
            if (topLevelRegions.Count > 1)
                throw new Exception("Multible top level regions.");
            return topLevelRegions[0];
        }

        public static Region CreateSingleRegionByPath(string path)
        {
            path = path.Replace("\"", "");
            if (!File.Exists(path))
                throw new Exception($"{path} does not exist");
            string regionData = File.ReadAllText(path);
            List<Region> topLevelRegions = Read.TopLevelRegion(regionData.Split(';'));
            if (topLevelRegions.Count == 0)
                throw new Exception("No valid top level region.");
            if (topLevelRegions.Count > 1)
                throw new Exception("Multible top level regions.");
            return topLevelRegions[0];
        }
        /// <summary>
        /// This will return a region based on the input string you provide it with. This can be used, to convert a RegionSaveString back to a Region.
        /// It is assumed, that there will be multible top level regions when using this method, so you must provide the name of the top level region you're seeking.
        /// It is not recommended to use this method though, because it is standart, to have one top level region and because the it will have to reread the region data everytime you use this.
        /// This method will throw an exception, if the requested region wasn't found, or if there were multible found.
        /// </summary>
        /// <param name="regionData"></param>
        /// <param name="regionName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Region CreateSingleRegionByString(string regionData, string regionName)
        {
            List<Region> foundTopLevelRegions = Read.TopLevelRegion(regionData.Split(';')).Where(x => x.regionName == regionName).ToList();
            if (foundTopLevelRegions.Count == 0)
                throw new Exception("No valid top level region.");
            if (foundTopLevelRegions.Count > 1)
                throw new Exception("Multible top level regions.");
            return foundTopLevelRegions[0];
        }
        /// <summary>
        /// Gets all top level regions on the provided region Data. This can be used, to convert a RegionSaveString back to a Region.
        /// </summary>
        /// <param name="regionData"></param>
        /// <returns></returns>
        public static List<Region> GetTopLevelRegions(string regionData)
        {
            return Read.TopLevelRegion(regionData.Split(';'));
        }
        internal Region(string regionName, List<string> subRegions, List<DirectValue> directValues)
        {
            this.regionName = regionName;
            this.subRegionsUnRead = new List<string>(subRegions);
            this.DirectValues = new List<DirectValue>(directValues);
        }
        public Region(string regionName)
        {
            this.regionName = regionName;
            this.subRegions = new();
            this.DirectValues = new();
        }


        public DirectValue[] FindDirectValueArray(string directValueName)
        {
            return DirectValues.Where(x => x.name == directValueName).ToArray();
        }
        public DirectValue FindDirectValue(string directValueName)
        {
            DirectValue[] temp = DirectValues.Where(x => x.name == directValueName).ToArray();
            if (temp.Length != 1)
                return new("nothingFound", "", false);
            return temp[0];
        }
        public Region[] FindSubregionWithNameArray(string name)
        {
            return SubRegions.Where(x => x.regionName == name).ToArray();
        }
        public Region? FindSubregionWithName(string directValueName, bool failAtNotFound = true, bool failAtMultibleFound = true)
        {
            Region[] foundRegions = SubRegions.Where(x => x.regionName == directValueName).ToArray();
            if (foundRegions.Length == 0)
            {
                if (failAtNotFound)
                    throw new Exception("There were no regions with the name  " + directValueName + " found");
                return null;
            } else if (foundRegions.Length > 1 && failAtMultibleFound)
            {
                throw new Exception("There are more than 1 regions with the name " + directValueName);

            }
            return foundRegions[0];
        }

        private static StringBuilder sb = new();
        private static string ConvertListToString(List<string> strings)
        {
            sb.Clear();
            foreach (string s in strings)
                if (s.Contains(';'))
                    sb.Append(s);
                else
                {
                    sb.Append(s);
                    sb.Append(";");
                }
            return sb.ToString();
        }
        private string GenerateSaveString()
        {
            List<string> saveStringList = new()
            {
                $"§{regionName}"
            };
            foreach (DirectValue directValue in DirectValues)
            {
                saveStringList.Add($"-{directValue.name}:{DirectValueClearify.EncodeInvalidChars(directValue.value)}");
            }

            if (SubRegions != null)
                foreach (Region region in SubRegions)
                    saveStringList.Add(region.RegionSaveString);
            saveStringList.Add("$");
            return ConvertListToString(saveStringList);
        }
    }

}