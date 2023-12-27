using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectStoreE
{
    public static class Formatter
    {

        private static void AppendTabamount(StringBuilder sb, uint amount)
        {
            sb.Append('\t', (int)amount);
        }
        private static void Format(Region region, StringBuilder sb, uint tabamount) 
        {
            AppendTabamount(sb, tabamount);
            sb.Append('§').Append(region.regionName).AppendLine(";");
            foreach(var directValue in region.DirectValues)
            {
                AppendTabamount(sb, tabamount + 1);
                sb.Append(directValue.name).Append(':').Append(DirectValueClearify.EncodeInvalidChars(directValue.value)).AppendLine(";");
            }
            foreach(var subRegion in region.SubRegions)
            {
                Format(subRegion, sb, tabamount + 1);
            }
            AppendTabamount(sb, tabamount);
            sb.AppendLine("$;");
        }
        public static string FormatRegion(Region region)
        {
            StringBuilder sb = new StringBuilder();
            Format(region, sb, 0);
            return sb.ToString();
        }

    }   
}
