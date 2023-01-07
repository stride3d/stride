using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace SDSL.Parsing.Grammars.SDSL;
public class SDSLMixinReader : SDSLGrammar
{
    public override void CreateAll()
    {
        CreateTokens();
        CreateTokenGroups();
        CreateLiterals();
        CreateExpressions();
    }
    public override void CreateShader()
    {
        var ws = WhiteSpace.Repeat(0);
        var ws1 = WhiteSpace.Repeat(1);


        var shaderGenericValue = new AlternativeParser(
            Literal("TypeName").Named("TypeName").Then(Identifier).SeparatedBy(ws1).Named("GenericType"),
            Literal("Semantic").Named("Semantic").Then(Identifier).SeparatedBy(ws1).Named("Semantic"),
            SimpleTypes.Then(Identifier).SeparatedBy(ws1).Named("GenericValue"),
            SimpleTypes
        ){ Name = "ShaderGeneric" }; 

        var shaderGenerics = new SequenceParser(
            "<",
            shaderGenericValue.Repeat(1).SeparatedBy(ws & Comma & ws),
            ">"
        ){ Name = "ShaderGenerics", Separator = ws };

        var inheritGenericsValues = new AlternativeParser(
            SimpleTypes,
            Identifier,
            Literals
        );

        var inheritGenerics = new SequenceParser(
            "<",
            inheritGenericsValues.Repeat(1).SeparatedBy(ws & Comma & ws),
            ">"
        ){ Separator = ws, Name = "Generics"};

        var compositionDeclaration = new SequenceParser(
            Literal("compose"),
            ws1,
            Identifier.Named("MixinName"),
            ws1,
            Identifier.Named("Name"),
            ws,
            Semi
        ){ Name = "CompositionDeclaration"};


        var shaderBody = new SequenceParser(
            LeftBrace,
            AnyChar.Repeat(0).Until("}"),
            RightBrace
        )
        {Name = "Body", Separator = ws};

        var inheritances = 
            Colon
            .Then(
                Identifier.Named("Name").Then(inheritGenerics.Optional()).SeparatedBy(ws).Named("Mixin")
                .Repeat(1).SeparatedBy(ws & Comma & ws)
            )
            .SeparatedBy(ws)
            .Named("Mixins");



        ShaderExpression.Add(
            Literal("shader") & ws1 & Identifier.Named("ShaderName"),
            shaderGenerics.Optional(),
            inheritances.Optional(),
            shaderBody,
            Semi
        );
        ShaderExpression.Separator = ws;
        ShaderExpression.Name = "ShaderProgram";

        NamespaceExpression.Add(
            ws,
            Literal("namespace") & ws1 & Identifier.Repeat(1).SeparatedBy(ws & Dot & ws).Named("Namespace"),
            LeftBrace,
            ShaderExpression,
            RightBrace,
            ws
        );
        NamespaceExpression.Separator = ws;

        ShaderFile.Add(
            NamespaceExpression,
            ws & ShaderExpression & ws
        );
        this.Inner = ShaderFile;
    }
}