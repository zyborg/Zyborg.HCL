using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
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
                HclParsers.IdParser.Parse("abc_def-ghi_jkl"));
            Assert.IsTrue(HclParsers.IdParser.TryParse("abc").WasSuccessful);
            Assert.IsTrue(HclParsers.IdParser.TryParse("a-bc").WasSuccessful);
            Assert.IsTrue(HclParsers.IdParser.TryParse("abc").WasSuccessful);
            Assert.IsFalse(HclParsers.IdParser.TryParse("-abc").WasSuccessful);
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
            var tests = new[] {
                @"
",
                @"
line1
line2
line3
",
            };

            foreach (var contentPre in tests)
            {
                var content = contentPre.TrimStart('\n', '\r');
                var heredoc = $"<<EOF{contentPre}EOF\n";

                Assert.AreEqual(content,
                    HclParsers.HereDocParser.Parse(heredoc));
            }
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
            var content = contentPre.Replace("++", "").TrimStart('\n', '\r');
            var parser = HclParsers.IndentedHereDocParser;

            Assert.AreEqual(content, parser.Parse(
                $"<<-EOF{contentPre.Replace("++", "  ")}EOF\n"));
            Assert.AreEqual(content, parser.Parse(
                $"<<-EOF{contentPre.Replace("++", "  ")} EOF\n"));
            Assert.AreEqual(content, parser.Parse(
                $"<<-EOF{contentPre.Replace("++", "  ")}  EOF\n"));
            Assert.AreEqual(content, parser.Parse(
                $"<<-EOF{contentPre.Replace("++", "  ")}   EOF\n"));
            Assert.AreEqual(content, parser.Parse(
                $"<<-EOF{contentPre.Replace("++", "  ")}         EOF\n"));
        }
    }
}
