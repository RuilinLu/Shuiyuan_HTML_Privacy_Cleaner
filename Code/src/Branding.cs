using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace ShuiyuanHtmlPrivacyCleaner
{
    internal static class Branding
    {
        private const string HeaderLogoResource = "ShuiyuanHtmlPrivacyCleaner.Figure.shuiyuan_html_watermark_tool_logo.png";

        public static Icon LoadAppIcon()
        {
            try
            {
                Icon icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                return icon == null ? null : (Icon)icon.Clone();
            }
            catch
            {
                return null;
            }
        }

        public static Image LoadHeaderLogo()
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                using Stream stream = assembly.GetManifestResourceStream(HeaderLogoResource);
                if (stream == null)
                {
                    return null;
                }

                using MemoryStream memory = new MemoryStream();
                stream.CopyTo(memory);
                memory.Position = 0;
                return Image.FromStream(memory);
            }
            catch
            {
                return null;
            }
        }
    }
}
