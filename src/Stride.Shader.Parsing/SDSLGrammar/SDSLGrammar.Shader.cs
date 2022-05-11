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
            ws,
            shaderGenericValue,
            ws,
            Comma.Then(shaderGenericValue).SeparatedBy(ws).Repeat(0),
            ws,
            ">"
        );

        var shaderContentTypes = new AlternativeParser();
        shaderContentTypes.Add(
            StructDefinition,
            //MethodDeclaration,
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

        var inheritances = Colon.Then(Identifier.Then(shaderGenerics.Optional()).Then(Comma.Then(Identifier.Then(shaderGenerics.Optional())).SeparatedBy(ws).Repeat(0))).SeparatedBy(ws).Optional();
        
        
        ShaderExpression.Add(
            ws,
            "shader" & ws1 & Identifier.Named("ShaderName").Then((ws1 & shaderGenerics).Optional()),
            shaderBody.Named("Body"),
            ";",
            ws
        );
        ShaderExpression.Separator = ws;
        ShaderExpression.Name = "ShaderProgram";
    }
}