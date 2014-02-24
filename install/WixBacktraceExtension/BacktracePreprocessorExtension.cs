namespace WixBacktraceExtension
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using Backtrace;
    using global::WixBacktraceExtension.Configuration;
    using SitePublication;
    using Microsoft.Tools.WindowsInstallerXml;

    /// <summary>
    /// Backtrace extension interface
    /// </summary>
    public class BacktracePreprocessorExtension : PreprocessorExtension
    {
        private List<string> _componentsGenerated;
        public const string ComponentTemplate = @"<Component Id='{0}' Directory='{1}'><File Id='{2}' Source='{3}' KeyPath='yes'/></Component>";
        public override string[] Prefixes { get { return new[] { "publish", "build", "include" }; } }

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
                    return BuildActions(pragma, cleanArgs, writer);

                case "include":
                    return ReferenceComponents(pragma, cleanArgs, writer);

                case "publish":
                    return PublishWebProject(pragma, cleanArgs, writer);

                default:
                    return false;
            }
        }

        bool BuildActions(string pragma, QuotedArgsSplitter cleanArgs, XmlWriter writer)
        {
            switch (pragma)
            {
                case "componentsFor":
                    return BuildComponents(cleanArgs, writer);

                case "transformConfigOf":
                    return TransformConfiguration(cleanArgs, writer);

                default:
                    return false;
            }
        }

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


        /// <summary>
        /// Build file components for a .Net app.config file.
        /// <para> </para>
        /// argument syntax is `build.transformConfigOf "c:\path\to\assembly.exe" for "Release" withId "MyComponentId" in "InstallDirID"`.
        /// The install directory should be declared in the .wxs file.
        /// <para> </para>
        /// Default `for` is "Release", default `in` is "INSTALLFOLDER". All other parameters must be supplied.
        /// </summary>
        bool TransformConfiguration(QuotedArgsSplitter args, XmlWriter writer)
        {
            var target = args.PrimaryRequired() + ".config";
            var directory = args.WithDefault("in", "INSTALLFOLDER");
            var config = args.WithDefault("for", "Release");
            var componentId = args.Required("withId");

            var transformPath = Path.Combine(Path.GetDirectoryName(target) ?? "", "App." + config + ".config");
            var original = target + ".original";

            if (!File.Exists(target)) throw new Exception("Expected to find \""+target+"\" but it was missing");
            if (!File.Exists(transformPath)) throw new Exception("Expected to find transform at \"" + transformPath + "\" but it was missing");

            File.Copy(target, original, true);
            ConfigTransform.Apply(original, transformPath, target);

            writer.WriteRaw(String.Format(ComponentTemplate,
                componentId,
                directory,
                "file_"+componentId,
                target
                ));

            return true;
        }

        /// <summary>
        /// Build file components for a .Net assembly's dependencies.
        /// <para>This DOES NOT include the target assembly itself.</para>
        /// </summary>
        /// <param name="args">argument syntax is `build.componentsFor "c:\path\to\assembly.exe" in "InstallDirID"`. The install directory should be declared in the .wxs file.</param>
        /// <param name="writer">output writer</param>
        bool BuildComponents(QuotedArgsSplitter args, XmlWriter writer)
        {
            var target = args.PrimaryRequired();
            var directory = args.WithDefault("in", "INSTALLFOLDER");

            var dependencies = new ReferenceBuilder(Assembly.ReflectionOnlyLoadFrom(target)).NonGacDependencies().ToList();

            foreach (var dependencyKey in dependencies)
            {
                var dependency = dependencyKey.ToString();

                // Components should be unique to the .msi
                // Component ids MUST be unique to the .msi
                if (_componentsGenerated.Contains(dependency)) continue;
                _componentsGenerated.Add(dependency);

                writer.WriteRaw(String.Format(ComponentTemplate,
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
