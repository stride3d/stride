using Eto.Parse;
using Eto.Parse.Parsers;

namespace SDSLParser
{
    public class SDSLGrammar : Grammar
    {
        public SDSLGrammar() : base("sdsl")
        {
            EnableMatchEvents = false;
			CaseSensitive = true;

            var shaderDeclaration = Terminals.Set("shader");
            var name = Terminals.Repeat(new RepeatCharItem(Char.IsLetter, 1, 1), new RepeatCharItem(Char.IsLetterOrDigit, 0));
            var lbracket = Terminals.Set("{");
            var rbracket = Terminals.Set("}");

            // var sdstring = new StringParser { AllowEscapeCharacters = true, Name = "string" };
			// var sdnumber = new NumberParser { AllowExponent = true, AllowSign = true, AllowDecimal = true, Name = "number" };
			// var sdboolean = new BooleanTerminal { Name = "bool", TrueValues = new string[] { "true" }, FalseValues = new string[] { "false" }, CaseSensitive = false };
			// var sdname = new StringParser { AllowEscapeCharacters = true, Name = "name" };
			// var sdnull = new LiteralTerminal { Value = "null", Name = "null", CaseSensitive = false };
			var ws = new RepeatCharTerminal(char.IsWhiteSpace);
			// var commaDelimiter = new RepeatCharTerminal(new RepeatCharItem(char.IsWhiteSpace), ',', new RepeatCharItem(char.IsWhiteSpace));

            Inner = 
                ws
                .Then(shaderDeclaration)
                .Then(name)
                .Then(lbracket)
                .Then(rbracket);

        }
    }
}