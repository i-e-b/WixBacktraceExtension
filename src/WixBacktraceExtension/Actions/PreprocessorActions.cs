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

    /// <summary>
    /// Backtrace core
    /// </summary>
    public class PreprocessorActions
    {
        /// <summary>
        /// Lock around actions, to ensure serialisability
        /// </summary>
        protected static readonly object Lock = new object();
        /// <summary>
        /// Wix condition representing always-true
        /// </summary>
        public const string ConditionAlways = "1";
        const string ComponentWithDirectoryTemplate="<Component Id='{0}' Guid='{1}' Directory='{2}'><Condition><![CDATA[{5}]]></Condition><File Id='{3}' Source='{4}' KeyPath='yes'/></Component>";
        const string ComponentNoDirectoryTemplate = "<Component Id='{0}' Guid='{1}'><Condition><![CDATA[{5}]]></Condition><File Id='{3}' Source='{4}' KeyPath='yes'/></Component>";

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

        /// <summary>
        /// Build a set of directory tags to match an on-disk folder hierarchy
        /// </summary>
        /// <param name="baseDir">Relative base for output</param>
        /// <param name="target">on-disk target directory</param>
        /// <param name="prefix">prefix of Directory tag ID</param>
        /// <param name="writer">output XML writer</param>
        public static void BuildDirectoriesRecursive(string baseDir, string target, string prefix, XmlWriter writer)
        {
            foreach (var dir in Directory.GetDirectories(target))
            {
                if (string.IsNullOrEmpty(dir)) continue;

                var id = IdForDirectory(baseDir, prefix, dir);
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
        public static bool BuildPublishedWebsiteComponents(string targetPath, QuotedArgsSplitter args, XmlWriter writer, ICollection<string> writtenPaths, ICollection<AssemblyKey> componentsGenerated)
        {
            var target = args.PrimaryRequired();
            var prefix = args.WithDefault("inDirectoriesWithPrefix", "").TrimEnd('_');
            var root = args.Required("rootDirectory");
            var ignore = args.WithDefault("ignoreExtensions", "").SplitFileExtensions();

            // Special treatment for top-level files:
            foreach (var filePath in Directory.GetFiles(target, "*.*", SearchOption.TopDirectoryOnly))
            {
                if (ignore.Any(filePath.EndsWith)) continue;

                var sanitisedName = Path.GetFileName(filePath).FilterJunk();
                var uniqueComponentId = StringExtensions.LimitRight(70, prefix + "_" + sanitisedName + "C");
                var uniqueFileId = StringExtensions.LimitRight(70, prefix + "_" + sanitisedName);
                var guid = NewUpperGuid();
                writer.WriteRaw(String.Format(ComponentWithDirectoryTemplate, uniqueComponentId, guid, root, uniqueFileId, filePath, ConditionAlways));
            }

            BuildSiteComponentsRecursive(targetPath, target, target, prefix, writer, writtenPaths, componentsGenerated, ignore);

            return true;
        }

        static void BuildSiteComponentsRecursive(string targetpath, string baseDir, string target, string prefix, XmlWriter writer, ICollection<string> writtenPaths, ICollection<AssemblyKey> componentsGenerated, ICollection<string> ignore)
        {
            foreach (var dir in Directory.GetDirectories(target))
            {
                var directoryId = IdForDirectory(baseDir, prefix, dir);

                WritePublishedFilesInDirectory(targetpath, writer, dir, directoryId, writtenPaths, componentsGenerated, ignore);
                BuildSiteComponentsRecursive(targetpath, baseDir, dir, prefix, writer, writtenPaths, componentsGenerated, ignore);
            }
        }

        static void WritePublishedFilesInDirectory(string targetPath, XmlWriter writer, string dir, string directoryId, ICollection<string> writtenPaths, ICollection<AssemblyKey> componentsGenerated, ICollection<string> ignore)
        {
            foreach (var filePath in Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly))
            {
                if (ignore.Any(filePath.EndsWith)) continue;

                var finalLocation = WorkAround255CharPathLimit(targetPath, filePath);
                var uniqueComponentId = "pubc" + NewUpperId();
                var uniqueFileId = "pub" + NewUpperId();
                var guid = NewUpperGuid();

                var fileName = Path.GetFileName(filePath);
                var installTarget = directoryId + "/" + fileName;

                if (fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    componentsGenerated.Add(ReferenceBuilder.AssemblyKeyForFile(filePath));
                }

                if (writtenPaths.Contains(installTarget)) continue;
                writtenPaths.Add(installTarget);

                writer.WriteRaw(String.Format(ComponentWithDirectoryTemplate, uniqueComponentId, guid, directoryId, uniqueFileId, finalLocation, ConditionAlways));
            }
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
            var directory = args.WithDefault("in", null);
            var config = args.WithDefault("for", "Release");
            var componentId = args.Required("withId");

            var transformPath = Path.Combine(Path.GetDirectoryName(target) ?? "", "App." + config + ".config");
            var original = target + ".original";

            if (!File.Exists(target)) throw new Exception("Expected to find \""+target+"\" but it was missing");
            if (!File.Exists(transformPath)) throw new Exception("Expected to find transform at \"" + transformPath + "\" but it was missing");

            File.Copy(target, original, true);
            ConfigTransform.Apply(original, transformPath, target);

            var template = (directory != null) ? (ComponentWithDirectoryTemplate) : (ComponentNoDirectoryTemplate);

            writer.WriteRaw(String.Format(template,
                componentId,
                Guid.NewGuid().ToString().ToUpper(),
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
        /// <param name="copyDuplicateDependencies">if true, all dependencies will be copied to target folder. Otherwise, only dependencies not included elsewhere will be added.</param>
        /// <param name="writtenPaths">Path that have files already (will be skipped), in the format `{DirectoryId}/{installed file name}`</param>
        /// <param name="includeTarget">If true, the target will have a component generated. If false, only dependencies will get a component</param>
        public static bool BuildComponents(string targetPath, ICollection<AssemblyKey> componentsGenerated, QuotedArgsSplitter args, XmlWriter writer, ICollection<string> writtenPaths, bool copyDuplicateDependencies, bool includeTarget)
        {
            var target = args.PrimaryRequired();
            var directory = args.WithDefault("in", null);
            var condition = args.WithDefault("if", "1");
            var setName = args.WithDefault("dependencySet", "");

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
                    if (copyDuplicateDependencies)
                    {
                        WriteCopy(targetPath, writer, directory, dependency, condition, setName);
                    }
                }
                else
                {
                    componentsGenerated.Add(dependencyKey);
                    WriteOriginal(targetPath, writer, dependency, directory, condition, setName);
                }
            }

            return true;
        }

        static void WriteOriginal(string targetPath, XmlWriter writer, string dependency, string directory, string condition, string setName)
        {
            var finalLocation = WorkAround255CharPathLimit(targetPath, AssemblyKey.FilePath(dependency));

            var template = (directory != null) ? (ComponentWithDirectoryTemplate) : (ComponentNoDirectoryTemplate);

            writer.WriteRaw(String.Format(template,
                StringExtensions.LimitRight(70, AssemblyKey.ComponentId(dependency, setName)),
                NewUpperGuid(),
                directory,
                StringExtensions.LimitRight(70, AssemblyKey.FileId(dependency, setName)),
                finalLocation,
                condition));
        }

        static void WriteCopy(string targetPath, XmlWriter writer, string directory, string dependency, string condition, string setName)
        {
            var finalLocation = WorkAround255CharPathLimit(targetPath, AssemblyKey.FilePath(dependency));

            var template = (directory != null) ? (ComponentWithDirectoryTemplate) : (ComponentNoDirectoryTemplate);

            writer.WriteRaw(String.Format(template,
                StringExtensions.LimitRight(70, AssemblyKey.ComponentId(dependency, setName) + NewUpperId()).ToUpper(),
                NewUpperGuid(),
                directory,
                StringExtensions.LimitRight(70, AssemblyKey.FileId(dependency, setName) + NewUpperId()).ToUpper(),
                finalLocation,
                condition));
        }

        /// <summary>
        /// If src file path is less than 250 charactes, it is returned as-is.
        /// <para>Otherwise, it is copied to another location with a shorter path, and that path is returned.</para>
        /// </summary>
        static string WorkAround255CharPathLimit(string targetPath, string src)
        {
            lock (Lock)
            {
                var loc = Path.GetFullPath(src);
                var fileName = Path.GetFileName(src);

                if (fileName == null) throw new Exception("Dependency had no file name?");

                if (loc.Length <= 250)
                {
                    return loc;
                }
                
                var dir = Path.Combine(targetPath, "longpath");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                var dst = Path.Combine(dir, fileName);

                if (!File.Exists(dst)) File.Copy(src, dst);

                return dst;
            }
        }

        /// <summary> New guid, with '-', all upper case </summary>
        static string NewUpperGuid()
        {
            return Guid.NewGuid().ToString().ToUpper();
        }
        /// <summary> New guid, without '-', prepended with '_', all upper case </summary>
        static string NewUpperId()
        {
            return "_" + Guid.NewGuid().ToString("N").ToUpper();
        }

        /// <summary>
        /// Convert a directory path and prefix into a well-known directory identifier of 72 chars or less
        /// </summary>
        static string IdForDirectory(string baseDir, string prefix, string dir)
        {
            return (dir.Replace(baseDir, prefix).FilterJunk().ToUpperInvariant().Replace("__","_").LimitRight(72));
        }
    }
}