using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shaders.Parsing.Grammars.SDSL;
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
        var ws = WhiteSpace.Repeat(0);
        var ws1 = WhiteSpace.Repeat(1);


        var returnStatement = new SequenceParser(
            Return,
            (ws1 & PrimaryExpression & ws & Semi)
            |(ws & Semi)
        );

        var attrParams =
            (
                LeftParen &
                PrimaryExpression.Repeat(0).SeparatedBy(ws & Comma & ws) &
                RightParen
            ).SeparatedBy(ws);

        Attribute.Add(
            LeftBracket,
            Identifier,
            ~attrParams,
            RightBracket
        );
        Attribute.Separator = ws;

        var arraySpecifier =
            (LeftBracket & PrimaryExpression.Named("Count") & RightBracket).SeparatedBy(ws);

        arraySpecifier.Name = "ArraySpecifier";
        

        var assignVar =
            Identifier.Named("Variable")
            .Then(arraySpecifier.Optional())
            .Then(AssignOperators.Named("AssignOp"))
            .Then(PrimaryExpression.Named("Value"))
            .Then(Semi)
            .SeparatedBy(ws);

        var assignChain =
            Identifier.Then(Dot.Then(Identifier).Repeat(0))
            .Then(AssignOperators.Named("AssignOp"))
            .Then(PrimaryExpression)
            .Then(Semi)
            .SeparatedBy(ws);


        var declareAssign =
            ValueTypes
            .Then(assignVar)
            .SeparatedBy(ws1);

        var simpleDeclare = 
            ((SimpleTypes | Identifier) & Identifier.Named("Variable") & arraySpecifier).SeparatedBy(ws)
            | ((SimpleTypes | Identifier) & Identifier.Named("Variable")).SeparatedBy(ws);

        Statement.Add(
            Block,
            ControlFlow,
            ForLoop,
            returnStatement.Named("Return"),
            assignChain.Named("AssignChain"),
            declareAssign.Named("DeclareAssign"),
            simpleDeclare.Named("SimpleDeclare"),
            assignVar.Named("Assign"),
            PrimaryExpression.Then(Semi).SeparatedBy(ws).Named("EmptyStatement")
        );

        Block.Add(
            LeftBrace,
            Statement.Repeat(0).SeparatedBy(ws),
            RightBrace
        );
        Block.Separator = ws;


        
    }
}