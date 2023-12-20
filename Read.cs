namespace ObjectStoreE
{
    public static class Read
    {
        public static Region GetTopLevelRegionOfString(string input)
        {
            List<Region> regions = TopLevelRegion(input.Split(';'));
            if (regions.Count == 1)
                return regions[0];
            if (regions.Count == 0)
                throw new Exception("Input was empty");
            throw new Exception("Input had multible regions");

        }
        public static List<Region> GetTopLevelRegionsOfString(string input)
        {
            return TopLevelRegion(input.Split(';'));
            

        }

        public static List<Region> TopLevelRegion(string[] file)
        {
            var result = new List<Region>();
            var currentSubRegions = new List<string>();
            var directValues = new List<DirectValue>();
            List<string> directValueTemp;
            string topLevelName = string.Empty;
            int depth = 0;
            bool canAddToSub;
            foreach (string line in file)
            {
                if (line == string.Empty) continue;
                canAddToSub = true;
                
                switch (line[..1])
                {
                    case "§":
                        if (depth == 0)
                        {
                            topLevelName = line.Substring(1);
                            canAddToSub = false;
                        }
                        depth++;
                        break;

                    case "$":
                        depth--;
                        if (depth < 0)
                            throw new InvalidDataException("Invalid file formating. Invalid depth.");
                        if (depth == 0)
                        {
                            canAddToSub = false;
                            result.Add(new Region(topLevelName, currentSubRegions, directValues));
                            currentSubRegions.Clear();
                            directValues.Clear();
                        }
                        break;

                    case "-":
                        if (depth == 1)
                        {
                            directValueTemp = line[1..].Split(':').ToList();
                            if (directValueTemp.Count != 2)
                                throw new InvalidDataException("Invalid file formating. Invalid direct value formating.");
                            directValues.Add(new DirectValue(directValueTemp[0], directValueTemp[1], true));
                            canAddToSub = false;
                        }
                        break;
                }
                if (canAddToSub && depth != 0)
                    currentSubRegions.Add(line);


            }
            if (depth != 0)
                throw new InvalidDataException("Data ended before properly exiting depth");

            return result;
        }
    }

}