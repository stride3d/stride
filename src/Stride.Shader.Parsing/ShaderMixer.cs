using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spv.Generator;

namespace Stride.Shader.Parsing;

public class ShaderMixer : Module
{
    //Dictionary<string, int> 
    List<ShaderMixin> Mixins { get; set; } = new();

    public ShaderMixer(uint version) : base(version)
    {
    }

}
