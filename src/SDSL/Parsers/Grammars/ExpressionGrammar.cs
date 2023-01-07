using Eto.Parse;
using Eto.Parse.Parsers;
using SDSL.Parsing.Grammars.SDSL;
using static Eto.Parse.Terminals;


namespace SDSL.Parsing.Grammars.Expression;

public class ExpressionGrammar : SDSLGrammar
{
    public ExpressionGrammar() : base()
    {
        Using(PrimaryExpression);
    }
}
