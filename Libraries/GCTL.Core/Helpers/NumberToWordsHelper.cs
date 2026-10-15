using System;

namespace GCTL.Core.Helpers
{
    public static class NumberToWordsHelper
    {
        private static readonly string[] UnitsMap = { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
        private static readonly string[] TensMap = { "Zero", "Ten", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

        public static string ConvertToTakaWords(decimal amount)
        {
            if (amount < 0)
                return "Minus " + ConvertToTakaWords(-amount);

            long wholePart = (long)Math.Floor(amount);
            int paisa = (int)Math.Round((amount - wholePart) * 100);

            string words = ConvertGroup(wholePart);
            if (string.IsNullOrWhiteSpace(words))
                words = "Zero";

            string result = words;
            if (paisa > 0)
            {
                result += $" Taka and {ConvertGroup(paisa)} Paisa Only";
            }
            else
            {
                result += " Taka Only";
            }
            return result;
        }

        private static string ConvertGroup(long number)
        {
            if (number == 0)
                return string.Empty;

            if (number >= 10000000) // Crore (1,00,00,000)
            {
                long crore = number / 10000000;
                long remainder = number % 10000000;
                return $"{ConvertGroup(crore)} Crore" + (remainder > 0 ? " " + ConvertGroup(remainder) : "");
            }

            if (number >= 100000) // Lakh (1,00,000)
            {
                long lakh = number / 100000;
                long remainder = number % 100000;
                return $"{ConvertGroup(lakh)} Lakh" + (remainder > 0 ? " " + ConvertGroup(remainder) : "");
            }

            if (number >= 1000) // Thousand
            {
                long thousand = number / 1000;
                long remainder = number % 1000;
                return $"{ConvertGroup(thousand)} Thousand" + (remainder > 0 ? " " + ConvertGroup(remainder) : "");
            }

            if (number >= 100) // Hundred
            {
                long hundred = number / 100;
                long remainder = number % 100;
                return $"{UnitsMap[hundred]} Hundred" + (remainder > 0 ? " " + ConvertGroup(remainder) : "");
            }

            if (number >= 20)
            {
                long ten = number / 10;
                long unit = number % 10;
                return TensMap[ten] + (unit > 0 ? "-" + UnitsMap[unit] : "");
            }

            return UnitsMap[number];
        }
    }
}
