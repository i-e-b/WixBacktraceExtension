namespace WixBacktraceExtension.Backtrace
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    public class ReferenceBuilder
    {
        readonly Assembly _assm;

        public ReferenceBuilder(Assembly assm)
        {
            _assm = assm;
        }

        public IEnumerable<AssemblyKey> NonGacDependencies()
        {
            var result = new HashSet<Assembly>();
            NonGacDependencies(_assm, result);
            return result.Select(a=> new AssemblyKey(a));
        }

        static void NonGacDependencies(Assembly src, ISet<Assembly> dst)
        {
            foreach (var next in
                src.GetReferencedAssemblies().Select(refd => NonGacAndReal(src, refd)).Where(assm => assm != null)
                )
            {
                dst.Add(next);
                NonGacDependencies(next, dst);
            }
        }


        static Assembly NonGacAndReal(Assembly src, AssemblyName dep)
        {
            try
            {
                Assembly real;
                try
                {
                    real = Assembly.ReflectionOnlyLoad(dep.FullName);
                }
                catch
                {
                    real = LookupReferencedAssemblyInternal(src.Location, dep);
                }
                var location = real.Location;
                if (real.GlobalAssemblyCache)
                {
                    return null;
                }
                if (string.IsNullOrEmpty(location))
                {
                    return null;
                }

                return real;
            }
            catch
            {
                return null;
            }
        }

        private static Assembly LookupReferencedAssemblyInternal(string basePath, AssemblyName name)
        {
            var guess = GuessName(name.FullName);
            var directoryName = Path.GetDirectoryName(basePath) ?? basePath;
            var dll = Path.Combine(directoryName, guess + ".dll");
            var exe = Path.Combine(directoryName, guess + ".exe");

            if (File.Exists(dll))
            {
                return Assembly.ReflectionOnlyLoadFrom(dll);
            }
            if (File.Exists(exe))
            {
                return Assembly.ReflectionOnlyLoadFrom(exe);
            }
            return null;
        }

        public static string GuessName(string fullName)
        {
            var idx = fullName.IndexOfAny(new[] { ' ', ',' });
            if (idx < 1) return fullName;
            return fullName.Substring(0, idx);
        }
    }
}