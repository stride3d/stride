using SDSL.Parsing;
using SDSL.Parsing.AST.Shader;

namespace SDSL;


public class ShaderSourceProvider
{
    public static ShaderSourceProvider Instance { get; } = new();

    public Dictionary<string, string> Sources;
    public Dictionary<string, ShaderProgram> Trees;
    private ShaderSourceProvider()
    {
        Sources = new();
        Trees = new();
    }


    public static void Register(string shaderCode)
    {
        var shaderProgram = ShaderMixinParser.ParseShader(shaderCode);
        var name = shaderProgram.Name;

        Instance.Sources[name] =shaderCode;
        Instance.Trees[name] = shaderProgram;
    }

    public static ShaderProgram GetTree(string name) => Instance.Trees[name];
    public static string GetSource(string name) => Instance.Sources[name];
}