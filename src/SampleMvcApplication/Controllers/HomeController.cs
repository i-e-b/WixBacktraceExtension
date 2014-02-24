namespace SampleMvcApplication.Controllers
{
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.IO;
    using System.Linq;
    using System.Web.Hosting;
    using System.Web.Mvc;
    using PluginContract;

    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var lookIn = Path.Combine(HostingEnvironment.ApplicationPhysicalPath, "bin\\Plugins");
            var plugins = PluginLoader.Compose(lookIn).Plugins.Select(p => p.Greeting()).ToList();

            var message = (plugins.Count < 1) ? ("No plugins loaded") : (string.Join(", ", plugins));

            return View(new Greets { PluginGreetings = message, LookIn = lookIn });
        }
    }

    public class Greets
    {
        public string PluginGreetings { get; set; }
        public string LookIn { get; set; }
    }

    public class PluginLoader
    {
        public static PluginLoader Compose(string lookIn)
        {
            if (!Directory.Exists(lookIn))
            {
                return new PluginLoader { Plugins = new List<IPluginContract>() };
            }

            using (var catalog = new AggregateCatalog(
                new AssemblyCatalog(typeof(PluginLoader).Assembly),
                new DirectoryCatalog(lookIn)
                ))
            {

                using (var container = new CompositionContainer(catalog))
                {
                    var composedProgram = new PluginLoader();
                    container.SatisfyImportsOnce(composedProgram);
                    return composedProgram;
                }
            }
        }

        [ImportMany(typeof(IPluginContract))]
        public IEnumerable<IPluginContract> Plugins { get; set; }

    }
}
