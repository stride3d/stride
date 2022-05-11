using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parsing;
public partial class SDSLGrammar : Grammar
{
    public AlternativeParser Declarations = new();
    public SequenceParser ShaderExpression = new();
    

    public SDSLGrammar UsingShader()
    {
        Inner = ShaderExpression;
        return this;
    }
    public void CreateShader()
    {
        var ws = WhiteSpace.Repeat(0);
        var ws1 = WhiteSpace.Repeat(1);

        var shaderGenericValue = new AlternativeParser(
            Literal("TypeName").Named("TypeName").Then(Identifier).SeparatedBy(ws1).Named("GenericType"),
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

        var shaderContentTypes = new AlternativeParser();
        shaderContentTypes.Add(
            StructDefinition,
            MethodDeclaration,
            ShaderValueDeclaration

            // | Attribute
        );

        var shaderBody = new SequenceParser(
            LeftBrace,
            shaderContentTypes.Repeat(0).SeparatedBy(ws),
            RightBrace
        ){Separator = ws};

        var inheritances = 
            Colon
            .Then(
                Identifier.Then(inheritGenerics.Optional()).SeparatedBy(ws)
                .Repeat(1).SeparatedBy(ws & Comma & ws)
            )
            .SeparatedBy(ws);
        
        
        ShaderExpression.Add(
            ws,
            "shader" & ws1 & Identifier.Named("ShaderName"),
            shaderGenerics.Optional(),
            inheritances.Optional(),
            shaderBody.Named("Body"),
            Semi,
            ws
        );
        ShaderExpression.Separator = ws;
        ShaderExpression.Name = "ShaderProgram";
    }
}