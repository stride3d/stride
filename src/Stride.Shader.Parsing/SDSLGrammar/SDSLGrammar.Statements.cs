using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parsing;
public partial class SDSLGrammar : Grammar
{
    public AlternativeParser StructDefinition = new();
    public AlternativeParser ParameterList = new();
    public AlternativeParser Attribute = new();
    public AlternativeParser Statement = new();
    public AlternativeParser ControlFlow = new();
    public AlternativeParser MethodDeclaration = new();
    public AlternativeParser ConstantBuffer = new();
    public AlternativeParser GenericDeclaration = new();
    public AlternativeParser Block = new();
    

    public SDSLGrammar UsingStatements()
    {
        Inner = Statement;
        return this;
    }
    public void CreateStatements()
    {
        var ws = WhiteSpace.Repeat(0);
        var ws1 = WhiteSpace.Repeat(1);  

        var declare = 
            Identifier.Then(Identifier).SeparatedBy(ws1).Then(Semi).SeparatedBy(ws);
        
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

        var declaratorSupplement = 
            Colon.Then(
                    Packoffset.Then(LeftParen).Then(Identifier.Then(Dot.Then(Identifier).Repeat(0))).Then(RightParen).SeparatedBy(ws).Named("PackOffset")
                    | Register.Then(LeftParen).Then(Identifier.Then(Comma.Then(Identifier).SeparatedBy(ws).Repeat(0).SeparatedBy(ws))).Then(RightParen).SeparatedBy(ws).Named("RegisterAllocation")
                    | Identifier.Named("Semantic")
                ).SeparatedBy(ws).Optional();
        var arrayRank = 
            LeftBracket.Then(PrimaryExpression).Then(RightBracket).SeparatedBy(ws).Named("ArrayRankSpecifier").Optional();


        GenericDeclaration.Add(
            Literal("stage").Named("Stage").Optional().Then(Literal("stream").Named("Stream").Optional())
            .Then((ValueTypes | Identifier).Named("Type"))
            .Then(Identifier.Named("Name").Then(arrayRank)).SeparatedBy(ws1)
            .Then((AssignOperators & PrimaryExpression).SeparatedBy(ws).Optional())
            .Then(declaratorSupplement.Optional())
            .Then(";").SeparatedBy(ws)
        );

        var returnStatement = 
            Return.Then(PrimaryExpression).SeparatedBy(ws1)
            .Then(Semi).SeparatedBy(ws);

        Attribute.Add(
            LeftBracket
                .Then(Identifier)
                    .Then(LeftParen)
                        .Then(Literals.Then(Comma.Then(Literals).Repeat(0).SeparatedBy(ws)))
                    .Then(RightParen)
            .Then(RightBracket)
            .SeparatedBy(ws)
        );

        StructDefinition.Add(
            Struct.Then(Identifier).SeparatedBy(ws1)
            .Then(LeftBrace)
                .Then(declare.Repeat(0).SeparatedBy(ws))
            .Then(RightBrace).Then(Semi).SeparatedBy(ws)
        );

        Statement.Add(
            Block.Named("BlockExpression")
            | returnStatement.Named("Return")
            | assignChain
            | declareAssign.Named("DeclareAssign")
            | assignVar.Named("Assign")
            | PrimaryExpression.Then(";").SeparatedBy(ws).Named("EmptyStatement")
        );

        Block.Add(
            LeftBrace.Then(Statement.Repeat(0).SeparatedBy(ws)).Then(RightBrace).SeparatedBy(ws)
        );
        var flowStatement = Statement;        
        
        var ifStatement = 
            If.Then(LeftParen).Then(PrimaryExpression).Then(RightParen).Then(flowStatement).SeparatedBy(ws);
        
        var elseIfStatement = 
            Else.Then(ifStatement).SeparatedBy(ws1);
        
        var elseStatement = 
            Else.Then(flowStatement).SeparatedBy(ws1);
        
        ControlFlow.Add(
            Attribute.Repeat(0).Named("Attributes").Then(
                ifStatement.Named("IfStatement")
                | elseStatement.Named("ElseStatement")
                | elseIfStatement.Named("ElseIfStatement")
            ).SeparatedBy(ws)
        );

        var parameter = Identifier.Then(Identifier).SeparatedBy(ws1);
        ParameterList.Add(
            LeftParen
            .Then(Comma.Optional().Then(parameter).SeparatedBy(ws).Repeat(0).SeparatedBy(ws))
            .Then(RightParen).SeparatedBy(ws)
        );

        MethodDeclaration.Add(
            Literal("abstract").Then(Literal("stage").Optional()).Then(Identifier).Then(Identifier).SeparatedBy(ws1)
                .Then(ParameterList).Then(Semi).SeparatedBy(ws).Named("AbstractMethod")
            | Literal("override").Optional().Then(Literal("stage").Optional())
                .Then(Identifier).Then(Identifier).SeparatedBy(ws1)
                    .Then(ParameterList)
                .Then(LeftBrace)
                    .Then(Statement.Repeat(0))
                .Then(RightBrace).SeparatedBy(ws).Named("MethodDeclaration")
        );
        
        ConstantBuffer.Add(
            Literal("cbuffer").Then(Identifier).SeparatedBy(ws1)
            .Then(LeftBrace)
            .Then()
        );
    }
}