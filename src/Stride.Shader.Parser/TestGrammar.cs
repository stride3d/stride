using Eto.Parse;
using Eto.Parse.Parsers;

namespace SDSLParser
{
    public class TestGrammar : Grammar
    {
        private static bool IsLetterDigitOrUnderscore(char c)
        {
            return char.IsLetterOrDigit(c) || c.Equals("_");
        }
        public TestGrammar() : base("test-sdsl")
        {
            EnableMatchEvents = false;
			CaseSensitive = true;

			var ws = new RepeatCharTerminal(char.IsWhiteSpace);
            var wso = ws.Optional();

            var eof = Terminals.End;

            var eol = Terminals.Eol;
            var eolo = eol.Optional();
            
            var shaderDeclaration = Terminals.Set("shader");
            
            var ldu = Terminals.LetterOrDigit | "_";
            var identifier = Terminals.LetterOrDigit.Then(ldu.Repeat().Optional()).Named("Identifier");
           
            var lbr = Terminals.Set("{");
            var rbr = Terminals.Set("}");

            var intParser = new NumberParser{AllowSign = true, AllowDecimal = false, AllowExponent = false, Name = "ConstantInt"};
            var floatParser = new NumberParser{AllowSign = true, AllowDecimal = false, AllowExponent = true, Name = "ConstantInt"};
            var boolParser = new BooleanTerminal{CaseSensitive = true, TrueValues = new string[]{"true"}, FalseValues = new string[]{"false"}};

            var constants = intParser | floatParser | boolParser; 

            var unary_op =
                Terminals.Set("&")
                | Terminals.Set("*")
                | Terminals.Set("+")
                | Terminals.Set("-")
                | Terminals.Set("/")
                | Terminals.Set("!");
            var assign_op =
                Terminals.Set("=")
                | Terminals.Set("*=")
                | Terminals.Set("/=")
                | Terminals.Set("%=")
                | Terminals.Set("+=")
                | Terminals.Set("-=")
                | Terminals.Set("<<=")
                | Terminals.Set(">>=")
                | Terminals.Set("&=")
                | Terminals.Set("^=")
                | Terminals.Set("|=");
                
            var primary_expr = identifier | constants;

            var unary_expr = new SequenceParser().WithName("UnaryExpression");
            var cast_expr = new SequenceParser().WithName("CastExpression");
            var postfix_expr = new SequenceParser().WithName("PostfixExpression");
            var assign_expr = new SequenceParser().WithName("AssignmentExpression");
            var l_or_expr = new SequenceParser().WithName("LogicalOrExpression");
            var l_and_expr = new SequenceParser().WithName("LogicalAndExpression");
            var or_expr = new SequenceParser().WithName("OrExpression");
            var xor_expr = new SequenceParser().WithName("XOrExpression");
            var and_expr = new SequenceParser().WithName("AndExpression");
            var eq_expr = new SequenceParser().WithName("EqualityExpression");
            var rel_expr = new SequenceParser().WithName("RelationalExpression");
            var shift_expr = new SequenceParser().WithName("ShiftExpression");
            var add_expr = new SequenceParser().WithName("AdditiveExpression");
            var mul_expr = new SequenceParser().WithName("MultiplicativeExpression");
            
            postfix_expr.Add(primary_expr.Named("Value"));
            unary_expr.Add(postfix_expr.Named("Value"));
            // cast_expr.Add(identifier.Named("Value"));
            var tmpCast = unary_expr | Terminals.Set("(") & identifier.Named("Target") & ")" & identifier.Named("Value");
            // cast_expr.Add("(", identifier.Named("Target"), ")", identifier.Named("Value"));
            cast_expr.Add(tmpCast);

            Inner = cast_expr;

        }
    }
}