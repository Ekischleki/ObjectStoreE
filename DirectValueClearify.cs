using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace ObjectStoreE

{
    public static class DirectValueClearify
    {
        private static readonly char[] invalidChars = { '%', ';', ':', '?' };
        public static string EncodeInvalidChars(string? text)
        {
            
            if (text == null)
                return "?";
            StringBuilder result = new(text.Length);
            
            foreach (char c in text)
            {

                if (invalidChars.Contains(c))
                {
                    result.Append('%');
                    result.Append(Array.IndexOf(invalidChars, c));
                    result.Append('%');
                }
                else
                    result.Append(c);
            }
            return result.ToString();
        }

        public static void EncodeInvalidChars(string? text, StringBuilder s)
        {

            if (text == null)
            {
                s.Append('?');
                return;
            }

            foreach (char c in text)
            {

                if (invalidChars.Contains(c))
                {
                    s.Append('%');
                    s.Append(Array.IndexOf(invalidChars, c));
                    s.Append('%');
                }
                else
                    s.Append(c);
            }
            return;
        }

        private static StringBuilder sb = new();
        public static string? DecodeInvalidCharCode(string? text)
        {
            if (text == "?")
                return null;
            bool inPercent = false;
            sb.Clear();
            string currentInt = string.Empty;
            foreach (char c in text)
            {
                if (inPercent) //special chars are inside a percent code like this %0% and the number needs to be extracted.
                {
                    if (c == '%') //Number extracted; return to normal
                    {
                        inPercent = false;
                        sb.Append(invalidChars[Convert.ToInt32(currentInt.ToString())]);
                        currentInt = string.Empty;
                        continue;

                    }
                    currentInt += c;
                }
                else
                {
                    if (c == '%') //Start number extraction
                    {
                        inPercent = true;
                        continue;
                    }
                    sb.Append(c);
                }

            }
            return sb.ToString();
        }
    }

}