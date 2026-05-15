using System;
using ShuiyuanHtmlPrivacyCleaner;

internal static class TestRunner
{
    private static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: TestRunner input.html output.html [term...]");
            return 2;
        }
        string[] terms = new string[Math.Max(args.Length - 2, 0)];
        for (int i = 2; i < args.Length; i++)
        {
            terms[i - 2] = args[i];
        }
        CleanerEngine engine = new CleanerEngine();
        CleaningResult result = engine.Clean(args[0], args[1], terms, true);
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine(result.ToDisplayText());
        return 0;
    }
}
