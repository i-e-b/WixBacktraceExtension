namespace WixBacktraceExtension
{
    using System;
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

        public IEnumerable<Assembly> NonGacDependencies()
        {
            return NonGacDependencies(_assm);
        }

        static IEnumerable<Assembly> NonGacDependencies(Assembly src)
        {
            foreach (var next in
                src.GetReferencedAssemblies().Select(refd => NonGacAndReal(src, refd)).Where(assm => assm != null)
                )
            {
                yield return next;
                foreach (var child in NonGacDependencies(next))
                {
                    yield return child;
                }
            }
        }


        static Assembly NonGacAndReal(Assembly src, AssemblyName dep)
        {
            try
            {
                File.AppendAllText(@"C:\Temp\log", "\r\nLooking for " + dep.FullName + ", maybe " + dep.CodeBase + " or near " + src.Location);
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
                    File.AppendAllText(@"C:\Temp\log", "ignored.");
                    return null;
                }
                if (string.IsNullOrEmpty(location))
                {
                    File.AppendAllText(@"C:\Temp\log", "not found.");
                    return null;
                }

                File.AppendAllText(@"C:\Temp\log", "found.");
                return real;
            }
            catch (Exception ex)
            {
                File.AppendAllText(@"C:\Temp\log", "\r\n\tError: " + ex.GetType() + ": " + ex.Message + "\r\n\r\n" + ex.StackTrace);
                return null;
            }
        }

        private static Assembly LookupReferencedAssemblyInternal(string basePath, AssemblyName name)
        {
            var guess = GuessName(name.FullName);
            var directoryName = Path.GetDirectoryName(basePath) ?? basePath;
            var dll = Path.Combine(directoryName, guess + ".dll");
            var exe = Path.Combine(directoryName, guess + ".exe");

            File.AppendAllText(@"C:\Temp\log", " guessing '"+dll+"' or '"+exe+"' ");

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