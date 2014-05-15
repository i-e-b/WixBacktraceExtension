namespace WixBacktraceExtension
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Serialization;
    using global::WixBacktraceExtension.Backtrace;
    using global::WixBacktraceExtension.Extensions;

    /// <summary>
    /// Saves and restores Wix processing sessions
    /// </summary>
    /// <remarks>
    /// Wix `candle.exe` calls into this plugin, and is call one for every source file. This
    /// means we can't do our unique item tracing between source files easily. To remedy,
    /// we save our data across sessions with a short timeout
    /// </remarks>
    public static class Session
    {
        /// <summary>
        /// Give a temp directory for this project
        /// </summary>
        public static string TempFolder()
        {
            var working = Directory.GetCurrentDirectory(); // Wix puts the working directory to the .wixproj location
            var key = working.CRC32();
            var name = working.LastPathElement().LimitRight(15);
            return Path.Combine(Path.GetTempPath(), "Backtrace_" + name + "_" + key.ToString("X"));
        }

        /// <summary>
        /// Save state to file
        /// </summary>
        public static void Save(ICollection<AssemblyKey> componentsGenerated, ICollection<string> pathsInstalledTo)
        {
            if (!Directory.Exists(TempFolder())) Directory.CreateDirectory(TempFolder());

            var sessionFile = Path.Combine(TempFolder(), "session.txt");
            var ser = new XmlSerializer(typeof(SessionData));

            using (var fs = File.Create(sessionFile))
            {
                ser.Serialize(fs, new SessionData
                {
                    WriteTime = DateTime.UtcNow,
                    Components = componentsGenerated.Select(ak=>ak.ToString()).ToList(),
                    Paths = pathsInstalledTo.ToList()
                });
            }
        }

        /// <summary>
        /// For testing. Set to true to load session even if it's stale.
        /// </summary>
        public static bool AlwaysLoad = false;

        /// <summary>
        /// Load state from file
        /// </summary>
        public static void Load(ICollection<AssemblyKey> componentsGenerated, ICollection<string> pathsInstalledTo)
        {
            if (!Directory.Exists(TempFolder())) return;
            var sessionFile = Path.Combine(TempFolder(), "session.txt");
            if (!File.Exists(sessionFile)) return;

            var ser = new XmlSerializer(typeof(SessionData));
            SessionData data;
            using (var fs = File.Open(sessionFile, FileMode.Open))
            {
                data = (SessionData)ser.Deserialize(fs);
            }

            if (!AlwaysLoad &&
                   (NoBuildOutputs() || SessionIsTooOld(data))
               )
            {
                // First session, start again.
                DeleteDirectory(TempFolder());
                if (!Directory.Exists(TempFolder())) Directory.CreateDirectory(TempFolder());
                return;
            }

            foreach (var ak in data.Components.Select(s => new AssemblyKey(s)))
            {
                if (!componentsGenerated.Contains(ak)) componentsGenerated.Add(ak);
            }
            foreach (var path in data.Paths)
            {
                if (!pathsInstalledTo.Contains(path)) pathsInstalledTo.Add(path);
                
            }
        }

        static bool SessionIsTooOld(SessionData sessionData)
        {
            return DateTime.UtcNow - sessionData.WriteTime > TimeSpan.FromMinutes(5);
        }

        /// <summary>
        /// If there are no build outputs, we are in the first call to "candle.exe" and we should clear the temp output
        /// <para>Otherwise, we are a chained output and we should just read the session</para>
        /// </summary>
        static bool NoBuildOutputs()
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "obj");
            if (!Directory.Exists(dir)) return true;

            var dirs = Directory.EnumerateDirectories(dir).Select(p => new DirectoryInfo(p)).ToList();
            dirs.Sort((a, b) => b.LastWriteTimeUtc.CompareTo(a.LastWriteTimeUtc));

            if (!dirs.Any()) return true;

            return !Directory.GetFiles(dirs.First().FullName, "*.wixobj", SearchOption.AllDirectories).Any();
        }


        /// <summary>
        /// A strong recursive directory delete.
        /// Directory.Delete("...", true) has quite a few bugs.
        /// </summary>
        /// <param name="target">Directory to delete</param>
        static void DeleteDirectory(string target)
        {
            var files = Directory.GetFiles(target);
            var dirs = Directory.GetDirectories(target);

            foreach (var file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (var dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(target, false);
        }

        /// <summary>
        /// Saved data
        /// </summary>
        [Serializable]
        public class SessionData
        {
            /// <summary>
            /// File time
            /// </summary>
            public DateTime WriteTime { get; set; }
            /// <summary>
            /// Written components
            /// </summary>
            public List<string> Components { get; set; }
            /// <summary>
            /// Written paths
            /// </summary>
            public List<string> Paths { get; set; }
        }
    }
}