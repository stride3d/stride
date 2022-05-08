using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parsing;
public partial class SDSLGrammar : Grammar
{
    public AlternativeParser ParameterList = new();
    public AlternativeParser ValueOrGeneric = new();

    public AlternativeParser MethodDeclaration = new();


    public SDSLGrammar UsingMethodDeclare()
    {
        Inner = MethodDeclaration;
        return this;
    }
    public void CreateMethodDeclaration()
    {
        var ws = WhiteSpace.Repeat(0);
        var ws1 = WhiteSpace.Repeat(1);


        ValueOrGeneric.Add(
            ValueTypes
            | Identifier.Then("<").Then(ValueOrGeneric.Then(Comma.Optional()).Repeat(1).SeparateChildrenBy(ws)).Then(">").SeparatedBy(ws)
            | Identifier
        );

        var declarePost =
            LeftBracket.Then(PrimaryExpression).Then(RightBracket).SeparatedBy(ws)
            | Colon.Then(Identifier).SeparatedBy(ws);

        var parameter = 
            StorageFlag.Optional()
            .Then(ValueOrGeneric)
            .Then(Identifier.Then())
            .SeparatedBy(ws1)
            .Then(declarePost.Optional())
            .SeparatedBy(ws);
        
        
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
    }
}