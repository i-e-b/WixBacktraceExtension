namespace WixBacktraceExtension.Backtrace
{
    using System.Reflection;

    /// <summary>
    /// "name, namespace, version|filepath" for an Assembly
    /// </summary>
    public class AssemblyKey
    {
        readonly string _key;

        public AssemblyKey(Assembly assm)
        {
            var first = assm.FullName.IndexOf(',');
            var second = assm.FullName.IndexOf(',', first + 1);
            _key = assm.FullName.Substring(0, second) + "|" + assm.Location;
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
    }
}