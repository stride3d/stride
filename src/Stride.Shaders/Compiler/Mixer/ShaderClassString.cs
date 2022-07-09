using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.AST.Shader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Mixer;

public class ShaderClassString : ShaderMixin
{
    string source;
    public override string Code => source;
    public override string ShaderName => AST.Name;


    public ShaderClassString(string code)
    {
        this.source = code;
    }

    public void Parse()
    {
        AST = ShaderMixinParser.ParseShader(source);
    }

    public override object Clone()
    {
        throw new NotImplementedException();
    }

    public override void EnumerateMixins(SortedSet<ShaderSource> shaderSources)
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
