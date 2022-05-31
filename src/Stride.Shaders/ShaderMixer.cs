using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spv.Generator;

namespace Stride.Shaders.Parsing;

public class ShaderMixer
{
    //Dictionary<string, int> 
    public SDSLParser Parser {get;set;}
    public List<ShaderMixin> Mixins { get; set; } = new();

    public ShaderMixer()
    {
        Parser = new();
    }
    public ShaderMixer(SDSLParser parser)
    {
        Parser = parser;
    }

    public void Add(string mixin)
    {
        Mixins.Add(new ShaderMixin(mixin,Parser));
    }
}
