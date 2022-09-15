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
    public IEnumerable<ShaderGenerics> Generics { get; set; }
    public IEnumerable<MixinToken> Mixins { get; set; }
    public IEnumerable<ShaderToken> Body { get; set; }

    public ShaderProgram(Match m, SymbolTable symbols)
    {
        Match = m;
        Symbols = symbols;
        Name = m["ShaderName"].StringValue;
        Body = m["Body"].Matches.Select(x => GetToken(x,symbols)).ToList();
        Mixins = m["Mixins"].Matches.Select(x => new MixinToken(x)).ToList();
    }
}