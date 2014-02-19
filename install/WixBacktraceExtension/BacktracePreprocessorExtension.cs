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
            if (args.Count != 1) throw new Exception("No path supplied to $(backtrace.nameOf(...))");

            var fileName = Path.GetFileNameWithoutExtension(args[0]);
            if (string.IsNullOrWhiteSpace(fileName)) throw new Exception("Asked to get safe name for invalid path in $(backtrace.nameOf(...))");

            return fileName;
        }

        static string GetDependencies(IList<string> args)
        {
            var assm = GetReflectionAssembly(args);

            var builder = new ReferenceBuilder(assm);

            var dependencies = builder.NonGacDependencies().ToList();

            if (dependencies.Count < 1) return null;

            return string.Join(";", dependencies.Select(a => a.Location));
        }

        static Assembly GetReflectionAssembly(IList<string> args)
        {
            Assembly assm;
            try
            {
                if (args.Count != 1) throw new Exception();

                var src = args[0];
                if (!File.Exists(src)) throw new Exception();

                assm = Assembly.ReflectionOnlyLoadFrom(src);
                if (assm == null) throw new Exception();
            }
            catch
            {
                throw new ArgumentException("$(backtrace.dependenciesOf(...)) should be supplied with the file path of a single .Net assembly or .Net executable");
            }
            return assm;
        }
    }
}
