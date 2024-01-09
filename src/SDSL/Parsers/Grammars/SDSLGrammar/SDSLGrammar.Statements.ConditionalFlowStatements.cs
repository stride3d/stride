using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;
using static SDSL.Parsing.Grammars.CommonParsers;

namespace SDSL.Parsing.Grammars.SDSL;
public partial class SDSLGrammar : Grammar
{
    public SequenceParser ControlFlow = new() { Name = "ControlFlow" };
    public void CreateConditionalFloSpacestatements()
    {


        var ifStatement = new SequenceParser(
            If,
            LeftParen,
            PrimaryExpression.Named("Condition"),
            RightParen,
            Statement
        ){Name = "IfStatement", Separator = Spaces};

        var elseIfStatement =
            Else.Then(ifStatement).SeparatedBy(Spaces1).Named("ElseIfStatement");

        var elseStatement =
            Else.Then(Statement).SeparatedBy(Spaces1).Named("ElseStatement");
        
        var conditionalFlow = new AlternativeParser(
            ifStatement & Spaces & elseIfStatement.Repeat(0).SeparatedBy(Spaces) & elseStatement,
            ifStatement & Spaces & elseIfStatement.Repeat(0).SeparatedBy(Spaces),
            ifStatement & Spaces & elseStatement,
            ifStatement
        ){Name = "ConditionalFlow"};

        ControlFlow.Add(
            Attribute.Repeat(0).Named("Attributes"),
            Spaces,
            conditionalFlow
            | ForLoop
        );
    }
}