using Stride.Shader.Parsing.AST.Shader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shader.Parsing;

public class ShaderMixin
{
    public string Code { get; set; }
    public string MixinName { get; set; }
    public ShaderToken AST { get; set; }

}
