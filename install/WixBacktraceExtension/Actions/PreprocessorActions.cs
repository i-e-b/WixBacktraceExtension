namespace WixBacktraceExtension.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using global::WixBacktraceExtension.Backtrace;
    using global::WixBacktraceExtension.Configuration;
    using global::WixBacktraceExtension.Extensions;

    public class PreprocessorActions
    {
        protected static readonly object Lock = new object();
        public const string ConditionAlways = "1";
        const string ConditionalComponentTemplate = "<Component Id='{0}' Guid='{1}' Directory='{2}'><Condition><![CDATA[{5}]]></Condition><File Id='{3}' Source='{4}' KeyPath='yes'/></Component>";

        /// <summary>
        /// Build Directory nodes to match those under a given file path.
        /// <para> </para>
        /// argument syntax is `build.directoriesMatching "c:\path\to\copy\" withPrefix "MYPREFIX_"`.
        /// <para> </para>
        /// The `Id` of each directory is the prefix plus the path from the target with underscore separators, all uppercase
        /// (i.e. from the example syntax, "c:\path\to\copy\a\b\c\" will have ID = "MYPREFIX_A_B_C").
        /// If `withPrefix` is excluded, no prefix will be added.
        /// </summary>
        public static bool BuildMatchingDirectories(QuotedArgsSplitter args, XmlWriter writer)
        {
            var target = args.PrimaryRequired();
            var prefix = args.WithDefault("withPrefix", "").TrimEnd('_');

            BuildDirectoriesRecursive(target, target, prefix, writer);

            return true;
        }

        public static void BuildDirectoriesRecursive(string baseDir, string target, string prefix, XmlWriter writer)
        {
            foreach (var dir in Directory.GetDirectories(target))
            {
                var id = dir.Replace(baseDir, prefix).FilterJunk().ToUpperInvariant();
                var name = Path.GetFileName(dir);

                writer.WriteStartElement("Directory");
                writer.WriteAttributeString("Id", id);
                writer.WriteAttributeString("Name", name);
                BuildDirectoriesRecursive(baseDir, dir, prefix, writer);
                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// Build file components for a website published by the backtrace extension. Includes ALL files in the publish folder.
        /// Components are NOT de-duplicated, and so are given guid IDs. You should encapsulate the output of the pragma in a ComponentGroup to reference.
        /// <para> </para>
        /// syntax is `components.publishedWebsiteIn "$(var.PublishTemp)" inDirectoriesWithPrefix "SITE" rootDirectory "SITE_INSTALLFOLDER"`.
        /// The install directories should be built with `build.directoriesMatching` with a matching prefix.
        /// Root directory (for files at the top level of the site folder) should be declared directly in the .wxs file and passed
        /// to this pragma in full.
        /// </summary>
        public static bool BuildPublishedWebsiteComponents(QuotedArgsSplitter args, XmlWriter writer, ICollection<string> writtenPaths, ICollection<AssemblyKey> componentsGenerated)
        {
            var target = args.PrimaryRequired();
            var prefix = args.WithDefault("inDirectoriesWithPrefix", "").TrimEnd('_');
            var root = args.Required("rootDirectory");

            // Special treatment for top-level files:
            foreach (var filePath in Directory.GetFiles(target, "*.*", SearchOption.TopDirectoryOnly))
            {
                var sanitisedName = Path.GetFileName(filePath).FilterJunk();
                var uniqueComponentId = prefix + "_component_" + sanitisedName;
                var uniqueFileId = prefix + "_" + sanitisedName;
                var guid = Guid.NewGuid().ToString();
                writer.WriteRaw(String.Format(ConditionalComponentTemplate, uniqueComponentId, guid, root, uniqueFileId, filePath, ConditionAlways));
            }


            BuildSiteComponentsRecursive(target, target, prefix, writer, writtenPaths, componentsGenerated);

            return true;
        }

        static void BuildSiteComponentsRecursive(string baseDir, string target, string prefix, XmlWriter writer, ICollection<string> writtenPaths, ICollection<AssemblyKey> componentsGenerated)
        {
            foreach (var dir in Directory.GetDirectories(target))
            {
                var directoryId = dir.Replace(baseDir, prefix).FilterJunk().ToUpperInvariant();

                WritePublishedFilesInDirectory(writer, dir, directoryId, writtenPaths, componentsGenerated);
                BuildSiteComponentsRecursive(baseDir, dir, prefix, writer, writtenPaths, componentsGenerated);
            }
        }

        static void WritePublishedFilesInDirectory(XmlWriter writer, string dir, string directoryId, ICollection<string> writtenPaths, ICollection<AssemblyKey> componentsGenerated)
        {
            foreach (var filePath in Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly))
            {
                var uniqueComponentId = "pubcmp_" + Guid.NewGuid().ToString("N");
                var uniqueFileId = "pub_" + Guid.NewGuid().ToString("N");
                var guid = Guid.NewGuid().ToString();

                var fileName = Path.GetFileName(filePath) ?? "";
                var installTarget = directoryId + "/" + fileName;

                if (fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    componentsGenerated.Add(ReferenceBuilder.AssemblyKeyForFile(filePath));
                }

                if (writtenPaths.Contains(installTarget)) continue;
                writtenPaths.Add(installTarget);


                writer.WriteRaw(String.Format(ConditionalComponentTemplate, uniqueComponentId, guid, directoryId, uniqueFileId, filePath, ConditionAlways));
            }
        }

        public static bool ReferenceComponents(string command, QuotedArgsSplitter args, XmlWriter writer)
        {
            var componentRefTemplate = @"<ComponentRef Id='{0}'/>"
                .Replace("'", "\"");

            var dependencies = new ReferenceBuilder(args.Primary).NonGacDependencies().ToList();

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
        /// argument syntax is `build.transformConfigOf "c:\path\to\assembly.exe" for "BuildConfiguration" withId "MyComponentId" in "InstallDirID"`.
        /// The install directory should be declared in the .wxs file.
        /// <para> </para>
        /// Default `for` is "Release", default `in` is "INSTALLFOLDER". All other parameters must be supplied.
        /// </summary>
        public static bool TransformConfiguration(QuotedArgsSplitter args, XmlWriter writer)
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

            writer.WriteRaw(String.Format(ConditionalComponentTemplate,
                componentId,
                Guid.NewGuid(),
                directory,
                "file_" + componentId,
                target,
                ConditionAlways
                ));

            return true;
        }

        /// <summary>
        /// Build file components for a .Net assembly's dependencies.
        /// <para>This DOES NOT include the target assembly itself.</para>
        /// </summary>
        /// <param name="componentsGenerated">mutable list of components that have been build (as they must be unique)</param>
        /// <param name="args">argument syntax is `build.componentsFor "c:\path\to\assembly.exe" in "InstallDirID"`. The install directory should be declared in the .wxs file.</param>
        /// <param name="writer">output writer</param>
        /// <param name="copyDependencies">if true, all dependencies will be copied to target folder. Otherwise, only dependencies not included elsewhere will be added.</param>
        /// <param name="writtenPaths">Path that have files already (will be skipped), in the format `{DirectoryId}/{installed file name}`</param>
        /// <param name="includeTarget">If true, the target will have a component generated. If false, only dependencies will get a component</param>
        public static bool BuildComponents(ICollection<AssemblyKey> componentsGenerated, QuotedArgsSplitter args, XmlWriter writer, ICollection<string> writtenPaths,
            bool copyDependencies, bool includeTarget)
        {
            var target = args.PrimaryRequired();
            var directory = args.WithDefault("in", "INSTALLFOLDER");
            var condition = args.WithDefault("if", "1");

            if (!File.Exists(target))
            {
                writer.WriteRaw("<?error WixBacktraceExtension: Could not find path " + target + " ?>");
            }

            var dependencies = new ReferenceBuilder(target).NonGacDependencies().ToList();

            if (includeTarget)
            {
                dependencies.Add(ReferenceBuilder.AssemblyKeyForFile(target));
            }

            // intention: highest version first.
            dependencies.Sort((b, a) => a.Version.CompareTo(b.Version));

            foreach (var dependencyKey in dependencies)
            {
                var dependency = dependencyKey.ToString();

                var installTarget = directory + "/" + Path.GetFileName(AssemblyKey.FilePath(dependency));
                if (writtenPaths.Contains(installTarget)) continue; // can't write the same target twice.
                writtenPaths.Add(installTarget);

                // Components should be unique to the .msi (can be reset with `components.resetUniqueFilter` pragma call)
                // Component id MUST be unique to the .msi
                if (componentsGenerated.Contains(dependencyKey))
                {
                    if (copyDependencies)
                    {
                        WriteCopy(writer, directory, dependency, condition);
                    }
                }
                else
                {
                    componentsGenerated.Add(dependencyKey);
                    WriteOriginal(writer, dependency, directory, condition);
                }
            }

            return true;
        }

        static void WriteOriginal(XmlWriter writer, string dependency, string directory, string condition)
        {
            var finalLocation = WorkAround255CharPathLimit(AssemblyKey.FilePath(dependency));

            writer.WriteRaw(String.Format(ConditionalComponentTemplate,
                StringExtensions.LimitRight(70, AssemblyKey.ComponentId(dependency)),
                Guid.NewGuid(),
                directory,
                StringExtensions.LimitRight(70, AssemblyKey.FileId(dependency)),
                finalLocation,
                condition));
        }

        static void WriteCopy(XmlWriter writer, string directory, string dependency, string condition)
        {
            var finalLocation = WorkAround255CharPathLimit(AssemblyKey.FilePath(dependency));

            writer.WriteRaw(String.Format(ConditionalComponentTemplate,
                StringExtensions.LimitRight(70, AssemblyKey.ComponentId(dependency) + "_" + Guid.NewGuid().ToString("N")),
                Guid.NewGuid(),
                directory,
                StringExtensions.LimitRight(70, AssemblyKey.FileId(dependency) + "_" + Guid.NewGuid().ToString("N")),
                finalLocation,
                condition));
        }

        static string WorkAround255CharPathLimit(string src)
        {
            lock (Lock)
            {
                var loc = Path.GetFullPath(src);
                var fileName = Path.GetFileName(src);

                if (fileName == null) throw new Exception("Dependency had no file name?");

                if (loc.Length <= 200)
                {
                    return loc;
                }

                var dir = Path.Combine(Session.TempFolder(), "longpath");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var dst = Path.Combine(dir, fileName);

                if (!File.Exists(dst)) File.Copy(src, dst);

                return dst;
            }
        }
    }
}