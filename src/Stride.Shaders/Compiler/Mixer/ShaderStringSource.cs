using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.AST.Shader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Mixer;

public class ShaderStringSource : ShaderSource
{

    public string Code { get; set; }
    public string ClassName => AST is null ? "" : AST.Name;
    public ShaderProgram? AST { get; set; }

    public ShaderStringSource(string code)
    {
        Code = code;
        AST = ShaderMixinParser.ParseShader(code);
    }

    public override object Clone()
    {
        return new ShaderStringSource(Code);
    }

    public override bool Equals(object against)
    {
        return against is ShaderStringSource other 
            && this.Code == other.Code
            && this.AST == other.AST;
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }

    public override void EnumerateMixins(SortedSet<ShaderSource> shaderSources)
    {
        throw new NotImplementedException();
    }
}
