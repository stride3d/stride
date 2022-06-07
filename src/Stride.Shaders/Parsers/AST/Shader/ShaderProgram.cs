using Eto.Parse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Parsing.AST.Shader;

public class ShaderProgram : ShaderToken
{
    public string Name {get;set;}
    public IEnumerable<ShaderGenerics> Generics { get; set; }
    public IEnumerable<Mixin> Mixins { get; set; }
    public IEnumerable<ShaderToken> Body { get; set; }

    public ShaderProgram(Match m)
    {
        Match = m;
        Name = m["ShaderName"].StringValue;
        Body = m["Body"].Matches.Select(GetToken).ToList();
    }
}