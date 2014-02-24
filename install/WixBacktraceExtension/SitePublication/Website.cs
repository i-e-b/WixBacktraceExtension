namespace WixBacktraceExtension.SitePublication
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using Backtrace;
    using Microsoft.Build.BuildEngine;

    public class Website
    {
        /// <summary>
        /// Call out to MSBuild, publish site as normal
        /// <para>Expects args to be from `"C:\path\to\site.csproj" to "C:\path\to\temp" config "Release"`</para>
        /// <para>Both the source project and target directory should already exist</para>
        /// <para>If `config` parameter is not given, it will default to "Release"</para>
        /// <para>Will attempt to transform `web.config` using `web.{config}.config` (i.e. `web.Release.config`)</para>
        /// </summary>
        public static bool PublishSiteToFolder(QuotedArgsSplitter args, XmlWriter writer)
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
            bpg.SetProperty("DeployTarget", "Package");
            bpg.SetProperty("PackageLocation", @"$(OutDir)\MSDeploy\Package.zip");
            bpg.SetProperty("_PackageTempDir", tempDir + "\\");

            // Web.config transform special sauce:
            bpg.SetProperty("TransformInputFile", @"$(ProjectPath)\Web.config");
            bpg.SetProperty("TransformFile", @"$(ProjectPath)\Web.$(Configuration).config");
            bpg.SetProperty("TransformOutputFile", @"$(DeployPath)\Web.config");

            var success = engine.BuildProjectFile(projectFile, null, bpg);

            if (success)
            {
                writer.WriteComment(" Publish succeeded, logs in " + tempDir.Replace("--", " - -"));
            }
            else
            {
                throw new Exception("Publish failure: see \"" + logFile + "\" for details");
            }

            engine.UnloadAllProjects();
            engine.UnregisterAllLoggers();
        }
    }
}