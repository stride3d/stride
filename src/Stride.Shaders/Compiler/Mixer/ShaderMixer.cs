using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spv.Generator;
using Stride.Shaders.Parsing;

namespace Stride.Shaders.Mixer;

public partial class ShaderMixer
{
    public ShaderSource Mixins { get; set; }
    public ShaderLoader Loader {get;set;}

    public Dictionary<string,object> Variables = new();

    public ShaderMixer(string code, ShaderLoader loader)
    {
        Loader = loader;
        var mixin = new ShaderClassString(code);
        Mixins = new ShaderArraySource(mixin.MixinNames.Select(loader.Get).Select(x => new ShaderClassString(x)));
    }

    public void AddMixin(ShaderSource mixin)
    {
        if (Mixins is ShaderClassString)
        {
            var sas = new ShaderArraySource
            {
                Mixins,
                mixin
            };
            Mixins = sas;
        }
        else if(Mixins is ShaderArraySource sas)
            sas.Add(mixin);
    }
}
