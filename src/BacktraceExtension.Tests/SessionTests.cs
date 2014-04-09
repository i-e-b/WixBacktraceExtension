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
            var Expected_A = new HashSet<AssemblyKey>(new[] { new AssemblyKey("key1|a|b|c"), new AssemblyKey("key2|a|b|c") });
            var Expected_B = new HashSet<string>(new[] { "/one", "/two", "/three" });

            Session.Save(Expected_A, Expected_B);

            var Actual_A = new HashSet<AssemblyKey>();
            var Actual_B = new HashSet<string>();

            Session.AlwaysLoad = true;
            Session.Load(Actual_A, Actual_B);
            Session.AlwaysLoad = false;

            Assert.That(Actual_A, Is.EquivalentTo(Expected_A), "Assembly keys");
            Assert.That(Actual_B, Is.EquivalentTo(Expected_B), "written paths");
        }

         
    }
}