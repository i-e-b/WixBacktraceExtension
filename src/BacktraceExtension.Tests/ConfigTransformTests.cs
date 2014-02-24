namespace BacktraceExtension.Tests
{
    using System;
    using System.IO;
    using System.Reflection;
    using NUnit.Framework;
    using WixBacktraceExtension.ConfigTransform;

    [TestFixture]
    public class ConfigTransformTests
    {
        string _srcPath;
        string _targetPath;
        string _transformPath;

        [SetUp]
        public void setup()
        {
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            _srcPath = Path.Combine(basePath, "App.config");
            _transformPath = Path.Combine(basePath, "App.Release.config");
            _targetPath = Path.Combine(basePath, "Output.config");
        }

        [Test]
        public void transform_is_performed_correctly()
        {
            ConfigTransform.Apply(_srcPath, _transformPath, _targetPath);

            Assert.That(File.Exists(_targetPath), Is.True);
            var result = File.ReadAllText(_targetPath);

            Assert.That(result, Contains.Substring("CORRECT"));
            Assert.That(result, Is.Not.StringContaining("WRONG"));

            Console.WriteLine(result);
        }

        [Test]
        public void missing_source_throws_exception()
        {
            Assert.Throws<ArgumentException>(() => ConfigTransform.Apply("bgoo", _transformPath, _targetPath));
        }
        [Test]
        public void missing_transform_throws_exception()
        {
            Assert.Throws<ArgumentException>(() => ConfigTransform.Apply(_srcPath, "mwha", _targetPath));
        }
        [Test]
        public void inaccessible_target_throws_exception()
        {
            Assert.Throws<ArgumentException>(() => ConfigTransform.Apply(_srcPath, _transformPath, "\\INVISIBULSHARE\booga"));
        }



    }
}