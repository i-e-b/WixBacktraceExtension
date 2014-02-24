namespace WixBacktraceExtension.Configuration
{
    using System;
    using Microsoft.Web.XmlTransform;

    /// <summary>
    /// Xml transform logger that does nothing
    /// </summary>
    class DummyTransformLogger : IXmlTransformationLogger
    {
        public void LogMessage(string message, params object[] messageArgs) { }
        public void LogMessage(MessageType type, string message, params object[] messageArgs) { }
        public void LogWarning(string message, params object[] messageArgs) { }
        public void LogWarning(string file, string message, params object[] messageArgs) { }
        public void LogWarning(string file, int lineNumber, int linePosition, string message, params object[] messageArgs) { }
        public void LogError(string message, params object[] messageArgs) { }
        public void LogError(string file, string message, params object[] messageArgs) { }
        public void LogError(string file, int lineNumber, int linePosition, string message, params object[] messageArgs) { }
        public void LogErrorFromException(Exception ex) { }
        public void LogErrorFromException(Exception ex, string file) { }
        public void LogErrorFromException(Exception ex, string file, int lineNumber, int linePosition) { }
        public void StartSection(string message, params object[] messageArgs) { }
        public void StartSection(MessageType type, string message, params object[] messageArgs) { }
        public void EndSection(string message, params object[] messageArgs) { }
        public void EndSection(MessageType type, string message, params object[] messageArgs) { }
    }
}