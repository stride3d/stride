using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace SDSL.Parsing.Grammars.SDSL;

public partial class SDSLGrammar : Grammar
{
    public SequenceParser WhileLoop = new() { Name = "WhileLoop"};
    public SequenceParser ForEachLoop = new() { Name = "ForEachLoop"};
    public SequenceParser ForLoop = new() { Name = "ForLoop"};

    public void CreateLoopFlowStatements()
    {
        var ws = WhiteSpace.Repeat(0);
        var ws1 = WhiteSpace.Repeat(1);

        var valueDeclare = new SequenceParser(
            ((SimpleTypes | Identifier) & Identifier).SeparatedBy(ws1).Named("NewVariable")
            | UnaryExpression.Named("ExistingVariable"),
            AssignOperators.Named("Operator"),
            PrimaryExpression
        )
        { Separator = ws, Name = "Initializer"};
        var valueAssign = new SequenceParser(
            Identifier,
            AssignOperators.Named("Operator"),
            PrimaryExpression
        )
        { Separator = ws };

        ForLoop.Add(
            For,
            LeftParen,
            valueDeclare,
            Semi,
            PrimaryExpression,
            Semi,
            valueAssign | PrimaryExpression,
            RightParen,
            Semi 
            | Statement

        );
        ForLoop.Separator = ws;

        WhileLoop.Add(
            While,
            LeftParen,
            PrimaryExpression.Named("Condition"),
            RightParen,
            Statement
        );
        WhileLoop.Separator = ws;

        ForEachLoop.Add(
            Literal("foreach"),
            LeftParen,
            ((SimpleTypes | Literal("var") | Identifier) & Identifier & In & PrimaryExpression).SeparatedBy(ws).Named("Declarator"),
            RightParen,
            Statement
        );
        ForEachLoop.Separator = ws;

    }
}