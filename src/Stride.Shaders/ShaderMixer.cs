using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spv.Generator;
using Stride.Shaders.Parsing;

namespace Stride.Shaders;

public class ShaderMixer : ShaderSource
{
    public ShaderMixinParser Parser {get;set;}
    public List<ShaderMixin> Mixins { get; set; } = new();

    public ShaderMixer()
    {
        Parser = new();
    }
    public ShaderMixer(ShaderMixinParser parser)
    {
        Parser = parser;
    }

    public void Add(string mixin)
    {
        Mixins.Add(new ShaderMixin(mixin,Parser));
    }

    public override object Clone()
    {
        throw new NotImplementedException();
    }

    public override bool Equals(object against)
    {
        throw new NotImplementedException();
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}
