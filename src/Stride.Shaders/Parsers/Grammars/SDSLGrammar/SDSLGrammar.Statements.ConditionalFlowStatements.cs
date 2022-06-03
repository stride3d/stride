using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shaders.Parsing.Grammars.SDSL;
public partial class SDSLGrammar : Grammar
{
    public SequenceParser ControlFlow = new() { Name = "ControlFlow" };
    public void CreateConditionalFlowStatements()
    {
        var ws = WhiteSpace.Repeat(0);
        var ws1 = WhiteSpace.Repeat(1);


        var ifStatement = new SequenceParser(
            If,
            LeftParen,
            PrimaryExpression.Named("Condition"),
            RightParen,
            Statement
        ){Name = "IfStatement", Separator = ws};

        var elseIfStatement =
            Else.Then(ifStatement).SeparatedBy(ws1).Named("ElseIfStatement");

        var elseStatement =
            Else.Then(Statement).SeparatedBy(ws1).Named("ElseStatement");
        
        var conditionalFlow = new AlternativeParser(
            ifStatement & ws & elseIfStatement.Repeat(0).SeparatedBy(ws) & elseStatement,
            ifStatement & ws & elseIfStatement.Repeat(0).SeparatedBy(ws),
            ifStatement & ws & elseStatement,
            ifStatement
        ){Name = "ConditionalFlow"};

        ControlFlow.Add(
            Attribute.Repeat(0).Named("Attributes"),
            ws,
            conditionalFlow
            | ForLoop
        );
    }
}