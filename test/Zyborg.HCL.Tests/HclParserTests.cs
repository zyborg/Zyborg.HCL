using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sprache;

namespace Zyborg.HCL.Tests
{
    [TestClass]
    public class HclParserTests
    {
        [TestMethod]
        public void TestdHexDigit()
        {
            foreach (var c in "0123456789abcdef")
                Assert.AreEqual(c, HclParser.HexDigit.Parse($"{c}"));
        }

        [TestMethod]
        public void TestEscapedHexChar()
        {
            Assert.AreEqual('\n', HclParser.EscapedHexChar.Parse("\\x0a"));
        }

        [TestMethod]
        public void TestEscapedSingleChar()
        {
            Assert.AreEqual('\\', HclParser.EscapedSingleChar.Parse("\\\\"));
            Assert.AreEqual('\"', HclParser.EscapedSingleChar.Parse("\\\""));
            Assert.AreEqual('\n', HclParser.EscapedSingleChar.Parse("\\n"));
        }

        [TestMethod]
        public void TestQuotedString()
        {
            Assert.AreEqual("ABC\n\\\r\"Aa",
                HclParser.QuotedString.Parse("\"ABC\\n\\\\\\x0d\\\"\\x41\\X61\""));
        }

        [TestMethod]
        public void TestIdentifier()
        {
            Assert.AreEqual("abc_def-ghi_jkl",
                HclParser.Id.Parse("abc_def-ghi_jkl"));
            Assert.IsTrue(HclParser.Id.TryParse("abc").WasSuccessful);
            Assert.IsTrue(HclParser.Id.TryParse("a-bc").WasSuccessful);
            Assert.IsTrue(HclParser.Id.TryParse("abc").WasSuccessful);
            Assert.IsFalse(HclParser.Id.TryParse("-abc").WasSuccessful);
        }

        [TestMethod]
        public void TestNullValue()
        {
            Assert.AreEqual(HclParser.NullValue.Instance,
                HclParser.Null.Parse("null"));
        }

        [TestMethod]
        public void TestBoolValue()
        {
            Assert.AreEqual(true,
                HclParser.Bool.Parse("true"));
            Assert.AreEqual(false,
                HclParser.Bool.Parse("false"));
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
                HclParser.HereDoc.Parse(heredoc));
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

            Assert.AreEqual(content, HclParser.IndentedHereDoc.Parse(
                $@"<<-EOF{contentPre.Replace("++", "  ")}EOF"));
            Assert.AreEqual(content, HclParser.IndentedHereDoc.Parse(
                $@"<<-EOF{contentPre.Replace("++", "  ")} EOF"));
            Assert.AreEqual(content, HclParser.IndentedHereDoc.Parse(
                $@"<<-EOF{contentPre.Replace("++", "  ")}  EOF"));
            Assert.AreEqual(content, HclParser.IndentedHereDoc.Parse(
                $@"<<-EOF{contentPre.Replace("++", "  ")}   EOF"));
            Assert.AreEqual(content, HclParser.IndentedHereDoc.Parse(
                $@"<<-EOF{contentPre.Replace("++", "  ")}         EOF"));
        }
    }
}
