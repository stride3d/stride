using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parsing;
public partial class SDSLGrammar : Grammar
{
    public SequenceParser Attribute = new();
    public AlternativeParser Statement = new();
    public SequenceParser ControlFlow = new();
    public SequenceParser ConstantBuffer = new();
    public AlternativeParser ShaderMethodCall = new();
    public SequenceParser Block = new();


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
            (
                Identifier
                .Then(
                    (
                        Dot.NotFollowedBy(MethodCall).Then(Identifier)
                        | Dot.Then(MethodCall)
                    )
                    .Repeat(0)
                )   
                .Then(";")
            )
            .SeparatedBy(ws)
        );

        var returnStatement =
            Return.Then(PrimaryExpression).SeparatedBy(ws1)
            .Then(Semi).SeparatedBy(ws);

        Attribute.Add(
            LeftBracket,
            ws,
            Identifier,
            ws,
            LeftParen,
            ws,
            (Identifier | Literals).Then(Comma.Optional()).Repeat(0).SeparateChildrenBy(ws),
            ws,
            RightParen,
            ws,
            RightBracket
        );

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
            Block.Named("BlockExpression"),
            returnStatement.Named("Return"),
            assignChain,
            ShaderMethodCall,
            declareAssign.Named("DeclareAssign"),
            assignVar.Named("Assign"),
            PrimaryExpression.Then(";").SeparatedBy(ws).Named("EmptyStatement")
        );

        Block.Add(
            LeftBrace,
            ws,
            Statement.Repeat(0).SeparatedBy(ws),
            ws,
            RightBrace
        );
        var flowStatement = Statement;

        var ifStatement =
            If.Then(LeftParen).Then(PrimaryExpression).Then(RightParen).Then(flowStatement).SeparatedBy(ws);

        var elseIfStatement =
            Else.Then(ifStatement).SeparatedBy(ws1);

        var elseStatement =
            Else.Then(flowStatement).SeparatedBy(ws1);

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