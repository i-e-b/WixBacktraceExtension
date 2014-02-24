namespace WixBacktraceExtension
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using Microsoft.Build.BuildEngine;
    using Microsoft.Tools.WindowsInstallerXml;

    public class BacktracePreprocessorExtension : PreprocessorExtension
    {
        public override string[] Prefixes { get { return new[] { "publish", "build", "include" }; } }

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
                    return PublishSiteToFolder(args, writer);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Call out to MSBuild, publish site as normal
        /// <para>Expects args to be from `"C:\path\to\site.csproj" to "C:\path\to\temp" config "Release"`</para>
        /// <para>Both the source project and target directory should already exist</para>
        /// </summary>
        static bool PublishSiteToFolder(QuotedArgsSplitter args, XmlWriter writer)
        {
            var projectFile = args.Primary;
            var tempDir = args.NamedArguments["to"];
            var config = args.WithDefault("config", "Release");
            if (!File.Exists(projectFile)) return true;
            if (!Directory.Exists(tempDir)) return true;


            BuildAndPublishProject(writer, tempDir, config, projectFile);
            MoveFilesToCorrectLocation(tempDir);

            return true;
        }

        static void MoveFilesToCorrectLocation(string srcDir)
        {
            var tempDir = srcDir + "_moving";
            Directory.Move(srcDir, tempDir);

            var expected = Path.Combine(tempDir, "_PublishedWebsites");
            if (!Directory.Exists(expected)) return;

            var siteContents = Directory.EnumerateDirectories(expected).Single();

            Directory.Move(siteContents, srcDir);
            Directory.Delete(tempDir, true);
        }

        static void BuildAndPublishProject(XmlWriter writer, string tempDir, string config, string projectFile)
        {
            // ReSharper disable once CSharpWarnings::CS0618
            var engine = new Engine();
            var logger = new FileLogger();
            var logFile = Path.Combine(tempDir, "publish.log");
            logger.Parameters = @"logfile=" + logFile;
            engine.RegisterLogger(logger);

            var bpg = new BuildPropertyGroup();
            bpg.SetProperty("OutDir", tempDir + "\\");
            bpg.SetProperty("Configuration", config);
            bpg.SetProperty("Platform", "AnyCPU");
            bpg.SetProperty("DeployOnBuild", "true");
            bpg.SetProperty("DeployTarget", "Package;_WPPCopyWebApplication");
            bpg.SetProperty("PackageLocation", @"$(OutDir)\MSDeploy\Package.zip");
            bpg.SetProperty("_PackageTempDir", tempDir + "\\");

            // Web.config transform special sauce:
            bpg.SetProperty("TransformInputFile", @"$(ProjectPath)\Web.config");
            bpg.SetProperty("TransformFile", @"$(ProjectPath)\Web.$(Configuration).config");
            bpg.SetProperty("TransformOutputFile", @"$(DeployPath)\Web.config");

            var success = engine.BuildProjectFile(projectFile, null, bpg);

            if (success)
            {
                writer.WriteComment(" Publish succeeded ");
                //File.AppendAllText(@"C:\temp\log.txt", "\r\nOK, in " + tempDir);
            }
            else
            {
                throw new Exception("Publish failure: see \"" + logFile + "\" for details");
            }

            engine.UnloadAllProjects();
            engine.UnregisterAllLoggers();
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

                writer.WriteRaw(string.Format(componentRefTemplate, AssemblyKey.ComponentId(dependency)));
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
