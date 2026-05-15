$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$figure = Join-Path $root "Figure"
$symbolSvg = Join-Path $figure "shuiyuan_symbol_app_icon.svg"
$logoSvg = Join-Path $figure "shuiyuan_html_watermark_tool_logo.svg"
$iconIco = Join-Path $figure "shuiyuan_symbol_app_icon.ico"
$symbolPng = Join-Path $figure "shuiyuan_symbol_app_icon.png"
$logoPng = Join-Path $figure "shuiyuan_html_watermark_tool_logo.png"

if (!(Test-Path -LiteralPath $symbolSvg)) {
    throw "Missing SVG: $symbolSvg"
}
if (!(Test-Path -LiteralPath $logoSvg)) {
    throw "Missing SVG: $logoSvg"
}

$source = @"
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

public static class BrandAssetGenerator
{
    private sealed class LinearGradientDef
    {
        public string Id;
        public Point StartPoint;
        public Point EndPoint;
        public readonly List<GradientStop> Stops = new List<GradientStop>();
    }

    public static void Generate(string symbolSvg, string logoSvg, string iconIco, string symbolPng, string logoPng)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(iconIco));
        SavePng(RenderSvg(symbolSvg, 512, 512), symbolPng);
        SavePng(RenderSvg(logoSvg, 760, 160), logoPng);
        SaveIco(symbolSvg, iconIco, new int[] { 16, 24, 32, 48, 64, 128, 256 });
    }

    private static RenderTargetBitmap RenderSvg(string path, int width, int height)
    {
        XDocument document = XDocument.Load(path, LoadOptions.PreserveWhitespace);
        XElement root = document.Root;
        Rect viewBox = ParseViewBox(root);
        Dictionary<string, string> classFills = ParseStyleMap(root);
        Dictionary<string, LinearGradientDef> gradients = ParseGradients(root);

        DrawingVisual visual = new DrawingVisual();
        using (DrawingContext dc = visual.RenderOpen())
        {
            dc.PushTransform(new ScaleTransform(width / viewBox.Width, height / viewBox.Height));
            dc.PushTransform(new TranslateTransform(-viewBox.X, -viewBox.Y));
            RenderChildren(root, dc, classFills, gradients);
            dc.Pop();
            dc.Pop();
        }

        RenderTargetBitmap bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static void RenderChildren(XElement parent, DrawingContext dc, Dictionary<string, string> classFills, Dictionary<string, LinearGradientDef> gradients)
    {
        foreach (XElement child in parent.Elements())
        {
            string name = child.Name.LocalName;
            Transform transform = ParseTransform((string)child.Attribute("transform"));
            bool pushed = transform != null && !transform.Value.IsIdentity;
            if (pushed)
            {
                dc.PushTransform(transform);
            }

            if (name == "g" || name == "svg" || name == "defs")
            {
                if (name != "defs")
                {
                    RenderChildren(child, dc, classFills, gradients);
                }
            }
            else
            {
                Geometry geometry = CreateGeometry(child);
                Brush brush = ResolveBrush(child, classFills, gradients);
                if (geometry != null && brush != null)
                {
                    dc.DrawGeometry(brush, null, geometry);
                }
            }

            if (pushed)
            {
                dc.Pop();
            }
        }
    }

    private static Geometry CreateGeometry(XElement element)
    {
        switch (element.Name.LocalName)
        {
            case "path":
                string data = (string)element.Attribute("d");
                return string.IsNullOrWhiteSpace(data) ? null : Geometry.Parse(data);
            case "circle":
                double cx = ParseDouble((string)element.Attribute("cx"));
                double cy = ParseDouble((string)element.Attribute("cy"));
                double r = ParseDouble((string)element.Attribute("r"));
                return new EllipseGeometry(new Point(cx, cy), r, r);
            case "rect":
                double x = ParseDouble((string)element.Attribute("x"));
                double y = ParseDouble((string)element.Attribute("y"));
                double width = ParseDouble((string)element.Attribute("width"));
                double height = ParseDouble((string)element.Attribute("height"));
                return new RectangleGeometry(new Rect(x, y, width, height));
            default:
                return null;
        }
    }

    private static Brush ResolveBrush(XElement element, Dictionary<string, string> classFills, Dictionary<string, LinearGradientDef> gradients)
    {
        string fill = (string)element.Attribute("fill");
        if (string.IsNullOrWhiteSpace(fill))
        {
            string styleFill = ExtractFillFromStyle((string)element.Attribute("style"));
            if (!string.IsNullOrWhiteSpace(styleFill))
            {
                fill = styleFill;
            }
        }

        if (string.IsNullOrWhiteSpace(fill))
        {
            string className = (string)element.Attribute("class");
            if (!string.IsNullOrWhiteSpace(className))
            {
                foreach (string part in className.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string mapped;
                    if (classFills.TryGetValue(part.Trim(), out mapped))
                    {
                        fill = mapped;
                        break;
                    }
                }
            }
        }

        if (string.IsNullOrWhiteSpace(fill) || string.Equals(fill, "none", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        Match gradientMatch = Regex.Match(fill, @"url\(#(?<id>[^)]+)\)", RegexOptions.IgnoreCase);
        if (gradientMatch.Success)
        {
            string id = gradientMatch.Groups["id"].Value;
            LinearGradientDef gradient;
            if (gradients.TryGetValue(id, out gradient))
            {
                LinearGradientBrush brush = new LinearGradientBrush
                {
                    StartPoint = gradient.StartPoint,
                    EndPoint = gradient.EndPoint,
                    MappingMode = BrushMappingMode.Absolute
                };
                foreach (GradientStop stop in gradient.Stops)
                {
                    brush.GradientStops.Add(stop);
                }
                brush.Freeze();
                return brush;
            }
            return null;
        }

        try
        {
            Color color = (Color)ColorConverter.ConvertFromString(fill);
            SolidColorBrush brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
        catch
        {
            return null;
        }
    }

    private static Rect ParseViewBox(XElement root)
    {
        string viewBox = (string)root.Attribute("viewBox");
        if (string.IsNullOrWhiteSpace(viewBox))
        {
            double width = ParseDouble((string)root.Attribute("width"));
            double height = ParseDouble((string)root.Attribute("height"));
            return new Rect(0, 0, width, height);
        }

        string[] parts = viewBox.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4)
        {
            throw new InvalidOperationException("Invalid viewBox: " + viewBox);
        }

        return new Rect(
            ParseDouble(parts[0]),
            ParseDouble(parts[1]),
            ParseDouble(parts[2]),
            ParseDouble(parts[3]));
    }

    private static Dictionary<string, string> ParseStyleMap(XElement root)
    {
        Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (XElement style in root.Descendants().Where(e => e.Name.LocalName == "style"))
        {
            string css = style.Value ?? string.Empty;
            foreach (Match match in Regex.Matches(css, @"\.([A-Za-z0-9_-]+)\s*\{[^}]*fill\s*:\s*([^;}\r\n]+)", RegexOptions.IgnoreCase))
            {
                map[match.Groups[1].Value.Trim()] = match.Groups[2].Value.Trim();
            }
        }
        return map;
    }

    private static Dictionary<string, LinearGradientDef> ParseGradients(XElement root)
    {
        Dictionary<string, LinearGradientDef> map = new Dictionary<string, LinearGradientDef>(StringComparer.OrdinalIgnoreCase);
        foreach (XElement element in root.Descendants().Where(e => e.Name.LocalName == "linearGradient"))
        {
            LinearGradientDef gradient = new LinearGradientDef
            {
                Id = (string)element.Attribute("id"),
                StartPoint = new Point(ParseDouble((string)element.Attribute("x1")), ParseDouble((string)element.Attribute("y1"))),
                EndPoint = new Point(ParseDouble((string)element.Attribute("x2")), ParseDouble((string)element.Attribute("y2")))
            };

            foreach (XElement stop in element.Elements().Where(e => e.Name.LocalName == "stop"))
            {
                string offsetText = (string)stop.Attribute("offset");
                double offset = ParseOffset(offsetText);
                string colorText = (string)stop.Attribute("stop-color") ?? ExtractStopColorFromStyle((string)stop.Attribute("style"));
                if (string.IsNullOrWhiteSpace(colorText))
                {
                    continue;
                }

                Color color = (Color)ColorConverter.ConvertFromString(colorText);
                gradient.Stops.Add(new GradientStop(color, offset));
            }

            if (!string.IsNullOrWhiteSpace(gradient.Id))
            {
                map[gradient.Id] = gradient;
            }
        }
        return map;
    }

    private static string ExtractFillFromStyle(string style)
    {
        if (string.IsNullOrWhiteSpace(style))
        {
            return null;
        }
        Match match = Regex.Match(style, @"(?:^|;)\s*fill\s*:\s*([^;]+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static string ExtractStopColorFromStyle(string style)
    {
        if (string.IsNullOrWhiteSpace(style))
        {
            return null;
        }
        Match match = Regex.Match(style, @"(?:^|;)\s*stop-color\s*:\s*([^;]+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static Transform ParseTransform(string transform)
    {
        if (string.IsNullOrWhiteSpace(transform))
        {
            return Transform.Identity;
        }

        TransformGroup group = new TransformGroup();
        foreach (Match match in Regex.Matches(transform, @"([A-Za-z]+)\s*\(([^)]*)\)"))
        {
            string name = match.Groups[1].Value;
            double[] values = match.Groups[2].Value
                .Split(new char[] { ' ', ',', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ParseDouble)
                .ToArray();

            switch (name)
            {
                case "translate":
                    group.Children.Add(new TranslateTransform(
                        values.Length > 0 ? values[0] : 0,
                        values.Length > 1 ? values[1] : 0));
                    break;
                case "scale":
                    group.Children.Add(new ScaleTransform(
                        values.Length > 0 ? values[0] : 1,
                        values.Length > 1 ? values[1] : (values.Length > 0 ? values[0] : 1)));
                    break;
                case "rotate":
                    if (values.Length >= 3)
                    {
                        group.Children.Add(new RotateTransform(values[0], values[1], values[2]));
                    }
                    else if (values.Length >= 1)
                    {
                        group.Children.Add(new RotateTransform(values[0]));
                    }
                    break;
                case "matrix":
                    if (values.Length == 6)
                    {
                        group.Children.Add(new MatrixTransform(values[0], values[1], values[2], values[3], values[4], values[5]));
                    }
                    break;
            }
        }

        return group.Children.Count == 0 ? Transform.Identity : group;
    }

    private static void SavePng(RenderTargetBitmap bitmap, string path)
    {
        PngBitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using (FileStream stream = File.Create(path))
        {
            encoder.Save(stream);
        }
    }

    private static void SaveIco(string symbolSvg, string path, int[] sizes)
    {
        List<byte[]> pngImages = new List<byte[]>();
        foreach (int size in sizes)
        {
            RenderTargetBitmap bitmap = RenderSvg(symbolSvg, size, size);
            pngImages.Add(ToPngBytes(bitmap));
        }

        using (BinaryWriter writer = new BinaryWriter(File.Create(path)))
        {
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)pngImages.Count);

            int offset = 6 + (16 * pngImages.Count);
            for (int i = 0; i < pngImages.Count; i++)
            {
                int size = sizes[i];
                byte[] image = pngImages[i];
                writer.Write((byte)(size >= 256 ? 0 : size));
                writer.Write((byte)(size >= 256 ? 0 : size));
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write(image.Length);
                writer.Write(offset);
                offset += image.Length;
            }

            foreach (byte[] image in pngImages)
            {
                writer.Write(image);
            }
        }
    }

    private static byte[] ToPngBytes(RenderTargetBitmap bitmap)
    {
        PngBitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using (MemoryStream stream = new MemoryStream())
        {
            encoder.Save(stream);
            return stream.ToArray();
        }
    }

    private static double ParseDouble(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        string normalized = value.Trim().Replace("px", string.Empty);
        return double.Parse(normalized, CultureInfo.InvariantCulture);
    }

    private static double ParseOffset(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        string normalized = value.Trim();
        if (normalized.EndsWith("%", StringComparison.Ordinal))
        {
            return ParseDouble(normalized.Substring(0, normalized.Length - 1)) / 100.0;
        }

        return ParseDouble(normalized);
    }
}
"@

Add-Type -TypeDefinition $source -ReferencedAssemblies @(
    "System.Xml",
    "System.Xml.Linq",
    "System.Xaml",
    "WindowsBase",
    "PresentationCore",
    "PresentationFramework"
)

[BrandAssetGenerator]::Generate($symbolSvg, $logoSvg, $iconIco, $symbolPng, $logoPng)
Write-Host "Generated brand assets:"
Write-Host " - $iconIco"
Write-Host " - $symbolPng"
Write-Host " - $logoPng"
