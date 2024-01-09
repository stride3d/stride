using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;
using static SDSL.Parsing.Grammars.CommonParsers;

namespace SDSL.Parsing.Grammars.SDSL;
public partial class SDSLGrammar : Grammar
{
    public SequenceParser Attribute = new() { Name = "Attribute" };
    public AlternativeParser Statement = new() { Name = "Statement"};
    public SequenceParser Block = new() { Name = "Block" };


    public SDSLGrammar UsingStatements()
    {
        Inner = Statement;
        return this;
    }
    public void CreateStatements()
    {

        var returnStatement = new SequenceParser(
            Return,
            (Spaces1 & PrimaryExpression & Spaces & Semi)
            |(Spaces & Semi)
        );

        var attrParams =
            (
                LeftParen &
                PrimaryExpression.Repeat(0).SeparatedBy(Spaces & Comma & Spaces) &
                RightParen
            ).SeparatedBy(Spaces);

        Attribute.Add(
            LeftBracket,
            Identifier,
            ~attrParams,
            RightBracket
        );
        Attribute.Separator = Spaces;

        var arraySpecifier =
            (LeftBracket & PrimaryExpression.Named("Count") & RightBracket).SeparatedBy(Spaces);

        arraySpecifier.Name = "ArraySpecifier";
        

        var assignVar =
            Identifier.Named("Variable")
            .Then(arraySpecifier.Optional())
            .Then(AssignOperators.Named("AssignOp"))
            .Then(PrimaryExpression.Named("Value"))
            .Then(Semi)
            .SeparatedBy(Spaces);

        var assignChain =
            Identifier.Then(Dot.Then(Identifier).Repeat(0))
            .Then(AssignOperators.Named("AssignOp"))
            .Then(PrimaryExpression)
            .Then(Semi)
            .SeparatedBy(Spaces);


        var declareAssign =
            ValueTypes
            .Then(assignVar)
            .SeparatedBy(Spaces1);

        var simpleDeclare = 
            ((SimpleTypes | Identifier) & Identifier.Named("Variable") & arraySpecifier).SeparatedBy(Spaces)
            | ((SimpleTypes | Identifier) & Identifier.Named("Variable")).SeparatedBy(Spaces);

        Statement.Add(
            Block,
            ControlFlow,
            ForLoop,
            returnStatement.Named("Return"),
            assignChain.Named("AssignChain"),
            declareAssign.Named("DeclareAssign"),
            simpleDeclare.Named("SimpleDeclare"),
            assignVar.Named("Assign"),
            PrimaryExpression.Then(Semi).SeparatedBy(Spaces).Named("EmptyStatement")
        );

        Block.Add(
            LeftBrace,
            Statement.Repeat(0).SeparatedBy(Spaces),
            RightBrace
        );
        Block.Separator = Spaces;


        
    }
}