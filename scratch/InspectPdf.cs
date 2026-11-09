using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string[] files = new[]
        {
            @"c:\Users\mrhan\Downloads\AgingReport (7).pdf",
            @"c:\Users\mrhan\Downloads\AgingReport (8).pdf"
        };

        foreach (var f in files)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("FILE: " + f);
            if (!File.Exists(f))
            {
                Console.WriteLine("Not found!");
                continue;
            }

            var bytes = File.ReadAllBytes(f);
            var text = Encoding.ASCII.GetString(bytes);

            // Find Creator/Producer
            var mProd = Regex.Matches(text, @"/(Producer|Creator|Title|Subject)\s*\(([^)]+)\)");
            foreach (Match m in mProd)
            {
                Console.WriteLine("Meta: " + m.Groups[1].Value + " = " + m.Groups[2].Value);
            }

            // Find stream objects that might be uncompressed or text
            var mWords = Regex.Matches(text, @"[A-Za-z0-9 _\-\:\.]{4,}");
            int count = 0;
            Console.WriteLine("--- Matching tokens ---");
            foreach (Match m in mWords)
            {
                var val = m.Value.Trim();
                if (Regex.IsMatch(val, @"(Aging|Debtors|Customer|Bill|Amount|Days|30|60|90|120|150|180|Total|Date|Swift|Formosa|proc|rpt)", RegexOptions.IgnoreCase))
                {
                    Console.WriteLine("  " + val);
                    count++;
                    if (count > 25) break;
                }
            }
        }
    }
}

