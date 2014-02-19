namespace WixBacktraceExtension
{
    using Microsoft.Tools.WindowsInstallerXml;

    public class WixBacktraceExtension : WixExtension
    {
        private BacktracePreprocessorExtension preprocessorExtension;
        public override PreprocessorExtension PreprocessorExtension
        {
            get
            {
                return preprocessorExtension ?? (preprocessorExtension = new BacktracePreprocessorExtension());
            }
        }
    }
}