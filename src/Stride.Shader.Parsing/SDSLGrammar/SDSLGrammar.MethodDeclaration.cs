using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parsing;
public partial class SDSLGrammar : Grammar
{
    public SequenceParser ParameterList = new() {Name = "ParameterList"};
    public SequenceParser ValueOrGeneric = new() {Name = "ValueOrGeneric"};

    public AlternativeParser MethodDeclaration = new() { Name = "MethodDeclaration" };


    public SDSLGrammar UsingMethodDeclare()
    {
        Inner = MethodDeclaration;
        return this;
    }
    public void CreateMethodDeclaration()
    {
        var ws = WhiteSpace.Repeat(0);
        var ws1 = WhiteSpace.Repeat(1);

        var genericsList = new SequenceParser { Name = "ShaderGenerics", Separator = ws };

        var parameterGenericsValues = new AlternativeParser(
            ValueTypes,
            Identifier.Then(genericsList.Optional()).SeparatedBy(ws)
        )
        { Name = "ParameterGenericValue" };

        genericsList.Add(
            "<",
            parameterGenericsValues.Repeat(1).SeparatedBy(ws & Comma & ws),
            ">"
        );

        ValueOrGeneric.Add(
            ValueTypes | Identifier,
            genericsList.Optional()
        );

        var declarePost =
            LeftBracket.Then(PrimaryExpression).Then(RightBracket).SeparatedBy(ws)
            | Colon.Then(Identifier).SeparatedBy(ws);

        var arraySpecifier =
            (LeftBracket & Literals & RightBracket)
            .SeparatedBy(ws);

        var parameter = new SequenceParser(
            ValueOrGeneric,
            ws1,
            Identifier,
            arraySpecifier.Optional(),
            (Equal & PrimaryExpression).SeparatedBy(ws).Optional()
        );
        var parameterWithStorage = new AlternativeParser(
            StorageFlag & ws1 & parameter,
            parameter
        );


        ParameterList.Add(
            LeftParen,
            parameterWithStorage.Repeat(0).SeparatedBy(ws & Comma & ws),
            RightParen
        );
        ParameterList.Separator = ws;

        MethodDeclaration.Add(
            // Abstract method
            Literal("abstract").Then(Literal("stage").Optional()).Then(Identifier).Then(Identifier).SeparatedBy(ws1)
                .Then(ParameterList).Then(Semi).SeparatedBy(ws).Named("AbstractMethod"),
            // Override or normal method
            Attribute.Repeat(0).SeparatedBy(ws)
            .Then(Literal("override").Optional())
            .Then(Literal("stage").Optional())
                .Then(Identifier).Then(Identifier).SeparatedBy(ws1)
                    .Then(ParameterList)
                .Then(LeftBrace)
                    .Then(Statement.Repeat(0).SeparatedBy(ws))
                .Then(RightBrace).SeparatedBy(ws).Named("MethodDeclaration")
        );
    }
}