using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spv.Generator;
using Stride.Shaders.Parsing;

namespace Stride.Shaders.Mixer;

public partial class ShaderMixer : ShaderSource
{
    public ShaderSource Mixins { get; set; }

    public Dictionary<string,object> Variables = new();

    public ShaderMixer(string code)
    {
        Mixins = new ShaderStringSource(code);
    }
    public ShaderMixer(ShaderSource m)
    {
        Mixins = m;
    }

    public void AddMixin(ShaderSource mixin)
    {
        if(Mixins is ShaderStringSource sss)
        {
            var sas = new ShaderArraySource();
            sas.Add(Mixins);
            sas.Add(mixin);
            Mixins = sas;
        }
        else if(Mixins is ShaderArraySource sas)
            sas.Add(mixin);
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
