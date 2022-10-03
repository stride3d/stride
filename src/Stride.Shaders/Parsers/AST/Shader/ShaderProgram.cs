using Eto.Parse;
using Stride.Shaders.Parsing.AST.Shader.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Parsing.AST.Shader;

public class ShaderProgram : ShaderToken
{
    public SymbolTable Symbols {get;set;}
    public string Name {get;set;}
    public IEnumerable<ShaderGenerics>? Generics { get; set; }
    public IEnumerable<MixinToken> Mixins { get; set; }
    public IEnumerable<ShaderToken> Body { get; set; }

    public ShaderProgram(Match m)
    {
        Match = m;
        Symbols = new();
        Name = m["ShaderName"].StringValue;
        Body = m["Body"].Matches.Select(x => GetToken(x,Symbols)).ToList();
        Mixins = m["Mixins"].Matches.Select(x => new MixinToken(x)).ToList();
    }

    public ErrorList SemanticChecks<T>() where T : MainMethod
    {
        var method = Body.OfType<T>().First();
        foreach (var s in Body.OfType<StructDefinition>())
            Symbols.PushType(s.StructName, s.Type);
        Symbols.PushStreamType(Body.OfType<ShaderVariableDeclaration>());
        method.CreateInOutStream(Symbols);
        Symbols.AddScope();
        //method.VariableChecking(Symbols);
        method.GenerateIl(Symbols);
        Symbols.Pop();

        
        
        return Symbols.Errors;
    }
}