using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parser;
public partial class SDSLGrammar : Grammar
{
    public AlternativeParser Statement = new();

    public SDSLGrammar UsingStatements()
    {
        Inner = Statement.Then(";");
        return this;
    }
    public void CreateStatements()
    {
        var ls = WhiteSpace.Repeat(0);
        var ls1 = WhiteSpace.Repeat(1);  

        var assignValue =
            Identifier.Named("Variable").NotFollowedBy(Identifier)
            .Then(AssignOperators.Named("AssignOp"))
            .Then(PrimaryExpression.Named("Value"))
            .SeparatedBy(ls);
        
        var assignVar =
            Identifier.Named("Type")
            .Then(assignValue).SeparatedBy(ls);

        Statement.Add(
            assignValue.Named("Assign")
            | assignVar
        );
    }
}