using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sprache;

namespace Zyborg.HCL.Tests
{
    [TestClass]
    public class HclParsersTests
    {
        [TestMethod]
        public void TestdHexDigit()
        {
            foreach (var c in "0123456789abcdef")
                Assert.AreEqual(c, HclParsers.HexDigitParser.Parse($"{c}"));
        }

        [TestMethod]
        public void TestEscapedHexChar()
        {
            Assert.AreEqual('\n', HclParsers.EscapedHexCharParser.Parse("\\x0a"));
        }

        [TestMethod]
        public void TestEscapedSingleChar()
        {
            Assert.AreEqual('\\', HclParsers.EscapedSingleCharParser.Parse("\\\\"));
            Assert.AreEqual('\"', HclParsers.EscapedSingleCharParser.Parse("\\\""));
            Assert.AreEqual('\n', HclParsers.EscapedSingleCharParser.Parse("\\n"));
        }

        [TestMethod]
        public void TestQuotedString()
        {
            Assert.AreEqual("ABC\n\\\r\"Aa",
                HclParsers.QuotedStringParser.Parse("\"ABC\\n\\\\\\x0d\\\"\\x41\\X61\""));
        }

        [TestMethod]
        public void TestIdentifier()
        {
            Assert.AreEqual("abc_def-ghi_jkl",
                HclParsers.Id.Parse("abc_def-ghi_jkl"));
            Assert.IsTrue(HclParsers.Id.TryParse("abc").WasSuccessful);
            Assert.IsTrue(HclParsers.Id.TryParse("a-bc").WasSuccessful);
            Assert.IsTrue(HclParsers.Id.TryParse("abc").WasSuccessful);
            Assert.IsFalse(HclParsers.Id.TryParse("-abc").WasSuccessful);
        }

        [TestMethod]
        public void TestNullValue()
        {
            Assert.AreEqual(HclParsers.NullValue.Instance,
                HclParsers.NullParser.Parse("null"));
        }

        [TestMethod]
        public void TestBoolValue()
        {
            Assert.AreEqual(true,
                HclParsers.BoolParser.Parse("true"));
            Assert.AreEqual(false,
                HclParsers.BoolParser.Parse("false"));
        }

        [TestMethod]
        public void TestHereDoc()
        {
            var contentPre = @"
line1
line2
line3
";
            var content = contentPre.Trim('\n', '\r');
            var heredoc = $@"<<EOF{contentPre}EOF";

            Assert.AreEqual(content,
                HclParsers.HereDocParser.Parse(heredoc));
        }

        [TestMethod]
        public void TestIndentedHereDoc()
        {
            var contentPre = @"
++  line1
++    line2
++line3
++ line4
";
            var content = contentPre.Replace("++", "").Trim('\n', '\r');

            Assert.AreEqual(content, HclParsers.IndentedHereDocParser.Parse(
                $@"<<-EOF{contentPre.Replace("++", "  ")}EOF"));
            Assert.AreEqual(content, HclParsers.IndentedHereDocParser.Parse(
                $@"<<-EOF{contentPre.Replace("++", "  ")} EOF"));
            Assert.AreEqual(content, HclParsers.IndentedHereDocParser.Parse(
                $@"<<-EOF{contentPre.Replace("++", "  ")}  EOF"));
            Assert.AreEqual(content, HclParsers.IndentedHereDocParser.Parse(
                $@"<<-EOF{contentPre.Replace("++", "  ")}   EOF"));
            Assert.AreEqual(content, HclParsers.IndentedHereDocParser.Parse(
                $@"<<-EOF{contentPre.Replace("++", "  ")}         EOF"));
        }
    }
}
