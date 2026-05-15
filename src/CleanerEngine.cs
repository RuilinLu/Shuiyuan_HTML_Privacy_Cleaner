using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ShuiyuanHtmlPrivacyCleaner
{
    internal sealed class CleaningResult
    {
        public string InputPath;
        public string OutputPath;
        public string InputEncoding;
        public int OriginalChars;
        public int CleanedChars;
        public readonly List<ReportItem> Items = new List<ReportItem>();
        public readonly List<AuditItem> Audits = new List<AuditItem>();
        public readonly List<string> ExtractedTerms = new List<string>();
        public readonly List<string> Warnings = new List<string>();
        public readonly List<string> BeforeStreams = new List<string>();
        public readonly List<string> AfterStreams = new List<string>();

        public string ToDisplayText()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("输入文件: " + InputPath);
            sb.AppendLine("输出文件: " + OutputPath);
            sb.AppendLine("读取编码: " + InputEncoding);
            sb.AppendLine("字符数: " + OriginalChars + " -> " + CleanedChars);
            sb.AppendLine();

            sb.AppendLine("清理前检测到的信息");
            foreach (ReportItem item in Items)
            {
                sb.AppendLine("- " + item.Name + ": " + item.Before + FormatDetail(item.BeforeDetail));
            }
            sb.AppendLine("- NTFS 数据流: " + JoinOrNone(BeforeStreams));
            if (ExtractedTerms.Count > 0)
            {
                sb.AppendLine("- 从 currentUser 提取到的待审核关键词: " + string.Join(", ", ExtractedTerms.ToArray()));
            }
            else
            {
                sb.AppendLine("- 从 currentUser 提取到的待审核关键词: 0");
            }
            sb.AppendLine();

            sb.AppendLine("实际移除或改写");
            foreach (ReportItem item in Items)
            {
                sb.AppendLine("- " + item.Name + ": " + item.Removed + FormatDetail(item.ActionDetail));
            }
            sb.AppendLine();

            sb.AppendLine("清理后审核结果");
            foreach (AuditItem audit in Audits)
            {
                sb.AppendLine("- " + audit.Name + ": " + audit.AfterCount + FormatDetail(audit.Detail));
            }
            sb.AppendLine("- NTFS 数据流: " + JoinOrNone(AfterStreams));

            if (Warnings.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("注意");
                foreach (string warning in Warnings)
                {
                    sb.AppendLine("- " + warning);
                }
            }
            return sb.ToString();
        }

        private static string FormatDetail(string detail)
        {
            return string.IsNullOrEmpty(detail) ? string.Empty : " (" + detail + ")";
        }

        private static string JoinOrNone(List<string> values)
        {
            return values.Count == 0 ? "未发现或无法读取" : string.Join(", ", values.ToArray());
        }
    }

    internal sealed class ReportItem
    {
        public string Name;
        public int Before;
        public int Removed;
        public int After;
        public string BeforeDetail;
        public string ActionDetail;

        public ReportItem(string name, int before, int removed, int after, string beforeDetail, string actionDetail)
        {
            Name = name;
            Before = before;
            Removed = removed;
            After = after;
            BeforeDetail = beforeDetail;
            ActionDetail = actionDetail;
        }
    }

    internal sealed class AuditItem
    {
        public string Name;
        public int AfterCount;
        public string Detail;

        public AuditItem(string name, int afterCount, string detail)
        {
            Name = name;
            AfterCount = afterCount;
            Detail = detail;
        }
    }

    internal sealed class ReadResult
    {
        public string Text;
        public string EncodingName;
    }

    internal sealed partial class CleanerEngine
    {
        private const string DefaultSuffix = "_去水印去个人信息";
        private const string AnonymousSuffix = "_全匿名版";
        private const string FullPageDataImageOverlayPattern = @"(?:<div\b[^>]*>\s*)?<div\b[^>]*style=(?:""(?=[^""]*position\s*:\s*fixed)(?=[^""]*pointer-events\s*:\s*none)(?=[^""]*background-image\s*:\s*url\(data:image/)(?=[^""]*opacity\s*:\s*0\.\d+)(?=[^""]*(?:top\s*:\s*0|inset\s*:\s*0))(?=[^""]*(?:left\s*:\s*0|inset\s*:\s*0))(?=[^""]*(?:width\s*:\s*100%|right\s*:\s*0|inset\s*:\s*0))(?=[^""]*(?:height\s*:\s*100%|bottom\s*:\s*0|inset\s*:\s*0))[^""]*""|'(?=[^']*position\s*:\s*fixed)(?=[^']*pointer-events\s*:\s*none)(?=[^']*background-image\s*:\s*url\(data:image/)(?=[^']*opacity\s*:\s*0\.\d+)(?=[^']*(?:top\s*:\s*0|inset\s*:\s*0))(?=[^']*(?:left\s*:\s*0|inset\s*:\s*0))(?=[^']*(?:width\s*:\s*100%|right\s*:\s*0|inset\s*:\s*0))(?=[^']*(?:height\s*:\s*100%|bottom\s*:\s*0|inset\s*:\s*0))[^']*')[^>]*>\s*</div>\s*(?:</div>\s*)?";
        private readonly List<string> _warnings = new List<string>();

        public static string SuggestOutputPath(string inputPath)
        {
            return SuggestOutputPath(inputPath, CleaningMode.PersonalOnly);
        }

        public static string SuggestOutputPath(string inputPath, CleaningMode mode)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                return string.Empty;
            }

            string directory = Path.GetDirectoryName(inputPath);
            string stem = Path.GetFileNameWithoutExtension(inputPath);
            string extension = Path.GetExtension(inputPath);
            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(stem))
            {
                return string.Empty;
            }

            string suffix = mode == CleaningMode.FullAnonymous ? AnonymousSuffix : DefaultSuffix;
            string candidate = Path.Combine(directory, stem + suffix + extension);
            if (!File.Exists(candidate))
            {
                return candidate;
            }

            for (int i = 2; i < 1000; i++)
            {
                candidate = Path.Combine(directory, stem + suffix + "_" + i + extension);
                if (!File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return Path.Combine(directory, stem + suffix + "_" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + extension);
        }

        public CleaningResult AnalyzeOnly(string inputPath, IEnumerable<string> personalTerms)
        {
            return AnalyzeOnly(inputPath, personalTerms, CleaningMode.PersonalOnly, null);
        }

        public CleaningResult AnalyzeOnly(string inputPath, IEnumerable<string> personalTerms, CleaningMode mode)
        {
            return AnalyzeOnly(inputPath, personalTerms, mode, null);
        }

        public CleaningResult AnalyzeOnly(string inputPath, IEnumerable<string> personalTerms, CleaningMode mode, IProgress<ProgressInfo> progress)
        {
            _warnings.Clear();
            ReportProgress(progress, 5, "正在读取输入 HTML...");
            ReadResult read = ReadHtml(inputPath);
            ReportProgress(progress, 25, "正在扫描输入文件...");
            CleaningResult result = BuildBaseResult(inputPath, string.Empty, read);
            result.BeforeStreams.AddRange(AlternateStreams.ListStreams(inputPath));
            ReportProgress(progress, 60, "正在生成分析报告...");
            FillReportAndAudit(result, read.Text, read.Text, new List<string>(NormalizeTerms(personalTerms)), new List<string>());
            result.Warnings.AddRange(_warnings);
            ReportProgress(progress, 100, "分析完成。");
            return result;
        }

        public CleaningResult Clean(string inputPath, string outputPath, IEnumerable<string> personalTerms, bool overwrite)
        {
            return Clean(inputPath, outputPath, personalTerms, overwrite, CleaningMode.PersonalOnly, null);
        }

        public CleaningResult Clean(string inputPath, string outputPath, IEnumerable<string> personalTerms, bool overwrite, CleaningMode mode)
        {
            return Clean(inputPath, outputPath, personalTerms, overwrite, mode, null);
        }

        public CleaningResult Clean(string inputPath, string outputPath, IEnumerable<string> personalTerms, bool overwrite, CleaningMode mode, IProgress<ProgressInfo> progress)
        {
            _warnings.Clear();

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = SuggestOutputPath(inputPath, mode);
            }
            if (string.Equals(Path.GetFullPath(inputPath), Path.GetFullPath(outputPath), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("输出文件不能和输入文件相同。");
            }
            if (File.Exists(outputPath) && !overwrite)
            {
                throw new InvalidOperationException("输出文件已存在，请更换路径，或勾选允许覆盖。");
            }

            ReportProgress(progress, 5, "正在读取输入 HTML...");
            ReadResult read = ReadHtml(inputPath);
            CleaningResult result = BuildBaseResult(inputPath, outputPath, read);

            ReportProgress(progress, 15, "正在读取输入文件的数据流信息...");
            result.BeforeStreams.AddRange(AlternateStreams.ListStreams(inputPath));

            List<string> userTerms = new List<string>(NormalizeTerms(personalTerms));
            List<string> extractedTerms;
            ReportProgress(progress, 20, "正在执行清理规则...");
            string cleaned = CleanText(read.Text, userTerms, out extractedTerms, mode, progress);

            ReportProgress(progress, 84, "正在修复 HTML 结构...");
            cleaned = EnsureHtmlStructure(cleaned);
            if (mode == CleaningMode.FullAnonymous)
            {
                ReportProgress(progress, 88, "正在固定原始 Discourse 框架并完成匿名化...");
                cleaned = FinalizeAnonymousDiscourseHtml(cleaned);
            }

            string outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            ReportProgress(progress, 90, "正在写出清理后的 HTML...");
            File.WriteAllText(outputPath, cleaned, new UTF8Encoding(false));

            result.CleanedChars = cleaned.Length;
            result.ExtractedTerms.AddRange(extractedTerms);
            result.AfterStreams.AddRange(AlternateStreams.ListStreams(outputPath));

            ReportProgress(progress, 96, "正在执行清理后审核...");
            FillReportAndAudit(result, read.Text, cleaned, userTerms, extractedTerms);
            result.Warnings.AddRange(_warnings);
            ReportProgress(progress, 100, "清理完成。");
            return result;
        }

        private static void ReportProgress(IProgress<ProgressInfo> progress, int percent, string message)
        {
            if (progress != null)
            {
                progress.Report(new ProgressInfo(percent, message));
            }
        }

        private CleaningResult BuildBaseResult(string inputPath, string outputPath, ReadResult read)
        {
            CleaningResult result = new CleaningResult();
            result.InputPath = inputPath;
            result.OutputPath = outputPath;
            result.InputEncoding = read.EncodingName;
            result.OriginalChars = read.Text.Length;
            result.CleanedChars = read.Text.Length;
            return result;
        }

        private static ReadResult ReadHtml(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("输入文件不存在。", path);
            }

            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension != ".html" && extension != ".htm")
            {
                throw new InvalidOperationException("请选择 .html 或 .htm 文件。");
            }

            byte[] data = File.ReadAllBytes(path);
            string text;
            Encoding utf8Bom = new UTF8Encoding(true, true);
            Encoding utf8 = new UTF8Encoding(false, true);
            if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
            {
                try
                {
                    text = utf8Bom.GetString(data);
                    return new ReadResult { Text = text, EncodingName = "utf-8-sig" };
                }
                catch (DecoderFallbackException)
                {
                }
            }

            try
            {
                text = utf8.GetString(data);
                return new ReadResult { Text = text, EncodingName = "utf-8" };
            }
            catch (DecoderFallbackException)
            {
            }

            try
            {
                text = Encoding.GetEncoding("gb18030").GetString(data);
                return new ReadResult { Text = text, EncodingName = "gb18030" };
            }
            catch
            {
                text = Encoding.UTF8.GetString(data);
                return new ReadResult { Text = text, EncodingName = "utf-8-with-replacement" };
            }
        }

        private string CleanText(string original, List<string> userTerms, out List<string> extractedTerms, CleaningMode mode, IProgress<ProgressInfo> progress)
        {
            string text = original;
            extractedTerms = new List<string>();

            ReportProgress(progress, 24, "正在移除可见水印、扩展残留和登录入口...");
            ApplyReplacement(ref text, @"<!--\s*Page saved with SingleFile\s+url:.*?saved date:.*?-->", string.Empty, RegexOptions.Singleline);
            RemoveSensitiveMetaTags(ref text);
            ApplyReplacement(ref text, @"\s*<style>div#watermark-background\{.*?</style>\s*", "\n", RegexOptions.Singleline);
            ApplyReplacement(ref text, @"\s*<div\s+id=(?:watermark-background|['""]watermark-background['""])\b[^>]*></div>\s*", "\n", RegexOptions.Singleline);
            ApplyReplacement(ref text, @"\s*<li\s+id=(?:current-user|['""]current-user['""])\b[\s\S]*?</li>\s*", "\n", RegexOptions.None);
            ApplyReplacement(ref text, @"\s*<li\s+data-list-item-name=(?:my-posts|['""]my-posts['""])\b[\s\S]*?</li>\s*", "\n", RegexOptions.None);
            ApplyReplacement(ref text, @"\s*<li\s+data-list-item-name=(?:浏览记录|['""]浏览记录['""])\b[\s\S]*?</li>\s*", "\n", RegexOptions.None);
            ApplyReplacement(ref text, @"\s*<div class=(?:""read-state(?: read)?""|'read-state(?: read)?'|read-state)\s*[^>]*>[\s\S]*?</div>\s*", "\n", RegexOptions.None);
            ApplyReplacement(ref text, FullPageDataImageOverlayPattern, "\n", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\s*<div id=thunderbit-crx-side-bar\b[\s\S]*?</template></div>\s*", "\n", RegexOptions.None);
            ApplyReplacement(ref text, @"\s*<button id=open-side-panel\b[\s\S]*?</button>\s*", "\n", RegexOptions.None);
            ApplyReplacement(ref text, @"\s*<div id=immersive-translate-popup\b[\s\S]*?</template></div>\s*", "\n", RegexOptions.None);
            ApplyReplacement(ref text, @"\sdata-immersive-translate-page-theme=(?:[^\s>]+|""[^""]*"")", string.Empty, RegexOptions.None);
            ApplyReplacement(ref text, @"&quot;topicTrackingStateMeta&quot;:&quot;\{.*?\}&quot;", @"&quot;topicTrackingStateMeta&quot;:&quot;{}&quot;", RegexOptions.Singleline);
            ApplyReplacement(ref text, @"<a href=https://shuiyuan\.sjtu\.edu\.cn/unread>\d+ 个未读</a>话题\s*和\s*<a href=https://shuiyuan\.sjtu\.edu\.cn/new>\d+ 个新</a>话题，或浏览", "浏览", RegexOptions.None);

            ReportProgress(progress, 40, "正在清理主题激活痕迹和 currentUser 数据...");
            RemoveWatermarkThemeActivations(ref text);
            ApplyReplacement(ref text, @"&quot;activatedThemes&quot;:&quot;\{.*?\}&quot;", @"&quot;activatedThemes&quot;:&quot;{}&quot;", RegexOptions.Singleline);
            ApplyReplacement(ref text, @"""activatedThemes""\s*:\s*""\{.*?\}""", @"""activatedThemes"":""{}""", RegexOptions.Singleline);

            List<string> termsFromCurrentUser;
            text = RemoveEncodedCurrentUser(text, out termsFromCurrentUser);
            AddUnique(extractedTerms, termsFromCurrentUser);
            text = RemoveRawCurrentUser(text, out termsFromCurrentUser);
            AddUnique(extractedTerms, termsFromCurrentUser);

            ReportProgress(progress, 56, "正在移除指定的个人关键词...");
            List<string> allTerms = new List<string>();
            AddUnique(allTerms, userTerms);
            AddUnique(allTerms, extractedTerms);
            RemovePersonalTerms(ref text, allTerms);

            if (mode == CleaningMode.FullAnonymous)
            {
                ReportProgress(progress, 72, "正在执行全匿名规则...");
                ApplyFullAnonymization(ref text);
            }

            return text;
        }

        private string EnsureHtmlStructure(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || !Regex.IsMatch(text, @"<html\b", RegexOptions.IgnoreCase))
            {
                return text;
            }

            text = Regex.Replace(text, @"<title>(?<value>[^<]*?)/title>", "<title>${value}</title>", RegexOptions.IgnoreCase);
            text = FixStyleSwallowingArtifacts(text);

            int bodyIndex = IndexOfRegex(text, @"<body\b", RegexOptions.IgnoreCase);
            int headCloseIndex = IndexOfRegex(text, @"</head>", RegexOptions.IgnoreCase);
            int headOpenIndex = IndexOfRegex(text, @"<head\b", RegexOptions.IgnoreCase);
            if (headOpenIndex >= 0 && headCloseIndex < 0)
            {
                if (bodyIndex >= 0)
                {
                    text = text.Insert(bodyIndex, "</head>");
                }
                else
                {
                    int insertAt = FindLikelyBodyInsertIndex(text);
                    text = text.Insert(insertAt, "</head><body>");
                }
            }
            else if (bodyIndex >= 0 && bodyIndex < headCloseIndex)
            {
                text = text.Insert(bodyIndex, "</head>");
            }

            bodyIndex = IndexOfRegex(text, @"<body\b", RegexOptions.IgnoreCase);
            if (bodyIndex < 0)
            {
                headCloseIndex = IndexOfRegex(text, @"</head>", RegexOptions.IgnoreCase);
                int insertAt = headCloseIndex >= 0 ? headCloseIndex + "</head>".Length : FindLikelyBodyInsertIndex(text);
                text = text.Insert(insertAt, "<body>");
            }

            if (headOpenIndex >= 0 && !Regex.IsMatch(text, @"<meta\b[^>]*charset=", RegexOptions.IgnoreCase))
            {
                int headTagEnd = FindTagEnd(text, "head");
                if (headTagEnd >= 0)
                {
                    text = text.Insert(headTagEnd, "<meta charset=utf-8>");
                }
            }

            if (!Regex.IsMatch(text, @"</body>", RegexOptions.IgnoreCase))
            {
                text += "\n</body>";
            }
            if (!Regex.IsMatch(text, @"</html>", RegexOptions.IgnoreCase))
            {
                text += "\n</html>\n";
            }
            return text;
        }

        private void RemoveSensitiveMetaTags(ref string text)
        {
            ApplyReplacement(
                ref text,
                @"\s*<meta\b[^>]*(?:name|property)\s*=\s*(?:""csrf-token""|'csrf-token'|csrf-token|""csrf-param""|'csrf-param'|csrf-param|""fragment""|'fragment'|fragment)[^>]*>\s*",
                "\n",
                RegexOptions.IgnoreCase);
            ApplyReplacement(
                ref text,
                @"\s*<meta\b[^>]*(?:name|property)\s*=\s*(?:""discourse_theme_id""|'discourse_theme_id'|discourse_theme_id|""discourse_current_homepage""|'discourse_current_homepage'|discourse_current_homepage)[^>]*>\s*",
                "\n",
                RegexOptions.IgnoreCase);
        }

        private static string FixStyleSwallowingArtifacts(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            MatchCollection matches = Regex.Matches(text, @"<style\b[^>]*>", RegexOptions.IgnoreCase);
            if (matches.Count == 0)
            {
                return text;
            }

            for (int i = matches.Count - 1; i >= 0; i--)
            {
                Match open = matches[i];
                int contentStart = open.Index + open.Length;
                int nextOpen = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;
                int close = text.IndexOf("</style>", contentStart, StringComparison.OrdinalIgnoreCase);
                if (close >= 0 && close < nextOpen)
                {
                    continue;
                }

                int split = FindFirstHtmlMarkerInStyle(text, contentStart, nextOpen);
                if (split >= 0)
                {
                    text = text.Insert(split, "</style>\n");
                }
            }

            return text;
        }

        private static int FindFirstHtmlMarkerInStyle(string text, int start, int end)
        {
            string[] markers = new string[]
            {
                "<body",
                "<div id=ember",
                "<div id=\"ember",
                "<section id=main",
                "<section id=\"main",
                "<div class=\"topic-post",
                "<div class=topic-post",
                "<article",
                "</body>",
                "</html>"
            };

            int best = -1;
            for (int i = 0; i < markers.Length; i++)
            {
                int found = text.IndexOf(markers[i], start, StringComparison.OrdinalIgnoreCase);
                if (found >= 0 && found < end && (best < 0 || found < best))
                {
                    best = found;
                }
            }

            return best;
        }

        private void FillReportAndAudit(CleaningResult result, string before, string after, List<string> userTerms, List<string> extractedTerms)
        {
            string hiddenPattern = FullPageDataImageOverlayPattern;
            string avatarPattern = @"<li\s+id=(?:current-user|['""]current-user['""])\b[\s\S]*?</li>";

            AddItem(result, "SingleFile 保存时间注释", before, after, @"<!--\s*Page saved with SingleFile\s+url:.*?saved date:.*?-->", RegexOptions.Singleline, string.Empty);
            AddItem(result, "鉴权 / 主题 Meta 信息", before, after, @"<meta\b[^>]*(?:name|property)\s*=\s*(?:""csrf-token""|'csrf-token'|csrf-token|""csrf-param""|'csrf-param'|csrf-param|""fragment""|'fragment'|fragment|""discourse_theme_id""|'discourse_theme_id'|discourse_theme_id|""discourse_current_homepage""|'discourse_current_homepage'|discourse_current_homepage)[^>]*>", RegexOptions.IgnoreCase, string.Empty);
            AddItem(result, "可见水印 CSS", before, after, @"<style>div#watermark-background\{.*?</style>", RegexOptions.Singleline, string.Empty);
            AddItem(result, "可见水印 DOM", before, after, @"<div\s+id=(?:watermark-background|['""]watermark-background['""])\b[^>]*></div>", RegexOptions.Singleline, string.Empty);
            AddItem(result, "右上角当前用户头像 / 账号菜单", before, after, avatarPattern, RegexOptions.None, "头像块 hash: " + FirstHash(before, avatarPattern, RegexOptions.None));
            AddItem(result, "“我的帖子”侧栏入口", before, after, @"<li\s+data-list-item-name=(?:my-posts|['""]my-posts['""])\b[\s\S]*?</li>", RegexOptions.None, string.Empty);
            AddItem(result, "“浏览记录”侧栏入口", before, after, @"<li\s+data-list-item-name=(?:浏览记录|['""]浏览记录['""])\b[\s\S]*?</li>", RegexOptions.None, string.Empty);
            AddItem(result, "帖子已读/未读状态标记", before, after, @"<div class=(?:""read-state(?: read)?""|'read-state(?: read)?'|read-state)\s*[^>]*>[\s\S]*?</div>", RegexOptions.None, string.Empty);
            AddItem(result, "隐藏低透明度水印层", before, after, hiddenPattern, RegexOptions.Singleline, "隐水印块 hash: " + FirstHash(before, hiddenPattern, RegexOptions.Singleline));
            AddItem(result, "水印主题激活痕迹", before, after, @"\\&quot;\d+\\&quot;:\\&quot;[^\\&]*(?:discourse-watermark|shuiyuan-watermark|watermark)[^\\&]*\\&quot;", RegexOptions.Singleline | RegexOptions.IgnoreCase, string.Empty);
            AddItem(result, "Thunderbit 扩展残留", before, after, @"<div id=thunderbit-crx-side-bar\b[\s\S]*?</template></div>", RegexOptions.None, string.Empty);
            AddItem(result, "沉浸式翻译扩展残留", before, after, @"<div id=immersive-translate-popup\b[\s\S]*?</template></div>", RegexOptions.None, string.Empty);
            AddItem(result, "扩展侧栏按钮", before, after, @"<button id=open-side-panel\b[\s\S]*?</button>", RegexOptions.None, string.Empty);
            AddItem(result, "currentUser 个人数据", before, after, @"&quot;currentUser&quot;:&quot;(?!null)[\s\S]*?&quot;", RegexOptions.None, "清理后允许 currentUser 为 null");
            AddItem(result, "topic tracking / 未读新帖计数", before, after, @"topicTrackingStateMeta&quot;:&quot;\{\\&quot;/|<a href=https://shuiyuan\.sjtu\.edu\.cn/unread>\d+ 个未读</a>", RegexOptions.None, string.Empty);

            AddItem(result, "站点域名 / 品牌痕迹", before, after, @"(?:shuiyuan\.sjtu\.edu\.cn|shuiyuan\.s3\.jcloud\.sjtu\.edu\.cn|sjtu\.edu\.cn|水源社区|水源广场)", RegexOptions.IgnoreCase, string.Empty);
            AddItem(result, "公开用户标识", before, after, @"(?:aria-label=(?:""[^""]+ 的个人资料""|'[^']+ 的个人资料')|data-user-card|data-user-id|/user_avatar/|https?://[^""'\s<>]+/u/)", RegexOptions.IgnoreCase, string.Empty);
            AddItem(result, "用户徽章 / 头衔 / Flair", before, after, @"<(?:span|div|a|img)\b[^>]*class=(?:""[^""]*(?:\buser-title\b|\bavatar-flair\b|\bposter-icon\b|\bbadge-wrapper\b|\buser-badge\b)[^""]*""|'[^']*(?:\buser-title\b|\bavatar-flair\b|\bposter-icon\b|\bbadge-wrapper\b|\buser-badge\b)[^']*'|[^\s>]*(?:\buser-title\b|\bavatar-flair\b|\bposter-icon\b|\bbadge-wrapper\b|\buser-badge\b)[^\s>]*)[^>]*>", RegexOptions.IgnoreCase, string.Empty);
            AddItem(result, "隐藏预加载数据", before, after, @"<discourse-assets-json>[\s\S]*?</discourse-assets-json>", RegexOptions.IgnoreCase, string.Empty);
            AddItem(result, "站点外链 / 原图 URL", before, after, @"https?://(?:shuiyuan(?:\.s3\.jcloud)?\.sjtu\.edu\.cn|[^""'\s<>]*sjtu\.edu\.cn)[^""'\s<>]*", RegexOptions.IgnoreCase, string.Empty);

            AddAudit(result, "watermark-background", after, "watermark-background");
            AddAudit(result, "id=current-user / toggle-current-user", after, "id=current-user", "id=\"current-user\"", "toggle-current-user");
            AddAudit(result, "我的帖子 / 浏览记录", after, "我的帖子", "浏览记录", "data-list-item-name=my-posts", "data-list-item-name=浏览记录");
            AddAudit(result, "read-state read / 帖子未读", after, "read-state read", "帖子未读");
            AddAudit(result, "Thunderbit / 沉浸式翻译", after, "thunderbit-crx-side-bar", "immersive-translate-popup", "open-side-panel");
            AddAudit(result, "SingleFile 保存时间", after, "Page saved with SingleFile");
            AddAudit(result, "currentUser 非空个人数据", after, "&quot;currentUser&quot;:&quot;{", "\"currentUser\":\"{");
            AddAudit(result, "水印主题激活", after, "discourse-watermark", "shuiyuan-watermark");
            AddAudit(result, "鉴权 / 主题 Meta 信息", after, "csrf-token", "csrf-param", "discourse_theme_id", "discourse_current_homepage");
            AddRegexAudit(result, "Windows 路径 / file:// 痕迹", after, @"(?<![A-Za-z])[A-Za-z]:[\\/]|file://", RegexOptions.IgnoreCase);
            AddRegexAudit(result, "站点域名 / 品牌痕迹", after, @"(?:shuiyuan\.sjtu\.edu\.cn|shuiyuan\.s3\.jcloud\.sjtu\.edu\.cn|sjtu\.edu\.cn|水源社区|水源广场)", RegexOptions.IgnoreCase);
            AddRegexAudit(result, "公开用户标识", after, @"(?:aria-label=(?:""[^""]+ 的个人资料""|'[^']+ 的个人资料')|data-user-card|data-user-id|/user_avatar/|https?://[^""'\s<>]+/u/)", RegexOptions.IgnoreCase);
            AddRegexAudit(result, "用户徽章 / 头衔 / Flair", after, @"<(?:span|div|a|img)\b[^>]*class=(?:""[^""]*(?:\buser-title\b|\bavatar-flair\b|\bposter-icon\b|\bbadge-wrapper\b|\buser-badge\b)[^""]*""|'[^']*(?:\buser-title\b|\bavatar-flair\b|\bposter-icon\b|\bbadge-wrapper\b|\buser-badge\b)[^']*'|[^\s>]*(?:\buser-title\b|\bavatar-flair\b|\bposter-icon\b|\bbadge-wrapper\b|\buser-badge\b)[^\s>]*)[^>]*>", RegexOptions.IgnoreCase);
            AddRegexAudit(result, "隐藏预加载数据", after, @"<discourse-assets-json>[\s\S]*?</discourse-assets-json>", RegexOptions.IgnoreCase);
            AddRegexAudit(result, "站点外链 / 原图 URL", after, @"https?://(?:shuiyuan(?:\.s3\.jcloud)?\.sjtu\.edu\.cn|[^""'\s<>]*sjtu\.edu\.cn)[^""'\s<>]*", RegexOptions.IgnoreCase);
            int styleOpen = CountRegex(after, @"<style\b", RegexOptions.IgnoreCase);
            int styleClose = CountRegex(after, @"</style>", RegexOptions.IgnoreCase);
            result.Audits.Add(new AuditItem("HTML style 标签配对", Math.Abs(styleOpen - styleClose), styleOpen == styleClose ? "已配对: " + styleOpen + "/" + styleClose : "不配对: " + styleOpen + "/" + styleClose));

            List<string> allTerms = new List<string>();
            AddUnique(allTerms, userTerms);
            AddUnique(allTerms, extractedTerms);
            for (int i = 0; i < allTerms.Count; i++)
            {
                string term = allTerms[i];
                if (term.Length >= 2)
                {
                    int plain = CountPlain(after, term);
                    string encoded = Uri.EscapeDataString(term);
                    int encodedCount = CountPlainIgnoreCase(after, encoded);
                    result.Audits.Add(new AuditItem("额外/提取关键词: " + SafeTermForReport(term), plain + encodedCount, "明文 " + plain + "，URL 编码 " + encodedCount));
                }
            }
        }

        private void AddItem(CleaningResult result, string name, string before, string after, string pattern, RegexOptions options, string beforeDetail)
        {
            int beforeCount = CountRegex(before, pattern, options);
            int afterCount = CountRegex(after, pattern, options);
            int removed = beforeCount - afterCount;
            if (removed < 0)
            {
                removed = 0;
            }

            string detail = afterCount == 0 ? "清理后不存在" : "清理后仍有 " + afterCount;
            result.Items.Add(new ReportItem(name, beforeCount, removed, afterCount, beforeDetail, detail));
        }

        private static void AddAudit(CleaningResult result, string name, string text, params string[] markers)
        {
            int count = 0;
            for (int i = 0; i < markers.Length; i++)
            {
                count += CountPlain(text, markers[i]);
            }

            result.Audits.Add(new AuditItem(name, count, count == 0 ? "未检出" : "仍需人工复核"));
        }

        private static void AddRegexAudit(CleaningResult result, string name, string text, string pattern, RegexOptions options)
        {
            int count = CountRegex(text, pattern, options);
            result.Audits.Add(new AuditItem(name, count, count == 0 ? "未检出" : "仍需人工复核"));
        }

        private static int CountRegex(string text, string pattern, RegexOptions options)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            try
            {
                return Regex.Matches(text, pattern, options).Count;
            }
            catch
            {
                return 0;
            }
        }

        private static int CountPlain(string text, string marker)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(marker))
            {
                return 0;
            }

            int count = 0;
            int pos = 0;
            while (true)
            {
                int found = text.IndexOf(marker, pos, StringComparison.Ordinal);
                if (found < 0)
                {
                    return count;
                }
                count++;
                pos = found + marker.Length;
            }
        }

        private static int CountPlainIgnoreCase(string text, string marker)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(marker))
            {
                return 0;
            }

            int count = 0;
            int pos = 0;
            while (true)
            {
                int found = text.IndexOf(marker, pos, StringComparison.OrdinalIgnoreCase);
                if (found < 0)
                {
                    return count;
                }
                count++;
                pos = found + marker.Length;
            }
        }

        private void ApplyReplacement(ref string text, string pattern, string replacement, RegexOptions options)
        {
            try
            {
                text = Regex.Replace(text, pattern, replacement, options);
            }
            catch (Exception ex)
            {
                _warnings.Add("规则执行失败，已跳过: " + Shorten(pattern, 60) + " / " + ex.Message);
            }
        }

        private static string RemoveEncodedCurrentUser(string text, out List<string> sensitiveTerms)
        {
            sensitiveTerms = new List<string>();
            string key = "&quot;currentUser&quot;:&quot;";
            int searchPos = 0;
            while (true)
            {
                int start = text.IndexOf(key, searchPos, StringComparison.Ordinal);
                if (start < 0)
                {
                    break;
                }

                int valueStart = start + key.Length;
                int quoteStart = FindUnescapedHtmlQuote(text, valueStart);
                if (quoteStart < 0)
                {
                    break;
                }

                string encodedValue = text.Substring(valueStart, quoteStart - valueStart);
                AddCurrentUserTerms(encodedValue, sensitiveTerms);
                string replacement = "&quot;currentUser&quot;:&quot;null&quot;";
                text = text.Substring(0, start) + replacement + text.Substring(quoteStart + "&quot;".Length);
                searchPos = start + replacement.Length;
            }

            for (int i = 0; i < sensitiveTerms.Count; i++)
            {
                string term = sensitiveTerms[i];
                if (IsDigits(term))
                {
                    text = text.Replace("/unread/" + term, "/unread");
                }
            }

            return text;
        }

        private static string RemoveRawCurrentUser(string text, out List<string> sensitiveTerms)
        {
            sensitiveTerms = new List<string>();
            MatchCollection matches = Regex.Matches(text, @"""currentUser""\s*:\s*""(?<value>(?:\\""|[^""])*)""", RegexOptions.Singleline);
            for (int i = 0; i < matches.Count; i++)
            {
                string value = matches[i].Groups["value"].Value;
                if (!string.Equals(value, "null", StringComparison.Ordinal))
                {
                    AddRawCurrentUserTerms(value, sensitiveTerms);
                }
            }

            text = Regex.Replace(
                text,
                @"""currentUser""\s*:\s*""(?<value>(?:\\""|[^""])*)""",
                delegate (Match match)
                {
                    string value = match.Groups["value"].Value;
                    return string.Equals(value, "null", StringComparison.Ordinal) ? match.Value : @"""currentUser"":""null""";
                },
                RegexOptions.Singleline);

            return text;
        }

        private static void AddCurrentUserTerms(string encodedValue, List<string> terms)
        {
            AddMatchValue(encodedValue, @"\\&quot;id\\&quot;:(\d+)", terms);
            AddMatchValue(encodedValue, @"\\&quot;username\\&quot;:\\&quot;(.*?)\\&quot;", terms);
            AddMatchValue(encodedValue, @"\\&quot;name\\&quot;:\\&quot;(.*?)\\&quot;", terms);
            AddMatchValue(encodedValue, @"\\&quot;avatar_template\\&quot;:\\&quot;(.*?)\\&quot;", terms);
        }

        private static void AddRawCurrentUserTerms(string value, List<string> terms)
        {
            AddMatchValue(value, @"\\?""id\\?""\s*:\s*(\d+)", terms);
            AddMatchValue(value, @"\\?""username\\?""\s*:\s*\\?""(.*?)\\?""", terms);
            AddMatchValue(value, @"\\?""name\\?""\s*:\s*\\?""(.*?)\\?""", terms);
            AddMatchValue(value, @"\\?""avatar_template\\?""\s*:\s*\\?""(.*?)\\?""", terms);
        }

        private static void AddMatchValue(string text, string pattern, List<string> terms)
        {
            Match match = Regex.Match(text, pattern, RegexOptions.Singleline);
            if (match.Success)
            {
                string value = match.Groups[1].Value.Replace("\\/", "/").Replace("\\\"", "\"");
                if (!string.IsNullOrWhiteSpace(value))
                {
                    AddUnique(terms, new string[] { value });
                }
            }
        }

        private void RemoveWatermarkThemeActivations(ref string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            try
            {
                text = Regex.Replace(
                    text,
                    @"(?:,)?\\&quot;\d+\\&quot;:\\&quot;[^\\&]*(?:discourse-watermark|shuiyuan-watermark|watermark)[^\\&]*\\&quot;(?:,)?",
                    string.Empty,
                    RegexOptions.Singleline | RegexOptions.IgnoreCase);
                text = Regex.Replace(text, @",(?=\s*[}\]])", string.Empty, RegexOptions.Singleline);
                text = Regex.Replace(text, @"(?<=[{,])\s*,+", ",", RegexOptions.Singleline);
                text = text.Replace("{,", "{").Replace(",}", "}").Replace("[,", "[");
            }
            catch (Exception ex)
            {
                _warnings.Add("水印主题清理失败，已跳过: " + ex.Message);
            }
        }

        private static int FindUnescapedHtmlQuote(string text, int start)
        {
            int pos = start;
            while (true)
            {
                int idx = text.IndexOf("&quot;", pos, StringComparison.Ordinal);
                if (idx < 0)
                {
                    return -1;
                }
                if (idx == 0 || text[idx - 1] != '\\')
                {
                    return idx;
                }
                pos = idx + "&quot;".Length;
            }
        }

        private void RemovePersonalTerms(ref string text, List<string> terms)
        {
            for (int i = 0; i < terms.Count; i++)
            {
                string term = terms[i];
                if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
                {
                    continue;
                }

                term = term.Trim();
                text = text.Replace(term, string.Empty);
                string encoded = Uri.EscapeDataString(term);
                if (!string.IsNullOrEmpty(encoded))
                {
                    try
                    {
                        text = Regex.Replace(text, Regex.Escape(encoded), string.Empty, RegexOptions.IgnoreCase);
                    }
                    catch
                    {
                        text = text.Replace(encoded, string.Empty);
                    }
                }
            }
        }

        private static IEnumerable<string> NormalizeTerms(IEnumerable<string> terms)
        {
            if (terms == null)
            {
                yield break;
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string raw in terms)
            {
                if (raw == null)
                {
                    continue;
                }

                string[] parts = raw.Replace("\r", "\n").Split(new char[] { '\n', ',', ';', '，', '；' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length; i++)
                {
                    string term = parts[i].Trim();
                    if (term.Length >= 2 && seen.Add(term))
                    {
                        yield return term;
                    }
                }
            }
        }

        private static void AddUnique(List<string> target, IEnumerable<string> values)
        {
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value) && !target.Contains(value))
                {
                    target.Add(value);
                }
            }
        }

        private static bool IsDigits(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                if (!char.IsDigit(value[i]))
                {
                    return false;
                }
            }
            return true;
        }

        private static string FirstHash(string text, string pattern, RegexOptions options)
        {
            try
            {
                Match match = Regex.Match(text, pattern, options);
                if (!match.Success)
                {
                    return "无";
                }
                return Sha256Short(match.Value);
            }
            catch
            {
                return "无法计算";
            }
        }

        private static string Sha256Short(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(bytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < 12 && i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private static string Shorten(string text, int max)
        {
            if (text == null || text.Length <= max)
            {
                return text;
            }
            return text.Substring(0, max) + "...";
        }

        private static string SafeTermForReport(string term)
        {
            if (term == null)
            {
                return string.Empty;
            }
            return term.Length <= 60 ? term : term.Substring(0, 60) + "...";
        }

        private static int IndexOfRegex(string text, string pattern, RegexOptions options)
        {
            Match match = Regex.Match(text, pattern, options);
            return match.Success ? match.Index : -1;
        }

        private static int FindTagEnd(string text, string tagName)
        {
            Match match = Regex.Match(text, @"<" + tagName + @"\b[^>]*>", RegexOptions.IgnoreCase);
            return match.Success ? match.Index + match.Length : -1;
        }

        private static int FindLikelyBodyInsertIndex(string text)
        {
            string[] patterns = new string[]
            {
                @"<body\b",
                @"<div\b",
                @"<main\b",
                @"<section\b",
                @"<article\b",
                @"<header\b",
                @"<aside\b",
                @"<script\b",
                @"<template\b"
            };

            int best = -1;
            for (int i = 0; i < patterns.Length; i++)
            {
                int index = IndexOfRegex(text, patterns[i], RegexOptions.IgnoreCase);
                if (index >= 0 && (best < 0 || index < best))
                {
                    best = index;
                }
            }

            return best >= 0 ? best : text.Length;
        }
    }

    internal static class AlternateStreams
    {
        private const int FindStreamInfoStandard = 0;
        private static readonly IntPtr InvalidHandle = new IntPtr(-1);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WIN32_FIND_STREAM_DATA
        {
            public long StreamSize;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 296)]
            public string cStreamName;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr FindFirstStreamW(string lpFileName, int infoLevel, out WIN32_FIND_STREAM_DATA lpFindStreamData, int dwFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool FindNextStreamW(IntPtr hFindStream, out WIN32_FIND_STREAM_DATA lpFindStreamData);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FindClose(IntPtr hFindFile);

        public static List<string> ListStreams(string path)
        {
            List<string> streams = new List<string>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return streams;
            }

            WIN32_FIND_STREAM_DATA data;
            IntPtr handle = FindFirstStreamW(path, FindStreamInfoStandard, out data, 0);
            if (handle == InvalidHandle)
            {
                streams.Add("无法读取 ADS");
                return streams;
            }

            try
            {
                streams.Add(string.IsNullOrEmpty(data.cStreamName) ? "::$DATA" : data.cStreamName);
                while (FindNextStreamW(handle, out data))
                {
                    streams.Add(string.IsNullOrEmpty(data.cStreamName) ? "::$DATA" : data.cStreamName);
                }
            }
            finally
            {
                FindClose(handle);
            }

            return streams;
        }
    }
}
