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
        public override string[] Prefixes { get { return new[] { "backtrace" }; } }

        public override string EvaluateFunction(string prefix, string function, string[] args)
        {
            switch (prefix)
            {
                case "backtrace":
                    switch (function)
                    {
                        case "dependenciesOf":
                            return GetDependencies(args);

                        case "nameOf":
                            return GetSafeName(args);

                        case "id":
                            return GetSafeId();
                    }
                    break;
            }
            return null;
        }

        static string GetSafeId()
        {
            return Guid.NewGuid().ToString("N");
        }

        static string GetSafeName(IList<string> args)
        {
            if (args.Count != 1) return Guid.NewGuid().ToString("N");

            var fileName = Path.GetFileNameWithoutExtension(args[0]);
            if (string.IsNullOrWhiteSpace(fileName)) return Guid.NewGuid().ToString("N");

            return fileName;
        }

        static string GetDependencies(IList<string> args)
        {
            if (args.Count != 1) return "-BAD ARGUMENTS-";

            var dependencies = new ReferenceBuilder(Assembly.LoadFrom(args[0])).NonGacDependencies().ToList();

            if (dependencies.Count < 1) return "-NONE FOUND-";

            return string.Join(";", dependencies.Select(a => a.Location));
        }

    }
}
