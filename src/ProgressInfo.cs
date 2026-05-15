namespace ShuiyuanHtmlPrivacyCleaner
{
    internal sealed class ProgressInfo
    {
        public int Percent { get; }
        public string Message { get; }

        public ProgressInfo(int percent, string message)
        {
            if (percent < 0)
            {
                percent = 0;
            }
            else if (percent > 100)
            {
                percent = 100;
            }

            Percent = percent;
            Message = message ?? string.Empty;
        }
    }
}
