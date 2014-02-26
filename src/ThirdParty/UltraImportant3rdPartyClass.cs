namespace ThirdParty
{
    using System.IO;
    using System.Reflection;
    using Newtonsoft.Json;

    /// <summary>
    /// Example of a chain of dependencies which are not direct dependencies
    /// </summary>
    public sealed class UltraImportant3rdPartyClass
    {
        /// <summary>
        /// Returns a string representation of current installed location
        /// </summary>
        public string WhereIAm()
        {
            var str = JsonConvert.DeserializeAnonymousType("{\"Hello\":\"World\"}", new { Hello = "what?" });

            return new FilePath(Assembly.GetExecutingAssembly().Location)
                .ToEnvironmentalPathWithoutFileName() + "; Hello, " + str.Hello;
        }
    }
}
