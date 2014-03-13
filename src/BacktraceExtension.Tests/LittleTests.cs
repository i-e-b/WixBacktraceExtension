namespace BacktraceExtension.Tests
{
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


    }
}