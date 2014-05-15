namespace BacktraceExtension.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using NUnit.Framework;
    using WixBacktraceExtension.Actions;
    using WixBacktraceExtension.Backtrace;
    using WixBacktraceExtension.Extensions;

    [TestFixture]
    public class PreprocessorTests
    {
        ReferenceBuilder _subject;

        [SetUp]
        public void setup()
        {
            _subject = new ReferenceBuilder(@"C:\Gits\WixExperiment\src\WixExperimentApp\bin\Debug\WixExperimentApp.exe");
        }

        [Test]
        public void reference_builder_can_target_single_file()
        {
            var key = ReferenceBuilder.AssemblyKeyForFile(Assembly.GetExecutingAssembly().Location);
            Assert.That(key.ToString(), Is.EqualTo("BacktraceExtension.Tests, Version=1.0.0.0|" + Assembly.GetExecutingAssembly().Location));
        }


        [Test]
        public void should_get_assembly_references ()
        {
            var result = _subject.NonGacDependencies().Select(a => ReferenceBuilder.GuessName(a.ToString())).ToList();

            Console.WriteLine(string.Join(",", result));

            Assert.That(result, Contains.Item("LessStupidPath"), "Missing NuGet dependency");
            Assert.That(result, Contains.Item("ThirdParty"), "Missing direct dependency");
            Assert.That(result, Is.Not.Contains("System"), "Returned original assembly, could lead to duplicates");
            Assert.That(result, Is.Not.Contains("WixExperimentApp"), "Returned original assembly, could lead to duplicates");
        }

        [Test]
        public void can_get_dll_name()
        {
            var name = ReferenceBuilder.GuessName(Assembly.GetExecutingAssembly().FullName);
            Assert.That(name, Is.EqualTo("BacktraceExtension.Tests"));
        }

        [Test]
        public void can_filter_folder_names()
        {
            const string src =     @"C:\this is - very\wrong/very/very |?* wrong.";
            const string expected = "C__this_is___very_wrong_very_very_____wrong_";

            Assert.That(src.FilterJunk(), Is.EqualTo(expected));
        }

        [Test]
        public void directory_builder_makes_valid_xml()
        {
            var root = Path.GetFullPath(".");
            if (Directory.Exists(root + "/one")) Directory.Delete(root + "/one", true);
            Directory.CreateDirectory(root + "/one/2.5/3.75");
            Directory.CreateDirectory(root + "/one/two/3.5/four");
            Directory.CreateDirectory(root + "/one/two/three/four");

            var sb = new StringBuilder();
            var writer = XmlWriter.Create(sb);
            PreprocessorActions.BuildDirectoriesRecursive(root, root, "PREFIX", writer);
            writer.Flush();
            writer.Close();

            Assert.That(sb.ToString(), Is.EqualTo(
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>"
                + "<Directory Id=\"PREFIX_ONE\" Name=\"one\">"
                /**/ + "<Directory Id=\"PREFIX_ONE_2_5\" Name=\"2.5\">"
                /******/ + "<Directory Id=\"PREFIX_ONE_2_5_3_75\" Name=\"3.75\" /></Directory>"
                /**/ + "<Directory Id=\"PREFIX_ONE_TWO\" Name=\"two\">"
                /******/ + "<Directory Id=\"PREFIX_ONE_TWO_3_5\" Name=\"3.5\">"
                /**********/ + "<Directory Id=\"PREFIX_ONE_TWO_3_5_FOUR\" Name=\"four\" /></Directory>"
                /******/ + "<Directory Id=\"PREFIX_ONE_TWO_THREE\" Name=\"three\">"
                /**********/ + "<Directory Id=\"PREFIX_ONE_TWO_THREE_FOUR\" Name=\"four\" />"
                + "</Directory></Directory></Directory>"));
        }


    }
}
