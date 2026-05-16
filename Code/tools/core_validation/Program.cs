using System;
using System.Collections.Generic;
using System.IO;
using ShuiyuanHtmlPrivacyCleaner;

namespace CoreValidation
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.Error.WriteLine("Usage: CoreValidation <input.html> <output.html> <mode: personal|full>");
                return 2;
            }

            CleaningMode mode = string.Equals(args[2], "full", StringComparison.OrdinalIgnoreCase)
                ? CleaningMode.FullAnonymous
                : CleaningMode.PersonalOnly;

            CleanerEngine engine = new CleanerEngine();
            CleaningResult result = engine.Clean(args[0], args[1], new List<string>(), true, mode);
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine(result.ToDisplayText(ReportVerbosity.Standard, ReportLanguage.SimplifiedChinese));
            return File.Exists(args[1]) ? 0 : 1;
        }
    }
}
