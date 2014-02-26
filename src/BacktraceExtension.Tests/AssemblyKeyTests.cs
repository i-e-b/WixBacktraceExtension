namespace BacktraceExtension.Tests
{
    using System.Reflection;
    using NUnit.Framework;
    using WixBacktraceExtension.Backtrace;

    [TestFixture]
    public class AssemblyKeyTests
    {
        [Test]
        public void assembly_is_keyed_by_name_and_version_and_location()
        {
            var target = Assembly.GetExecutingAssembly();
            var subject = new AssemblyKey(target);

            Assert.That(subject.ToString(), Is.EqualTo("BacktraceExtension.Tests, Version=1.0.0.0|" + target.Location));
        }

        [Test]
        public void assembly_version_is_read()
        {
            var target = Assembly.GetExecutingAssembly();
            var subject = new AssemblyKey(target);

            Assert.That(subject.Version, Is.EqualTo(1.0d));
        }

        [Test]
        public void assembly_file_name_is_read()
        {
            var target = Assembly.GetExecutingAssembly();
            var subject = new AssemblyKey(target);

            Assert.That(subject.FileName, Is.EqualTo("BacktraceExtension.Tests.dll"));
        }

        [Test]
        public void can_get_component_key_from_assembly_key_string()
        {
            const string keystring = @"BacktraceExtension.Tests, Version=1.0.0.0|C:\path\file.dll";
            const string expected = @"cmp_BacktraceExtension_Tests_1_0_0_0";

            Assert.That(AssemblyKey.ComponentId(keystring), Is.EqualTo(expected));
        }

        [Test]
        public void can_get_file_key_from_assembly_key_string()
        {
            const string keystring = @"BacktraceExtension.Tests, Version=1.0.0.0|C:\path\file.dll";
            const string expected = @"file_BacktraceExtension_Tests_1_0_0_0";

            Assert.That(AssemblyKey.FileId(keystring), Is.EqualTo(expected));
        }

        [Test]
        public void can_get_file_path_from_assembly_key_string()
        {
            const string keystring = @"BacktraceExtension.Tests, Version=1.0.0.0|C:\path\file.dll";
            const string expected = @"C:\path\file.dll";

            Assert.That(AssemblyKey.FilePath(keystring), Is.EqualTo(expected));
        }
         
    }
}