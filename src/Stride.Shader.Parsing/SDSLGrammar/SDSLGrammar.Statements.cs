using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parsing;
public partial class SDSLGrammar : Grammar
{
    public SequenceParser Attribute = new() { Name = "Attribute" };
    public AlternativeParser Statement = new() { Name = "Statement"};
    public SequenceParser ControlFlow = new() { Name = "ControlFlow"};
    public SequenceParser ConstantBuffer = new() { Name = "ConstantBuffer"};
    public SequenceParser ShaderMethodCall = new() { Name = "ShaderMethodCall" };
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

        ShaderMethodCall.Add(
            Identifier.Repeat(1).SeparatedBy(ws & Dot & ws).Named("AccessorChain"),
            LeftParen,
            (Identifier | PrimaryExpression).Repeat(0).SeparatedBy(ws & Comma & ws).Named("Parameters"),
            RightParen
        );
        ShaderMethodCall.Separator = ws;

        var returnStatement =
            Return.Then(PrimaryExpression).SeparatedBy(ws1)
            .Then(Semi).SeparatedBy(ws);

        Attribute.Add(
            LeftBracket,
            Identifier,
            LeftParen,
            (Identifier | Literals).Repeat(0).SeparatedBy(ws & Comma & ws),
            RightParen,
            RightBracket
        );
        Attribute.Separator = ws;

        var assignVar =
            Identifier.Named("Variable").NotFollowedBy(Identifier)
            .Then(AssignOperators.Named("AssignOp"))
            .Then(PrimaryExpression.Named("Value"))
            .Then(Semi)
            .SeparatedBy(ws);

        var assignChain =
            Identifier.Then(Dot.Then(Identifier).Repeat(0))
            .Then(AssignOperators)
            .Then(PrimaryExpression)
            .Then(Semi)
            .SeparatedBy(ws);


        var declareAssign =
            Identifier.Named("Type")
            .Then(assignVar)
            .SeparatedBy(ws1);

        

        Statement.Add(
            Block,
            returnStatement.Named("Return"),
            ShaderMethodCall,
            assignChain.Named("AssignChain"),
            declareAssign.Named("DeclareAssign"),
            assignVar.Named("Assign"),
            PrimaryExpression.Then(";").SeparatedBy(ws).Named("EmptyStatement")
        );

        Block.Add(
            LeftBrace,
            Statement.Repeat(0).SeparatedBy(ws),
            RightBrace
        );
        Block.Separator = ws;


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
        );


        ConstantBuffer.Add(
            "cbuffer",
            ws1,
            Identifier,
            ws,
            LeftBrace,
            ws,
            RightBrace
        );
    }
}