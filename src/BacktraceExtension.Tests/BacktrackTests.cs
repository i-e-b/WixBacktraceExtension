namespace BacktraceExtension.Tests
{
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using WixBacktraceExtension;

    [TestFixture]
    public class BacktrackTests
    {
        ReferenceBuilder _subject;

        [SetUp]
        public void setup()
        {
            _subject = new ReferenceBuilder(Assembly.GetExecutingAssembly());
        }

        [Test]
        public void should_get_assembly_references ()
        {
            var result = _subject.NonGacDependencies().ToList();

            Assert.That(result, Is.Not.Empty);
        }

    }
}
