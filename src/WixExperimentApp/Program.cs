namespace WixExperimentApp
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.IO;
    using System.Reflection;
    using PluginContract;
    using ThirdParty;

    class Program
    {
        static void Main()
        {
            Compose().Run();
        }

        static Program Compose()
        {
            var plugDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".", "Plugins");
            if (!Directory.Exists(plugDir)) Directory.CreateDirectory(plugDir);

            using (var catalog = new AggregateCatalog(
                new AssemblyCatalog(Assembly.GetExecutingAssembly()),
                new DirectoryCatalog(plugDir)
                ))
            {

                using (var container = new CompositionContainer(catalog))
                {
                    var composedProgram = new Program();
                    container.SatisfyImportsOnce(composedProgram);
                    return composedProgram;
                }
            }
        }

        [ImportMany(typeof(IPluginContract))]
        public IEnumerable<IPluginContract> Plugins { get; set; }

        void Run()
        {
            Console.WriteLine("Hello from the core program!");
            Console.WriteLine("Installed at " + (new UltraImportant3rdPartyClass().WhereIAm()));
            foreach (var plugin in Plugins)
            {
                Console.WriteLine(plugin.Greeting());
            }
            Console.WriteLine("Bye");
            Console.ReadKey();
        }
    }
}
