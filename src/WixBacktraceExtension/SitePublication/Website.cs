namespace WixBacktraceExtension.SitePublication
{
    using System.IO;
    using System.Linq;
    using System.Xml;
    using Backtrace;
    using Configuration;
    using Microsoft.Build.BuildEngine;

    /// <summary>
    /// Website publishing code. Not stable.
    /// </summary>
    public class Website
    {
        /// <summary>
        /// Call out to MSBuild, publish site as normal
        /// <para>Expects args to be  `publish.webSiteProject "C:\path\to\site.csproj" to "C:\path\to\temp" for "BuildConfiguration"`</para>
        /// <para>Both the source project and target directory should already exist. You can create a temp directory
        /// into a variable with &lt;?define PublishTemp=$(publish.tempDirectory)?&gt;</para>
        /// <para>If `for` parameter is not given, it will default to "Release"</para>
        /// <para>Will attempt to transform `web.config` using `web.{for}.config` (i.e. `web.Release.config`)</para>
        /// </summary>
        public static bool PublishSiteToFolder(QuotedArgsSplitter args, XmlWriter writer)
        {
            var projectFile = args.PrimaryRequired();
            var tempDir = args.NamedArguments["to"];
            var config = args.WithDefault("for", "Release");
            if (!File.Exists(projectFile)) return true;
            if (!Directory.Exists(tempDir)) return true;

            BuildAndPublishProject(writer, tempDir, config, projectFile);
            MoveFilesToCorrectLocation(tempDir);
            TransformConfiguration(tempDir, config);

            return true;
        }

        static void TransformConfiguration(string filesDirectory, string config)
        {
            var target = Path.Combine(filesDirectory, "Web.config");
            var original = target + ".original";
            var transform = Path.Combine(filesDirectory, "Web." + config + ".config");

            if (!File.Exists(target) || !File.Exists(transform)) return;

            File.Copy(target, original, true);
            ConfigTransform.Apply(original, transform, target);

            File.Delete(original);
            foreach (var transformFile in Directory.GetFiles(filesDirectory, "Web.*.config", SearchOption.TopDirectoryOnly))
            {
                File.Delete(transformFile);
            }
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
            bpg.SetProperty("DeployTarget", "Package");
            bpg.SetProperty("PackageLocation", @"$(OutDir)\MSDeploy\Package.zip");
            bpg.SetProperty("_PackageTempDir", tempDir + "\\");

            var success = engine.BuildProjectFile(projectFile, null, bpg);

            if (success)
            {
                writer.WriteComment(" Publish succeeded, logs in " + tempDir.Replace("--", "_"));
            }
            else
            {
                writer.WriteRaw("<?error WixBacktraceExtension: Could publish website. Logs at " + tempDir.Replace("--", "_") + "\\publish.log ?>");
            }

            engine.UnloadAllProjects();
            engine.UnregisterAllLoggers();
        }
    }
}