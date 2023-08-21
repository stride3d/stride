using Eto.Parse;
using SDSL.Parsing.AST.Shader.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDSL.Parsing.AST.Shader;

public class ShaderProgram : ShaderToken
{
    public SymbolTable Symbols {get;set;}
    public string Name {get;set;}
    public List<ShaderGenerics>? Generics { get; set; }
    public List<MixinToken> Mixins { get; set; }
    public List<ShaderToken> Body { get; set; }

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
        // foreach (var s in Body.OfType<StructDefinition>())
        //     Symbols.PushType(s.StructName, s.Type);
        // Symbols.PushStreamType(Body.OfType<ShaderVariableDeclaration>());
        method.CreateInOutStream(Symbols);
        // Symbols.AddScope();
        //method.VariableChecking(Symbols);
        method.GenerateIl(Symbols);
        // Symbols.Pop();

        
        
        // return Symbols.Errors;
        return null;
    }
}