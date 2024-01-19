using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;
using static SDSL.Parsing.Grammars.CommonParsers;

namespace SDSL.Parsing.Grammars.SDSL;

public partial class SDSLGrammar : Grammar
{
    public SequenceParser WhileLoop = new() { Name = "WhileLoop"};
    public SequenceParser ForEachLoop = new() { Name = "ForEachLoop"};
    public SequenceParser ForLoop = new() { Name = "ForLoop"};

    public void CreateLoopFlowStatements()
    {
        var valueDeclare = new SequenceParser(
            ((SimpleTypes | Identifier) & Identifier).SeparatedBy(Spaces1).Named("NewVariable")
            | UnaryExpression.Named("ExistingVariable"),
            AssignOperators.Named("Operator"),
            PrimaryExpression
        )
        { Separator = Spaces, Name = "Initializer"};
        var valueAssign = new SequenceParser(
            Identifier,
            AssignOperators.Named("Operator"),
            PrimaryExpression
        )
        { Separator = Spaces };

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
        ForLoop.Separator = Spaces;

        WhileLoop.Add(
            While,
            LeftParen,
            PrimaryExpression.Named("Condition"),
            RightParen,
            Statement
        );
        WhileLoop.Separator = Spaces;

        ForEachLoop.Add(
            Literal("foreach"),
            LeftParen,
            ((SimpleTypes | Literal("var") | Identifier) & Identifier & In & PrimaryExpression).SeparatedBy(Spaces).Named("Declarator"),
            RightParen,
            Statement
        );
        ForEachLoop.Separator = Spaces;

    }
}