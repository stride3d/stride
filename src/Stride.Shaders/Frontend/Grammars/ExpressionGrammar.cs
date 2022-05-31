using Eto.Parse;
using Eto.Parse.Parsers;
using Stride.Shaders.Parsing.Grammars.SDSL;
using static Eto.Parse.Terminals;


namespace Stride.Shaders.Parsing.Grammars.Expression;

public class ExpressionGrammar : SDSLGrammar
{
    public ExpressionGrammar() : base()
    {
        Using(PrimaryExpression);
    }
}
