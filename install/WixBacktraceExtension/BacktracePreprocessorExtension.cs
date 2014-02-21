namespace WixBacktraceExtension
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using Microsoft.Tools.WindowsInstallerXml;

    public class BacktracePreprocessorExtension : PreprocessorExtension
    {
        public override string[] Prefixes { get { return new[] { "build", "include" }; } }

        private List<string> _componentsGenerated;

        public override void InitializePreprocess()
        {
            base.InitializePreprocess();
            _componentsGenerated = new List<string>();
        }

        public override void FinalizePreprocess()
        {
            base.FinalizePreprocess();
            _componentsGenerated.Clear();
        }

        /// <summary>
        /// The syntax is &lt;?pragma prefix.name args?&gt; where the arguments are just a string. Don't close the XmlWriter
        /// </summary>
        /// <param name="sourceLineNumbers"></param>
        /// <param name="prefix">The pragma prefix. This is matched to <see cref="Prefixes"/> to find this plugin. We have 'build' and 'include' for different sections of the .wxs</param>
        /// <param name="pragma">The command being requested. This is our dispatch key</param>
        /// <param name="args">Any arguments passed to the pragma. This is a raw string, but any $() references will have been resolved for us</param>
        /// <param name="writer">Output into the final .wxs XML file</param>
        /// <returns></returns>
        public override bool ProcessPragma(SourceLineNumberCollection sourceLineNumbers, string prefix, string pragma, string args, XmlWriter writer)
        {
            switch (prefix)
            {
                case "build":
                    return BuildComponents(pragma, args, writer);

                case "include":
                    return ReferenceComponents(pragma, args, writer);

                default:
                    return false;
            }
        }

        static bool ReferenceComponents(string command, string argString, XmlWriter writer)
        {
            var componentRefTemplate = @"<ComponentRef Id='{0}'/>"
                .Replace("'", "\"");

            if (command != "componentRefsFor") return false;
            var args = new QuotedArgsSplitter(argString);

            var dependencies = new ReferenceBuilder(Assembly.ReflectionOnlyLoadFrom(args.Primary)).NonGacDependencies().ToList();

            foreach (var dependencyKey in dependencies)
            {
                var dependency = dependencyKey.ToString();

                writer.WriteRaw(string.Format(componentRefTemplate, AssemblyKey.ComponentId(dependency)));
            }

            return true;
        }

        bool BuildComponents(string command, string argString, XmlWriter writer)
        {
            var componentTemplate =
@"<Component Id='{0}' Directory='{1}'>
    <File Id='{2}' Source='{3}' KeyPath='yes'/>
</Component>"
                .Replace("'", "\"");

            if (command != "componentsFor") return false;
            var args = new QuotedArgsSplitter(argString);
            var directory = args.WithDefault("in", "INSTALLFOLDER");

            var dependencies = new ReferenceBuilder(Assembly.ReflectionOnlyLoadFrom(args.Primary)).NonGacDependencies().ToList();

            foreach (var dependencyKey in dependencies)
            {
                var dependency = dependencyKey.ToString();

                // Components should be unique to the .msi
                // Component ids MUST be unique to the .msi
                if (_componentsGenerated.Contains(dependency)) continue;
                _componentsGenerated.Add(dependency);

                writer.WriteRaw(string.Format(componentTemplate,
                    AssemblyKey.ComponentId(dependency),
                    directory,
                    AssemblyKey.FileId(dependency),
                    AssemblyKey.FilePath(dependency)
                    ));
            }

            return true;
        }

    }
}
