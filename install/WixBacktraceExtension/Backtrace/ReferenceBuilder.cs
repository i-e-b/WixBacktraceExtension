namespace WixBacktraceExtension.Backtrace
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Mono.Cecil;

    public class ReferenceBuilder
    {
        readonly string _filePath;

        public ReferenceBuilder(string filePath)
        {
            _filePath = filePath;
        }

        public IEnumerable<AssemblyKey> NonGacDependencies()
        {
            var result = new HashSet<AssemblyKey>();
            NonGacDependencies(_filePath, result);
            return result;
        }


        static void NonGacDependencies(string srcFilePath, ISet<AssemblyKey> dst)
        {
            var basePath = Path.GetDirectoryName(srcFilePath) ?? "";
            foreach (var next in ReferencesForAssemblyPath(srcFilePath))
            {
                var key = Resolve(basePath, next);
                if (key == null) continue; // we only look in the local folder and below, so we *should* miss GAC assemblies

                if (dst.Contains(key)) return; // already handled

                dst.Add(key);
                NonGacDependencies(key.TargetFilePath, dst);
            }
        }

        static AssemblyKey Resolve(string basePath, AssemblyNameReference name)
        {
            var guess = GuessName(name.FullName);

            var dll = Path.Combine(basePath, guess + ".dll");
            var exe = Path.Combine(basePath, guess + ".exe");

            if (File.Exists(dll))
            {
                return new AssemblyKey(dll, name.FullName);
            }
            if (File.Exists(exe))
            {
                return new AssemblyKey(exe, name.FullName);
            }


            dll = Directory.GetFiles(basePath, guess + ".dll", SearchOption.AllDirectories).FirstOrDefault();
            exe = Directory.GetFiles(basePath, guess + ".exe", SearchOption.AllDirectories).FirstOrDefault();

            if (File.Exists(dll))
            {
                return new AssemblyKey(dll, name.FullName);
            }
            if (File.Exists(exe))
            {
                return new AssemblyKey(exe, name.FullName);
            }
            return null;
        }

        static IEnumerable<AssemblyNameReference> ReferencesForAssemblyPath(string filePath)
        {
            try
            {
                var defn = AssemblyDefinition.ReadAssembly(filePath);
                return defn.Modules.SelectMany(mod => mod.AssemblyReferences).ToList();
            }
            catch
            {
                return new AssemblyNameReference[0];
            }
        }

        public static string GuessName(string fullName)
        {
            var idx = fullName.IndexOfAny(new[] { ' ', ',' });
            if (idx < 1) return fullName;
            return fullName.Substring(0, idx);
        }

        public static AssemblyKey AssemblyKeyForFile(string filePath)
        {
            var defn = AssemblyDefinition.ReadAssembly(filePath);
            return new AssemblyKey(filePath, defn.FullName);
        }
    }
}