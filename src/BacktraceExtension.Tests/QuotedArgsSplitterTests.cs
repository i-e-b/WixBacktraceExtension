namespace BacktraceExtension.Tests
{
    using NUnit.Framework;
    using WixBacktraceExtension;

    [TestFixture]
    public class QuotedArgsSplitterTests
    {
        [Test]
        public void unquoted_args_are_passed_through()
        {
            var subject = new QuotedArgsSplitter("unquoted args");
            Assert.That(subject.Primary, Is.EqualTo("unquoted args"));
        }

        [Test]
        public void single_quoted_argument_is_primary_with_quotes_removed()
        {
            var subject = new QuotedArgsSplitter("\"quoted\"");
            Assert.That(subject.Primary, Is.EqualTo("quoted"));
        }

        [Test]
        public void named_arguments_with_primary()
        {
            var subject = new QuotedArgsSplitter("\"primary\" then \"secondary\" and \"tertiary\"");
            Assert.That(subject.Primary, Is.EqualTo("primary"));
            Assert.That(subject.NamedArguments["then"], Is.EqualTo("secondary"));
            Assert.That(subject.NamedArguments["and"], Is.EqualTo("tertiary"));
        }

        [Test]
        public void named_arguments_without_primary()
        {
            var subject = new QuotedArgsSplitter("first \"primary\" then \"secondary\" and \"tertiary\"");
            Assert.That(subject.Primary, Is.Null);

            Assert.That(subject.NamedArguments["first"], Is.EqualTo("primary"));
            Assert.That(subject.NamedArguments["then"], Is.EqualTo("secondary"));
            Assert.That(subject.NamedArguments["and"], Is.EqualTo("tertiary"));
        }


    }
}