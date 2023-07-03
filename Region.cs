using System.Xml.Linq;

namespace ObjectStoreE
{
    public class Region
    {
        public string regionName;
        private List<Region>? subRegions;
        private readonly List<string>? subRegionsUnRead;
        public List<DirectValue> DirectValues { get; set; }
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
        public static Region CreateSingleRegionByString(string regionData)
        {
            List<Region> topLevelRegions = Read.TopLevelRegion(regionData.Split(';'));
            if (topLevelRegions.Count == 0)
                throw new Exception("No valid top level region.");
            if (topLevelRegions.Count > 1)
                throw new Exception("Multible top level regions.");
            return topLevelRegions[0];
        }
        /// <summary>
        /// This will return a region based on the input string you provide it with. This can be used, to convert a RegionSaveString back to a Region.
        /// It is assumed, that there will be multible top level regions when using this method, so you must provide
        /// </summary>
        /// <param name="regionData"></param>
        /// <param name="regionName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Region CreateSingleRegionByString(string regionData, string regionName)
        {
            List<Region> topLevelRegions = Read.TopLevelRegion(regionData.Split(';'));
            if (topLevelRegions.Count == 0)
                throw new Exception("No valid top level region.");
            if (topLevelRegions.Count > 1)
                throw new Exception("Multible top level regions.");
            return topLevelRegions[0];
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

        private static string ConvertListToString(List<string> strings)
        {
            string result = string.Empty;
            foreach (string s in strings)
                if (s.Contains(';'))
                    result += s;
                else
                    result += s + ";";
            return result;
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