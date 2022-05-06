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

        var genericValue = 
            Literal("TypeName").Then(Identifier).SeparatedBy(ws1).Named("GenericType")
            | ValueTypes.Then(Identifier).SeparatedBy(ws1).Named("GenericValue")
            | ValueTypes;       

        var generics = 
            Literal("<")
            .Then(genericValue)
            .Then(
                Comma.Then(genericValue).SeparatedBy(ws).Repeat(0)
            )
            .Then(">").SeparatedBy(ws);

        var shaderContentTypes =
            GenericDeclaration
            | Entries
            | MethodDeclaration;

        var shaderBody = 
            LeftBrace.Then(shaderContentTypes.Repeat(0).SeparatedBy(ws)).Then(RightBrace).SeparatedBy(ws);

        var inheritances = Colon.Then(Identifier.Then(generics.Optional()).Then(Comma.Then(Identifier.Then(generics.Optional())).SeparatedBy(ws).Repeat(0))).SeparatedBy(ws).Optional();
        
        
        Shader.Add(
            Literal("shader")
            .Then(Identifier.Then(generics.Optional())).SeparatedBy(ws1).Then(inheritances.Named("Inherit"))
            .Then(shaderBody).Named("ShaderProgram")
            .Then(";").SeparatedBy(ws)
            .Then(ws)
        );
    }
}