using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parsing;
public partial class SDSLGrammar : Grammar
{
    public SequenceParser ParameterList = new();
    public SequenceParser ValueOrGeneric = new();

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

        var genericsList = new SequenceParser();
        genericsList.Add(
            "<",
            (Identifier & genericsList.Optional())
                .Repeat(0).SeparatedBy(ws & Comma & ws),
            ">"
        );
        genericsList.Separator = ws;

        ValueOrGeneric.Add(
            ValueTypes | Identifier,
            genericsList.Optional()
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
            LeftParen,
            parameter.Repeat(0).SeparatedBy(ws & Comma & ws),
            RightParen
        );
        ParameterList.Separator = ws;

        MethodDeclaration.Add(
            // Abstract method
            Literal("abstract").Then(Literal("stage").Optional()).Then(Identifier).Then(Identifier).SeparatedBy(ws1)
                .Then(ParameterList).Then(Semi).SeparatedBy(ws).Named("AbstractMethod"),
            // Override or normal method
            Literal("override").Optional()
            .Then(Literal("stage").Optional())
                .Then(Identifier).Then(Identifier).SeparatedBy(ws1)
                    .Then(ParameterList)
                .Then(LeftBrace)
                    .Then(Statement.Repeat(0).SeparatedBy(ws))
                .Then(RightBrace).SeparatedBy(ws).Named("MethodDeclaration")
        );
    }
}