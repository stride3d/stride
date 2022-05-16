using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parsing;
public partial class SDSLGrammar : Grammar
{
    public SequenceParser ControlFlow = new() { Name = "ControlFlow" };
    public void CreateConditionalFlowStatements()
    {
        var ws = WhiteSpace.Repeat(0);
        var ws1 = WhiteSpace.Repeat(1);


        var ifStatement =
            If.Then(LeftParen).Then(PrimaryExpression).Then(RightParen).Then(Statement).SeparatedBy(ws);

        var elseIfStatement =
            Else.Then(ifStatement).SeparatedBy(ws1);

        var elseStatement =
            Else.Then(Statement).SeparatedBy(ws1);

        ControlFlow.Add(
            Attribute.Repeat(0).Named("Attributes"),
            ws,
            ifStatement.Named("IfStatement")
            | elseStatement.Named("ElseStatement")
            | elseIfStatement.Named("ElseIfStatement")
            | ForLoop
        );
    }
}