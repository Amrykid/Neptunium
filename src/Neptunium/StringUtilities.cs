using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptunium
{
    public static class StringUtilities
    {
        public static bool FuzzyEquals(this string str1, string str2, double matchPercentage = .5)
        {
            if (matchPercentage > 1.0)
                throw new ArgumentOutOfRangeException(nameof(matchPercentage), "Percent must be between 1.0 (100%) and 0.0 (0%).");

            int distance = LevenshteinDistance(str1, str2);

            int longest = Math.Max(str1.Length, str2.Length);
            int threshold = (int)(Math.Floor(longest * matchPercentage));

            return distance <= threshold;
        }

        public static int LevenshteinDistance(string str1, string str2)
        {
            // https://en.wikipedia.org/wiki/Levenshtein_distance

            int[,] matrix = new int[str1.Length, str2.Length];

            for (int i = 0; i < matrix.GetLength(0); i++)
                for (int j = 0; j < matrix.GetLength(1); j++)
                    matrix[i, j] = 0;

            for (int i = 1; i < str1.Length; i++)
                matrix[i, 0] = i;

            for (int j = 1; j < str2.Length; j++)
                matrix[0, j] = j;

            for (int j = 1; j < str2.Length; j++)
            {
                for (int i = 1; i < str1.Length; i++)
                {
                    int subCost = 0;

                    if (str1[i] == str2[j])
                    {
                        subCost = 0;
                    }
                    else
                    {
                        subCost = 1;
                    }

                    matrix[i, j] = Math.Min(Math.Min(
                                    matrix[i - 1, j] + 1, //deletion
                                    matrix[i, j - 1] + 1), //insertion
                                    matrix[i - 1, j - 1] + subCost); //substitution
                }
            }

            return matrix[str1.Length - 1, str2.Length - 1];
        }
    }
}
