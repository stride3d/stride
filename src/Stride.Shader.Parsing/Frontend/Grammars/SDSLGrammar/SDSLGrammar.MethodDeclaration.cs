using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parsing.Grammars.SDSL;
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

        var abstractMethod = new SequenceParser(
            Literal("abstract"),
            ws1,
            ~Literal("stage"),
            ws1,
            Identifier,
            ws1,
            Identifier,
            ws,
            ParameterList,
            ws,
            Semi
        )
        { Name = "AbstractMethod"};

        var method = new SequenceParser(
            Attribute.Repeat(0).SeparatedBy(ws),
            ~(Stage.Named("Stage") & WhiteSpace),
            ~((Literal("override").Named("Override") | Literal("static").Named("Static")) & ws1),
            Identifier.Named("ReturnType") & ws1 & Identifier.Named("MethodName"),
            ParameterList,
            LeftBrace,
            Statement.Repeat(0).SeparatedBy(ws).Until("}"),
            RightBrace
        )
        { Name = "Method", Separator = ws};

        MethodDeclaration.Add(
            abstractMethod,
            method
        );
    }
}