using global::WixBacktraceExtension.Backtrace;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WixBacktraceExtension
{
    /// <summary>
    /// Saves and restores Wix processing sessions
    /// </summary>
    /// <remarks>
    /// Wix `candle.exe` calls into <c>this</c> plug in, and is call one for every source file. This
    /// means we can't do our unique item tracing between source files easily. To remedy,
    /// we save our data across sessions with a short timeout
    /// </remarks>
    public static class Session
    {
        /// <summary>
        /// Save state to file
        /// </summary>
        public static void Save(string targetPath, Dictionary<string, HashSet<AssemblyKey>> componentsGenerated, Dictionary<string, HashSet<string>> pathsInstalledTo)
        {
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            var sessionFile = Path.Combine(targetPath, "session.txt");

            var data = new SessionData
                {
                    WriteTime = DateTime.UtcNow,
                    Components = componentsGenerated,
                    Paths = pathsInstalledTo
                };

            File.WriteAllText(sessionFile, JsonConvert.SerializeObject(data));
        }

        /// <summary>
        /// For testing. Set to <c>true</c> to load session even if it's stale.
        /// </summary>
        public static bool AlwaysLoad = false;

        /// <summary>
        /// Load state from file
        /// </summary>
        public static void Load(string targetPath, Dictionary<string, HashSet<AssemblyKey>> componentsGenerated, Dictionary<string, HashSet<string>> pathsInstalledTo)
        {
            if (!Directory.Exists(targetPath))
            {
                return;
            }

            var sessionFile = Path.Combine(targetPath, "session.txt");

            if (!File.Exists(sessionFile)) return;

            var data = JsonConvert.DeserializeObject<SessionData>(File.ReadAllText(sessionFile));

            if (!AlwaysLoad && (NoBuildOutputs() || SessionIsTooOld(data)))
            {
                DeleteDirectory(targetPath);

                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                }

                return;
            }

            componentsGenerated.Clear();

            foreach (var kvp in data.Components)
            {
                componentsGenerated.Add(kvp.Key, kvp.Value);
            }

            pathsInstalledTo.Clear();

            foreach (var kvp in data.Paths)
            {
                pathsInstalledTo.Add(kvp.Key, kvp.Value);
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
        /// <see cref="Directory"/>.Delete("...", true) has quite a few bugs.
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
            /// <see cref="File"/> time
            /// </summary>
            public DateTime WriteTime { get; set; }

            /// <summary>
            /// Written components
            /// </summary>
            public Dictionary<string, HashSet<AssemblyKey>> Components { get; set; }

            /// <summary> 
            /// Written paths
            /// </summary>
            public Dictionary<string, HashSet<string>> Paths { get; set; }
        }
    }
}