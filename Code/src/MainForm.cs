using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShuiyuanHtmlPrivacyCleaner
{
    internal sealed class MainForm : Form
    {
        private readonly TextBox _inputBox = new TextBox();
        private readonly TextBox _outputBox = new TextBox();
        private readonly TextBox _termsBox = new TextBox();
        private readonly TextBox _reportBox = new TextBox();
        private readonly CheckBox _overwriteBox = new CheckBox();
        private readonly ComboBox _modeBox = new ComboBox();
        private readonly ComboBox _languageBox = new ComboBox();
        private readonly ComboBox _reportStyleBox = new ComboBox();
        private readonly Label _statusLabel = new Label();
        private readonly Button _browseInputButton = new Button();
        private readonly Button _browseOutputButton = new Button();
        private readonly Button _analyzeButton = new Button();
        private readonly Button _cleanButton = new Button();
        private readonly Button _openFolderButton = new Button();
        private CleaningResult _lastResult;

        public MainForm()
        {
            Text = "HTML 隐私清理工具 V7";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1080, 760);
            Size = new Size(1240, 840);
            Font = new Font("Microsoft YaHei UI", 9F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Icon = Branding.LoadAppIcon();

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 5;
            root.Padding = new Padding(14);
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            root.Controls.Add(BuildHeaderPanel(), 0, 0);
            root.Controls.Add(BuildPathPanel(), 0, 1);
            root.Controls.Add(BuildTermsPanel(), 0, 2);
            root.Controls.Add(BuildActionPanel(), 0, 3);

            _reportBox.Dock = DockStyle.Fill;
            _reportBox.Multiline = true;
            _reportBox.ScrollBars = ScrollBars.Both;
            _reportBox.WordWrap = true;
            _reportBox.Font = new Font("Microsoft YaHei UI", 9.5F);
            _reportBox.ReadOnly = true;
            root.Controls.Add(_reportBox, 0, 4);

            _statusLabel.Text = "就绪";
            _statusLabel.Dock = DockStyle.Bottom;
            _statusLabel.Height = 24;
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(_statusLabel);
        }

        private Control BuildHeaderPanel()
        {
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.ColumnCount = 2;
            panel.RowCount = 1;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            PictureBox logoBox = new PictureBox();
            logoBox.Dock = DockStyle.Fill;
            logoBox.SizeMode = PictureBoxSizeMode.Zoom;
            logoBox.Margin = new Padding(0, 0, 12, 0);
            logoBox.Image = Branding.LoadHeaderLogo();
            panel.Controls.Add(logoBox, 0, 0);

            TableLayoutPanel textPanel = new TableLayoutPanel();
            textPanel.Dock = DockStyle.Fill;
            textPanel.RowCount = 2;
            textPanel.ColumnCount = 1;
            textPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            textPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Label title = new Label();
            title.Text = "离线清理 SingleFile 水源快照中的水印、登录态和隐私痕迹";
            title.Dock = DockStyle.Fill;
            title.TextAlign = ContentAlignment.BottomLeft;
            title.Font = new Font(Font.FontFamily, 15F, FontStyle.Bold);
            textPanel.Controls.Add(title, 0, 0);

            Label subtitle = new Label();
            subtitle.Text = "V7 保留原始 Discourse/SingleFile 框架，支持仅清除保存者与全匿名两种模式，并生成可读审核报告。";
            subtitle.Dock = DockStyle.Fill;
            subtitle.TextAlign = ContentAlignment.TopLeft;
            subtitle.ForeColor = Color.FromArgb(74, 108, 179);
            textPanel.Controls.Add(subtitle, 0, 1);

            panel.Controls.Add(textPanel, 1, 0);
            return panel;
        }

        private Control BuildPathPanel()
        {
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.ColumnCount = 3;
            panel.RowCount = 2;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            panel.Controls.Add(MakeLabel("输入 HTML"), 0, 0);
            _inputBox.Dock = DockStyle.Fill;
            _inputBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            _inputBox.TextChanged += delegate { AutoSuggestOutput(); };
            panel.Controls.Add(_inputBox, 1, 0);

            ConfigureButton(_browseInputButton, "选择...");
            _browseInputButton.Click += delegate { BrowseInput(); };
            panel.Controls.Add(_browseInputButton, 2, 0);

            panel.Controls.Add(MakeLabel("输出 HTML"), 0, 1);
            _outputBox.Dock = DockStyle.Fill;
            _outputBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            panel.Controls.Add(_outputBox, 1, 1);

            ConfigureButton(_browseOutputButton, "另存为...");
            _browseOutputButton.Click += delegate { BrowseOutput(); };
            panel.Controls.Add(_browseOutputButton, 2, 1);
            return panel;
        }

        private Control BuildTermsPanel()
        {
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.ColumnCount = 2;
            panel.RowCount = 1;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            panel.Controls.Add(MakeLabel("审核关键词"), 0, 0);

            _termsBox.Dock = DockStyle.Fill;
            _termsBox.Multiline = true;
            _termsBox.ScrollBars = ScrollBars.Vertical;
            _termsBox.AcceptsReturn = true;
            _termsBox.PlaceholderText = "每行一个关键词，也支持逗号或分号分隔。可填写用户名、ID、头像编号、时间戳、可疑字符串等。";
            panel.Controls.Add(_termsBox, 1, 0);
            return panel;
        }

        private Control BuildActionPanel()
        {
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.ColumnCount = 1;
            panel.RowCount = 2;
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

            FlowLayoutPanel settingsRow = new FlowLayoutPanel();
            settingsRow.Dock = DockStyle.Fill;
            settingsRow.FlowDirection = FlowDirection.LeftToRight;
            settingsRow.WrapContents = false;
            Label modeLabel = new Label();
            modeLabel.Text = "模式";
            modeLabel.AutoSize = true;
            modeLabel.Margin = new Padding(0, 12, 4, 3);
            settingsRow.Controls.Add(modeLabel);

            _modeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _modeBox.Width = 320;
            _modeBox.Items.Add("仅清除当前保存者 / 登录者信息");
            _modeBox.Items.Add("全匿名模式（连站点与其他用户标识一起处理）");
            _modeBox.SelectedIndex = 0;
            _modeBox.SelectedIndexChanged += delegate { AutoSuggestOutput(); };
            settingsRow.Controls.Add(_modeBox);

            Label languageLabel = new Label();
            languageLabel.Text = "语言";
            languageLabel.AutoSize = true;
            languageLabel.Margin = new Padding(18, 12, 4, 3);
            settingsRow.Controls.Add(languageLabel);

            _languageBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _languageBox.Width = 140;
            _languageBox.Items.Add("中文简体");
            _languageBox.Items.Add("中文繁體");
            _languageBox.Items.Add("English");
            _languageBox.SelectedIndex = 0;
            _languageBox.SelectedIndexChanged += delegate { RefreshReport(); };
            settingsRow.Controls.Add(_languageBox);

            Label reportLabel = new Label();
            reportLabel.Text = "报告";
            reportLabel.AutoSize = true;
            reportLabel.Margin = new Padding(18, 12, 4, 3);
            settingsRow.Controls.Add(reportLabel);

            _reportStyleBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _reportStyleBox.Width = 160;
            _reportStyleBox.Items.Add("易读摘要");
            _reportStyleBox.Items.Add("标准报告");
            _reportStyleBox.Items.Add("专业明细");
            _reportStyleBox.SelectedIndex = 1;
            _reportStyleBox.SelectedIndexChanged += delegate { RefreshReport(); };
            settingsRow.Controls.Add(_reportStyleBox);

            panel.Controls.Add(settingsRow, 0, 0);

            FlowLayoutPanel actionRow = new FlowLayoutPanel();
            actionRow.Dock = DockStyle.Fill;
            actionRow.FlowDirection = FlowDirection.LeftToRight;
            actionRow.WrapContents = false;

            ConfigureButton(_analyzeButton, "仅分析");
            _analyzeButton.Width = 98;
            _analyzeButton.Click += async delegate { await RunAnalyzeAsync(); };
            actionRow.Controls.Add(_analyzeButton);

            ConfigureButton(_cleanButton, "清理并审核");
            _cleanButton.Width = 110;
            _cleanButton.Click += async delegate { await RunCleanAsync(); };
            actionRow.Controls.Add(_cleanButton);

            ConfigureButton(_openFolderButton, "打开输出位置");
            _openFolderButton.Width = 118;
            _openFolderButton.Click += delegate { OpenOutputFolder(); };
            actionRow.Controls.Add(_openFolderButton);

            _overwriteBox.Text = "允许覆盖输出文件";
            _overwriteBox.AutoSize = true;
            _overwriteBox.Margin = new Padding(18, 12, 3, 3);
            actionRow.Controls.Add(_overwriteBox);
            panel.Controls.Add(actionRow, 0, 1);
            return panel;
        }

        private static Label MakeLabel(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            return label;
        }

        private static void ConfigureButton(Button button, string text)
        {
            button.Text = text;
            button.Height = 30;
            button.Margin = new Padding(6, 6, 6, 6);
        }

        private void BrowseInput()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "HTML 文件 (*.html;*.htm)|*.html;*.htm|所有文件 (*.*)|*.*";
                dialog.Title = "选择 SingleFile 保存的 HTML 文件";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _inputBox.Text = dialog.FileName;
                    AutoSuggestOutput();
                }
            }
        }

        private void BrowseOutput()
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "HTML 文件 (*.html)|*.html|HTM 文件 (*.htm)|*.htm|所有文件 (*.*)|*.*";
                dialog.Title = "选择清理后的输出文件";
                if (!string.IsNullOrWhiteSpace(_outputBox.Text))
                {
                    dialog.FileName = _outputBox.Text;
                }

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _outputBox.Text = dialog.FileName;
                }
            }
        }

        private void AutoSuggestOutput()
        {
            if (string.IsNullOrWhiteSpace(_inputBox.Text))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(_outputBox.Text) && _outputBox.Focused)
            {
                return;
            }

            try
            {
                _outputBox.Text = CleanerEngine.SuggestOutputPath(_inputBox.Text, GetSelectedMode());
            }
            catch
            {
            }
        }

        private async Task RunAnalyzeAsync()
        {
            try
            {
                RequireInput();
                SetBusy("分析中...");

                string input = _inputBox.Text;
                string[] terms = new string[] { _termsBox.Text };
                CleaningMode mode = GetSelectedMode();
                CleanerEngine engine = new CleanerEngine();

                CleaningResult result = await RunWithProgressAsync(
                    "分析中...",
                    "正在分析 HTML",
                    "准备扫描文件...",
                    delegate (IProgress<ProgressInfo> progress)
                    {
                        return engine.AnalyzeOnly(input, terms, mode, progress);
                    });

                _lastResult = result;
                RefreshReport();
                SetReady("分析完成");
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private async Task RunCleanAsync()
        {
            try
            {
                RequireInput();
                if (string.IsNullOrWhiteSpace(_outputBox.Text))
                {
                    _outputBox.Text = CleanerEngine.SuggestOutputPath(_inputBox.Text, GetSelectedMode());
                }

                string input = _inputBox.Text;
                string output = _outputBox.Text;
                string[] terms = new string[] { _termsBox.Text };
                bool overwrite = _overwriteBox.Checked;
                CleaningMode mode = GetSelectedMode();
                CleanerEngine engine = new CleanerEngine();

                SetBusy(mode == CleaningMode.FullAnonymous
                    ? "全匿名清理中，请稍候..."
                    : "清理中...");

                CleaningResult result = await RunWithProgressAsync(
                    mode == CleaningMode.FullAnonymous ? "全匿名处理进度" : "清理进度",
                    mode == CleaningMode.FullAnonymous ? "开始全匿名清理..." : "开始清理...",
                    mode == CleaningMode.FullAnonymous ? "准备执行全匿名规则..." : "准备执行清理规则...",
                    delegate (IProgress<ProgressInfo> progress)
                    {
                        return engine.Clean(input, output, terms, overwrite, mode, progress);
                    });

                _lastResult = result;
                RefreshReport();
                SetReady("清理完成");
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void RequireInput()
        {
            if (string.IsNullOrWhiteSpace(_inputBox.Text))
            {
                throw new InvalidOperationException("请先选择输入 HTML 文件。");
            }

            if (!File.Exists(_inputBox.Text))
            {
                throw new FileNotFoundException("输入文件不存在。", _inputBox.Text);
            }
        }

        private CleaningMode GetSelectedMode()
        {
            return _modeBox.SelectedIndex == 1 ? CleaningMode.FullAnonymous : CleaningMode.PersonalOnly;
        }

        private ReportLanguage GetSelectedLanguage()
        {
            if (_languageBox.SelectedIndex == 1)
            {
                return ReportLanguage.TraditionalChinese;
            }
            if (_languageBox.SelectedIndex == 2)
            {
                return ReportLanguage.English;
            }
            return ReportLanguage.SimplifiedChinese;
        }

        private ReportVerbosity GetSelectedReportVerbosity()
        {
            if (_reportStyleBox.SelectedIndex == 0)
            {
                return ReportVerbosity.Friendly;
            }
            if (_reportStyleBox.SelectedIndex == 2)
            {
                return ReportVerbosity.Technical;
            }
            return ReportVerbosity.Standard;
        }

        private void RefreshReport()
        {
            if (_lastResult == null)
            {
                return;
            }
            _reportBox.Text = _lastResult.ToDisplayText(GetSelectedReportVerbosity(), GetSelectedLanguage());
        }

        private void OpenOutputFolder()
        {
            try
            {
                string target = _outputBox.Text;
                if (!string.IsNullOrWhiteSpace(target) && File.Exists(target))
                {
                    Process.Start("explorer.exe", "/select,\"" + target + "\"");
                    return;
                }

                if (!string.IsNullOrWhiteSpace(target))
                {
                    string folder = Path.GetDirectoryName(target);
                    if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
                    {
                        Process.Start("explorer.exe", "\"" + folder + "\"");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void SetBusy(string text)
        {
            Cursor = Cursors.WaitCursor;
            _statusLabel.Text = text;
            SetControlsEnabled(false);
        }

        private void SetReady(string text)
        {
            Cursor = Cursors.Default;
            _statusLabel.Text = text;
            SetControlsEnabled(true);
        }

        private void SetControlsEnabled(bool enabled)
        {
            _inputBox.Enabled = enabled;
            _outputBox.Enabled = enabled;
            _termsBox.Enabled = enabled;
            _overwriteBox.Enabled = enabled;
            _modeBox.Enabled = enabled;
            _languageBox.Enabled = enabled;
            _reportStyleBox.Enabled = enabled;
            _browseInputButton.Enabled = enabled;
            _browseOutputButton.Enabled = enabled;
            _analyzeButton.Enabled = enabled;
            _cleanButton.Enabled = enabled;
            _openFolderButton.Enabled = enabled;
        }

        private async Task<T> RunWithProgressAsync<T>(string progressTitle, string statusText, string initialMessage, Func<IProgress<ProgressInfo>, T> work)
        {
            ProgressForm progressForm = new ProgressForm(progressTitle, initialMessage);
            Progress<ProgressInfo> progress = new Progress<ProgressInfo>(delegate (ProgressInfo info)
            {
                progressForm.UpdateProgress(info);
                _statusLabel.Text = string.IsNullOrWhiteSpace(info.Message) ? statusText : info.Message;
            });

            progressForm.Show(this);
            progressForm.BringToFront();
            progressForm.Update();

            try
            {
                _statusLabel.Text = statusText;
                await Task.Yield();
                T result = await Task.Run(delegate { return work(progress); });
                progressForm.UpdateProgress(new ProgressInfo(100, "处理完成。"));
                return result;
            }
            finally
            {
                if (!progressForm.IsDisposed)
                {
                    progressForm.Close();
                    progressForm.Dispose();
                }
            }
        }

        private void ShowError(Exception ex)
        {
            Cursor = Cursors.Default;
            _statusLabel.Text = "出错";
            SetControlsEnabled(true);
            MessageBox.Show(this, ex.Message, "处理失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
