using Eto.Parse;
using Eto.Parse.Parsers;
using static Eto.Parse.Terminals;

namespace Stride.Shader.Parsing;
public partial class SDSLGrammar : Grammar
{
    public AlternativeParser Declarations = new();
    public AlternativeParser Shader = new();
    

    public SDSLGrammar UsingShader()
    {
        Inner = Shader;
        return this;
    }
    public void CreateShader()
    {
        var ws = WhiteSpace.Repeat(0);
        var ws1 = WhiteSpace.Repeat(1);

        var shaderGenericValue = 
            Literal("TypeName").Then(Identifier).SeparatedBy(ws1).Named("GenericType")
            | ValueTypes.Then(Identifier).SeparatedBy(ws1).Named("GenericValue")
            | ValueTypes;       

        var shaderGenerics = 
            Literal("<")
            .Then(shaderGenericValue)
            .Then(
                Comma.Then(shaderGenericValue).SeparatedBy(ws).Repeat(0)
            )
            .Then(">").SeparatedBy(ws);

        var shaderContentTypes =
            MethodDeclaration
            | ShaderValueDeclaration
            // | Attribute
            ;

        var shaderBody = 
            LeftBrace.Then(shaderContentTypes.Repeat(0).SeparatedBy(ws)).Then(RightBrace).SeparatedBy(ws);

        var inheritances = Colon.Then(Identifier.Then(shaderGenerics.Optional()).Then(Comma.Then(Identifier.Then(shaderGenerics.Optional())).SeparatedBy(ws).Repeat(0))).SeparatedBy(ws).Optional();
        
        
        Shader.Add(
            ws &
            Literal("shader")
            .Then(Identifier.Then(shaderGenerics.Optional())).SeparatedBy(ws1)
            .Then(shaderBody.Or(inheritances.Named("Inherit").Then(shaderBody).SeparatedBy(ws)))
            .Then(";").SeparatedBy(ws).Named("ShaderProgram")
            & ws
        );
    }
}