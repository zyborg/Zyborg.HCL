using System.Globalization;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;
using Sprache;

[assembly: InternalsVisibleTo("Zyborg.HCL.Tests")]  

namespace Zyborg.HCL
{
    public class HclParser
    {
        public static readonly Parser<char> HexDigit = Parse.Char(
                c => (c >= '0' && c <= '9')
                    || (c >= 'a' && c <= 'f')
                    || (c >= 'A' && c <= 'F'), "hexadecimal digit");

        public static readonly Parser<char> EscapedHexChar = (
            from escape in Parse.Char('\\')
            from hex in Parse.IgnoreCase('x')
            from digits in HexDigit.Repeat(2).Text()
            select (char)int.Parse(digits, NumberStyles.HexNumber));

        public static readonly Parser<char> EscapedSingleChar = (
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

        public static readonly Parser<string> QuotedString = (
            from openQuote in Sprache.Parse.Char('"')
            from content in EscapedHexChar
                .Or(EscapedSingleChar)
                .Or(Parse.CharExcept('"'))
                .Many().Text()
            from closeQuote in Parse.Char('"')
            select content).Token();

        public static readonly Parser<char> IdStart =
            Parse.LetterOrDigit.Or(Parse.Chars('_'));
        public static readonly Parser<char> IdContinue =
            IdStart.Or(Parse.Char('-'));
        public static readonly Parser<string> Id = 
            Parse.Identifier(IdStart, IdContinue);
        
        public static readonly Parser<NullValue> Null =
            from literal in Parse.String("null")
            select NullValue.Instance;
        
        public static readonly Parser<bool> Bool =
            from literal in Parse.String("true")
                .Or(Parse.String("false")).Text()
            select literal == "true";

        public static readonly Parser<

        public HclParser()
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
    }
}