using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shaders.Parsing.Grammars.SDSL;
public partial class SDSLGrammar : Grammar
{
    public AlternativeParser Declarations = new();
    public SequenceParser ShaderExpression = new();
    public SequenceParser ConstantBuffer = new() { Name = "ConstantBuffer" };


    public SDSLGrammar UsingShader()
    {
        Inner = ShaderExpression;
        return this;
    }
    public void CreateShader()
    {
        var ws = WhiteSpace.Repeat(0);
        var ws1 = WhiteSpace.Repeat(1);


        var typeDefinition = new SequenceParser(
            Literal("typedef") & " ",
            Identifier & " ",
            ~("<" & (Identifier | PrimaryExpression).Repeat(1).SeparatedBy(ws & "," & ws).Until(">") & ">").Named("TypedefGenerics"),
            Identifier,
            Semi
        )
        { Name = "TypeDef", Separator = ws};


        ConstantBuffer.Add(
            "cbuffer",
            ws1,
            Identifier,
            ws,
            LeftBrace,
            ws,
            RightBrace
        );


        var shaderGenericValue = new AlternativeParser(
            Literal("TypeName").Named("TypeName").Then(Identifier).SeparatedBy(ws1).Named("GenericType"),
            Literal("Semantic").Named("Semantic").Then(Identifier).SeparatedBy(ws1).Named("Semantic"),
            ValueTypes.Then(Identifier).SeparatedBy(ws1).Named("GenericValue"),
            ValueTypes
        ){ Name = "ShaderGeneric" }; 

        var shaderGenerics = new SequenceParser(
            "<",
            shaderGenericValue.Repeat(1).SeparatedBy(ws & Comma & ws),
            ">"
        ){ Name = "ShaderGenerics", Separator = ws };

        var inheritGenericsValues = new AlternativeParser(
            ValueTypes,
            Identifier,
            Literals
        );

        var inheritGenerics = new SequenceParser(
            "<",
            inheritGenericsValues.Repeat(1).SeparatedBy(ws & Comma & ws),
            ">"
        ){ Separator = ws, Name = "InheritanceGenerics"};

        var compositionDeclaration = new SequenceParser(
            Literal("compose"),
            ws1,
            Identifier.Named("MixinName"),
            ws1,
            Identifier.Named("Name"),
            ws,
            Semi
        ){ Name = "CompositionDeclaration"};


        var shaderContentTypes = new AlternativeParser(
            typeDefinition,
            StructDefinition,
            compositionDeclaration,
            MethodDeclaration,
            ShaderValueDeclaration

        );

        var shaderBody = new SequenceParser(
            LeftBrace,
            shaderContentTypes.Repeat(0).SeparatedBy(ws).Until(ws & "}"),
            RightBrace
        )
        {Name = "Body", Separator = ws};

        var inheritances = 
            Colon
            .Then(
                Identifier.Then(inheritGenerics.Optional()).SeparatedBy(ws)
                .Repeat(1).SeparatedBy(ws & Comma & ws)
            )
            .SeparatedBy(ws);



        ShaderExpression.Add(
            ws,
            Literal("shader") & ws1 & Identifier.Named("ShaderName"),
            shaderGenerics.Optional(),
            inheritances.Optional(),
            shaderBody,
            Semi,
            ws
        );
        ShaderExpression.Separator = ws;
        ShaderExpression.Name = "ShaderProgram";
    }
}