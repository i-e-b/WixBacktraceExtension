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

        [Test]
        public void limit_right_works()
        {
            Assert.That(StringExtensions.LimitRight(5, "1234567890"), Is.EqualTo("67890"));
            Assert.That(StringExtensions.LimitRight(15, "1234567890"), Is.EqualTo("1234567890"));
        }

        [Test]
        public void how_get_extension_works()
        {
            Assert.That(Path.GetExtension(@"C:\temp\myfile.dll"), Is.EqualTo(".dll"));
            Assert.That(Path.GetExtension(@"C:\temp\myfile.DLL"), Is.EqualTo(".DLL"));
        }

        [Test]
        public void file_extension_splitter()
        {
            Assert.That("dll,*.txt|.xml".SplitFileExtensions(), Is.EquivalentTo(new[] { ".dll", ".txt", ".xml" }));
        }


    }
}