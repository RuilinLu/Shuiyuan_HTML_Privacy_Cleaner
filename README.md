# Shuiyuan HTML Privacy Cleaner V7

一个完全离线运行的 Windows 工具，用来清理由 SingleFile 保存出来的水源社区 / Discourse 单文件 HTML 快照。它提供图形界面，也支持命令行批处理。

V7 的重点是：在原始 SingleFile/Discourse HTML 上就地处理，不把帖子抽取出来重写成另一个页面；全匿名模式会继续保留论坛阅读框架、帖子楼层、回复引用、图片、表情和话题统计区，同时去除保存者、公开用户、站点身份和隐藏预加载数据等隐私痕迹。

## 适用前提

这个工具处理的是 `.html` 文件，不是截图图片，也不是在线网页。推荐流程：

1. 在 Edge 浏览器中安装 SingleFile。
2. 打开需要归档的水源社区页面。
3. 用 SingleFile 保存当前页面，得到一个单文件 `.html` 快照。
4. 用本工具清理这个 `.html` 文件，再分发清理后的副本。

SingleFile 的安装和使用可以参考：<https://github.com/RuilinLu/SingleFile>

目前重点测试过的组合是 Edge + SingleFile + Windows x64 + `win-x64` 版本。本项目也构建了 `win-x86` 和 `win-arm64` 版本，其中 `win-x86` 已在 x64 Windows 上做过命令行清理验证，`win-arm64` 已完成发布产物构建和架构校验，但没有 ARM64 真机运行验证。

## 下载和安装

如果只是使用程序，下载对应架构的 `ShuiyuanHtmlPrivacyCleaner.exe` 即可，不需要安装 .NET，也不需要联网。

- 大多数 Windows 电脑：下载 `win-x64/ShuiyuanHtmlPrivacyCleaner.exe`
- 旧 32 位 Windows：下载 `win-x86/ShuiyuanHtmlPrivacyCleaner.exe`
- Windows on ARM 设备：下载 `win-arm64/ShuiyuanHtmlPrivacyCleaner.exe`

双击 EXE 会打开图形界面。程序是自包含发布包，所有清理规则、图标、匿名头像和匿名身份数据都内置在 EXE 里。

## 文档导航

- [安装指南](docs/INSTALL.md)
- [界面和模式使用说明](docs/USER_GUIDE.md)
- [隐私模型和限制](docs/PRIVACY_MODEL.md)
- [验证指南](docs/VALIDATION.md)
- [开发构建指南](docs/DEVELOPMENT.md)
- [发布流程](docs/RELEASE.md)
- [安全说明](SECURITY.md)
- [贡献指南](CONTRIBUTING.md)

## 界面说明

界面从上到下分为五块：

1. 顶部标识区  
   显示工具名称、版本和说明。左上角使用项目内置 Logo。

2. 文件路径区  
   `输入 HTML` 选择 SingleFile 保存出来的原始 `.html` 文件。  
   `输出 HTML` 选择清理后文件的保存位置。留空时程序会自动在原文件旁边生成带后缀的新文件名。  
   `选择...` 用来选择输入文件。  
   `另存为...` 用来选择输出文件。

3. 审核关键词区  
   每行输入一个你额外担心的关键词，例如用户名、昵称、ID、头像编号、时间戳、路径片段。程序会在清理后检查这些关键词的明文和 URL 编码形式是否仍然存在。

4. 模式、语言和报告区  
   `模式` 有两种：

   - `仅清除当前保存者 / 登录者信息`：移除水印、隐水印、SingleFile 保存时间、右上角账号入口、currentUser、我的帖子、浏览记录、未读计数、扩展残留等。其他公开帖子作者、站点名称和帖子内容会保留。
   - `全匿名模式`：在上面的基础上，继续匿名化所有公开用户，去除徽章/头衔/Flair、站点域名、站点名称、侧栏导航、隐藏预加载数据、原图 URL 和站点外链。每个真实用户会稳定映射到同一套虚构头像、昵称和用户名。

   `语言` 支持简体中文、繁体中文和英文。  
   `报告` 支持易读摘要、标准报告和专业明细。  
   `允许覆盖输出文件` 只有在你确认要覆盖已有输出时再勾选。

5. 操作按钮和报告区  
   `仅分析` 只扫描输入文件，不写出新 HTML。  
   `清理并审核` 会生成输出 HTML，并立即对输出结果做自动审核。  
   `打开输出位置` 打开输出文件所在文件夹。  
   下方大文本框会显示报告，包含清理前发现什么、实际移除/改写什么、清理后还剩什么。

运行时会显示真实百分比进度条。全匿名模式处理大文件时会更慢，但不应该空白卡死；如果进度长时间不动，可以先用命令行模式复现并查看错误文件。

## 两种模式的处理边界

### 仅清除当前保存者模式

适合你只想去掉“这个 HTML 是谁保存的”这类痕迹，同时保留帖子作为普通公开网页快照的场景。

会重点清理：

- SingleFile 保存时间注释
- 可见背景水印和低透明度隐水印层
- 右上角当前登录用户头像 / 账号菜单
- `currentUser` 个人数据
- `我的帖子`、`浏览记录` 等侧栏入口
- 已读/未读状态和 topic tracking 计数
- Thunderbit、沉浸式翻译等扩展浮层残留
- 用户手动填写的额外审核关键词
- Windows 本地路径和 `file://` 痕迹

这个模式会保留公开站点名称、公开帖子作者、公开头像、公开链接和正文中原本存在的用户提及。

### 全匿名模式

适合要把 HTML 分发给不应该知道来源站点或任何真实用户身份的人。

会额外处理：

- 将同一个真实用户稳定替换为同一个虚构头像、昵称和用户名
- 替换帖子头部、头像旁名称、`@用户名`、回复引用、topic map 参与者头像等位置
- 删除或中和用户徽章、头衔、Flair、用户卡片、用户 ID、头像原始 URL
- 删除站点域名、站点品牌、侧栏导航、站点活动入口、原图 URL、预加载 JSON
- 保留正文、图片、表情、帖子结构和 Discourse 阅读布局

V7 会保留 Discourse 的话题统计 / 参与者摘要行，例如“浏览量、赞、链接、用户 + 参与者头像”。这块不是隐私垃圾，它是页面内容摘要。全匿名模式会把其中的头像和可识别字段匿名化，并强制横向一行显示，避免头像竖排错位。

## 命令行用法

图形界面之外，也可以批量处理：

```powershell
ShuiyuanHtmlPrivacyCleaner.exe --clean input.html output.html --mode personal --report report.txt --report-style standard --language zh-CN --overwrite
```

全匿名模式：

```powershell
ShuiyuanHtmlPrivacyCleaner.exe --clean input.html output_full.html --mode full --report report_full.txt --report-style standard --language zh-CN --overwrite
```

常用参数：

- `--mode personal`：仅清除当前保存者信息
- `--mode full`：全匿名模式
- `--report report.txt`：写出报告
- `--report-style friendly|standard|technical`：报告详细程度
- `--language zh-CN|zh-Hant|en`：报告语言
- `--term 关键词`：额外审核关键词，可重复传入
- `--overwrite`：允许覆盖输出文件

## 本地构建

需要 Windows 和 .NET SDK 9。

```powershell
.\build.ps1
```

构建完成后输出：

- `dist/win-x64/ShuiyuanHtmlPrivacyCleaner.exe`
- `dist/win-x86/ShuiyuanHtmlPrivacyCleaner.exe`
- `dist/win-arm64/ShuiyuanHtmlPrivacyCleaner.exe`
- `CHECKSUMS-SHA256.txt`

项目主体是 C# / WinForms。`tools/avatar_pack_generator` 用于生成内置匿名身份包，`tools/GenerateBrandAssets.ps1` 用于从 SVG 生成 Windows 图标资源。

## 验收重点

发布前建议至少检查：

1. 清理后的 HTML 能在 Edge 中打开，不是空白页。
2. 全匿名模式下，同一用户在帖子头部、头像、回复引用、`@用户名`、topic map 中使用同一套虚构身份。
3. 话题统计 / 参与者摘要行仍然存在，并横向一行显示。
4. 报告中的水印、currentUser、站点域名、公开用户标识、徽章/Flair、隐藏预加载数据、站点外链等审核项为 0。
5. 源码仓库和发布包不包含私人测试 HTML、截图、报告或本机路径。

## 隐私说明

本工具针对 SingleFile 保存的水源/Discourse HTML 快照做静态清理。它会尽力去除已知水印、登录态、站点身份、用户身份和隐藏预加载数据，但不能数学证明任意未知隐写信息 100% 不存在。分发重要文件前，建议使用全匿名模式、额外关键词审核，并手动打开输出 HTML 复核可见内容。
