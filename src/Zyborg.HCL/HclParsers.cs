using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json.Linq;
using Sprache;

[assembly: InternalsVisibleTo("Zyborg.HCL.Tests")]  

namespace Zyborg.HCL
{
    public class HclParsers
    {
        public static readonly Parser<char> HexDigitParser = Parse.Char(
                c => (c >= '0' && c <= '9')
                    || (c >= 'a' && c <= 'f')
                    || (c >= 'A' && c <= 'F'), "hexadecimal digit");

        public static readonly Parser<char> EscapedHexCharParser = (
            from escape in Parse.Char('\\')
            from hex in Parse.IgnoreCase('x')
            from digits in HexDigitParser.Repeat(2).Text()
            select (char)int.Parse(digits, NumberStyles.HexNumber));

        public static readonly Parser<char> EscapedSingleCharParser = (
            from escape in Parse.Char('\\')
            from literal in Parse.AnyChar
            select literal).Select(x => {
                switch (x)
                {
                    case 'n': return '\n';
                    case 'r': return '\r';
                    case 't': return '\t';
                    // case '"': return '"';
                    // case '\'': return '\'';
                    // case '\\': return '\\';
                    default:
                        return x;
                }
            });

        public static readonly Parser<string> QuotedStringParser = (
            from openQuote in Sprache.Parse.Char('"')
            from content in EscapedHexCharParser
                .Or(EscapedSingleCharParser)
                .Or(Parse.CharExcept('"'))
                .Many().Text()
            from closeQuote in Parse.Char('"')
            select content).Token();

        public static readonly Parser<char> IdStartParser =
            Parse.LetterOrDigit.Or(Parse.Chars('_'));
        public static readonly Parser<char> IdContinueParser =
            IdStartParser.Or(Parse.Char('-'));
        public static readonly Parser<string> IdParser = 
            Parse.Identifier(IdStartParser, IdContinueParser);
        
        public static readonly Parser<NullValue> NullParser =
            from literal in Parse.String("null")
            select NullValue.Instance;
        
        public static readonly Parser<bool> BoolParser =
            from literal in Parse.String("true")
                .Or(Parse.String("false")).Text()
            select literal == "true";

        public static readonly Parser<string> HereDocParser =
            from start in Parse.String("<<")
            from endId in IdParser
            from nl in Parse.LineEnd
            from content in Parse.Until((
                from line in Parse.CharExcept("\r\n").Many().Text()
                from eol in Parse.LineEnd
                select line + eol
                ), (
                from endId in Parse.String(endId)
                from nl in Parse.LineEnd
                select endId))
            select string.Join("", content);

        public static readonly Parser<string> IndentedHereDocParser =
            from start in Parse.String("<<-")
            from endId in IdParser
            from nl in Parse.LineEnd
            from content in Parse.Until((
                from indent in Parse.Char(' ').Many().Text()
                from line in Parse.CharExcept("\r\n").Many().Text()
                from eol in Parse.LineEnd
                select (indent, line, eol)
                ), (
                from ws in Parse.WhiteSpace.Many()
                from endId in Parse.String(endId)
                from nl in Parse.LineEnd
                select endId))
            select StripIndents(content);

        public static string StripIndents(IEnumerable<(string indent, string line, string eol)> lines)
        {
            string indent = null;
            foreach (var l in lines)
            {
                if (indent == null || l.indent.Length < indent.Length)
                    indent = l.indent;
            }
            var indentLen = indent?.Length ?? 0;
            var buff = new StringBuilder();
            foreach (var l in lines)
            {
                buff.Append(l.indent.Remove(0, indentLen))
                    .Append(l.line)
                    .Append(l.eol);
            }

            return buff.ToString();
        }

        public static readonly Parser<JValue> PrimitiveValueParser =
            NullParser.Select(x => JValue.CreateNull())
                .Or(BoolParser.Select(x => new JValue(x)))
                .Or(IndentedHereDocParser.Select(x => new JValue(x)))
                .Or(HereDocParser.Select(x => new JValue(x)))
                .Or(QuotedStringParser.Select(x => new JValue(x)))
                .Token();

        public static readonly Parser<JProperty> PropertyParser =
            from propKey in QuotedStringParser.Or(IdParser)
            from propEqu in Parse.Char('=').Token()
            from propVal in PrimitiveValueParser
            select new JProperty(propKey, propVal);

        public static readonly Parser<JObject> ObjectParser =
            from enter in Parse.Char('{').Token()
            from props in PropertyParser.Many()
            from leave in Parse.Char('}').Token()
            select new JObject(props.ToArray());

        public HclParsers()
        {
        }

        public JObject ParseHcl(string hcl)
        {
            return null;
        }

        public class NullValue
        {
            public static readonly NullValue Instance = new NullValue();

            private NullValue()
            { }
        }

        public class IndentedHerDocLine
        {
            public string _indent;
            public string _content;
            public string _terminator; 
        }
    }
}