using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace ShuiyuanHtmlPrivacyCleaner
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                return RunCommandLine(args);
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
            return 0;
        }

        private static int RunCommandLine(string[] args)
        {
            try
            {
                if (args.Length < 3 || !EqualsArg(args[0], "--clean"))
                {
                    return 2;
                }

                string input = args[1];
                string output = args[2];
                string report = string.Empty;
                bool overwrite = false;
                CleaningMode mode = CleaningMode.PersonalOnly;
                List<string> terms = new List<string>();

                for (int i = 3; i < args.Length; i++)
                {
                    if (EqualsArg(args[i], "--term") && i + 1 < args.Length)
                    {
                        terms.Add(args[++i]);
                    }
                    else if (EqualsArg(args[i], "--report") && i + 1 < args.Length)
                    {
                        report = args[++i];
                    }
                    else if (EqualsArg(args[i], "--overwrite"))
                    {
                        overwrite = true;
                    }
                    else if (EqualsArg(args[i], "--mode") && i + 1 < args.Length)
                    {
                        mode = ParseMode(args[++i]);
                    }
                }

                CleanerEngine engine = new CleanerEngine();
                CleaningResult result = engine.Clean(input, output, terms, overwrite, mode);
                string text = result.ToDisplayText();
                if (!string.IsNullOrWhiteSpace(report))
                {
                    string directory = Path.GetDirectoryName(report);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    File.WriteAllText(report, text, System.Text.Encoding.UTF8);
                }
                return 0;
            }
            catch (Exception ex)
            {
                try
                {
                    string fallback = Path.Combine(Path.GetTempPath(), "ShuiyuanHtmlPrivacyCleaner-error.txt");
                    File.WriteAllText(fallback, ex.ToString(), System.Text.Encoding.UTF8);
                }
                catch
                {
                }
                return 1;
            }
        }

        private static bool EqualsArg(string left, string right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static CleaningMode ParseMode(string value)
        {
            if (string.Equals(value, "full", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "anonymous", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "anon", StringComparison.OrdinalIgnoreCase))
            {
                return CleaningMode.FullAnonymous;
            }
            return CleaningMode.PersonalOnly;
        }
    }
}
