namespace BacktraceExtension.Tests
{
    using System.IO;
    using NUnit.Framework;
    using WixBacktraceExtension.Extensions;

    [TestFixture]
    public class LittleTests
    {
        [Test]
        public void last_element_of_path()
        {
            var last1 = @"C:\Projects\WixExperiment\install\SetupProject".LastPathElement();
            var last2 = @"C:\Projects\WixExperiment\install\SetupProject\".LastPathElement();

            Assert.That(last1, Is.EqualTo(last2));
        }

         
    }
}