# 水源 HTML 隐私清理工具 V5

这是一个可完全离线运行的 Windows 工具，用来清理用 SingleFile 保存的水源/Discourse HTML 快照。程序提供图形界面，也支持命令行批量处理。

V5 的关键变化：全匿名模式不再提取发言后重写一个新页面，而是直接在原始 SingleFile/Discourse HTML 上就地匿名化，尽量保留原版论坛布局、楼层、引用、时间线和静态阅读体验。

## 前提背景

这个工具处理的不是网页截图图片，也不是正在联网浏览的网页，而是 SingleFile 浏览器扩展保存出来的单文件 `.html` 快照。推荐流程是：

1. 在 Edge 浏览器中安装 SingleFile。
2. 打开需要保存的水源社区页面。
3. 用 SingleFile 保存当前页面，得到一个 `.html` 文件。
4. 再用本工具清理这个 `.html` 文件中的水印、登录态和隐私痕迹。

SingleFile 项目和使用说明可以参考这个仓库：

- <https://github.com/RuilinLu/SingleFile>

我目前重点测试过的组合是：

- Edge 浏览器
- SingleFile 扩展
- Windows x64
- `dist/win-x64/ShuiyuanHtmlPrivacyCleaner.exe`

其他浏览器、Windows x86、Windows ARM64 的 EXE 已经构建出来，其中 x86 在 x64 Windows 上做过命令行清理验证，ARM64 做过二进制架构校验；但我没有 ARM64 真机，所以 ARM64 运行效果需要对应设备再验证。

## Release 怎么安装

如果你只是使用工具，不需要下载源码：

1. 打开 GitHub Releases。
2. 根据你的 Windows 架构下载对应 EXE：
   - `ShuiyuanHtmlPrivacyCleaner-win-x64.exe`：推荐，大多数 64 位 Windows 电脑使用这个。
   - `ShuiyuanHtmlPrivacyCleaner-win-x86.exe`：仅用于 32 位 Windows。
   - `ShuiyuanHtmlPrivacyCleaner-win-arm64.exe`：仅用于 Windows ARM64 设备。
3. 双击 EXE 即可运行，不需要安装 .NET，也不需要联网。
4. 如果 Windows SmartScreen 提示未知发布者，需要手动选择“更多信息”再运行。这个工具没有代码签名证书。

如果你想审计或二次开发，下载源码或克隆仓库后看 `Code/src`。

## 能清理什么

- SingleFile 保存时间注释
- 可见平铺水印
- 隐藏低透明度整页水印
- 右上角当前登录用户头像和账号入口
- `currentUser` 中的保存者账号数据
- “我的帖子”“浏览记录”、未读/新帖计数、阅读状态标记
- Thunderbit、沉浸式翻译等浏览器扩展残留
- CSRF token、主题激活、主题 tracking 等保存态痕迹
- 用户手动填写的额外审核关键词
- 全匿名模式下的站点域名、站点品牌、外链、预加载 JSON、公开用户标识、头像、用户徽章/头衔/flair

## 两种模式

`仅清除当前保存者 / 登录者信息`

- 用途：你只是想删除自己的水印、登录态、头像、ID、昵称和保存痕迹。
- 会清理当前保存者的 `currentUser`、水印、隐水印、右上角账号入口、保存时间、扩展残留、CSRF token。
- 会保留页面原有站点名称、帖子里其他公开用户、公开头像、用户徽章、公开链接和论坛原始结构。
- 因此报告里“站点域名 / 品牌痕迹”“公开用户标识”“用户徽章 / 头衔 / Flair”仍可能大于 0，这是这个模式的设计目标。

`全匿名模式（连站点与其他用户标识一起处理）`

- 用途：你想把 HTML 作为匿名存档分享，不希望看出保存者、其他用户、站点来源或外部资源地址。
- 会在原始 Discourse/SingleFile 框架内就地改写，不走“提取发言再重新生成页面”的流程。
- 会把同一用户的昵称、用户名、`@用户名`、用户链接、头像 title/alt 等尽量统一替换为同一个 `用户1`、`用户2`。
- 会移除所有用户徽章、头衔、flair 节点，不限定于某几个徽章名称。
- 会删除 Discourse 预加载 JSON 和站点外链，避免隐藏数据中残留原始用户、图片 URL 或站点域名。

## 界面说明

主窗口从上到下分为这些区域：

1. 顶部标题区  
   左侧显示 `Figure/shuiyuan_html_watermark_tool_logo.svg` 生成的 logo，右侧显示工具标题和当前版本说明。

2. `输入 HTML`  
   选择原始 SingleFile 保存的 `.html` 或 `.htm` 文件。

3. `输出 HTML`  
   选择清理后的保存位置。切换模式时，程序会自动建议 `_去水印去个人信息` 或 `_全匿名版` 文件名。

4. `审核关键词`  
   每行一个关键词，也支持逗号、分号、中文逗号、中文分号。适合填写用户名、用户 ID、头像编号、时间戳、昵称、可疑字符串。

5. `模式` 下拉框  
   选择“仅清除当前保存者 / 登录者信息”或“全匿名模式”。

6. `仅分析`  
   只扫描输入文件并生成报告，不写出新 HTML。

7. `清理并审核`  
   执行清理、写出 HTML，并立刻对输出文件做一遍审核。

8. `打开输出位置`  
   在资源管理器中定位输出文件；如果文件还没生成，就打开输出目录。

9. `允许覆盖输出文件`  
   勾选后允许覆盖已有输出文件；不勾选时，输出文件已存在会报错。

10. 报告区  
   显示“清理前检测到的信息”“实际移除或改写”“清理后审核结果”和注意事项。

11. 底部状态栏  
   显示当前阶段，例如“就绪”“正在执行清理规则”“清理完成”“出错”。

## 进度窗口

点击 `仅分析` 或 `清理并审核` 后，程序会弹出独立进度窗口。

- 绿色进度条是真实百分比，只从左到右走一遍。
- 百分比文字会显示为 `当前进度: 72%` 这种形式。
- 阶段文字会显示正在读取、扫描、清理、修复 HTML、写出文件、审核等步骤。
- 全匿名模式处理大文件时可能比个人模式慢，这是因为它要扫描和替换所有公开用户与隐藏预加载数据。

## 推荐流程

1. 在 Edge 中打开目标页面。
2. 用 SingleFile 保存当前页面为单个 `.html` 文件。
3. 打开 `dist/win-x64/ShuiyuanHtmlPrivacyCleaner.exe`，如果是 32 位 Windows 用 `win-x86`，Windows ARM64 用 `win-arm64`。
4. 选择输入 HTML。
5. 确认输出 HTML。
6. 选择清理模式。
7. 有特别担心的昵称、ID、头像编号或字符串时，填入 `审核关键词`。
8. 先点 `仅分析` 看原文件里有什么。
9. 再点 `清理并审核`。
10. 打开输出 HTML，确认页面能正常显示。
11. 只分享清理后的 HTML 和报告，不分享原始 HTML。

## 命令行模式

```powershell
.\ShuiyuanHtmlPrivacyCleaner.exe --clean "input.html" "output.html" `
  --mode full `
  --term "用户名" `
  --term "用户ID" `
  --report "report.txt" `
  --overwrite
```

参数：

- `--clean input output`：指定输入和输出 HTML。
- `--mode personal`：仅清除当前保存者 / 登录者信息。
- `--mode full`：全匿名模式。
- `--term`：追加审核关键词，可重复多次。
- `--report`：写出文本报告。
- `--overwrite`：允许覆盖已有输出文件。

路径中有空格时必须加英文双引号。

## 图标与 logo

项目使用两个图形源文件：

- `Figure/shuiyuan_symbol_app_icon.svg`：生成 EXE 图标。
- `Figure/shuiyuan_html_watermark_tool_logo.svg`：生成程序窗口左上角 logo。

构建时会生成：

- `Figure/shuiyuan_symbol_app_icon.ico`
- `Figure/shuiyuan_symbol_app_icon.png`
- `Figure/shuiyuan_html_watermark_tool_logo.png`

## 源码与发布目录

发布包结构：

```text
APP/V5/
  Code/
    src/
    Figure/
    tools/
    tests/
    ShuiyuanHtmlPrivacyCleaner.csproj
    build.ps1
    README.md
    README.html
  dist/
    win-x64/
    win-x86/
    win-arm64/
  Figure/
  README.md
  README.html
  CHECKSUMS-SHA256.txt
```

`Code` 是完整开源源码；`dist` 是已经发布好的离线 EXE。

## 技术栈和语言占比说明

主要语言是 C#：

- `Code/src/*.cs`：核心程序，WinForms 图形界面、命令行入口、清理引擎、报告生成。
- `Code/tools/*.ps1` 和 `build.ps1`：PowerShell 构建脚本，用来从 SVG 生成图标并发布三架构 EXE。
- `README.md` / `README.html`：使用说明文档。

项目不是 Python 工具，也不依赖 Node.js。最终 EXE 是 .NET 9 Windows self-contained 单文件发布。

## 构建

构建机器需要 Windows 和 .NET 9 SDK：

```powershell
.\build.ps1
```

构建脚本会生成图标资源，并发布：

- `dist/win-x64`
- `dist/win-x86`
- `dist/win-arm64`

发布出来的 EXE 是 self-contained 单文件程序，目标机器只要是 Windows，不需要预装 .NET。

## V5 验收结果

本轮 V5 已用真实样本验证：

- 真实样本 A 全匿名：Edge 实际打开后 `topic-post=21`，`cooked=21`，页面高度约 `5952px`，不是空白页。
- 真实样本 B 全匿名：Edge 实际打开后 `topic-post=62`，`cooked=62`，页面高度约 `15947px`，不是空白页。
- 全匿名输出中保存者关键词、站点域名、公开用户标识、用户徽章节点、隐藏预加载数据、站点外链、CSRF、SingleFile 保存时间、水印、隐水印均审核为 0。
- 个人模式在两个样本上均清除了保存者个人关键词、水印、隐水印、`currentUser` 和登录态，但按设计保留公开站点和其他用户信息。

严格说明：任何工具都不能数学证明未知隐写或任意构造隐藏信息 100% 不存在。V5 的审核覆盖了当前已知的 SingleFile/Discourse 水印、隐水印、登录态、预加载数据、用户标识、外链和站点痕迹。

## 版本命名

- `APP/V1`：第一版个人信息清理工具。
- `APP/V2`：加入双模式雏形。
- `APP/V3`：加入进度窗口，但全匿名大文件仍可能体验差。
- `APP/V4`：修复中文和进度体验，曾使用静态重建方案。
- `APP/V5`：全匿名改为在原始 Discourse/SingleFile 框架内就地匿名化，修复 `<style>` 吞 DOM 导致空白页的问题。
