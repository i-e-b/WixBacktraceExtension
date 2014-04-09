namespace WixBacktraceExtension
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;
    using Backtrace;
    using Actions;
    using SitePublication;
    using Microsoft.Tools.WindowsInstallerXml;

    /// <summary>
    /// Backtrace extension interface
    /// </summary>
    public class BacktracePreprocessorExtension : PreprocessorExtension
    {
        private readonly HashSet<AssemblyKey> _componentsGenerated;
        private readonly HashSet<string> _pathsInstalledTo;
        public override string[] Prefixes { get { return new[] { "publish", "build", "components" }; } }

        public BacktracePreprocessorExtension()
        {
            _componentsGenerated = new HashSet<AssemblyKey>();
            _pathsInstalledTo = new HashSet<string>();
        }

        /// <summary>
        /// Prefixed variables, called like $(prefix.name)
        /// </summary>
        /// <param name="prefix">This is matched to <see cref="Prefixes"/> to find this plugin.</param>
        /// <param name="name">Name of the variable whose value is to be returned.</param>
        public override string GetVariableValue(string prefix, string name)
        {
            if (prefix != "publish" || name != "tempDirectory") return null; // making temp directories is all we do here.

            var target = Path.Combine(Session.TempFolder(), "publishoutput");
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
            try
            {
                var cleanArgs = new QuotedArgsSplitter(args);
                switch (prefix)
                {
                    case "build":
                        return BuildActions(pragma, cleanArgs, writer);

                    case "components":
                        return ComponentActions(pragma, cleanArgs, writer);

                    case "publish":
                        return PublishCommands(pragma, cleanArgs, writer);

                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                writer.WriteRaw("<?error Exception in Backtrace extension: " + ex.GetType() + " - " + ex.Message + "\r\n" + ex.StackTrace);
                return true;
            }
        }

        bool ComponentActions(string pragma, QuotedArgsSplitter cleanArgs, XmlWriter writer)
        {
            switch (pragma)
            {
                case "allDependenciesOf":
                    return PreprocessorActions.BuildComponents(_componentsGenerated, cleanArgs, writer, _pathsInstalledTo, copyDependencies: true, includeTarget: false);

                case "uniqueDependenciesOf":
                    return PreprocessorActions.BuildComponents(_componentsGenerated, cleanArgs, writer, _pathsInstalledTo, copyDependencies: false, includeTarget: false);

                case "targetAndAllDependenciesOf":
                    return PreprocessorActions.BuildComponents(_componentsGenerated, cleanArgs, writer, _pathsInstalledTo, copyDependencies: true, includeTarget: true);

                case "targetAndUniqueDependenciesOf":
                    return PreprocessorActions.BuildComponents(_componentsGenerated, cleanArgs, writer, _pathsInstalledTo, copyDependencies: false, includeTarget: true);

                case "transformedConfigOf":
                    return PreprocessorActions.TransformConfiguration(cleanArgs, writer);

                case "publishedWebsiteIn":
                    return PreprocessorActions.BuildPublishedWebsiteComponents(cleanArgs, writer, _pathsInstalledTo, _componentsGenerated);

                default:
                    return false;
            }
        }

        static bool BuildActions(string pragma, QuotedArgsSplitter cleanArgs, XmlWriter writer)
        {
            switch (pragma)
            {
                case "directoriesMatching":
                    return PreprocessorActions.BuildMatchingDirectories(cleanArgs, writer);

                default:
                    return false;
            }
        }

        static bool PublishCommands(string command, QuotedArgsSplitter args, XmlWriter writer)
        {
            switch (command)
            {
                case "webSiteProject":
                    return Website.PublishSiteToFolder(args, writer);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Startup actions.
        /// </summary>
        public override void InitializePreprocess()
        {
            Session.Load(_componentsGenerated, _pathsInstalledTo);
            base.InitializePreprocess();
        }

        /// <summary>
        /// Actions performed once XML has been finally generated, before WiX project is compiled
        /// </summary>
        public override void FinalizePreprocess()
        {
            Session.Save(_componentsGenerated, _pathsInstalledTo);
            base.FinalizePreprocess();
        }
    }
}
