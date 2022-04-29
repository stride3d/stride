using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parser;
public partial class SDSLGrammar : Grammar
{
    public AlternativeParser DirectiveTerm = new();
    public AlternativeParser MulExpression = new();
    

    public SDSLGrammar UsingDirectiveExpression()
    {
        Inner = MulExpression;
        return this;
    }

    public void CreateDirectiveExpressions()
    {
        DirectiveTerm =
            Literals
            | Identifier
            | StringLiteral;
        
        var multiply = DirectiveTerm.Then("*").Then(MulExpression);
        var divide = DirectiveTerm.Then(Div).Then(MulExpression);
        var mod = DirectiveTerm.Then(Mod).Then(MulExpression);

        
        MulExpression =
            ((DirectiveTerm - multiply).WithName("Term")
            | multiply).WithName("MulExpression");
            // | divide
            // | mod;
    }
}