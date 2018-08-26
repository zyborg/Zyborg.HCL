using System.Collections.Generic;
using System.Globalization;
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
        public static readonly Parser<string> Id = 
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
            from endId in Id
            from nl in Parse.LineEnd
            from content in Parse.Until(Parse.AnyChar, (
                from nl in Parse.LineEnd
                from endId in Parse.String(endId)
                select endId)).Text()
            select content;

        public static readonly Parser<string> IndentedHereDocParser =
            from start in Parse.String("<<-")
            from endId in Id
            from nl in Parse.LineEnd
            from content in Parse.Until(Parse.AnyChar, (
                from nl in Parse.LineEnd
                from ws in Parse.WhiteSpace.Many()
                from endId in Parse.String(endId)
                select endId)).Text()
            select StripIndents(IndentedLinesParser.Parse(content));

        public static readonly Parser<IEnumerable<IndentedHerDocLine>> IndentedLinesParser = (
                from indent in Parse.Char(' ').Many().Text()
                from content in Parse.CharExcept("\n\r").Many().Text()
                from terminator in Parse.LineTerminator.Optional()
                select new IndentedHerDocLine {
                    _indent = indent,
                    _content = content,
                    _terminator = terminator.GetOrElse(string.Empty),
                }).Many();

        public static string StripIndents(IEnumerable<IndentedHerDocLine> lines)
        {
            string indent = null;
            foreach (var l in lines)
            {
                if (indent == null || l._indent.Length < indent.Length)
                    indent = l._indent;
            }
            var indentLen = indent?.Length ?? 0;
            var buff = new StringBuilder();
            foreach (var l in lines)
            {
                if (indentLen > 0)
                {
                    l._indent = l._indent.Remove(0, indentLen);
                }
                buff.Append(l._indent)
                    .Append(l._content)
                    .Append(l._terminator);

            }

            return buff.ToString();
        }

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