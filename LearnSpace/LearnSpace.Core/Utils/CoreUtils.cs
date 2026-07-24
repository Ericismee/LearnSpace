using System;

namespace LearnSpace.Core.Utils
{
    public static class CoreUtils
    {
        public static string GetNumericHash(string input)
        {
            if (string.IsNullOrEmpty(input)) return "0000000000";
            long hash = 0;
            foreach (char c in input)
            {
                hash = (hash * 31 + c) % 10000000000L;
            }
            return Math.Abs(hash).ToString("D10");
        }
    }
}
