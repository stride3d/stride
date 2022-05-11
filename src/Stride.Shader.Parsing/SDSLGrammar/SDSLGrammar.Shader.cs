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

        var shaderGenericValue = new AlternativeParser();
        shaderGenericValue.Add(
            Literal("TypeName").Then(Identifier).SeparatedBy(ws1).Named("GenericType"),
            ValueTypes.Then(Identifier).SeparatedBy(ws1).Named("GenericValue"),
            ValueTypes
        );

        var shaderGenerics = new SequenceParser();
        shaderGenerics.Add(
            "<",
            shaderGenericValue.Repeat(1).SeparatedBy(ws & Comma & ws),
            ">"
        );
        shaderGenerics.Separator = ws;

        var inheritGenericsValues = new AlternativeParser(
            ValueTypes,
            Identifier,
            Literals
        );

        var inheritGenerics = new SequenceParser();
        inheritGenerics.Add(
            "<",
            inheritGenericsValues.Repeat(1).SeparatedBy(ws & Comma & ws),
            ">"
        );
        inheritGenerics.Separator = ws;

        var shaderContentTypes = new AlternativeParser();
        shaderContentTypes.Add(
            StructDefinition,
            MethodDeclaration,
            ShaderValueDeclaration

            // | Attribute
        );

        var shaderBody = new SequenceParser();
        shaderBody.Add(
            LeftBrace,
            shaderContentTypes.Repeat(0).SeparatedBy(ws),
            RightBrace
        );
        shaderBody.Separator = ws;

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
            ";",
            ws
        );
        ShaderExpression.Separator = ws;
        ShaderExpression.Name = "ShaderProgram";
    }
}