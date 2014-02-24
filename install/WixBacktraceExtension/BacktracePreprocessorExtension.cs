namespace WixBacktraceExtension
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using Backtrace;
    using SitePublication;
    using Microsoft.Tools.WindowsInstallerXml;

    /// <summary>
    /// Backtrace extension interface
    /// </summary>
    public class BacktracePreprocessorExtension : PreprocessorExtension
    {
        private List<string> _componentsGenerated;
        public override string[] Prefixes { get { return new[] { "publish", "build", "include" }; } }

        /// <summary>
        /// Startup actions.
        /// </summary>
        public override void InitializePreprocess()
        {
            base.InitializePreprocess();
            _componentsGenerated = new List<string>();
        }

        /// <summary>
        /// Actions performed once XML has been finally generated, before WiX project is compiled
        /// </summary>
        public override void FinalizePreprocess()
        {
            base.FinalizePreprocess();
            _componentsGenerated.Clear();
        }

        /// <summary>
        /// Prefixed variables, called like $(prefix.name)
        /// </summary>
        /// <param name="prefix">This is matched to <see cref="Prefixes"/> to find this plugin.</param>
        /// <param name="name">Name of the variable whose value is to be returned.</param>
        public override string GetVariableValue(string prefix, string name)
        {
            if (prefix != "publish" || name != "tempDirectory") return null; // making temp directories is all we do here.

            var target = Path.Combine(Path.GetTempPath(), "publish_" + Guid.NewGuid());
            Directory.CreateDirectory(target);

            return target;
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
            var cleanArgs = new QuotedArgsSplitter(args);
            switch (prefix)
            {
                case "build":
                    return BuildComponents(pragma, cleanArgs, writer);

                case "include":
                    return ReferenceComponents(pragma, cleanArgs, writer);

                case "publish":
                    return PublishWebProject(pragma, cleanArgs, writer);

                default:
                    return false;
            }
        }

        static bool PublishWebProject(string command, QuotedArgsSplitter args, XmlWriter writer)
        {
            switch (command)
            {
                case "webSiteProject":
                    return Website.PublishSiteToFolder(args, writer);

                default:
                    return false;
            }
        }

        static bool ReferenceComponents(string command, QuotedArgsSplitter args, XmlWriter writer)
        {
            var componentRefTemplate = @"<ComponentRef Id='{0}'/>"
                .Replace("'", "\"");

            if (command != "componentRefsFor") return false;

            var dependencies = new ReferenceBuilder(Assembly.ReflectionOnlyLoadFrom(args.Primary)).NonGacDependencies().ToList();

            foreach (var dependencyKey in dependencies)
            {
                var dependency = dependencyKey.ToString();

                writer.WriteRaw(String.Format(componentRefTemplate, AssemblyKey.ComponentId(dependency)));
            }

            return true;
        }

        bool BuildComponents(string command, QuotedArgsSplitter args, XmlWriter writer)
        {
            var componentTemplate =
@"<Component Id='{0}' Directory='{1}'>
    <File Id='{2}' Source='{3}' KeyPath='yes'/>
</Component>"
                .Replace("'", "\"");

            if (command != "componentsFor") return false;
            var directory = args.WithDefault("in", "INSTALLFOLDER");

            var dependencies = new ReferenceBuilder(Assembly.ReflectionOnlyLoadFrom(args.Primary)).NonGacDependencies().ToList();

            foreach (var dependencyKey in dependencies)
            {
                var dependency = dependencyKey.ToString();

                // Components should be unique to the .msi
                // Component ids MUST be unique to the .msi
                if (_componentsGenerated.Contains(dependency)) continue;
                _componentsGenerated.Add(dependency);

                writer.WriteRaw(String.Format(componentTemplate,
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
