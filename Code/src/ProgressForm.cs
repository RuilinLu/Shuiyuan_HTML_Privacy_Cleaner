using System;
using System.Drawing;
using System.Windows.Forms;

namespace ShuiyuanHtmlPrivacyCleaner
{
    internal sealed class ProgressForm : Form
    {
        private readonly Label _messageLabel = new Label();
        private readonly Label _percentLabel = new Label();
        private readonly Label _elapsedLabel = new Label();
        private readonly ProgressBar _progressBar = new ProgressBar();
        private readonly Timer _timer = new Timer();
        private readonly DateTime _startedAt = DateTime.Now;

        public ProgressForm(string title, string initialMessage)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;
            ShowInTaskbar = false;
            TopMost = true;
            ClientSize = new Size(520, 176);
            Font = new Font("Microsoft YaHei UI", 9F);
            Icon = Branding.LoadAppIcon();

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(16);
            root.ColumnCount = 1;
            root.RowCount = 4;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            Controls.Add(root);

            _messageLabel.Dock = DockStyle.Fill;
            _messageLabel.TextAlign = ContentAlignment.MiddleLeft;
            _messageLabel.Text = initialMessage;
            _messageLabel.Font = new Font(Font.FontFamily, 10F);
            root.Controls.Add(_messageLabel, 0, 0);

            _progressBar.Dock = DockStyle.Fill;
            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.Minimum = 0;
            _progressBar.Maximum = 100;
            _progressBar.Value = 0;
            root.Controls.Add(_progressBar, 0, 1);

            _percentLabel.Dock = DockStyle.Fill;
            _percentLabel.TextAlign = ContentAlignment.MiddleLeft;
            root.Controls.Add(_percentLabel, 0, 2);

            _elapsedLabel.Dock = DockStyle.Fill;
            _elapsedLabel.TextAlign = ContentAlignment.MiddleLeft;
            root.Controls.Add(_elapsedLabel, 0, 3);

            _timer.Interval = 500;
            _timer.Tick += delegate { UpdateElapsed(); };
            _timer.Start();

            UpdateProgress(new ProgressInfo(0, initialMessage));
            UpdateElapsed();
        }

        public void UpdateProgress(ProgressInfo info)
        {
            if (info == null)
            {
                return;
            }

            _progressBar.Value = info.Percent;
            _messageLabel.Text = string.IsNullOrWhiteSpace(info.Message) ? "正在处理..." : info.Message;
            _percentLabel.Text = "当前进度: " + info.Percent.ToString() + "%";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Stop();
                _timer.Dispose();
            }

            base.Dispose(disposing);
        }

        private void UpdateElapsed()
        {
            TimeSpan elapsed = DateTime.Now - _startedAt;
            _elapsedLabel.Text = "已运行: " + Math.Floor(elapsed.TotalSeconds).ToString("0") + " 秒";
        }
    }
}
