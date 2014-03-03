namespace WixBacktraceExtension.Backtrace
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// "name, namespace, version|filepath" for an Assembly
    /// </summary>
    public class AssemblyKey : IEquatable<AssemblyKey>
    {
        public override int GetHashCode()
        {
            return (_key != null ? _key.GetHashCode() : 0);
        }

        readonly string _key;

        public AssemblyKey(Assembly assm) : this(assm.Location, assm.FullName) { }

        public AssemblyKey(string filePath, string assemblyFullName)
        {
            var bits = assemblyFullName.Split(',');
            _key = string.Join(",", bits.Take(2)) + "|" + filePath;
            Version = double.Parse(string.Join(".", (bits[1].Split('=')[1]).Split('.').Take(2)));
        }

        public bool Equals(AssemblyKey other)
        {
            return FilePath(_key) == FilePath(other._key)
                || ComponentId(_key) == ComponentId(other._key);
        }

        public override bool Equals(object obj)
        {
            return obj is AssemblyKey && Equals((AssemblyKey)obj);
        }

        public override string ToString()
        {
            return _key;
        }

        /// <summary>
        /// turn an assembly key string into a unique component key name
        /// </summary>
        public static string ComponentId(string keystring)
        {
            var bits = keystring.Split('|');
            return "cmp_" + bits[0].Replace(", Version=", "_").Replace(".", "_");
        }

        /// <summary>
        /// Get the source file path for an assembly key
        /// </summary>
        public static string FilePath(string keystring)
        {
            return keystring.Split('|')[1];
        }

        /// <summary>
        /// turn an assembly key string into a unique file key name
        /// </summary>
        public static string FileId(string keystring)
        {
            var bits = keystring.Split('|');
            return "file_" + bits[0].Replace(", Version=", "_").Replace(".", "_");
        }

        /// <summary>
        /// Major.Minor version of assembly
        /// </summary>
        public double Version { get; private set; }
        public string FileName { get { return Path.GetFileName(FilePath(_key)); } }
        public string TargetFilePath { get { return FilePath(_key); } }
    }
}