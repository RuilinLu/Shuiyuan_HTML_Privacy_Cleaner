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
        public readonly HashSet<string> DisplayAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public readonly HashSet<string> UsernameAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public int FirstIndex = int.MaxValue;
        public string Pseudonym = string.Empty;
        public AnonymousIdentity Identity;
    }

    internal sealed class AnonymousIdentity
    {
        public int Index;
        public string DisplayName;
        public string Username;
        public string AvatarDataUri;
    }

    internal sealed partial class CleanerEngine
    {
        private const string TransparentAvatarDataUri = "data:image/gif;base64,R0lGODlhAQABAAAAACwAAAAAAQABAAA=";
        private static readonly string[] AnonymousAvatarPalette = new string[]
        {
            "#2f6f73", "#8f4f76", "#6b6fb3", "#b36a40", "#4f7f52", "#8a6f2f",
            "#3f7aa6", "#9b4f4f", "#5f6b3f", "#7453a6", "#477070", "#a05f7a"
        };
        private static readonly string[] AnonymousSkinPalette = new string[]
        {
            "#f3c7a5", "#e8b38f", "#d99a73", "#c7815f", "#b86f55", "#f0d0b8", "#dfaa86", "#c89572"
        };
        private static readonly string[] AnonymousHairPalette = new string[]
        {
            "#2c221f", "#4b342d", "#5c4033", "#7b4f35", "#2f3340", "#6f4e37", "#3d2b1f", "#1f2933"
        };
        private static readonly string[] FakeSurnames = new string[]
        {
            "林", "陈", "周", "赵", "沈", "顾", "许", "宋", "唐", "韩", "叶", "程", "陆", "夏", "何", "高",
            "钟", "梁", "苏", "曹", "薛", "杜", "丁", "秦", "姜", "袁", "邵", "邹", "龚", "黎", "潘", "严",
            "萧", "卢", "田", "方", "余", "孟", "白", "石", "江", "侯", "邱", "范", "尹", "魏", "罗", "梅",
            "章", "任", "乔", "贺", "毛", "汤", "傅", "郝", "熊", "金", "廖", "孔", "龙", "万", "段", "戴"
        };
        private static readonly string[] FakeGivenA = new string[]
        {
            "知", "景", "予", "星", "明", "清", "言", "安", "书", "云", "若", "一", "南", "北", "沐", "晓",
            "青", "秋", "禾", "嘉", "宁", "子", "以", "文", "庭", "可", "思", "远", "辰", "初", "怀", "映",
            "泽", "其", "亦", "舒", "允", "芷", "槿", "闻", "衡", "和", "乔", "越", "朗", "临", "简", "澄"
        };
        private static readonly string[] FakeGivenB = new string[]
        {
            "然", "川", "舟", "言", "禾", "野", "溪", "临", "白", "予", "晏", "南", "北", "安", "宁", "秋",
            "遥", "岚", "星", "澈", "晴", "棠", "序", "知", "夏", "冬", "初", "弦", "森", "青", "晖", "云",
            "澄", "远", "砚", "行", "简", "庭", "越", "衡", "鸣", "洲", "沅", "礼", "墨", "嘉", "临", "和"
        };
        private static readonly string[] FakeUsernameLeft = new string[]
        {
            "river", "harbor", "maple", "cedar", "orbit", "silver", "mint", "ember", "aurora", "pixel", "paper", "stone",
            "cloud", "sunset", "meadow", "cobalt", "breeze", "lantern", "willow", "marble", "forest", "violet", "copper", "thunder"
        };
        private static readonly string[] FakeUsernameRight = new string[]
        {
            "note", "field", "bridge", "signal", "lane", "studio", "garden", "reader", "runner", "pilot", "canvas", "thread",
            "anchor", "window", "compass", "planet", "folder", "circle", "branch", "mirror", "valley", "screen", "archive", "marker"
        };

        private void ApplyFullAnonymization(ref string text)
        {
            NeutralizeSiteMetadata(ref text);
            ReplaceSiteBranding(ref text);
            RemoveSiteNavigationShell(ref text);

            List<UserAliasProfile> profiles = ExtractUserAliasProfiles(text);
            AssignUserIdentities(profiles);

            AnnotateAnonymousPosts(ref text, profiles);
            NormalizeReplyToTabs(ref text, profiles);
            ReplaceUserAliases(ref text, profiles);
            NeutralizeAvatarImages(ref text, profiles);
            RestoreAnonymousIdentityMarkers(ref text, profiles);
            RefreshAnonymousReplyToTabs(ref text, profiles);
            RefreshPostHeaderIdentities(ref text, profiles);
            NeutralizeSiteSpecificUrls(ref text);
            StripUserDataAttributes(ref text);
            StripBadgeAndFlairNodes(ref text);
            StripHiddenIdentityNodes(ref text);
            NormalizeAvatarOnlyPosterStacks(ref text);
            RemoveInteractiveAndRecommendationShell(ref text);
            RemoveBalancedShellElements(ref text);
            NormalizeNamesBlocks(ref text);
            ReplaceSiteTerms(ref text);
            AnonymizeTopicTaxonomy(ref text);
            StripSiteClasses(ref text);
            NeutralizeResidualSiteTokens(ref text);
            RemoveResidualExternalUrls(ref text);
        }

        private string FinalizeAnonymousDiscourseHtml(string text)
        {
            string finalized = text;
            NormalizeNamesBlocks(ref finalized);
            StripBadgeAndFlairNodes(ref finalized);
            StripHiddenIdentityNodes(ref finalized);
            RemoveBalancedShellElements(ref finalized);
            RemoveAnonymousAvatarNameBlocks(ref finalized);
            NormalizeAvatarOnlyPosterStacks(ref finalized);
            AnonymizeTopicTaxonomy(ref finalized);
            NeutralizeResidualSiteTokens(ref finalized);
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

        private void RemoveSiteNavigationShell(ref string text)
        {
            ApplyReplacement(
                ref text,
                @"\s*<div\b[^>]*class=(?:""[^""]*\bsidebar-wrapper\b[^""]*""|'[^']*\bsidebar-wrapper\b[^']*'|[^\s>]*\bsidebar-wrapper\b[^\s>]*)[^>]*>\s*<section\b[^>]*class=(?:""[^""]*\bsidebar-container\b[^""]*""|'[^']*\bsidebar-container\b[^']*'|[^\s>]*\bsidebar-container\b[^\s>]*)[^>]*>[\s\S]*?</section>\s*</div>\s*",
                "\n",
                RegexOptions.IgnoreCase);

            ApplyReplacement(
                ref text,
                @"\s*<nav\b[^>]*class=(?:""[^""]*\bsidebar\b[^""]*""|'[^']*\bsidebar\b[^']*'|[^\s>]*\bsidebar\b[^\s>]*)[^>]*>[\s\S]*?</nav>\s*",
                "\n",
                RegexOptions.IgnoreCase);

            ApplyReplacement(
                ref text,
                @"\s*<div\b[^>]*(?:data-section-name=(?:""community""|'community'|community)|data-section-name=(?:""categories""|'categories'|categories)|id=(?:""sidebar-section[^""]*""|'sidebar-section[^']*'|sidebar-section[^\s>]*))[^>]*>[\s\S]*?</div>\s*",
                "\n",
                RegexOptions.IgnoreCase);
        }

        private void NeutralizeSiteSpecificUrls(ref string text)
        {
            text = Regex.Replace(
                text,
                @"\b(?<attr>href|src|srcset|poster|content|data-download-href|data-onebox-src|data-orig-src|data-small-upload|data-large-upload|data-thumbnail-src)=(?<quote>[""']?)(?<url>https?://(?!www\.w3\.org/)[^""'\s>]+)(?<endquote>[""']?)",
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

        private void NeutralizeAvatarImages(ref string text, List<UserAliasProfile> profiles)
        {
            NeutralizeRawSvgAvatarImages(ref text, profiles);

            text = Regex.Replace(
                text,
                @"<img\b(?<attrs>[^>]*\bclass=(?:""[^""]*\bavatar\b[^""]*""|'[^']*\bavatar\b[^']*'|[^\s>]*\bavatar\b[^\s>]*)[^>]*)>",
                delegate (Match match)
                {
                    string attrs = match.Groups["attrs"].Value;
                    AnonymousIdentity identity = FindIdentityFromText(attrs, profiles) ?? BuildFallbackIdentity();
                    string displayName = identity.DisplayName;
                    string avatarUri = identity.AvatarDataUri;
                    attrs = Regex.Replace(attrs, @"\bsrc=([""']).*?\1", "src=\"" + avatarUri + "\"", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    attrs = Regex.Replace(attrs, @"\bsrc=(?![""'])[^\s>]+", "src=\"" + avatarUri + "\"", RegexOptions.IgnoreCase);
                    if (!Regex.IsMatch(attrs, @"\bsrc\s*=", RegexOptions.IgnoreCase))
                    {
                        attrs += " src=\"" + avatarUri + "\"";
                    }

                    attrs = Regex.Replace(attrs, @"\bsrcset=([""']).*?\1", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    attrs = Regex.Replace(attrs, @"\bsrcset=(?![""'])[^\s>]+", string.Empty, RegexOptions.IgnoreCase);
                    attrs = SetOrAppendAttribute(attrs, "alt", displayName);
                    attrs = SetOrAppendAttribute(attrs, "title", displayName);
                    attrs = SetOrAppendAttribute(attrs, "data-anon-index", identity.Index.ToString());
                    attrs = SetOrAppendAttribute(attrs, "data-anon-display", displayName);
                    attrs = SetOrAppendAttribute(attrs, "data-anon-username", identity.Username);
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

            ApplyReplacement(ref text, @"https?://[^""'\s>]+/user_avatar/[^""'\s>]+", TransparentAvatarDataUri, RegexOptions.IgnoreCase);
        }

        private void NeutralizeRawSvgAvatarImages(ref string text, List<UserAliasProfile> profiles)
        {
            text = Regex.Replace(
                text,
                @"<img\b(?<attrs>[\s\S]{0,1500}?class=(?:""[^""]*\bavatar\b[^""]*""|'[^']*\bavatar\b[^']*'|[^\s>]*\bavatar\b[^\s>]*)[\s\S]{0,1500}?)(?=</a>|</span>|</div>)",
                delegate (Match match)
                {
                    string attrs = match.Groups["attrs"].Value;
                    AnonymousIdentity identity = FindIdentityFromText(attrs, profiles) ?? BuildFallbackIdentity();
                    return "<img src=\"" + identity.AvatarDataUri + "\" class=\"avatar\" width=\"48\" height=\"48\" alt=\"" + HtmlAttribute(identity.DisplayName) + "\" title=\"" + HtmlAttribute(identity.DisplayName) + "\" data-anon-index=\"" + identity.Index.ToString() + "\" data-anon-display=\"" + HtmlAttribute(identity.DisplayName) + "\" data-anon-username=\"" + HtmlAttribute(identity.Username) + "\">";
                },
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
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

        private void RemoveInteractiveAndRecommendationShell(ref string text)
        {
            ApplyReplacement(ref text, @"\s*<div\b[^>]*class=(?:""[^""]*\bglobal-notice\b[^""]*""|'[^']*\bglobal-notice\b[^']*'|[^\s>]*\bglobal-notice\b[^\s>]*)[^>]*>[\s\S]*?</div>\s*</div>\s*</div>\s*", "\n", RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\s*<div\b[^>]*class=(?:""[^""]*\bforced-anonymous\b[^""]*""|'[^']*\bforced-anonymous\b[^']*'|[^\s>]*\bforced-anonymous\b[^\s>]*)[^>]*>[\s\S]*?</div>\s*", "\n", RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\s*<div\b[^>]*class=(?:""[^""]*\bmore-topics__container\b[^""]*""|'[^']*\bmore-topics__container\b[^']*'|[^\s>]*\bmore-topics__container\b[^\s>]*)[^>]*>[\s\S]*?(?=</section>)", "\n", RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\s*<section\b[^>]*class=(?:""[^""]*\bmore-topics\b[^""]*""|'[^']*\bmore-topics\b[^']*'|[^\s>]*\bmore-topics\b[^\s>]*)[^>]*>[\s\S]*?</section>\s*", "\n", RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\s*<div\b[^>]*class=(?:""[^""]*\bsuggested-topics\b[^""]*""|'[^']*\bsuggested-topics\b[^']*'|[^\s>]*\bsuggested-topics\b[^\s>]*)[^>]*>[\s\S]*?</div>\s*", "\n", RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\s*<div\b[^>]*class=(?:""[^""]*\btopic-footer-main-buttons\b[^""]*""|'[^']*\btopic-footer-main-buttons\b[^']*'|[^\s>]*\btopic-footer-main-buttons\b[^\s>]*)[^>]*>[\s\S]*?</div>\s*", "\n", RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"\s*<div\b[^>]*class=(?:""[^""]*\bnotifications-button-footer\b[^""]*""|'[^']*\bnotifications-button-footer\b[^']*'|[^\s>]*\bnotifications-button-footer\b[^\s>]*)[^>]*>[\s\S]*?</div>\s*", "\n", RegexOptions.IgnoreCase);
        }

        private static void RemoveBalancedShellElements(ref string text)
        {
            RemovePreTopicAreaSummary(ref text);

            string[] classNames = new string[]
            {
                "sidebar-wrapper",
                "sidebar-container",
                "sidebar-sections",
                "sidebar-section",
                "sidebar-section-wrapper",
                "sidebar-section-header",
                "sidebar-section-content",
                "sidebar-section-link-wrapper",
                "sidebar-footer-wrapper",
                "who-read",
                "topic-navigation",
                "timeline-container",
                "topic-timeline",
                "d-toc-wrapper",
                "topic-above-post-stream-outlet",
                "more-topics__container",
                "more-topics",
                "suggested-topics",
                "topic-map__additional-contents",
                "topic-map__buttons",
            };

            for (int i = 0; i < classNames.Length; i++)
            {
                RemoveElementsByClassName(ref text, classNames[i]);
            }
        }

        private static void RemovePreTopicAreaSummary(ref string text)
        {
            Match start = Regex.Match(text, @"<div\b[^>]*class=(?:""container posts""|'container posts'|container\s+posts)[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match end = Regex.Match(text, @"<div\b[^>]*class=(?:""row""|'row'|row)[^>]*>\s*<section\b[^>]*\bid=(?:""topic""|'topic'|topic)\b", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (start.Success && end.Success && end.Index > start.Index)
            {
                text = text.Remove(start.Index, end.Index - start.Index);
            }
        }

        private static void RemoveAnonymousAvatarNameBlocks(ref string text)
        {
            text = Regex.Replace(text, @"\s*<div\b[^>]*class=(?:""anonymous-avatar-name""|'anonymous-avatar-name'|anonymous-avatar-name)[^>]*>[\s\S]*?</div>\s*", string.Empty, RegexOptions.IgnoreCase);
        }

        private static void NormalizeAvatarOnlyPosterStacks(ref string text)
        {
            string posterAnchor = @"<a\b(?=[^>]*\bclass=(?:""[^""]*\bposter\b[^""]*""|'[^']*\bposter\b[^']*'|[^\s>]*\bposter\b[^\s>]*))[^>]*>\s*<img\b[^>]*\bclass=(?:""[^""]*\bavatar\b[^""]*""|'[^']*\bavatar\b[^']*'|[^\s>]*\bavatar\b[^\s>]*)[^>]*>\s*</a>";
            string stackPattern = @"\s*<div>\s*(?:(?:<div>\s*)?" + posterAnchor + @"\s*</div>\s*)+</div>\s*";
            int guard = 0;
            while (guard++ < 100)
            {
                string next = Regex.Replace(
                    text,
                    stackPattern,
                    delegate (Match match)
                    {
                        List<string> anchors = new List<string>();
                        foreach (Match anchorMatch in Regex.Matches(match.Value, posterAnchor, RegexOptions.IgnoreCase | RegexOptions.Singleline))
                        {
                            anchors.Add(anchorMatch.Value);
                        }

                        if (anchors.Count == 0)
                        {
                            return match.Value;
                        }

                        return "\n<div class=\"topic-map__users-list anonymous-topic-map-users\">" + string.Join("", anchors.ToArray()) + "</div>\n";
                    },
                    RegexOptions.IgnoreCase | RegexOptions.Singleline,
                    TimeSpan.FromSeconds(2));
                if (next.Length == text.Length)
                {
                    return;
                }
                text = next;
            }
        }

        private static void RemoveElementsByClassName(ref string text, string className)
        {
            string pattern = @"<(?<tag>div|section|aside|nav|span)\b(?=[^>]*\bclass=(?:""[^""]*\b" + Regex.Escape(className) + @"\b[^""]*""|'[^']*\b" + Regex.Escape(className) + @"\b[^']*'|[^\s>]*\b" + Regex.Escape(className) + @"\b[^\s>]*))[^>]*>";
            int guard = 0;
            while (guard++ < 200)
            {
                Match match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (!match.Success)
                {
                    return;
                }

                string tagName = match.Groups["tag"].Value;
                int end = FindMatchingElementEnd(text, match.Index, tagName);
                if (end <= match.Index)
                {
                    text = text.Remove(match.Index, match.Length);
                }
                else
                {
                    text = text.Remove(match.Index, end - match.Index);
                }
            }
        }

        private static int FindMatchingElementEnd(string text, int startIndex, string tagName)
        {
            Match first = Regex.Match(text.Substring(startIndex), @"<" + Regex.Escape(tagName) + @"\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!first.Success)
            {
                return -1;
            }

            int depth = 1;
            int scanStart = startIndex + first.Index + first.Length;
            Regex tagRegex = new Regex(@"</?" + Regex.Escape(tagName) + @"\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            while (scanStart < text.Length)
            {
                Match match = tagRegex.Match(text, scanStart);
                if (!match.Success)
                {
                    return -1;
                }

                bool closing = match.Value.StartsWith("</", StringComparison.Ordinal);
                bool selfClosing = match.Value.EndsWith("/>", StringComparison.Ordinal);
                if (closing)
                {
                    depth--;
                    if (depth == 0)
                    {
                        return match.Index + match.Length;
                    }
                }
                else if (!selfClosing)
                {
                    depth++;
                }

                scanStart = match.Index + match.Length;
            }

            return -1;
        }

        private void ReplaceSiteTerms(ref string text)
        {
            ReplacePlainAndEncoded(ref text, "shuiyuan.s3.jcloud.sjtu.edu.cn", "anonymous-site");
            ReplacePlainAndEncoded(ref text, "shuiyuan.sjtu.edu.cn", "anonymous-site");
            ReplacePlainAndEncoded(ref text, "sjtu.edu.cn", "anonymous-site");
            ReplacePlainAndEncoded(ref text, "水源社区", "匿名讨论站");
            ReplacePlainAndEncoded(ref text, "水源广场", "匿名板块");
            ReplacePlainAndEncoded(ref text, "水源活动", "匿名活动");
            ReplacePlainAndEncoded(ref text, "所有类别", "全部分类");
            ReplacePlainAndEncoded(ref text, "所有话题", "全部话题");
            ReplacePlainAndEncoded(ref text, "探索文档话题", "浏览文档");
            ReplacePlainAndEncoded(ref text, "谈笑风生", "匿名分区");
            ReplacePlainAndEncoded(ref text, "热点新闻", "匿名分区");
            ReplacePlainAndEncoded(ref text, "站务公告", "站点公告");
            ReplacePlainAndEncoded(ref text, "上海交通大学", "匿名站点");
        }

        private static void AnonymizeTopicTaxonomy(ref string text)
        {
            int tagIndex = 0;
            text = Regex.Replace(
                text,
                @"<a\b(?<attrs>[^>]*class=(?:""[^""]*\bdiscourse-tag\b[^""]*""|'[^']*\bdiscourse-tag\b[^']*'|[^\s>]*\bdiscourse-tag\b[^\s>]*)[^>]*)>[\s\S]*?</a>",
                delegate (Match match)
                {
                    tagIndex++;
                    string attrs = match.Groups["attrs"].Value;
                    attrs = Regex.Replace(attrs, @"\bhref=([""']).*?\1", "href=\"#\"", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    attrs = Regex.Replace(attrs, @"\bhref=(?![""'])[^\s>]+", "href=\"#\"", RegexOptions.IgnoreCase);
                    attrs = SetOrAppendAttribute(attrs, "title", "匿名标签" + tagIndex.ToString());
                    return "<a" + attrs + ">匿名标签" + tagIndex.ToString() + "</a>";
                },
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            text = Regex.Replace(
                text,
                @"(<(?:span|a)\b[^>]*class=(?:""[^""]*\bbadge-category\b[^""]*""|'[^']*\bbadge-category\b[^']*'|[^\s>]*\bbadge-category\b[^\s>]*)[^>]*?)\s+title=(?:""[^""]*""|'[^']*'|[^\s>]+)",
                "$1",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
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
                @"\bclass=(?![""'])(?<value>[^\s>]+)",
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
                else if (string.Equals(parts[i], "sticky-avatar", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(parts[i], "post--sticky-avatar", StringComparison.OrdinalIgnoreCase))
                {
                    parts[i] = "anonymous-static-avatar";
                }
            }

            List<string> unique = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < parts.Length; i++)
            {
                if (seen.Add(parts[i]))
                {
                    unique.Add(parts[i]);
                }
            }

            return string.Join(" ", unique.ToArray());
        }

        private static void NeutralizeResidualSiteTokens(ref string text)
        {
            ReplacePlainAndEncoded(ref text, "avatar-flair", "anonymous-badge-removed");
            ReplacePlainAndEncoded(ref text, "user-title", "anonymous-title-removed");
            ReplacePlainAndEncoded(ref text, "poster-icon", "anonymous-poster-marker-removed");
            ReplacePlainAndEncoded(ref text, "badge-wrapper", "anonymous-badge-wrapper-removed");
            ReplacePlainAndEncoded(ref text, "data-user-card", "data-anonymous-card");
            ReplacePlainAndEncoded(ref text, "data-user-id", "data-anonymous-id");
        }

        private void RemoveResidualExternalUrls(ref string text)
        {
            ApplyReplacement(ref text, @"https?://(?:shuiyuan(?:\.s3\.jcloud)?\.sjtu\.edu\.cn|[^""'\s<>]*sjtu\.edu\.cn)[^\s""'<>]*", "#", RegexOptions.IgnoreCase);
            ApplyReplacement(ref text, @"https?://(?!www\.w3\.org/)[^\s""'<>]+", "#", RegexOptions.IgnoreCase);
        }

        private void NormalizeNamesBlocks(ref string text)
        {
            text = Regex.Replace(
                text,
                @"<div\b(?<attrs>[^>]*class=(?:""[^""]*\bnames\b[^""]*""|'[^']*\bnames\b[^']*'|[^>]*\bnames\b[^>]*)[^>]*)>(?<inner>[\s\S]*?)</div>",
                delegate (Match match)
                {
                    string attrs = match.Groups["attrs"].Value;
                    string inner = match.Groups["inner"].Value;
                    inner = Regex.Replace(inner, @"\bhref=([""']).*?\1", "href=\"#\"", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    inner = Regex.Replace(inner, @"\bhref=(?![""'])[^\s>]+", "href=\"#\"", RegexOptions.IgnoreCase);
                    inner = Regex.Replace(inner, @"\sdata-user-card=(?:[^\s>]+|""[^""]*""|'[^']*')", string.Empty, RegexOptions.IgnoreCase);
                    inner = Regex.Replace(inner, @"\sdata-user-id=(?:[^\s>]+|""[^""]*""|'[^']*')", string.Empty, RegexOptions.IgnoreCase);
                    return "<div" + attrs + ">" + inner + "</div>";
                },
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        private void AnnotateAnonymousPosts(ref string text, List<UserAliasProfile> profiles)
        {
            text = Regex.Replace(
                text,
                @"<div\b(?=[^>]*\bclass=(?:""[^""]*\btopic-post\b[^""]*""|'[^']*\btopic-post\b[^']*'|[^\s>]*\btopic-post\b[^\s>]*))[\s\S]*?(?=<div\b(?=[^>]*\bclass=(?:""[^""]*\btopic-post\b[^""]*""|'[^']*\btopic-post\b[^']*'|[^\s>]*\btopic-post\b[^\s>]*))|<div\b[^>]*class=(?:""[^""]*\btopic-navigation\b[^""]*""|'[^']*\btopic-navigation\b[^']*'|[^\s>]*\btopic-navigation\b[^\s>]*)|\z)",
                delegate (Match match)
                {
                    string post = match.Value;
                    UserAliasProfile profile = FindProfileForPost(post, profiles);
                    if (profile == null || profile.Identity == null)
                    {
                        return post;
                    }

                    AnonymousIdentity identity = profile.Identity;
                    string nameHtml = BuildAnonymousNameBlock(identity);

                    post = Regex.Replace(
                        post,
                        @"<div\b(?<attrs>[^>]*\bclass=(?:""[^""]*\bpost-avatar\b[^""]*""|'[^']*\bpost-avatar\b[^']*'|[^\s>]*\bpost-avatar\b[^\s>]*)[^>]*)>",
                        delegate (Match avatarMatch)
                        {
                            string attrs = EnsureAnonymousAvatarAttributes(avatarMatch.Groups["attrs"].Value, identity);
                            return "<div" + attrs + ">";
                        },
                        RegexOptions.IgnoreCase | RegexOptions.Singleline,
                        TimeSpan.FromSeconds(2));

                    post = Regex.Replace(
                        post,
                        @"<img\b(?<attrs>[^>]*\bclass=(?:""[^""]*\bavatar\b[^""]*""|'[^']*\bavatar\b[^']*'|[^\s>]*\bavatar\b[^\s>]*)[^>]*)>",
                        delegate (Match imgMatch)
                        {
                            string attrs = EnsureAnonymousAvatarAttributes(imgMatch.Groups["attrs"].Value, identity);
                            return "<img" + attrs + ">";
                        },
                        RegexOptions.IgnoreCase | RegexOptions.Singleline,
                        TimeSpan.FromSeconds(2));

                    int cookedIndex = IndexOfRegexStatic(post, @"<div\b[^>]*\bclass=(?:""[^""]*\bcooked\b[^""]*""|'[^']*\bcooked\b[^']*'|[^\s>]*\bcooked\b[^\s>]*)", RegexOptions.IgnoreCase);
                    string header = cookedIndex > 0 ? post.Substring(0, cookedIndex) : post;
                    string body = cookedIndex > 0 ? post.Substring(cookedIndex) : string.Empty;
                    header = ReplaceFirstRegex(
                        header,
                        @"<div\b[^>]*\bclass=(?:""[^""]*\bnames\b[^""]*""|'[^']*\bnames\b[^']*'|[^\s>]*\bnames\b[^\s>]*)[^>]*>[\s\S]*?</div>",
                        nameHtml,
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    if (header.IndexOf("anonymous-names", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        header = Regex.Replace(
                            header,
                            @"(<div\b[^>]*\bclass=(?:""[^""]*\btopic-meta-data\b[^""]*""|'[^']*\btopic-meta-data\b[^']*'|[^\s>]*\btopic-meta-data\b[^\s>]*)[^>]*>)",
                            "$1" + nameHtml,
                            RegexOptions.IgnoreCase | RegexOptions.Singleline,
                            TimeSpan.FromSeconds(2));
                    }

                    return header + body;
                },
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        private void NormalizeReplyToTabs(ref string text, List<UserAliasProfile> profiles)
        {
            Dictionary<string, AnonymousIdentity> identityByPostNumber = BuildPostIdentityMap(text, profiles);

            text = Regex.Replace(
                text,
                @"<a\b(?<attrs>[^>]*\bclass=(?:""[^""]*\breply-to-tab\b[^""]*""|'[^']*\breply-to-tab\b[^']*'|[^\s>]*\breply-to-tab\b[^\s>]*)[^>]*)>[\s\S]*?</a>",
                delegate (Match match)
                {
                    string block = match.Value;
                    AnonymousIdentity identity = FindIdentityFromText(block, profiles);
                    if (identity == null)
                    {
                        string postNumber = ExtractReplyTargetPostNumber(block);
                        if (!string.IsNullOrEmpty(postNumber))
                        {
                            identityByPostNumber.TryGetValue(postNumber, out identity);
                        }
                    }

                    if (identity == null)
                    {
                        identity = BuildFallbackIdentity();
                    }

                    return BuildAnonymousReplyToTab(identity);
                },
                RegexOptions.IgnoreCase | RegexOptions.Singleline,
                TimeSpan.FromSeconds(2));
        }

        private Dictionary<string, AnonymousIdentity> BuildPostIdentityMap(string text, List<UserAliasProfile> profiles)
        {
            Dictionary<string, AnonymousIdentity> map = new Dictionary<string, AnonymousIdentity>(StringComparer.OrdinalIgnoreCase);
            foreach (Match match in Regex.Matches(
                text,
                @"<div\b(?=[^>]*\bclass=(?:""[^""]*\btopic-post\b[^""]*""|'[^']*\btopic-post\b[^']*'|[^\s>]*\btopic-post\b[^\s>]*))[\s\S]*?(?=<div\b(?=[^>]*\bclass=(?:""[^""]*\btopic-post\b[^""]*""|'[^']*\btopic-post\b[^']*'|[^\s>]*\btopic-post\b[^\s>]*))|<div\b[^>]*class=(?:""[^""]*\btopic-navigation\b[^""]*""|'[^']*\btopic-navigation\b[^']*'|[^\s>]*\btopic-navigation\b[^\s>]*)|\z)",
                RegexOptions.IgnoreCase | RegexOptions.Singleline))
            {
                string post = match.Value;
                Match numberMatch = Regex.Match(post, @"\bdata-post-number=(?:""(?<value>\d+)""|'(?<value>\d+)'|(?<value>\d+))", RegexOptions.IgnoreCase);
                if (!numberMatch.Success)
                {
                    continue;
                }

                UserAliasProfile profile = FindProfileForPost(post, profiles);
                if (profile != null && profile.Identity != null)
                {
                    map[numberMatch.Groups["value"].Value] = profile.Identity;
                }
            }

            return map;
        }

        private static string ExtractReplyTargetPostNumber(string block)
        {
            Match hrefMatch = Regex.Match(block, @"\bhref=(?:""(?<href>[^""]+)""|'(?<href>[^']+)'|(?<href>[^\s>]+))", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!hrefMatch.Success)
            {
                return string.Empty;
            }

            string href = hrefMatch.Groups["href"].Value;
            Match postAnchor = Regex.Match(href, @"(?:#post_|/)(?<number>\d+)(?:[/?#].*)?$", RegexOptions.IgnoreCase);
            return postAnchor.Success ? postAnchor.Groups["number"].Value : string.Empty;
        }

        private static string BuildAnonymousReplyToTab(AnonymousIdentity identity)
        {
            string display = HtmlText(identity.DisplayName);
            string username = HtmlText(identity.Username);
            string attrDisplay = HtmlAttribute(identity.DisplayName);
            string attrUsername = HtmlAttribute(identity.Username);
            string title = HtmlAttribute("回复给 " + identity.DisplayName + " (@" + identity.Username + ")");
            return "<a href=\"#\" class=\"reply-to-tab anonymous-reply-to\" role=\"button\" title=\"" + title + "\" data-anon-index=\"" + identity.Index.ToString() + "\" data-anon-display=\"" + attrDisplay + "\" data-anon-username=\"" + attrUsername + "\">" +
                "<svg class=\"fa d-icon d-icon-share svg-icon fa-width-auto svg-string\" width=\"1em\" height=\"1em\" aria-hidden=\"true\" xmlns=\"http://www.w3.org/2000/svg\"><use href=\"#share\"></use></svg>" +
                "<img class=\"avatar\" width=\"24\" height=\"24\" src=\"" + identity.AvatarDataUri + "\" alt=\"" + attrDisplay + "\" title=\"" + attrDisplay + "\" data-anon-index=\"" + identity.Index.ToString() + "\" data-anon-display=\"" + attrDisplay + "\" data-anon-username=\"" + attrUsername + "\">" +
                "<span class=\"anonymous-reply-to-text\"><span class=\"anonymous-reply-to-display\">" + display + "</span><span class=\"anonymous-reply-to-username\">@" + username + "</span></span>" +
                "</a>";
        }

        private static void RefreshAnonymousReplyToTabs(ref string text, List<UserAliasProfile> profiles)
        {
            Dictionary<int, AnonymousIdentity> identities = new Dictionary<int, AnonymousIdentity>();
            for (int i = 0; i < profiles.Count; i++)
            {
                if (profiles[i].Identity != null && !identities.ContainsKey(profiles[i].Identity.Index))
                {
                    identities.Add(profiles[i].Identity.Index, profiles[i].Identity);
                }
            }

            text = Regex.Replace(
                text,
                @"<a\b(?<attrs>[^>]*\bclass=(?:""[^""]*\breply-to-tab\b[^""]*""|'[^']*\breply-to-tab\b[^']*'|[^\s>]*\breply-to-tab\b[^\s>]*)[^>]*)>[\s\S]*?</a>",
                delegate (Match match)
                {
                    string block = match.Value;
                    Match indexMatch = Regex.Match(block, @"\bdata-anon-index=(?:""(?<index>\d+)""|'(?<index>\d+)'|(?<index>\d+))", RegexOptions.IgnoreCase);
                    if (indexMatch.Success && int.TryParse(indexMatch.Groups["index"].Value, out int index) && identities.TryGetValue(index, out AnonymousIdentity identity))
                    {
                        return BuildAnonymousReplyToTab(identity);
                    }

                    AnonymousIdentity inferred = FindIdentityFromText(block, profiles) ?? BuildFallbackIdentity();
                    return BuildAnonymousReplyToTab(inferred);
                },
                RegexOptions.IgnoreCase | RegexOptions.Singleline,
                TimeSpan.FromSeconds(2));
        }

        private static void RefreshPostHeaderIdentities(ref string text, List<UserAliasProfile> profiles)
        {
            Dictionary<int, AnonymousIdentity> identities = BuildIdentityIndexMap(profiles);

            text = Regex.Replace(
                text,
                @"<div\b(?=[^>]*\bclass=(?:""[^""]*\btopic-post\b[^""]*""|'[^']*\btopic-post\b[^']*'|[^\s>]*\btopic-post\b[^\s>]*))[\s\S]*?(?=<div\b(?=[^>]*\bclass=(?:""[^""]*\btopic-post\b[^""]*""|'[^']*\btopic-post\b[^']*'|[^\s>]*\btopic-post\b[^\s>]*))|<div\b[^>]*class=(?:""[^""]*\btopic-navigation\b[^""]*""|'[^']*\btopic-navigation\b[^']*'|[^\s>]*\btopic-navigation\b[^\s>]*)|\z)",
                delegate (Match match)
                {
                    string post = match.Value;
                    AnonymousIdentity identity = FindIdentityFromAnonymousIndex(post, identities);
                    if (identity == null)
                    {
                        return post;
                    }

                    int bodyIndex = IndexOfRegexStatic(post, @"<div\b[^>]*\bclass=(?:""[^""]*\bpost__body\b[^""]*""|'[^']*\bpost__body\b[^']*'|[^\s>]*\bpost__body\b[^\s>]*)", RegexOptions.IgnoreCase);
                    string header = bodyIndex >= 0 ? post.Substring(0, bodyIndex) : post;
                    string body = bodyIndex >= 0 ? post.Substring(bodyIndex) : string.Empty;

                    header = ReplaceFirstRegex(
                        header,
                        @"<div\b(?<attrs>[^>]*\bclass=(?:""[^""]*\bpost-avatar\b[^""]*""|'[^']*\bpost-avatar\b[^']*'|[^\s>]*\bpost-avatar\b[^\s>]*)[^>]*)>",
                        delegate (Match avatarMatch)
                        {
                            string attrs = EnsureAnonymousAvatarAttributes(avatarMatch.Groups["attrs"].Value, identity);
                            return "<div" + attrs + ">";
                        },
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    header = ReplaceFirstRegex(
                        header,
                        @"<img\b(?<attrs>[^>]*\bclass=(?:""[^""]*\bavatar\b[^""]*""|'[^']*\bavatar\b[^']*'|[^\s>]*\bavatar\b[^\s>]*)[^>]*)>",
                        delegate (Match imageMatch)
                        {
                            return BuildAnonymousAvatarImageTag(imageMatch.Groups["attrs"].Value, identity, "48");
                        },
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    header = ReplaceFirstRegex(
                        header,
                        @"<div\b[^>]*\bclass=(?:""[^""]*\bnames\b[^""]*""|'[^']*\bnames\b[^']*'|[^\s>]*\bnames\b[^\s>]*)[^>]*>[\s\S]*?</div>",
                        BuildAnonymousNameBlock(identity),
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    return header + body;
                },
                RegexOptions.IgnoreCase | RegexOptions.Singleline,
                TimeSpan.FromSeconds(3));
        }

        private static Dictionary<int, AnonymousIdentity> BuildIdentityIndexMap(List<UserAliasProfile> profiles)
        {
            Dictionary<int, AnonymousIdentity> identities = new Dictionary<int, AnonymousIdentity>();
            for (int i = 0; i < profiles.Count; i++)
            {
                if (profiles[i].Identity != null && !identities.ContainsKey(profiles[i].Identity.Index))
                {
                    identities.Add(profiles[i].Identity.Index, profiles[i].Identity);
                }
            }

            return identities;
        }

        private static AnonymousIdentity FindIdentityFromAnonymousIndex(string text, Dictionary<int, AnonymousIdentity> identities)
        {
            string[] patterns = new string[]
            {
                @"<div\b(?=[^>]*\banonymous-names\b)[^>]*\bdata-anon-index=(?:""(?<index>\d+)""|'(?<index>\d+)'|(?<index>\d+))",
                @"<div\b(?=[^>]*\bpost-avatar\b)[^>]*\bdata-anon-index=(?:""(?<index>\d+)""|'(?<index>\d+)'|(?<index>\d+))",
                @"\bdata-anon-index=(?:""(?<index>\d+)""|'(?<index>\d+)'|(?<index>\d+))",
            };

            for (int i = 0; i < patterns.Length; i++)
            {
                Match match = Regex.Match(text, patterns[i], RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (match.Success && int.TryParse(match.Groups["index"].Value, out int index) && identities.TryGetValue(index, out AnonymousIdentity identity))
                {
                    return identity;
                }
            }

            return null;
        }

        private static string BuildAnonymousAvatarImageTag(string attrs, AnonymousIdentity identity, string size)
        {
            string updated = attrs;
            updated = Regex.Replace(updated, @"\bsrc=([""']).*?\1", "src=\"" + identity.AvatarDataUri + "\"", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            updated = Regex.Replace(updated, @"\bsrc=(?![""'])[^\s>]+", "src=\"" + identity.AvatarDataUri + "\"", RegexOptions.IgnoreCase);
            if (!Regex.IsMatch(updated, @"\bsrc\s*=", RegexOptions.IgnoreCase))
            {
                updated += " src=\"" + identity.AvatarDataUri + "\"";
            }

            updated = Regex.Replace(updated, @"\bsrcset=([""']).*?\1", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            updated = Regex.Replace(updated, @"\bsrcset=(?![""'])[^\s>]+", string.Empty, RegexOptions.IgnoreCase);
            updated = SetOrAppendAttribute(updated, "width", size);
            updated = SetOrAppendAttribute(updated, "height", size);
            updated = SetOrAppendAttribute(updated, "alt", identity.DisplayName);
            updated = SetOrAppendAttribute(updated, "title", identity.DisplayName);
            updated = SetOrAppendAttribute(updated, "data-anon-user", identity.DisplayName);
            updated = SetOrAppendAttribute(updated, "data-anon-index", identity.Index.ToString());
            updated = SetOrAppendAttribute(updated, "data-anon-display", identity.DisplayName);
            updated = SetOrAppendAttribute(updated, "data-anon-username", identity.Username);
            updated = Regex.Replace(
                updated,
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
            return "<img" + updated + ">";
        }

        private static string BuildAnonymousNameBlock(AnonymousIdentity identity)
        {
            string displayName = HtmlText(identity.DisplayName);
            string username = HtmlText(identity.Username);
            string attrDisplay = HtmlAttribute(identity.DisplayName);
            string attrUsername = HtmlAttribute(identity.Username);
            return "<div class=\"names anonymous-names\" data-anon-index=\"" + identity.Index.ToString() + "\" data-anon-display=\"" + attrDisplay + "\" data-anon-username=\"" + attrUsername + "\">" +
                "<span class=\"first full-name\"><a href=\"#\" aria-label=\"" + attrDisplay + "\" tabindex=\"0\">" + displayName + "</a></span>" +
                "<span class=\"second username\"><a href=\"#\" aria-label=\"" + attrUsername + "\" tabindex=\"0\">@" + username + "</a></span>" +
                "</div>";
        }

        private static string EnsureAnonymousAvatarAttributes(string attrs, AnonymousIdentity identity)
        {
            string updated = attrs;

            updated = SetOrAppendAttribute(updated, "data-anon-user", identity.DisplayName);
            updated = SetOrAppendAttribute(updated, "data-anon-index", identity.Index.ToString());
            updated = SetOrAppendAttribute(updated, "data-anon-display", identity.DisplayName);
            updated = SetOrAppendAttribute(updated, "data-anon-username", identity.Username);

            updated = Regex.Replace(
                updated,
                @"\bclass=(?<quote>[""'])(?<value>.*?)\k<quote>",
                delegate (Match match)
                {
                    string quote = match.Groups["quote"].Value;
                    string classValue = match.Groups["value"].Value;
                    return "class=" + quote + classValue + quote;
                },
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            updated = Regex.Replace(
                updated,
                @"\bclass=(?![""'])(?<value>[^\s>]+)",
                delegate (Match match)
                {
                    string classValue = match.Groups["value"].Value;
                    return "class=\"" + classValue + "\"";
                },
                RegexOptions.IgnoreCase);

            return updated;
        }

        private static void RestoreAnonymousIdentityMarkers(ref string text, List<UserAliasProfile> profiles)
        {
            Dictionary<int, AnonymousIdentity> identities = new Dictionary<int, AnonymousIdentity>();
            for (int i = 0; i < profiles.Count; i++)
            {
                if (profiles[i].Identity != null && !identities.ContainsKey(profiles[i].Identity.Index))
                {
                    identities.Add(profiles[i].Identity.Index, profiles[i].Identity);
                }
            }

            foreach (KeyValuePair<int, AnonymousIdentity> pair in identities)
            {
                string index = pair.Key.ToString();
                AnonymousIdentity identity = pair.Value;
                string indexPattern = @"\bdata-anon-index\s*=\s*(?:""" + Regex.Escape(index) + @"""|'" + Regex.Escape(index) + @"'|" + Regex.Escape(index) + @")";

                text = Regex.Replace(
                    text,
                    @"<div\b(?=[^>]*\banonymous-names\b)(?=[^>]*" + indexPattern + @")[^>]*>[\s\S]*?</div>",
                    BuildAnonymousNameBlock(identity),
                    RegexOptions.IgnoreCase | RegexOptions.Singleline,
                    TimeSpan.FromSeconds(2));

                text = Regex.Replace(
                    text,
                    @"<(?<tag>[A-Za-z][\w:-]*)\b(?<attrs>[^>]*" + indexPattern + @"[^>]*)>",
                    delegate (Match match)
                    {
                        string tag = match.Groups["tag"].Value;
                        string attrs = match.Groups["attrs"].Value;
                        attrs = SetOrAppendAttribute(attrs, "data-anon-user", identity.DisplayName);
                        attrs = SetOrAppendAttribute(attrs, "data-anon-display", identity.DisplayName);
                        attrs = SetOrAppendAttribute(attrs, "data-anon-username", identity.Username);
                        if (string.Equals(tag, "img", StringComparison.OrdinalIgnoreCase))
                        {
                            attrs = SetOrAppendAttribute(attrs, "alt", identity.DisplayName);
                            attrs = SetOrAppendAttribute(attrs, "title", identity.DisplayName);
                            attrs = Regex.Replace(attrs, @"\bsrc=([""']).*?\1", "src=\"" + identity.AvatarDataUri + "\"", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                        }

                        return "<" + tag + attrs + ">";
                    },
                    RegexOptions.IgnoreCase | RegexOptions.Singleline,
                    TimeSpan.FromSeconds(2));
            }

            RemoveAnonymousAvatarNameBlocks(ref text);
        }

        private static UserAliasProfile FindProfileForPost(string post, List<UserAliasProfile> profiles)
        {
            int cookedIndex = IndexOfRegexStatic(post, @"<div\b[^>]*\bclass=(?:""[^""]*\bcooked\b[^""]*""|'[^']*\bcooked\b[^']*'|[^\s>]*\bcooked\b[^\s>]*)", RegexOptions.IgnoreCase);
            string header = cookedIndex > 0 ? post.Substring(0, cookedIndex) : post;
            UserAliasProfile best = null;
            int bestLength = 0;
            for (int i = 0; i < profiles.Count; i++)
            {
                foreach (string alias in profiles[i].Aliases)
                {
                    if (alias.Length > bestLength && ContainsAliasText(header, alias))
                    {
                        best = profiles[i];
                        bestLength = alias.Length;
                    }
                }
            }

            return best;
        }

        private static AnonymousIdentity FindIdentityFromText(string text, List<UserAliasProfile> profiles)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            Match idMatch = Regex.Match(text, @"\bdata-anon-index=([""']?)(?<index>\d+)\1", RegexOptions.IgnoreCase);
            if (idMatch.Success && int.TryParse(idMatch.Groups["index"].Value, out int id))
            {
                for (int i = 0; i < profiles.Count; i++)
                {
                    if (profiles[i].Identity != null && profiles[i].Identity.Index == id)
                    {
                        return profiles[i].Identity;
                    }
                }
            }

            UserAliasProfile best = null;
            int bestLength = 0;
            for (int i = 0; i < profiles.Count; i++)
            {
                AnonymousIdentity identity = profiles[i].Identity;
                if (identity == null)
                {
                    continue;
                }

                if (ContainsAliasText(text, identity.DisplayName) && identity.DisplayName.Length > bestLength)
                {
                    best = profiles[i];
                    bestLength = identity.DisplayName.Length;
                }
                if (ContainsAliasText(text, identity.Username) && identity.Username.Length > bestLength)
                {
                    best = profiles[i];
                    bestLength = identity.Username.Length;
                }
                foreach (string alias in profiles[i].Aliases)
                {
                    if (alias.Length > bestLength && ContainsAliasText(text, alias))
                    {
                        best = profiles[i];
                        bestLength = alias.Length;
                    }
                }
            }

            return best == null ? null : best.Identity;
        }

        private static AnonymousIdentity BuildFallbackIdentity()
        {
            AnonymousIdentity identity = GenerateAnonymousIdentity(0);
            identity.DisplayName = "匿名访客";
            identity.Username = "anonymous_guest";
            identity.AvatarDataUri = BuildAnonymousAvatarDataUri(identity);
            return identity;
        }

        private static string BuildAnonymousAvatarDataUri(AnonymousIdentity identity)
        {
            int index = Math.Max(identity.Index, 1);
            string bg = AnonymousAvatarPalette[(index - 1) % AnonymousAvatarPalette.Length];
            string bg2 = AnonymousAvatarPalette[(index * 7) % AnonymousAvatarPalette.Length];
            string skin = AnonymousSkinPalette[(index * 3) % AnonymousSkinPalette.Length];
            string hair = AnonymousHairPalette[(index * 5) % AnonymousHairPalette.Length];
            int hairVariant = index % 4;
            int mouthVariant = (index / 3) % 3;
            int accessory = (index / 11) % 6;
            int pattern = (index / 5) % 5;
            int outfit = (index / 7) % AnonymousAvatarPalette.Length;
            string patternSvg = pattern == 0
                ? "<circle cx=\"18\" cy=\"18\" r=\"8\" fill=\"rgba(255,255,255,.18)\"/><circle cx=\"78\" cy=\"26\" r=\"5\" fill=\"rgba(255,255,255,.16)\"/>"
                : pattern == 1
                    ? "<path d=\"M0 76h96v20H0z\" fill=\"rgba(255,255,255,.16)\"/><path d=\"M0 0l96 96\" stroke=\"rgba(255,255,255,.14)\" stroke-width=\"8\"/>"
                    : pattern == 2
                        ? "<path d=\"M12 16h72\" stroke=\"rgba(255,255,255,.16)\" stroke-width=\"5\"/><path d=\"M8 78h80\" stroke=\"rgba(255,255,255,.12)\" stroke-width=\"5\"/>"
                        : pattern == 3
                            ? "<circle cx=\"48\" cy=\"48\" r=\"40\" fill=\"none\" stroke=\"rgba(255,255,255,.14)\" stroke-width=\"8\"/>"
                            : "<path d=\"M0 28c20 12 40 12 60 0s28-12 36-4v72H0z\" fill=\"rgba(255,255,255,.14)\"/>";
            string hairPath = hairVariant == 0
                ? "<path d=\"M25 42c2-18 17-29 36-25 11 2 19 11 21 24-16-8-35-9-57 1z\" fill=\"" + hair + "\"/>"
                : hairVariant == 1
                    ? "<path d=\"M20 44c0-21 19-35 42-29 12 3 18 12 18 27-12-11-34-13-60 2z\" fill=\"" + hair + "\"/>"
                    : hairVariant == 2
                        ? "<path d=\"M23 43c5-24 39-34 57-11 2 4 3 8 4 13-20-12-40-11-61-2z\" fill=\"" + hair + "\"/>"
                        : "<path d=\"M24 41c8-20 33-29 51-15 5 4 8 10 8 18-18-9-38-11-59-3z\" fill=\"" + hair + "\"/>";
            string mouth = mouthVariant == 0
                ? "<path d=\"M39 64c6 6 16 6 22 0\" stroke=\"#703a35\" stroke-width=\"3\" fill=\"none\" stroke-linecap=\"round\"/>"
                : mouthVariant == 1
                    ? "<path d=\"M40 66h20\" stroke=\"#703a35\" stroke-width=\"3\" fill=\"none\" stroke-linecap=\"round\"/>"
                    : "<path d=\"M42 64c4 4 12 4 16 0\" stroke=\"#703a35\" stroke-width=\"3\" fill=\"none\" stroke-linecap=\"round\"/>";
            string glasses = accessory == 0
                ? "<circle cx=\"40\" cy=\"51\" r=\"7\" fill=\"none\" stroke=\"#2f3437\" stroke-width=\"2\"/><circle cx=\"60\" cy=\"51\" r=\"7\" fill=\"none\" stroke=\"#2f3437\" stroke-width=\"2\"/><path d=\"M47 51h6\" stroke=\"#2f3437\" stroke-width=\"2\"/>"
                : accessory == 1
                    ? "<path d=\"M25 37c8-21 40-28 54-4l-5-15H30z\" fill=\"rgba(255,255,255,.78)\"/><path d=\"M30 20h43\" stroke=\"#2f3437\" stroke-width=\"4\" stroke-linecap=\"round\"/>"
                    : accessory == 2
                        ? "<path d=\"M35 70c6 10 20 10 26 0\" stroke=\"" + hair + "\" stroke-width=\"5\" fill=\"none\" stroke-linecap=\"round\"/>"
                        : accessory == 3
                            ? "<circle cx=\"24\" cy=\"57\" r=\"3\" fill=\"#f4d35e\"/><circle cx=\"72\" cy=\"57\" r=\"3\" fill=\"#f4d35e\"/>"
                            : accessory == 4
                                ? "<path d=\"M31 48c5-4 11-4 16 0M53 48c5-4 11-4 16 0\" stroke=\"#2f3437\" stroke-width=\"2.5\" fill=\"none\" stroke-linecap=\"round\"/>"
                : string.Empty;
            string svg =
                "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"96\" height=\"96\" viewBox=\"0 0 96 96\">" +
                "<defs><linearGradient id=\"g\" x1=\"0\" y1=\"0\" x2=\"1\" y2=\"1\"><stop offset=\"0\" stop-color=\"" + bg + "\"/><stop offset=\"1\" stop-color=\"" + bg2 + "\"/></linearGradient></defs>" +
                "<rect width=\"96\" height=\"96\" rx=\"48\" fill=\"url(#g)\"/>" +
                patternSvg +
                "<path d=\"M18 92c4-20 18-31 30-31s26 11 30 31z\" fill=\"" + AnonymousAvatarPalette[outfit] + "\"/>" +
                "<circle cx=\"48\" cy=\"54\" r=\"28\" fill=\"" + skin + "\"/>" +
                hairPath +
                "<circle cx=\"39\" cy=\"52\" r=\"3.5\" fill=\"#2f3437\"/><circle cx=\"59\" cy=\"52\" r=\"3.5\" fill=\"#2f3437\"/>" +
                glasses +
                "<path d=\"M49 55c-2 4-2 7 2 8\" stroke=\"#a86f58\" stroke-width=\"2\" fill=\"none\" stroke-linecap=\"round\"/>" +
                mouth +
                "<circle cx=\"28\" cy=\"62\" r=\"5\" fill=\"#f2b6a6\" opacity=\".45\"/><circle cx=\"68\" cy=\"62\" r=\"5\" fill=\"#f2b6a6\" opacity=\".45\"/>" +
                "<text x=\"48\" y=\"90\" text-anchor=\"middle\" font-family=\"Arial,Microsoft YaHei,sans-serif\" font-size=\"10\" font-weight=\"700\" fill=\"rgba(255,255,255,.9)\">" + identity.Index.ToString("00000") + "</text>" +
                "</svg>";
            return "data:image/svg+xml;charset=utf-8," + Uri.EscapeDataString(svg);
        }

        private static AnonymousIdentity GenerateAnonymousIdentity(int index)
        {
            int safeIndex = Math.Max(index, 1);
            AnonymousIdentity packed = AnonymousIdentityPack.TryGetIdentity(safeIndex);
            if (packed != null && !string.IsNullOrEmpty(packed.DisplayName) && !string.IsNullOrEmpty(packed.Username) && !string.IsNullOrEmpty(packed.AvatarDataUri))
            {
                return packed;
            }

            int zero = safeIndex - 1;
            string surname = FakeSurnames[zero % FakeSurnames.Length];
            string givenA = FakeGivenA[(zero * 17 + zero / FakeSurnames.Length) % FakeGivenA.Length];
            string givenB = FakeGivenB[(zero * 31 + zero / (FakeSurnames.Length * 3)) % FakeGivenB.Length];
            string username = FakeUsernameLeft[(zero * 7 + zero / 11) % FakeUsernameLeft.Length] + "_" +
                FakeUsernameRight[(zero * 13 + zero / 17) % FakeUsernameRight.Length] + "_" +
                safeIndex.ToString("00000");
            AnonymousIdentity identity = new AnonymousIdentity();
            identity.Index = safeIndex;
            identity.DisplayName = surname + givenA + givenB;
            identity.Username = username;
            identity.AvatarDataUri = BuildAnonymousAvatarDataUri(identity);
            return identity;
        }

        private static string ReplacementForAlias(UserAliasProfile profile, string alias)
        {
            if (profile == null || profile.Identity == null)
            {
                return "匿名用户";
            }

            if (profile.UsernameAliases.Contains(alias) || IsAsciiAlias(alias))
            {
                return profile.Identity.Username;
            }

            return profile.Identity.DisplayName;
        }

        private static string SetOrAppendAttribute(string attrs, string name, string value)
        {
            string escapedValue = HtmlAttribute(value);
            if (Regex.IsMatch(attrs, @"\b" + Regex.Escape(name) + @"\s*=", RegexOptions.IgnoreCase))
            {
                attrs = Regex.Replace(attrs, @"\b" + Regex.Escape(name) + @"=([""']).*?\1", name + "=\"" + escapedValue + "\"", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                attrs = Regex.Replace(attrs, @"\b" + Regex.Escape(name) + @"=(?![""'])[^\s>]+", name + "=\"" + escapedValue + "\"", RegexOptions.IgnoreCase);
                return attrs;
            }

            attrs = Regex.Replace(attrs, @"(?<=\s)" + Regex.Escape(name) + @"(?=\s|$)", string.Empty, RegexOptions.IgnoreCase);
            attrs = Regex.Replace(attrs, @"\s{2,}", " ");
            return attrs + " " + name + "=\"" + escapedValue + "\"";
        }

        private static string ReplaceFirstRegex(string input, string pattern, string replacement, RegexOptions options)
        {
            Match match = Regex.Match(input, pattern, options);
            if (!match.Success)
            {
                return input;
            }

            return input.Substring(0, match.Index) + replacement + input.Substring(match.Index + match.Length);
        }

        private static string ReplaceFirstRegex(string input, string pattern, MatchEvaluator evaluator, RegexOptions options)
        {
            Match match = Regex.Match(input, pattern, options);
            if (!match.Success)
            {
                return input;
            }

            return input.Substring(0, match.Index) + evaluator(match) + input.Substring(match.Index + match.Length);
        }

        private static bool ContainsAliasText(string text, string alias)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(alias))
            {
                return false;
            }

            if (text.IndexOf(alias, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            string encoded = Uri.EscapeDataString(alias);
            return !string.IsNullOrEmpty(encoded) && text.IndexOf(encoded, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string HtmlText(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private static string HtmlAttribute(string value)
        {
            return HtmlText(value).Replace("\"", "&quot;");
        }

        private static int IndexOfRegexStatic(string text, string pattern, RegexOptions options)
        {
            Match match = Regex.Match(text, pattern, options);
            return match.Success ? match.Index : -1;
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
                "html,body{max-width:100%!important;overflow-x:hidden!important;}" +
                ".anonymous-site-brand{display:inline-flex;align-items:center;min-height:36px;font-size:20px;font-weight:700;color:#2f4858;}" +
                "img.avatar{background:#eef2f6!important;border-radius:50%!important;border:1px solid #d7dfe8!important;object-fit:cover!important;}" +
                ".sidebar-wrapper,.sidebar-sections,.sidebar-section{display:none!important;}" +
                ".topic-navigation,.timeline-container,.topic-timeline,.d-toc-wrapper{display:none!important;}" +
                ".wrap,.container.posts,.topic-area,.posts-wrapper,.post-stream{display:block!important;float:none!important;position:static!important;clear:both!important;box-sizing:border-box!important;}" +
                "#topic-title.container,.topic-area,.post-stream{width:min(100%,1100px)!important;max-width:1100px!important;margin-left:auto!important;margin-right:auto!important;}" +
                "#topic-title{clear:both!important;display:block!important;position:relative!important;margin-top:24px!important;margin-bottom:18px!important;padding:16px 18px!important;box-sizing:border-box!important;}" +
                ".post-stream{padding:0 18px 24px!important;overflow:visible!important;}" +
                ".topic-post{display:block!important;position:static!important;float:none!important;clear:both!important;overflow:visible!important;width:100%!important;max-width:100%!important;margin:0 0 18px 0!important;box-sizing:border-box!important;}" +
                ".topic-post>article,.topic-post article.boxed,.topic-post article.onscreen-post{display:block!important;position:static!important;float:none!important;clear:both!important;width:100%!important;max-width:100%!important;margin:0!important;box-sizing:border-box!important;}" +
                ".topic-post .post__row{display:grid!important;grid-template-columns:56px minmax(0,1fr)!important;column-gap:16px!important;align-items:start!important;float:none!important;position:static!important;width:min(100%,1100px)!important;max-width:1100px!important;margin-left:auto!important;margin-right:auto!important;box-sizing:border-box!important;}" +
                ".topic-post .topic-avatar{grid-column:1!important;float:none!important;position:static!important;top:auto!important;left:auto!important;width:56px!important;margin:0!important;padding:0!important;z-index:auto!important;}" +
                ".topic-post .topic-body,.topic-post .post__body{grid-column:2!important;float:none!important;position:static!important;display:block!important;width:auto!important;max-width:100%!important;margin:0!important;padding:0!important;clear:none!important;box-sizing:border-box!important;}" +
                ".topic-post .topic-avatar,.anonymous-static-avatar .topic-avatar{position:static!important;top:auto!important;left:auto!important;}" +
                ".topic-post .topic-meta-data{display:flex!important;align-items:flex-start!important;gap:10px!important;float:none!important;position:static!important;width:100%!important;margin:0 0 8px 0!important;min-height:24px!important;}" +
                ".topic-post .post-infos{display:flex!important;align-items:center!important;gap:8px!important;margin-left:auto!important;float:none!important;position:static!important;white-space:nowrap!important;}" +
                ".topic-post .post__regular,.topic-post .post__contents,.topic-post .contents,.topic-post .regular,.topic-post .cooked{display:block!important;float:none!important;position:static!important;clear:none!important;width:auto!important;max-width:100%!important;margin-left:0!important;box-sizing:border-box!important;}" +
                ".topic-meta-data .anonymous-names{display:flex!important;align-items:baseline!important;gap:.45em!important;flex-wrap:wrap!important;margin-bottom:2px!important;min-width:0!important;line-height:1.25!important;}" +
                ".topic-meta-data .anonymous-names .first.full-name,.topic-meta-data .anonymous-names .second.username{display:inline-flex!important;align-items:baseline!important;margin:0!important;line-height:1.25!important;}" +
                ".names .second.username{display:inline-flex!important;margin-left:0!important;color:#6b7680!important;font-size:.92em!important;}" +
                ".names .first.full-name a{color:inherit!important;pointer-events:none!important;text-decoration:none!important;}" +
                ".names .second.username a{color:#6b7680!important;pointer-events:none!important;text-decoration:none!important;}" +
                ".anonymous-reply-to{display:inline-flex!important;align-items:center!important;gap:.28em!important;max-width:100%!important;vertical-align:middle!important;}" +
                ".anonymous-reply-to .avatar{width:24px!important;height:24px!important;flex:0 0 24px!important;}" +
                ".anonymous-reply-to-text{display:inline-flex!important;align-items:baseline!important;gap:.35em!important;min-width:0!important;max-width:24em!important;white-space:nowrap!important;overflow:hidden!important;text-overflow:ellipsis!important;}" +
                ".anonymous-reply-to-display{font-weight:600!important;color:var(--primary)!important;overflow:hidden!important;text-overflow:ellipsis!important;}" +
                ".anonymous-reply-to-username{color:#6b7680!important;font-size:.92em!important;overflow:hidden!important;text-overflow:ellipsis!important;}" +
                "a.trigger-user-card,a.poster-avatar,a.main-avatar,a.mention,a.lightbox,a.back{pointer-events:none!important;}" +
                "a.trigger-user-card[href],a.poster-avatar[href],a.main-avatar[href],a.mention[href],a.lightbox[href],a.back[href]{cursor:default!important;}" +
                ".topic-body,.contents,.cooked,.regular{max-width:100%!important;overflow-wrap:anywhere!important;}" +
                ".cooked img:not(.avatar),.cooked video,.cooked canvas,.cooked iframe,.onebox,.lightbox-wrapper,.aspect-image{max-width:100%!important;height:auto!important;}" +
                ".topic-footer-main-buttons,.topic-footer-buttons{display:flex!important;flex-wrap:wrap!important;gap:8px!important;align-items:center!important;max-width:100%!important;}" +
                ".topic-map,.post__topic-map{display:block!important;position:static!important;float:none!important;clear:both!important;width:min(100%,1100px)!important;max-width:1100px!important;margin:18px auto!important;box-sizing:border-box!important;overflow-x:auto!important;overflow-y:visible!important;}" +
                ".topic-map__contents{display:flex!important;align-items:center!important;gap:18px!important;flex-wrap:nowrap!important;width:max-content!important;min-width:100%!important;box-sizing:border-box!important;}" +
                ".topic-map__stats{display:inline-flex!important;align-items:center!important;gap:22px!important;flex-wrap:nowrap!important;width:auto!important;flex:0 0 auto!important;}" +
                ".topic-map__stats button,.topic-map__stats .btn{display:inline-flex!important;flex-direction:column!important;align-items:center!important;justify-content:center!important;min-width:44px!important;gap:2px!important;background:transparent!important;border:0!important;padding:4px 2px!important;box-shadow:none!important;pointer-events:none!important;}" +
                ".topic-map__stat-label{display:block!important;font-size:13px!important;line-height:1.2!important;color:#667780!important;white-space:nowrap!important;}" +
                ".topic-map__users-list,.anonymous-topic-map-users{display:inline-flex!important;align-items:center!important;gap:7px!important;flex-wrap:nowrap!important;vertical-align:middle!important;min-width:max-content!important;max-width:none!important;overflow:visible!important;flex:0 0 auto!important;}" +
                ".topic-map__users-list>div{display:inline-flex!important;margin:0!important;padding:0!important;}" +
                ".topic-map__users-list .poster,.anonymous-topic-map-users .poster{display:inline-flex!important;width:40px!important;height:40px!important;flex:0 0 40px!important;margin:0!important;}" +
                ".topic-map__users-list img.avatar,.anonymous-topic-map-users img.avatar{width:40px!important;height:40px!important;}" +
                ".suggested-topics,.more-topics,.more-topics__container{clear:both!important;margin-left:0!important;max-width:min(100%,1100px)!important;overflow-x:auto!important;}" +
                ".suggested-topics .topic-list,.more-topics .topic-list{width:100%!important;table-layout:auto!important;}" +
                ".suggested-topics .topic-list td,.suggested-topics .topic-list th,.more-topics .topic-list td,.more-topics .topic-list th{white-space:normal!important;overflow-wrap:anywhere!important;}" +
                "@media(max-width:640px){.topic-post .post__row{grid-template-columns:44px minmax(0,1fr)!important;column-gap:10px!important}.topic-post .topic-avatar{width:44px!important}.anonymous-reply-to-text{max-width:14em!important}}" +
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

            RegisterAliasPairs(text, profiles, aliasMap, @"\\&quot;name\\&quot;:\\&quot;(?<left>.*?)\\&quot;,\\&quot;username\\&quot;:\\&quot;(?<right>.*?)\\&quot;", false, true);
            RegisterAliasPairs(text, profiles, aliasMap, @"\\&quot;username\\&quot;:\\&quot;(?<left>.*?)\\&quot;[\s\S]{0,320}?\\&quot;display_username\\&quot;:\\&quot;(?<right>.*?)\\&quot;", true, false);
            RegisterAliasPairs(text, profiles, aliasMap, @"""name""\s*:\s*""(?<left>(?:\\""|[^""])*)""\s*,\s*""username""\s*:\s*""(?<right>(?:\\""|[^""])*)""", false, true);
            RegisterAliasPairs(text, profiles, aliasMap, @"""username""\s*:\s*""(?<left>(?:\\""|[^""])*)""[\s\S]{0,320}?""display_username""\s*:\s*""(?<right>(?:\\""|[^""])*)""", true, false);
            RegisterAliasPairs(text, profiles, aliasMap, @"class=(?:""first full-name""|'first full-name'|first\s+full-name)[\s\S]{0,220}?>(?<left>[^<]{2,80})</a>[\s\S]{0,220}?class=(?:""second username""|'second username'|second\s+username)[\s\S]{0,220}?>(?<right>[^<]{2,80})</a>", false, true);
            RegisterAliasPairs(text, profiles, aliasMap, @"class=(?:""second username""|'second username'|second\s+username)[\s\S]{0,220}?>(?<left>[^<]{2,80})</a>[\s\S]{0,220}?class=(?:""first full-name""|'first full-name'|first\s+full-name)[\s\S]{0,220}?>(?<right>[^<]{2,80})</a>", true, false);
            RegisterAliasPairs(text, profiles, aliasMap, @"class=(?:""quote no-group""|'quote no-group'|quote\s+no-group)[\s\S]{0,320}?data-username=(?:""(?<right>[^""]+)""|'(?<right>[^']+)'|(?<right>[^\s>]+))[\s\S]{0,220}?class=(?:""avatar""|'avatar'|avatar)[^>]*>\s*(?<left>[^<]{2,80})", false, true);
            RegisterAliasPairs(text, profiles, aliasMap, @"href=(?:https?://[^""'\s>]+)?/u/(?<right>[^""'\s>/]+)[^>]*>\s*(?<left>[^<]{2,80})\s*</a>", false, true);

            RegisterAliasSingles(text, profiles, aliasMap, @"/user_avatar/(?:anonymous-site|shuiyuan(?:\.s3\.jcloud)?\.sjtu\.edu\.cn)/(?<value>[^/]+)/");
            RegisterAliasSingles(text, profiles, aliasMap, @"data-user-card=(?:""(?<value>[^""]+)""|'(?<value>[^']+)'|(?<value>[^\s>]+))");
            RegisterAliasSingles(text, profiles, aliasMap, @"data-username=(?:""(?<value>[^""]+)""|'(?<value>[^']+)'|(?<value>[^\s>]+))");
            RegisterAliasSingles(text, profiles, aliasMap, @"href=(?:https?://[^""'\s>]+)?/u/(?<value>[^""'\s>/]+)");

            return profiles;
        }

        private void RegisterAliasPairs(string text, List<UserAliasProfile> profiles, Dictionary<string, UserAliasProfile> aliasMap, string pattern, bool leftIsUsername, bool rightIsUsername)
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
                MarkAliasKind(profile, left, leftIsUsername);
                MarkAliasKind(profile, right, rightIsUsername);
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
                MarkAliasKind(profile, value, true);
                UpdateFirstIndex(text, profile, value);
            }
        }

        private static void MarkAliasKind(UserAliasProfile profile, string alias, bool isUsername)
        {
            if (profile == null || string.IsNullOrWhiteSpace(alias))
            {
                return;
            }

            if (isUsername || IsAsciiAlias(alias))
            {
                profile.UsernameAliases.Add(alias);
            }
            else
            {
                profile.DisplayAliases.Add(alias);
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

        private static void AssignUserIdentities(List<UserAliasProfile> profiles)
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
                AnonymousIdentity identity = GenerateAnonymousIdentity(i + 1);
                profiles[i].Identity = identity;
                profiles[i].Pseudonym = identity.DisplayName;
            }
        }

        private void ReplaceUserAliases(ref string text, List<UserAliasProfile> profiles)
        {
            Dictionary<string, string> replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < profiles.Count; i++)
            {
                foreach (string alias in profiles[i].Aliases)
                {
                    string replacement = ReplacementForAlias(profiles[i], alias);
                    if (alias.Length < 2 || alias == replacement)
                    {
                        continue;
                    }

                    if (!replacements.ContainsKey(alias))
                    {
                        replacements.Add(alias, replacement);
                    }

                    string encodedAlias = Uri.EscapeDataString(alias);
                    string encodedPseudonym = Uri.EscapeDataString(replacement);
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

            List<string> protectedDataUrls = ProtectDataUrls(ref text);
            string pattern = string.Join("|", keys.ConvertAll(Regex.Escape).ToArray());
            text = Regex.Replace(
                text,
                pattern,
                delegate (Match match) { return replacements[match.Value]; },
                RegexOptions.IgnoreCase);
            RestoreDataUrls(ref text, protectedDataUrls);
        }

        private static List<string> ProtectDataUrls(ref string text)
        {
            List<string> values = new List<string>();
            text = Regex.Replace(
                text,
                @"data:[^""'\s<>]+",
                delegate (Match match)
                {
                    int index = values.Count;
                    values.Add(match.Value);
                    return "__SHUIYUAN_PROTECTED_DATA_URI_" + index.ToString() + "__";
                },
                RegexOptions.IgnoreCase);
            return values;
        }

        private static void RestoreDataUrls(ref string text, List<string> values)
        {
            for (int i = 0; i < values.Count; i++)
            {
                text = text.Replace("__SHUIYUAN_PROTECTED_DATA_URI_" + i.ToString() + "__", values[i]);
            }
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
