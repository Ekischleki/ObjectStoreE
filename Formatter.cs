﻿using System;
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
        private static void Format(Region region, StringBuilder sb, uint tabamount, bool escapeValues, string regionName) 
        {
            AppendTabamount(sb, tabamount);
            sb.Append('§').Append(regionName).AppendLine(";");
            foreach(var directValue in region.DirectValues)
            {
                AppendTabamount(sb, tabamount + 1);
                
                sb.Append('-').Append(directValue.name).Append(':').Append(escapeValues ? DirectValueClearify.EncodeInvalidChars(directValue.value) : (directValue.value ?? "<null>")).AppendLine(";");
            }
            foreach(var subRegion in region.Subregions)
            {
                Format(subRegion.value, sb, tabamount + 1, escapeValues, subRegion.name);
            }
            AppendTabamount(sb, tabamount);
            sb.AppendLine("$;");
        }
        public static string FormatRegion(Region region, bool escapeValues = true)
        {
            StringBuilder sb = new StringBuilder();
            Format(region, sb, 0, escapeValues, "ROOT_REGION (This is not supposed to be parsed, and is just meant to visualize data)");
            return sb.ToString();
        }

    }   
}
