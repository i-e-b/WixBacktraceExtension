namespace WixExperimentApp
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.IO;
    using System.Reflection;

    class Program
    {
        static void Main()
        {
            Compose().Run();
        }

        static Program Compose()
        {
            if (!Directory.Exists("./Plugins"))Directory.CreateDirectory("./Plugins");

            using (var catalog = new AggregateCatalog(
                new AssemblyCatalog(Assembly.GetExecutingAssembly()),
                new DirectoryCatalog("./Plugins")
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
            foreach (var plugin in Plugins)
            {
                Console.WriteLine(plugin.Greeting());
            }
            Console.WriteLine("Bye");
            Console.ReadKey();
        }
    }
}
