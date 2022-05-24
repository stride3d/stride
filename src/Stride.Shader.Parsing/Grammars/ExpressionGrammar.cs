using Eto.Parse;
using Eto.Parse.Parsers;
using Stride.Shader.Parsing.Grammars.SDSL;
using static Eto.Parse.Terminals;


namespace Stride.Shader.Parsing.Grammars.Expression;

public class ExpressionGrammar : SDSLGrammar
{
    public ExpressionGrammar()
    {
        Name = "expression";
        CreateAll();
        Inner = PrimaryExpression.Then(";");
    }
}
