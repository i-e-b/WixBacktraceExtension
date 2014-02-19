namespace WixBacktraceExtension
{
    using System.Collections.Generic;
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
                src.GetReferencedAssemblies().Select(NonGacAndReal).Where(assm => assm != null)
                )
            {
                yield return next;
                foreach (var child in NonGacDependencies(next))
                {
                    yield return child;
                }
            }
        }


        static Assembly NonGacAndReal(AssemblyName dep)
        {
            try
            {
                var real = Assembly.Load(dep.FullName);
                var location = real.Location;
                if (string.IsNullOrEmpty(location)) return null;
                location = location.Replace("file://", "");

                if (location.ToLowerInvariant().Contains(@"\windows\microsoft.net")) return null;
                if (location.Contains("GAC_MSIL")) return null;

                return real;
            }
            catch
            {
                return null;
            }
        }
    }
}