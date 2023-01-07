using SDSL.Parsing;
using SDSL.Parsing.AST.Shader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDSL.Mixer;

public class ShaderClassString : ShaderSource
{
    public string ShaderSourceCode {get;set;}

    public ShaderClassString(string code)
    {
        ShaderSourceCode = code;
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
