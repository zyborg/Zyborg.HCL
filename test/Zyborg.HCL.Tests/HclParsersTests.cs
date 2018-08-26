using System;
using System.Linq;
using System.Text;
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

        [TestMethod]
        public void TestValueParser()
        {
            var tests = new (string valueString, JTokenType valueType, object value)[] {
                ("null", JTokenType.Null, null),
                (" null ", JTokenType.Null, null),
                ("true", JTokenType.Boolean, true),
                ("false ", JTokenType.Boolean, false),
                ("\"This is a test\"", JTokenType.String, "This is a test"),
                (" \"This\\nis\\ra\\ttest\" ", JTokenType.String, "This\nis\ra\ttest"),
                (@"  <<EOF
EOF
", JTokenType.String, @""),
                (@"  <<-EOF
EOF
", JTokenType.String, @""),
                (@"  <<-EOF
 EOF
", JTokenType.String, @""),
                ($@"  <<-EOF
{"\t"}EOF
", JTokenType.String, @""),
                ($@"  <<-EOF
    line1
        line2
            line3
{"\t"}EOF
", JTokenType.String, @"line1
    line2
        line3
"),
            };

            foreach (var t in tests)
            {
                var v = HclParsers.PrimitiveValueParser.Parse(t.valueString);
                Assert.AreEqual(t.valueType, v.Type);
                Assert.AreEqual(t.value, v.Value);
            }
        }

        [TestMethod]
        public void TestProperty()
        {
            var tests = new (string propKeySample, string propKey, string propValSample, JTokenType valueType, object value)[] {
                ("key", "key", "null", JTokenType.Null, null),
                ("key_123", "key_123", "true", JTokenType.Boolean, true),
                ("key-456", "key-456", "false ", JTokenType.Boolean, false),
                ("\"key 789\"", "key 789", "\"This is a test\"", JTokenType.String, "This is a test"),
                ("key", "key", " \"This\\nis\\ra\\ttest\" ", JTokenType.String, "This\nis\ra\ttest"),
                ("key", "key", @"  <<EOF
EOF
", JTokenType.String, @""),
                ("key", "key", @"  <<-EOF
EOF
", JTokenType.String, @""),
                ("key", "key", @"  <<-EOF
 EOF
", JTokenType.String, @""),
                ("key", "key", $@"  <<-EOF
{"\t"}EOF
", JTokenType.String, @""),
                ("multi-line_string", "multi-line_string", $@"  <<-EOF
    line1
        line2
            line3
{"\t"}EOF
", JTokenType.String, @"line1
    line2
        line3
"),
            };

            foreach (var t in tests)
            {
                var sample = $"{t.propKeySample} = {t.propValSample}";
                var p = HclParsers.PropertyParser.Parse(sample);
                Assert.AreEqual(t.propKey, p.Name);
                Assert.AreEqual(t.valueType, p.Value.Type);
                Assert.AreEqual(t.value, ((JValue)p.Value).Value);
            }

        }

        [TestMethod]
        public void TestObject()
        {
            var tests = new (string propKeySample, string propKey, string propValSample, JTokenType valueType, object value)[] {
                ("key", "key", "null", JTokenType.Null, null),
                ("key_123", "key_123", "true", JTokenType.Boolean, true),
                ("key-456", "key-456", "false ", JTokenType.Boolean, false),
                ("\"key 789\"", "key 789", "\"This is a test\"", JTokenType.String, "This is a test"),
                ("key2", "key2", " \"This\\nis\\ra\\ttest\" ", JTokenType.String, "This\nis\ra\ttest"),
                ("key3", "key3", @"  <<EOF
EOF
", JTokenType.String, @""),
                ("key4", "key4", @"  <<-EOF
EOF
", JTokenType.String, @""),
                ("key5", "key5", @"  <<-EOF
 EOF
", JTokenType.String, @""),
                ("key6", "key6", $@"  <<-EOF
{"\t"}EOF
", JTokenType.String, @""),
                ("multi-line_string", "multi-line_string", $@"  <<-EOF
    line1
        line2
            line3
{"\t"}EOF
", JTokenType.String, @"line1
    line2
        line3
"),
            };

            var buff = new StringBuilder("{");
            var rng = new Random();
            foreach (var t in tests)
            {
                buff.Append(t.propKeySample)
                    .Append(" = ")
                    .Append(t.propValSample);

                if (rng.Next() % 2 == 0)
                    buff.Append("\r\n");
            }
            buff.Append("}");

            var jobj = HclParsers.ObjectParser.Parse(buff.ToString());

            var props = jobj.Properties().ToDictionary(p => p.Name, p => p.Value);

            foreach (var t in tests)
            {
                var p = jobj.Property(t.propKey);
                props.Remove(p.Name);

                Assert.AreEqual(t.propKey, p.Name);
                Assert.AreEqual(t.valueType, p.Value.Type);
                Assert.AreEqual(t.value, ((JValue)p.Value).Value);
            }

            Assert.AreEqual(0, props.Count);
        }
    }
}
