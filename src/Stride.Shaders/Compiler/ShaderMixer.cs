using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spv.Generator;
using Stride.Shaders.Parsing;

namespace Stride.Shaders.Compiling;

public partial class ShaderMixer : ShaderSource
{
    public ShaderSource Mixins { get; set; }
    public MixinVirtualTable LocalVTable {get;set;}

    public MixinVirtualTable VirtualVTable {get;set;}

    public Dictionary<string,object> Variables = new();

    public ShaderMixer(string code)
    {

    }
    // public ShaderMixer(string code, Dictionary<string,object> macros)
    // {
        
    // }

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
