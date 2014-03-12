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
    public class Session
    {
        /// <summary>
        /// Give a temp directory for this project
        /// </summary>
        public static string TempFolder()
        {
            var working = Directory.GetCurrentDirectory(); // Wix puts the working directory to the .wixproj location
            var key = working.CRC32();
            var name = working.LastPathElement();
            return Path.Combine(Path.GetTempPath(), "BacktraceTemp_" + name + "_" + key.ToString("X"));
        }

        public static void Save(List<AssemblyKey> componentsGenerated, List<string> pathsInstalledTo)
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
                    Paths = pathsInstalledTo
                });
            }
        }

        public static void Load(List<AssemblyKey> componentsGenerated, List<string> pathsInstalledTo)
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
            if (DateTime.UtcNow - data.WriteTime > TimeSpan.FromSeconds(30)) // time allowed between the end of one session and the start of another.
            {
                // Too old, start again.
                DeleteDirectory(TempFolder());
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

        [Serializable]
        public class SessionData
        {
            public DateTime WriteTime { get; set; }
            public List<string> Components { get; set; }
            public List<string> Paths { get; set; }
        }
    }
}