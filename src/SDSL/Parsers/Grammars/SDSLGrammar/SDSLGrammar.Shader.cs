using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;
using static SDSL.Parsing.Grammars.CommonParsers;



namespace SDSL.Parsing.Grammars.SDSL;
public partial class SDSLGrammar : Grammar
{
    public AlternativeParser Declarations = new();
    public SequenceParser ShaderExpression = new();
    public SequenceParser ResourceGroup = new() { Name = "ResourceGroup" };
    public SequenceParser ConstantBuffer = new() { Name = "ConstantBuffer" };
    public SequenceParser NamespaceExpression = new() {Name = "Namespace"};
    public AlternativeParser ShaderFile = new(){Name = "ShaderFile"};

    public SDSLGrammar UsingShader()
    {
        Inner = ShaderExpression;
        return this;
    }
    public virtual void CreateShader()
    {


        var typeDefinition = new SequenceParser(
            Literal("typedef") & " ",
            Identifier & " ",
            ~("<" & (Identifier | PrimaryExpression).Repeat(1).SeparatedBy(Spaces & "," & Spaces).Until(">") & ">").Named("TypedefGenerics"),
            Identifier,
            Semi
        )
        { Name = "TypeDef", Separator = Spaces};


        ConstantBuffer.Add(
            "cbuffer" & Spaces1 & Identifier.Repeat(1).SeparatedBy(Spaces & Dot & Spaces).Named("GroupName"),
            LeftBrace,
            ShaderValueDeclaration.Repeat(0).SeparatedBy(Spaces).Named("Variables"),
            RightBrace
        );
        ConstantBuffer.Separator = Spaces;
        ResourceGroup.Add(
            "rgroup" & Spaces1 & Identifier.Repeat(1).SeparatedBy(Spaces & Dot & Spaces).Named("GroupName"),
            LeftBrace,
            ShaderValueDeclaration.Repeat(0).SeparatedBy(Spaces).Named("Variables"),
            RightBrace
        );
        ResourceGroup.Separator = Spaces;


        var shaderGenericValue = new AlternativeParser(
            Literal("TypeName").Named("TypeName").Then(Identifier).SeparatedBy(Spaces1).Named("GenericType"),
            Literal("Semantic").Named("Semantic").Then(Identifier).SeparatedBy(Spaces1).Named("Semantic"),
            SimpleTypes.Then(Identifier).SeparatedBy(Spaces1).Named("GenericValue"),
            SimpleTypes
        ){ Name = "ShaderGeneric" }; 

        var shaderGenerics = new SequenceParser(
            "<",
            shaderGenericValue.Repeat(1).SeparatedBy(Spaces & Comma & Spaces),
            ">"
        ){ Name = "ShaderGenerics", Separator = Spaces };

        var inheritGenericsValues = new AlternativeParser(
            SimpleTypes,
            Identifier,
            Literals
        );

        var inheritGenerics = new SequenceParser(
            "<",
            inheritGenericsValues.Repeat(1).SeparatedBy(Spaces & Comma & Spaces),
            ">"
        ){ Separator = Spaces, Name = "Generics"};

        var compositionDeclaration = new SequenceParser(
            Literal("compose"),
            Spaces1,
            Identifier.Named("MixinName"),
            Spaces1,
            Identifier.Named("Name"),
            Spaces,
            Semi
        ){ Name = "CompositionDeclaration"};


        var shaderContentTypes = new AlternativeParser(
            typeDefinition,
            StructDefinition,
            ConstantBuffer,
            ResourceGroup,
            compositionDeclaration,
            MethodDeclaration,
            ShaderCompositionDeclaration,
            ShaderValueDeclaration

        );

        var shaderBody = new SequenceParser(
            LeftBrace,
            Spaces,
            shaderContentTypes.Repeat(0).SeparatedBy(Spaces).Until(Spaces & "}"),
            RightBrace
        )
        {Name = "Body", Separator = Spaces};

        var inheritances = 
            Colon
            .Then(
                Identifier.Named("Name").Then(inheritGenerics.Optional()).SeparatedBy(Spaces).Named("Mixin")
                .Repeat(1).SeparatedBy(Spaces & Comma & Spaces)
            )
            .SeparatedBy(Spaces)
            .Named("Mixins");



        ShaderExpression.Add(
            Literal("shader") & Spaces1 & Identifier.Named("ShaderName"),
            shaderGenerics.Optional(),
            inheritances.Optional(),
            shaderBody
        );
        ShaderExpression.Separator = Spaces;
        ShaderExpression.Name = "ShaderProgram";

        NamespaceExpression.Add(
            Spaces,
            Literal("namespace") & Spaces1 & Identifier.Repeat(1).SeparatedBy(Spaces & Dot & Spaces).Named("Namespace"),
            LeftBrace,
            ShaderExpression.Repeat(0),
            RightBrace,
            Spaces
        );
        NamespaceExpression.Separator = Spaces;

        ShaderFile.Add(
            ShaderExpression.Repeat(1),
            NamespaceExpression.Repeat(1)
        );
    }
}