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
            | ValueTypes.Then(Identifier).SeparatedBy(ws1).Named("GenericValue");       

        var generics = 
            Literal("<").Then(Comma.Optional().Then(genericValue).Repeat(1)).Then(">").SeparatedBy(ws);

        var mixins = 
            Comma.Optional().Then(Identifier).Then(generics).SeparatedBy(ws).Repeat(0).SeparatedBy(ws);

        var comments = 
            (SingleLineComment
            | BlockComment).Repeat(0);
        var shaderContentTypes =
            GenericDeclaration
            | MethodDeclaration
            | comments;

        var shaderBody = 
            LeftBrace.Then(shaderContentTypes.Repeat(0).SeparatedBy(ws)).Then(RightBrace).SeparatedBy(ws);

        Shader.Add(
            Literal("shader")
            .Then(Identifier.Then(generics.Optional()).SeparatedBy(ws)).SeparatedBy(ws1)
            //.Then(Literal(":").Then(mixins).SeparatedBy(ws).Optional())
            .Then(shaderBody).Named("ShaderProgram")
            .Then(";").SeparatedBy(ws)
        );
    }
}