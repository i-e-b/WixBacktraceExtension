namespace WixBacktraceExtension
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Tools.WindowsInstallerXml;

    public class BacktracePreprocessorExtension : PreprocessorExtension
    {
        public override string[] Prefixes { get { return new[] { "get" }; } }


        private List<string> _seen;
        public override void InitializePreprocess()
        {
            base.InitializePreprocess();
            _seen = new List<string>();
        }

        public override void FinalizePreprocess()
        {
            base.FinalizePreprocess();
            _seen.Clear();
        }

        /// <summary>
        /// The syntax is &lt;?pragma prefix.name args?&gt; where the arguments are just a string. Don't close the XmlWriter
        /// </summary>
        /// <param name="sourceLineNumbers"></param>
        /// <param name="prefix"></param>
        /// <param name="pragma"></param>
        /// <param name="args"></param>
        /// <param name="writer"></param>
        /// <returns></returns>
        public override bool ProcessPragma(SourceLineNumberCollection sourceLineNumbers, string prefix, string pragma, string args, System.Xml.XmlWriter writer)
        {
            File.AppendAllText(@"C:\temp\log.txt", "\r\nPRAGMA: " + prefix + "." + pragma);
            return base.ProcessPragma(sourceLineNumbers, prefix, pragma, args, writer);
        }

        public override string EvaluateFunction(string prefix, string function, string[] args)
        {
            switch (prefix)
            {
                case "get":
                    return BacktraceFunctions(function, args);

                default:
                    return null;
            }
        }

        string BacktraceFunctions(string function, IList<string> args)
        {
            switch (function)
            {
                case "dependenciesOf":
                    return GetDependencies(args);

                case "distinct":
                    return GetDistinct(args);

                case "componentId":
                    return GetComponentId(args);

                case "fileId":
                    return GetFileId(args);

                case "filePath":
                    return GetFilePath(args);

                default:
                    return null;
            }
        }

        string GetDistinct(IList<string> args)
        {
            if (args.Count != 1) return " - Bad input to get.distinct. Should be output from get.dependenciesOf() - ";

            var output = new List<string>();
            var bits = args[0].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var bit in bits)
            {
                if (_seen.Contains(bit)) continue;
                _seen.Add(bit);
                output.Add(bit);
            }

            if (output.Count < 1) return "";
            return string.Join(";", output);
        }

        static string GetFileId(IList<string> args)
        {
            if (args.Count != 1) return " - Bad input to get.fileId. Should be single value from get.dependenciesOf enumeration - ";
            if (string.IsNullOrWhiteSpace(args[0])) return "";

            return AssemblyKey.FileKey(args[0]);
        }

        static string GetFilePath(IList<string> args)
        {
            if (args.Count != 1) return " - Bad input to get.filePath. Should be single value from get.dependenciesOf enumeration - ";
            if (string.IsNullOrWhiteSpace(args[0])) return "";

            return AssemblyKey.FilePath(args[0]);
        }

        static string GetComponentId(IList<string> args)
        {
            if (args.Count != 1) return " - Bad input to get.componentId. Should be single value from get.dependenciesOf enumeration - ";
            if (string.IsNullOrWhiteSpace(args[0])) return "";

            return AssemblyKey.ComponentKey(args[0]);
        }

        static string GetDependencies(IList<string> args)
        {
            if (args.Count != 1) return " - Bad input to get.dependencies. Should be the path to a .Net assembly (.dll or .exe) - ";
            if (string.IsNullOrWhiteSpace(args[0])) return "";

            var dependencies = new ReferenceBuilder(Assembly.LoadFrom(args[0])).NonGacDependencies().ToList();

            if (dependencies.Count < 1) return "";

            return string.Join(";", dependencies);
        }

    }
}
