using System.Text;

namespace ObjectStoreE
{
    public class Region
    {
        /// <summary>
        /// We need to store values in a dict for better access, but to save space, we don't want to generate a list each time we only save a single value.
        /// </summary>
        /// <typeparam name="T"></typeparam>


        private readonly Dictionary<string, IPossibleCollection<Region>> subregions;
        private readonly Dictionary<string, IPossibleCollection<string?>> directValues;

        public IEnumerable<(string name, string? value)> DirectValues { 
            get 
            {
                foreach (var value in directValues)
                {
                    foreach (var item in value.Value.GetCollection)
                    {
                        yield return (value.Key, item);
                    }
                }
            }
        }
        public IEnumerable<(string name, Region value)> Subregions
        {
            get
            {
                foreach (var value in subregions)
                {
                    foreach (var item in value.Value.GetCollection)
                    {
                        yield return (value.Key, item);
                    }
                }
            }
        }
        public void AddSubRegion(string regionName, Region region)
        {

            if (subregions.TryGetValue(regionName, out var value))
            {
                subregions[regionName] = value.Add(region);
            }
            else
            {
                subregions.Add(regionName, new NonCollection<Region>(region));
            }
        }
        public void AddDirectValue(string directValueName, string? value)
        {

            if (directValues.TryGetValue(directValueName, out var directValueInstance))
            {
                directValueInstance.Add(value);
            }
            else
            {
                directValues.Add(directValueName, new NonCollection<string?>(value));
            }
        }

        public override string ToString()
        {
            return RegionSaveString;
        }
        public string RegionSaveString
        {
            get
            {
                StringBuilder sb = new();
                foreach (var region in subregions)
                {
                    region.Value.ForEach(x => x.GenerateSaveString(sb, region.Key));
                }
                foreach (var directValue in directValues)
                {
                    directValue.Value.ForEach(x =>
                    {
                        sb.Append('-').Append(directValue.Key).Append(':');
                        DirectValueClearify.EncodeInvalidChars(x, sb);
                        sb.Append(';');
                    });
                }
                return sb.ToString();
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
            var start = 0;
            return Read.Region(regionData.Split(';'), ref start);
        }

        public static Region CreateSingleRegionByPath(string path)
        {
            path = path.Replace("\"", "");
            return CreateSingleRegionByString(File.ReadAllText(path));
        }

        public Region()
        {
            subregions = new();
            directValues = new();
        }
        public IEnumerable<Region> FindSubregionsWithName(string name)
            => subregions.GetValueOrDefault(name, IPossibleCollection<Region>.Empty).GetCollection;

        public IEnumerable<string?> FindDirectValuesWithName(string name)
            => directValues.GetValueOrDefault(name, IPossibleCollection<string?>.Empty).GetCollection;

        public string? FindDirectValue(string directValueName)
            => directValues[directValueName].GetSingleValue;


        public string? FindDirectValueOrDefault(string directValueName, string? defaultAtNotFound = null, string? defaultAtNullValue = null)
        {
            var directValue = directValues.GetValueOrDefault(directValueName);
            if (directValue == null)
                return defaultAtNotFound;
            else
                return directValue.GetSingleValue ?? defaultAtNullValue;
        }



        public Region? FindSubregionWithNameOrDefault(string directValueName)
            => subregions.GetValueOrDefault(directValueName)?.GetSingleValue;
        public Region FindSubregionWithName(string directValueName)
            => subregions[directValueName].GetSingleValue;


        private void GenerateSaveString(StringBuilder sb, string name)
        {
            sb.Append('§').Append(name).Append(';');
            foreach (var directValue in directValues)
            {
                directValue.Value.ForEach(x =>
                {
                    sb.Append('-').Append(directValue.Key).Append(':');
                    DirectValueClearify.EncodeInvalidChars(x, sb);
                    sb.Append(';');
                });
            }
            foreach (var subRegion in subregions)
            {
                subRegion.Value.ForEach(x => x.GenerateSaveString(sb, subRegion.Key));
            }
            sb.Append("$;");

        }


    }

}