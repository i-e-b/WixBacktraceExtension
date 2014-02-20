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
            var assm = Assembly.ReflectionOnlyLoadFrom(@"C:\Projects\WixExperiment\src\WixExperimentApp\bin\Debug\WixExperimentApp.exe");
            _subject = new ReferenceBuilder(assm);
        }

        [Test]
        public void should_get_assembly_references ()
        {
            var result = _subject.NonGacDependencies().Select(a=>ReferenceBuilder.GuessName(a.FullName)).ToList();

            Assert.That(result, Contains.Item("LessStupidPath"));
            Assert.That(result, Contains.Item("ThirdParty"));
            Assert.That(result, Is.Not.Contains("WixExperimentApp"));
        }

        [Test]
        public void can_get_dll_name()
        {
            var name = ReferenceBuilder.GuessName(Assembly.GetExecutingAssembly().FullName);
            Assert.That(name, Is.EqualTo("BacktraceExtension.Tests"));
        }


    }
}
