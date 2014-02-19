namespace ThirdParty
{
    using System.IO;
    using System.Reflection;

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
            return new FilePath(Assembly.GetExecutingAssembly().Location)
                .ToEnvironmentalPathWithoutFileName();
        }
    }
}
