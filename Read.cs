namespace ObjectStoreE
{
    public static class Read
    {

        public static Region Region(string[] file, ref int currentLine)
        {
            var result = new Region();
            for (; currentLine < file.Length; currentLine++)
            {
                string line = file[currentLine];
                if (line == string.Empty) continue;
                switch (line[0])
                {
                    case '§':
                        result.AddSubRegion(line[1..], Region(file, ref currentLine));
                        break;

                    case '$':
                        return result;

                    case '-':
                        var split = line.Split(':', 2, StringSplitOptions.None);
                        result.AddDirectValue(split[0], DirectValueClearify.DecodeInvalidCharCode(split[1]));
                        break;
                    default:
                        throw new Exception($"Invalid item header '{line[0]}'");
                }
            }
            return result;
        }
    }

}