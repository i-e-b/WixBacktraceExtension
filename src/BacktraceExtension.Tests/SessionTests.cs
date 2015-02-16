using System.IO;

namespace BacktraceExtension.Tests
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using WixBacktraceExtension;
    using WixBacktraceExtension.Backtrace;

    [TestFixture]
    public class SessionTests
    {

        [Test]
        public void can_save_and_restore_a_session()
        {
            var Expected_Components_A = new HashSet<AssemblyKey>(new[] { new AssemblyKey("key1|a|b|c"), new AssemblyKey("key2|a|b|c") });
            var Expected_Components_B = new HashSet<AssemblyKey>(new[] { new AssemblyKey("key1|x|y|z"), new AssemblyKey("key2|x|y|z") });
            var Expected_Paths = new HashSet<string>(new[] { "/one", "/two", "/three" });

            var Expected_Component_Set = new Dictionary<string, HashSet<AssemblyKey>> {
                {"default", Expected_Components_A},
                {"other", Expected_Components_B}
            };

            var Expected_Paths_Set = new Dictionary<string, HashSet<string>> {
                {"default", Expected_Paths},
                {"other", Expected_Paths}
            };
            
            var Actual_Components =  new Dictionary<string, HashSet<AssemblyKey>>();
            var Actual_Paths =  new Dictionary<string, HashSet<string>>();
            var tempFolder = Directory.GetCurrentDirectory();
            
            Session.Save(tempFolder, Expected_Component_Set, Expected_Paths_Set);

            Session.AlwaysLoad = true;
            Session.Load(tempFolder, Actual_Components, Actual_Paths);
            Session.AlwaysLoad = false;

            Assert.That(Actual_Components["default"], Is.EquivalentTo(Expected_Components_A), "Assembly keys");
            Assert.That(Actual_Components["other"], Is.EquivalentTo(Expected_Components_B), "Assembly keys");
            Assert.That(Actual_Paths["default"], Is.EquivalentTo(Expected_Paths), "written paths");
            Assert.That(Actual_Paths["other"], Is.EquivalentTo(Expected_Paths), "written paths");
        }

         
    }
}