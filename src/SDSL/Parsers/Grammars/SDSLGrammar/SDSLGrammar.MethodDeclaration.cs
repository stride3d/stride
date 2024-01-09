using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;
using static SDSL.Parsing.Grammars.CommonParsers;

namespace SDSL.Parsing.Grammars.SDSL;
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
        

        var genericsList = new SequenceParser { Name = "ShaderGenerics", Separator = Spaces };

        var parameterGenericsValues = new AlternativeParser(
            SimpleTypes,
            Identifier.Then(genericsList.Optional()).SeparatedBy(Spaces)
        )
        { Name = "ParameterGenericValue" };

        genericsList.Add(
            "<",
            parameterGenericsValues.Repeat(1).SeparatedBy(Spaces & Comma & Spaces),
            ">"
        );

        ValueOrGeneric.Add(
            SimpleTypes | Identifier,
            genericsList.Optional()
        );

        var declarePost =
            LeftBracket.Then(PrimaryExpression).Then(RightBracket).SeparatedBy(Spaces)
            | Colon.Then(Identifier).SeparatedBy(Spaces);

        var arraySpecifier =
            (LeftBracket & PrimaryExpression & RightBracket)
            .SeparatedBy(Spaces);

        var parameter = new SequenceParser(
            ValueOrGeneric,
            Spaces1,
            (Identifier & Spaces & arraySpecifier) | Identifier,
            (Equal & Spaces & PrimaryExpression).Optional()
        );
        var parameterWithStorage = new AlternativeParser(
            StorageFlag & Spaces1 & parameter,
            parameter
        )
        { Name = "MethodParameter" };


        ParameterList.Add(
            LeftParen,
            parameterWithStorage.Repeat(0).SeparatedBy(Spaces & Comma & Spaces),
            RightParen
        );
        ParameterList.Separator = Spaces;

        var abstractMethod = new SequenceParser(
            Literal("abstract"),
            Spaces1,
            ~Literal("stage"),
            Spaces1,
            Identifier,
            Spaces1,
            Identifier,
            Spaces,
            ParameterList,
            Spaces,
            Semi
        )
        { Name = "AbstractMethod"};

        var method = new SequenceParser(
            Attribute.Repeat(0).SeparatedBy(Spaces),
            ~(Stage.Named("Stage") & WhiteSpace),
            ~((Literal("override").Named("Override") | Literal("static").Named("Static")) & Spaces1),
            ValueTypes.Named("ReturnType") & Spaces1 & Identifier.Named("MethodName"),
            ParameterList,
            LeftBrace,
            Statement.Repeat(0).SeparatedBy(Spaces).Until("}").Named("Statements"),
            RightBrace
        )
        { Name = "Method", Separator = Spaces};

        MethodDeclaration.Add(
            abstractMethod,
            method
        );
    }
}