namespace WixBacktraceExtension
{
    using System;
    using System.IO;
    using System.Text;
    using System.Xml;
    using Microsoft.Web.XmlTransform;

    /// <summary>
    /// Performs configuration transforms for web.config and app.config files.
    /// </summary>
    public class ConfigTransform
    {
        public static void Apply(string srcPath, string transformPath, string targetPath)
        {
            if (!File.Exists(srcPath) || !File.Exists(transformPath))
            {
                throw new ArgumentException("Can't find source or transform files.");
            }

            Encoding encoding;
            var srcDoc = LoadXml(srcPath, out encoding);
            var transform = LoadXdt(transformPath, encoding);

            if (!transform.Apply(srcDoc)) throw new Exception("Configuration file transformation failed");
            var finalOutput = GetIndentedXml(srcDoc, encoding);

            File.WriteAllText(targetPath, finalOutput, encoding);
        }

        static string GetIndentedXml(XmlNode doc, Encoding encoding)
        {
            var xmlWriterSettings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = new string(' ', 4),
                Encoding = encoding
            };

            using (var buffer = new StringWriter())
            {
                using (var xmlWriter = XmlWriter.Create(buffer, xmlWriterSettings))
                {
                    doc.WriteTo(xmlWriter);
                }

                return buffer.ToString();
            }
        }

        static XmlTransformation LoadXdt(string transformPath, Encoding encoding)
        {
            var transformFile = File.ReadAllText(transformPath, encoding);

            /*if ((parameters != null && parameters.Count > 0) || forceParametersTask)
            {
                var parametersTask = new ParametersTask();
                if (parameters != null)
                {
                    parametersTask.AddParameters(parameters);
                }

                transformFile = parametersTask.ApplyParameters(transformFile);
            }*/

            return new XmlTransformation(transformFile, false, new DummyTransformLogger());
        }

        static XmlDocument LoadXml(string srcPath, out Encoding encoding)
        {
            var document = new XmlDocument { PreserveWhitespace = true };

            document.Load(srcPath);
            encoding = Encoding.UTF8;
            if (document.FirstChild.NodeType == XmlNodeType.XmlDeclaration)
            {
                var xmlDeclaration = (XmlDeclaration)document.FirstChild;
                if (!string.IsNullOrEmpty(xmlDeclaration.Encoding))
                {
                    encoding = Encoding.GetEncoding(xmlDeclaration.Encoding);
                }
            }
            return document;
        }
    }
}