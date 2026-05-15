using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ShuiyuanHtmlPrivacyCleaner
{
    internal enum CleaningMode
    {
        PersonalOnly = 0,
        FullAnonymous = 1,
    }

    internal sealed class UserAliasProfile
    {
        public readonly HashSet<string> Aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public int FirstIndex = int.MaxValue;
        public string Pseudonym = string.Empty;
    }

    internal sealed partial class CleanerEngine
    {
        private const string TransparentAvatarDataUri = "data:image/gif;base64,R0lGODlhAQABAAAAACwAAAAAAQABAAA=";

        private void ApplyFullAnonymization(ref string text)
        {
            NeutralizeSiteMetadata(ref text);
            ReplaceSiteBranding(ref text);

            List<UserAliasProfile> profiles = ExtractUserAliasProfiles(text);
            AssignUserPseudonyms(profiles);

            NeutralizeSiteSpecificUrls(ref text);
            NeutralizeAvatarImages(ref text);
            StripUserDataAttributes(ref text);
            StripBadgeAndFlairNodes(ref text);
            StripHiddenIdentityNodes(ref text);
            ReplaceUserAliases(ref text, profiles);
            NormalizeNamesBlocks(ref text);
            ReplaceSiteTerms(ref text);
            StripSiteClasses(ref text);
            RemoveResidualExternalUrls(ref text);
        }

        private string FinalizeAnonymousDiscourseHtml(string text)
        {
            string finalized = text;
            NormalizeNamesBlocks(ref finalized);
            StripBadgeAndFlairNodes(ref finalized);
            StripHiddenIdentityNodes(ref finalized);
            InjectAnonymousDiscourseStyle(ref finalized);
            finalized = FixStyleSwallowingArtifacts(finalized);
            return finalized;
        }

        private void NeutralizeSiteMetadata(ref string text)
        {
            text = Regex.Replace(
                text,
                @"<meta\b(?<before>[^>]*)(?<attr>(?:property|name))=(?<quote>[""'])(?<key>og:[^""']+|twitter:[^""']+)\k<quote>(?<after>[^>]*?)\bcontent=(?<quote2>[""']).*?\k<quote2>(?<tail>[^>]*)>",
                "<meta${before}${attr}=${quote}${key}${quote}${after}content=${quote2}${quote2}${tail}>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            ApplyReplacement(ref text, @"\s*<link\b[^>]*(?:canonical|opensearch|alternate)[^>]*>\s*", "\n", RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\s*<script\b[^>]*type=(?:""application/ld\+json""|'application/ld\+json')[^>]*>[\s\S]*?</script>\s*", "\n", RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\s*<discourse-assets-json>[\s\S]*?</discourse-assets-json>\s*", "\n", RegexOptions.IgnoreCase);

            text = Regex.Replace(
                text,
                @"\s*<meta\b[^>]*(?:name|property)\s*=\s*(?:""application-name""|'application-name'|application-name|""generator""|'generator'|generator|""csrf-token""|'csrf-token'|csrf-token|""csrf-param""|'csrf-param'|csrf-param|""fragment""|'fragment'|fragment|""discourse_theme_id""|'discourse_theme_id'|discourse_theme_id|""discourse_current_homepage""|'discourse_current_homepage'|discourse_current_homepage)[^>]*>\s*",
                string.Empty,
                RegexOptions.IgnoreCase);
        }

        private void ReplaceSiteBranding(ref string text)
        {
            text = Regex.Replace(
                text,
                @"<a\b[^>]*class=(?:""[^""]*\bhome-logo\b[^""]*""|'[^']*\bhome-logo\b[^']*'|[^\s>]*\bhome-logo\b[^\s>]*)[^>]*>[\s\S]*?</a>",
                "<span class=\"anonymous-site-brand\">匿名讨论存档</span>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            ApplyReplacement(ref text, @"\s*<img\b[^>]*id=(?:""site-logo""|'site-logo'|site-logo)\b[^>]*>\s*", string.Empty, RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\s*<img\b[^>]*class=(?:""[^""]*\blogo-big\b[^""]*""|'[^']*\blogo-big\b[^']*'|[^>]*\blogo-big\b[^>]*)>\s*", string.Empty, RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\s*<img\b[^>]*class=(?:""[^""]*\blogo-small\b[^""]*""|'[^']*\blogo-small\b[^']*'|[^>]*\blogo-small\b[^>]*)>\s*", string.Empty, RegexOptions.IgnoreCase);
        }

        private void NeutralizeSiteSpecificUrls(ref string text)
        {
            text = Regex.Replace(
                text,
                @"\b(?<attr>href|src|content|data-download-href)=(?<quote>[""']?)(?<url>https?://(?!www\.w3\.org/)[^""'\s>]+)(?<endquote>[""']?)",
                delegate (Match match)
                {
                    string attr = match.Groups["attr"].Value;
                    string quote = match.Groups["quote"].Value;
                    string endquote = match.Groups["endquote"].Value;
                    string url = match.Groups["url"].Value;

                    if (url.IndexOf("shuiyuan", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        url.IndexOf("sjtu.edu.cn", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        string.Equals(attr, "href", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(attr, "data-download-href", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.IsNullOrEmpty(quote))
                        {
                            quote = "\"";
                            endquote = "\"";
                        }

                        string replacement = string.Equals(attr, "src", StringComparison.OrdinalIgnoreCase)
                            ? TransparentAvatarDataUri
                            : "#";
                        return attr + "=" + quote + replacement + endquote;
                    }

                    return match.Value;
                },
                RegexOptions.IgnoreCase);
        }

        private void NeutralizeAvatarImages(ref string text)
        {
            text = Regex.Replace(
                text,
                @"<img\b(?<attrs>[^>]*\bclass=(?:""[^""]*\bavatar\b[^""]*""|'[^']*\bavatar\b[^']*'|[^\s>]*\bavatar\b[^\s>]*)[^>]*)>",
                delegate (Match match)
                {
                    string attrs = match.Groups["attrs"].Value;
                    attrs = Regex.Replace(attrs, @"\bsrc=([""']).*?\1", "src=\"" + TransparentAvatarDataUri + "\"", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    attrs = Regex.Replace(attrs, @"\bsrc=(?![""'])[^\s>]+", "src=\"" + TransparentAvatarDataUri + "\"", RegexOptions.IgnoreCase);
                    attrs = Regex.Replace(attrs, @"\balt=([""']).*?\1", "alt=\"\"", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    attrs = Regex.Replace(attrs, @"\btitle=([""']).*?\1", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    attrs = Regex.Replace(
                        attrs,
                        @"\bstyle=([""'])(?<style>.*?)\1",
                        delegate (Match styleMatch)
                        {
                            string style = styleMatch.Groups["style"].Value;
                            style = Regex.Replace(style, @"background-image\s*:[^;]+;?", string.Empty, RegexOptions.IgnoreCase);
                            style = Regex.Replace(style, @"\s{2,}", " ");
                            style = style.Trim();
                            return string.IsNullOrEmpty(style) ? string.Empty : " style=\"" + style + "\"";
                        },
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    return "<img" + attrs + ">";
                },
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            ApplyReplacement(ref text, @"background-image\s*:\s*var\(--sf-img-\d+\)!important;?", string.Empty, RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"https?://[^""'\s>]+/user_avatar/[^""'\s>]+", TransparentAvatarDataUri, RegexOptions.IgnoreCase);
        }

        private void StripUserDataAttributes(ref string text)
        {
            ApplyReplacement(ref text, @"\sdata-user-card=(?:[^\s>]+|""[^""]*""|'[^']*')", string.Empty, RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\sdata-user-id=(?:[^\s>]+|""[^""]*""|'[^']*')", string.Empty, RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\sdata-username=(?:[^\s>]+|""[^""]*""|'[^']*')", string.Empty, RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\sdata-post=(?:[^\s>]+|""[^""]*""|'[^']*')", string.Empty, RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\sdata-topic=(?:[^\s>]+|""[^""]*""|'[^']*')", string.Empty, RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\sdata-user-title=(?:[^\s>]+|""[^""]*""|'[^']*')", string.Empty, RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\sdata-category-id=(?:[^\s>]+|""[^""]*""|'[^']*')", string.Empty, RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\sdata-category-name=(?:[^\s>]+|""[^""]*""|'[^']*')", string.Empty, RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\sdata-tag-name=(?:[^\s>]+|""[^""]*""|'[^']*')", string.Empty, RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\saria-label=(?:""[^""]*(?:的个人资料|profile)[^""]*""|'[^']*(?:的个人资料|profile)[^']*')", " aria-label=\"匿名用户\"", RegexOptions.IgnoreCase);
        }

        private void StripBadgeAndFlairNodes(ref string text)
        {
            ApplyReplacement(ref text, @"\s*<span\b[^>]*class=(?:""[^""]*\buser-title\b[^""]*""|'[^']*\buser-title\b[^']*'|[^>]*\buser-title\b[^>]*)[^>]*>[\s\S]*?</span>\s*", string.Empty, RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\s*<div\b[^>]*class=(?:""[^""]*\bavatar-flair\b[^""]*""|'[^']*\bavatar-flair\b[^']*'|[^>]*\bavatar-flair\b[^>]*)[^>]*>[\s\S]*?</div>\s*", string.Empty, RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\s*<span\b[^>]*class=(?:""[^""]*\bavatar-flair\b[^""]*""|'[^']*\bavatar-flair\b[^']*'|[^>]*\bavatar-flair\b[^>]*)[^>]*>[\s\S]*?</span>\s*", string.Empty, RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\s*<span\b[^>]*class=(?:""[^""]*\bposter-icon\b[^""]*""|'[^']*\bposter-icon\b[^']*'|[^>]*\bposter-icon\b[^>]*)[^>]*>[\s\S]*?</span>\s*", string.Empty, RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\s*<span\b[^>]*class=(?:""[^""]*\bbadge-wrapper\b[^""]*""|'[^']*\bbadge-wrapper\b[^']*'|[^>]*\bbadge-wrapper\b[^>]*)[^>]*>[\s\S]*?</span>\s*", string.Empty, RegexOptions.IgnoreCase);
        }

        private void StripHiddenIdentityNodes(ref string text)
        {
            ApplyReplacement(ref text, @"\s*<div\b[^>]*class=(?:""[^""]*\buser-card\b[^""]*""|'[^']*\buser-card\b[^']*'|[^>]*\buser-card\b[^>]*)[^>]*>[\s\S]*?</div>\s*", "\n", RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\s*<div\b[^>]*class=(?:""[^""]*\bgroup-card\b[^""]*""|'[^']*\bgroup-card\b[^']*'|[^>]*\bgroup-card\b[^>]*)[^>]*>[\s\S]*?</div>\s*", "\n", RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\s*<style\b[^>]*\b(?:id=(?:""d-styles""|'d-styles'|d-styles)|class=(?:""[^""]*\bsf-hidden\b[^""]*""|'[^']*\bsf-hidden\b[^']*'|[^\s>]*\bsf-hidden\b[^\s>]*))[^>]*>[\s\S]*?</style>\s*", "\n", RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\s*<span\b[^>]*class=(?:""[^""]*\bsf-hidden\b[^""]*""|'[^']*\bsf-hidden\b[^']*'|[^>]*\bsf-hidden\b[^>]*)[^>]*>[\s\S]*?</span>\s*", string.Empty, RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\s*<div\b[^>]*class=(?:""[^""]*\bsf-hidden\b[^""]*""|'[^']*\bsf-hidden\b[^']*'|[^>]*\bsf-hidden\b[^>]*)[^>]*>[\s\S]*?</div>\s*", string.Empty, RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\s*<div\b[^>]*class=(?:""[^""]*\bquote-controls\b[^""]*""|'[^']*\bquote-controls\b[^']*'|[^>]*\bquote-controls\b[^>]*)[^>]*>[\s\S]*?</div>\s*", string.Empty, RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\s*<button\b[^>]*class=(?:""[^""]*\bquote-toggle\b[^""]*""|'[^']*\bquote-toggle\b[^']*'|[^>]*\bquote-toggle\b[^>]*)[^>]*>[\s\S]*?</button>\s*", string.Empty, RegexOptions.IgnoreCase);
        }

        private void ReplaceSiteTerms(ref string text)
        {
            ReplacePlainAndEncoded(ref text, "shuiyuan.s3.jcloud.sjtu.edu.cn", "anonymous-site");
            ReplacePlainAndEncoded(ref text, "shuiyuan.sjtu.edu.cn", "anonymous-site");
            ReplacePlainAndEncoded(ref text, "sjtu.edu.cn", "anonymous-site");
            ReplacePlainAndEncoded(ref text, "水源社区", "匿名讨论站");
            ReplacePlainAndEncoded(ref text, "水源广场", "匿名板块");
            ReplacePlainAndEncoded(ref text, "谈笑风生", "匿名分区");
            ReplacePlainAndEncoded(ref text, "热点新闻", "匿名分区");
            ReplacePlainAndEncoded(ref text, "上海交通大学", "匿名站点");
        }

        private void StripSiteClasses(ref string text)
        {
            text = Regex.Replace(
                text,
                @"\bclass=(?<quote>[""'])(?<value>.*?)\k<quote>",
                delegate (Match match)
                {
                    string quote = match.Groups["quote"].Value;
                    string value = SanitizeClassTokens(match.Groups["value"].Value);
                    return "class=" + quote + value + quote;
                },
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            text = Regex.Replace(
                text,
                @"\bclass=(?<value>[^\s>]+)",
                delegate (Match match)
                {
                    return "class=" + SanitizeClassTokens(match.Groups["value"].Value);
                },
                RegexOptions.IgnoreCase);
        }

        private static string SanitizeClassTokens(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            string[] parts = value.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].StartsWith("category-", StringComparison.OrdinalIgnoreCase))
                {
                    parts[i] = "category-anonymous";
                }
                else if (parts[i].StartsWith("tag-", StringComparison.OrdinalIgnoreCase))
                {
                    parts[i] = "tag-anonymous";
                }
            }

            return string.Join(" ", parts);
        }

        private void RemoveResidualExternalUrls(ref string text)
        {
            ApplyReplacement(ref text, @"https?://(?:shuiyuan(?:\.s3\.jcloud)?\.sjtu\.edu\.cn|[^""'\s<>]*sjtu\.edu\.cn)[^\s""'<>]*", "#", RegexOptions.IgnoreCase);
        }

        private void NormalizeNamesBlocks(ref string text)
        {
            text = Regex.Replace(
                text,
                @"<div\b[^>]*class=(?:""[^""]*\bnames\b[^""]*""|'[^']*\bnames\b[^']*'|[^>]*\bnames\b[^>]*)[^>]*>(?<inner>[\s\S]*?)</div>",
                delegate (Match match)
                {
                    string inner = match.Groups["inner"].Value;
                    Match aliasMatch = Regex.Match(inner, @"用户\d+", RegexOptions.IgnoreCase);
                    string alias = aliasMatch.Success ? aliasMatch.Value : "匿名用户";
                    return "<div class=\"names\"><span class=\"first full-name\"><a href=\"#\" aria-label=\"匿名用户\" tabindex=\"0\">" + alias + "</a></span></div>";
                },
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        private void InjectAnonymousDiscourseStyle(ref string text)
        {
            const string marker = "anonymous-discourse-sanitizer";
            if (text.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return;
            }

            string styleBlock =
                "<style id=\"" + marker + "\">" +
                ".anonymous-site-brand{display:inline-flex;align-items:center;min-height:36px;font-size:20px;font-weight:700;color:#2f4858;}" +
                "img.avatar{background:#eef2f6!important;border-radius:50%!important;border:1px solid #d7dfe8!important;}" +
                ".names .second.username{display:none!important;}" +
                ".names .first.full-name a{color:inherit!important;pointer-events:none!important;text-decoration:none!important;}" +
                "a.trigger-user-card,a.poster-avatar,a.main-avatar,a.mention,a.lightbox,a.back{pointer-events:none!important;}" +
                "a.trigger-user-card[href],a.poster-avatar[href],a.main-avatar[href],a.mention[href],a.lightbox[href],a.back[href]{cursor:default!important;}" +
                "</style>";

            int headClose = IndexOfRegex(text, @"</head>", RegexOptions.IgnoreCase);
            if (headClose >= 0)
            {
                text = text.Insert(headClose, styleBlock);
            }
            else
            {
                text = styleBlock + text;
            }
        }

        private List<UserAliasProfile> ExtractUserAliasProfiles(string text)
        {
            List<UserAliasProfile> profiles = new List<UserAliasProfile>();
            Dictionary<string, UserAliasProfile> aliasMap = new Dictionary<string, UserAliasProfile>(StringComparer.OrdinalIgnoreCase);

            RegisterAliasPairs(text, profiles, aliasMap, @"\\&quot;name\\&quot;:\\&quot;(?<left>.*?)\\&quot;,\\&quot;username\\&quot;:\\&quot;(?<right>.*?)\\&quot;");
            RegisterAliasPairs(text, profiles, aliasMap, @"\\&quot;username\\&quot;:\\&quot;(?<left>.*?)\\&quot;[\s\S]{0,320}?\\&quot;display_username\\&quot;:\\&quot;(?<right>.*?)\\&quot;");
            RegisterAliasPairs(text, profiles, aliasMap, @"""name""\s*:\s*""(?<left>(?:\\""|[^""])*)""\s*,\s*""username""\s*:\s*""(?<right>(?:\\""|[^""])*)""");
            RegisterAliasPairs(text, profiles, aliasMap, @"""username""\s*:\s*""(?<left>(?:\\""|[^""])*)""[\s\S]{0,320}?""display_username""\s*:\s*""(?<right>(?:\\""|[^""])*)""");
            RegisterAliasPairs(text, profiles, aliasMap, @"class=(?:""first full-name""|'first full-name'|first\s+full-name)[\s\S]{0,220}?>(?<left>[^<]{2,80})</a>[\s\S]{0,220}?class=(?:""second username""|'second username'|second\s+username)[\s\S]{0,220}?>(?<right>[^<]{2,80})</a>");
            RegisterAliasPairs(text, profiles, aliasMap, @"class=(?:""second username""|'second username'|second\s+username)[\s\S]{0,220}?>(?<left>[^<]{2,80})</a>[\s\S]{0,220}?class=(?:""first full-name""|'first full-name'|first\s+full-name)[\s\S]{0,220}?>(?<right>[^<]{2,80})</a>");
            RegisterAliasPairs(text, profiles, aliasMap, @"class=(?:""quote no-group""|'quote no-group'|quote\s+no-group)[\s\S]{0,320}?data-username=(?:""(?<right>[^""]+)""|'(?<right>[^']+)'|(?<right>[^\s>]+))[\s\S]{0,220}?class=(?:""avatar""|'avatar'|avatar)[^>]*>\s*(?<left>[^<]{2,80})");
            RegisterAliasPairs(text, profiles, aliasMap, @"href=(?:https?://[^""'\s>]+)?/u/(?<right>[^""'\s>/]+)[^>]*>\s*(?<left>[^<]{2,80})\s*</a>");

            RegisterAliasSingles(text, profiles, aliasMap, @"/user_avatar/(?:anonymous-site|shuiyuan(?:\.s3\.jcloud)?\.sjtu\.edu\.cn)/(?<value>[^/]+)/");
            RegisterAliasSingles(text, profiles, aliasMap, @"data-user-card=(?:""(?<value>[^""]+)""|'(?<value>[^']+)'|(?<value>[^\s>]+))");
            RegisterAliasSingles(text, profiles, aliasMap, @"data-username=(?:""(?<value>[^""]+)""|'(?<value>[^']+)'|(?<value>[^\s>]+))");
            RegisterAliasSingles(text, profiles, aliasMap, @"href=(?:https?://[^""'\s>]+)?/u/(?<value>[^""'\s>/]+)");

            return profiles;
        }

        private void RegisterAliasPairs(string text, List<UserAliasProfile> profiles, Dictionary<string, UserAliasProfile> aliasMap, string pattern)
        {
            foreach (Match match in Regex.Matches(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline))
            {
                string left = CleanAliasCandidate(match.Groups["left"].Value);
                string right = CleanAliasCandidate(match.Groups["right"].Value);
                if (left.Length < 2 && right.Length < 2)
                {
                    continue;
                }

                UserAliasProfile profile = MergeOrCreateProfile(profiles, aliasMap, left, right);
                UpdateFirstIndex(text, profile, left);
                UpdateFirstIndex(text, profile, right);
            }
        }

        private void RegisterAliasSingles(string text, List<UserAliasProfile> profiles, Dictionary<string, UserAliasProfile> aliasMap, string pattern)
        {
            foreach (Match match in Regex.Matches(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline))
            {
                string value = CleanAliasCandidate(match.Groups["value"].Value);
                if (value.Length < 2)
                {
                    continue;
                }

                UserAliasProfile profile = MergeOrCreateProfile(profiles, aliasMap, value);
                UpdateFirstIndex(text, profile, value);
            }
        }

        private static UserAliasProfile MergeOrCreateProfile(List<UserAliasProfile> profiles, Dictionary<string, UserAliasProfile> aliasMap, params string[] aliases)
        {
            UserAliasProfile chosen = null;

            for (int i = 0; i < aliases.Length; i++)
            {
                string alias = aliases[i];
                if (string.IsNullOrEmpty(alias))
                {
                    continue;
                }

                if (aliasMap.TryGetValue(alias, out UserAliasProfile existing))
                {
                    chosen = existing;
                    break;
                }
            }

            if (chosen == null)
            {
                chosen = new UserAliasProfile();
                profiles.Add(chosen);
            }

            for (int i = 0; i < aliases.Length; i++)
            {
                string alias = aliases[i];
                if (alias.Length < 2)
                {
                    continue;
                }

                chosen.Aliases.Add(alias);
                aliasMap[alias] = chosen;

                if (IsAsciiAlias(alias))
                {
                    string lower = alias.ToLowerInvariant();
                    chosen.Aliases.Add(lower);
                    aliasMap[lower] = chosen;
                }
            }

            return chosen;
        }

        private static void AssignUserPseudonyms(List<UserAliasProfile> profiles)
        {
            profiles.Sort(delegate (UserAliasProfile left, UserAliasProfile right)
            {
                int byIndex = left.FirstIndex.CompareTo(right.FirstIndex);
                if (byIndex != 0)
                {
                    return byIndex;
                }

                return MaxAliasLength(right).CompareTo(MaxAliasLength(left));
            });

            for (int i = 0; i < profiles.Count; i++)
            {
                profiles[i].Pseudonym = "用户" + (i + 1).ToString();
            }
        }

        private void ReplaceUserAliases(ref string text, List<UserAliasProfile> profiles)
        {
            Dictionary<string, string> replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < profiles.Count; i++)
            {
                foreach (string alias in profiles[i].Aliases)
                {
                    if (alias.Length < 2 || alias == profiles[i].Pseudonym)
                    {
                        continue;
                    }

                    if (!replacements.ContainsKey(alias))
                    {
                        replacements.Add(alias, profiles[i].Pseudonym);
                    }

                    string encodedAlias = Uri.EscapeDataString(alias);
                    string encodedPseudonym = Uri.EscapeDataString(profiles[i].Pseudonym);
                    if (!string.IsNullOrEmpty(encodedAlias) && !replacements.ContainsKey(encodedAlias))
                    {
                        replacements.Add(encodedAlias, encodedPseudonym);
                    }
                }
            }

            if (replacements.Count == 0)
            {
                return;
            }

            List<string> keys = new List<string>(replacements.Keys);
            keys.Sort(delegate (string left, string right) { return right.Length.CompareTo(left.Length); });

            string pattern = string.Join("|", keys.ConvertAll(Regex.Escape).ToArray());
            text = Regex.Replace(
                text,
                pattern,
                delegate (Match match) { return replacements[match.Value]; },
                RegexOptions.IgnoreCase);
        }

        private static void ReplacePlainAndEncoded(ref string text, string oldValue, string newValue)
        {
            if (string.IsNullOrWhiteSpace(oldValue) || oldValue == newValue)
            {
                return;
            }

            text = text.Replace(oldValue, newValue);

            string encodedOld = Uri.EscapeDataString(oldValue);
            string encodedNew = Uri.EscapeDataString(newValue);
            if (!string.IsNullOrEmpty(encodedOld))
            {
                text = Regex.Replace(text, Regex.Escape(encodedOld), encodedNew, RegexOptions.IgnoreCase);
            }
        }

        private static string CleanAliasCandidate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string cleaned = value.Trim();
            cleaned = cleaned.Replace("\\/", "/").Replace("\\\"", "\"").Replace("\\&quot;", "\"");
            cleaned = cleaned.Trim().Trim('@').Trim(':', '：', '"', '\'', '<', '>', '/', '\\');

            if (cleaned.Length < 2 || cleaned.Length > 80)
            {
                return string.Empty;
            }

            if (cleaned.IndexOf("http", StringComparison.OrdinalIgnoreCase) >= 0 ||
                cleaned.IndexOf("{size}", StringComparison.OrdinalIgnoreCase) >= 0 ||
                cleaned.IndexOf("topic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                cleaned.IndexOf("uploads", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return string.Empty;
            }

            return cleaned;
        }

        private static void UpdateFirstIndex(string text, UserAliasProfile profile, string alias)
        {
            if (string.IsNullOrEmpty(alias))
            {
                return;
            }

            int index = text.IndexOf(alias, StringComparison.OrdinalIgnoreCase);
            if (index >= 0 && index < profile.FirstIndex)
            {
                profile.FirstIndex = index;
            }
        }

        private static bool IsAsciiAlias(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] > 127)
                {
                    return false;
                }
            }

            return true;
        }

        private static int MaxAliasLength(UserAliasProfile profile)
        {
            int max = 0;
            foreach (string alias in profile.Aliases)
            {
                if (alias.Length > max)
                {
                    max = alias.Length;
                }
            }

            return max;
        }
    }
}
