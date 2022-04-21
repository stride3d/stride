using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parser;
public class SDSLGrammar : Grammar
{
    public SDSLGrammar() : base("sdsl")
    {
        EnableMatchEvents = false;
        CaseSensitive = true;

        var ws = new RepeatCharTerminal(char.IsWhiteSpace);
        var wso = ws.Optional();

        var eof = End;

        var eol = Eol;
        var eolo = eol.Optional();
        
        var shaderDeclaration = Set("shader");
        
        var ldu = LetterOrDigit | "_";
        var identifier = LetterOrDigit.Then(ldu.Repeat().Optional()).WithName("Identifier");
        
        var lbr = Set("{");
        var rbr = Set("}");

        var floatParser = new NumberParser{AllowSign = true, AllowDecimal = true, AllowExponent = true,ValueType = typeof(double), Name = "ConstantDouble", AddMatch = true, AddError = true};
        var intParser = new NumberParser{AllowSign = true, AllowDecimal = false, AllowExponent = false, ValueType = typeof(long), Name = "ConstantInteger", AddMatch = true, AddError = true};
        var boolParser = new BooleanTerminal{CaseSensitive = true, TrueValues = new string[]{"true"}, FalseValues = new string[]{"false"}, AddError = true, AddMatch = true, Name = "ConstantBoolean"};

        var constants = floatParser.Or(intParser).Or(boolParser).WithName("Constant"); 

        //Step 1 parse addition

        var primary_expr = identifier.Or(constants).WithName("PrimaryExpression");

        

        var assign = identifier.Then(Set("=")).Then(primary_expr).WithName("AssignExpression");
        
        var parenthesis_expr = new SequenceParser();
        parenthesis_expr.Add(
            primary_expr
            .Or(
                Set("(").Then(parenthesis_expr).Then(")")
            )
        );
        parenthesis_expr.WithName("ParenthesisExpression");

        var mul_expr = new SequenceParser();

        mul_expr.Add(
            parenthesis_expr.WithName("BasicMul")
            .Or(
                mul_expr.Then("*").Then(mul_expr)
            )
        );
        mul_expr.WithName("MultExpression");
        
        
        
        
        Inner = mul_expr;

    }
}
