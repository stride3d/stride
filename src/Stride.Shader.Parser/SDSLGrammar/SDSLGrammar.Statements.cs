using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parser;
public partial class SDSLGrammar : Grammar
{
    public AlternativeParser StructDefinition = new();
    public AlternativeParser Attribute = new();
    public AlternativeParser Statement = new();
    public AlternativeParser ControlFlow = new();
    public AlternativeParser MethodDeclaration = new();
    public AlternativeParser Block = new();
    

    public SDSLGrammar UsingStatements()
    {
        Inner = ControlFlow;
        return this;
    }
    public void CreateStatements()
    {
        var ws = WhiteSpace.Repeat(0);
        var ws1 = WhiteSpace.Repeat(1);  

        var declare = 
            Identifier.Then(Identifier).SeparatedBy(ws1).Then(";").SeparatedBy(ws);
        var assignVar =
            Identifier.Named("Variable").NotFollowedBy(Identifier)
            .Then(AssignOperators.Named("AssignOp"))
            .Then(PrimaryExpression.Named("Value"))
            .Then(Semi)
            .SeparatedBy(ws);
        
        

        var declareAssign =
            Identifier.Named("Type")
            .Then(assignVar)
            .SeparatedBy(ws1);

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
            Attribute.Named("Attribute")
            | Block.Named("BlockExpression")
            | returnStatement
            | assignVar.Named("AssignExpression")
            | declareAssign.Named("DeclareAssign")
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
        var parameterList = 
            LeftParen
            .Then(Comma.Optional().Then(parameter).SeparatedBy(ws).Repeat(0).SeparatedBy(ws))
            .Then(RightParen).SeparatedBy(ws);

        MethodDeclaration.Add(
            Identifier.Then(Identifier).SeparatedBy(ws1)
                .Then(parameterList)
                .Then(LeftBrace).Then(Statement.Repeat(0)).Then(RightBrace).SeparatedBy(ws).Named("MethodDeclaration")
        );
    }
}