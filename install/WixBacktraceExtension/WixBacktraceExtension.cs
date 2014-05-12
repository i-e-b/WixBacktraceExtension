namespace WixBacktraceExtension
{
    using Microsoft.Tools.WindowsInstallerXml;

    /// <summary>
    /// Bindings from Wix to the backtrace extension.
    /// </summary>
    public class WixBacktraceExtension : WixExtension
    {
        private BacktracePreprocessorExtension preprocessorExtension;

        /// <summary>
        /// Injection point
        /// </summary>
        public override PreprocessorExtension PreprocessorExtension
        {
            get
            {
                return preprocessorExtension ?? (preprocessorExtension = new BacktracePreprocessorExtension());
            }
        }
    }
}