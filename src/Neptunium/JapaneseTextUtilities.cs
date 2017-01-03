using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium
{
    public static class JapaneseTextUtilities
    {
        //Credit: http://stackoverflow.com/a/15806958
        private static IEnumerable<char> GetCharsInRange(string text, int min, int max)
        {
            return text.Where(e => e >= min && e <= max);
        }

        public static IEnumerable<char> GetRomajiFromString(this string str)
        {
            return GetCharsInRange(str, 0x0020, 0x007E);
        }
        public static IEnumerable<char> GetHiraganaFromString(this string str)
        {
            return GetCharsInRange(str, 0x3040, 0x309F);
        }
        public static IEnumerable<char> GetKatakanaFromString(this string str)
        {
            return GetCharsInRange(str, 0x30A0, 0x30FF);
        }
        public static IEnumerable<char> GetKanjiFromString(this string str)
        {
            return GetCharsInRange(str, 0x4E00, 0x9FBF);
        }

    }
}
