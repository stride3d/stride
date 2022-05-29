using Eto.Parse;
using Stride.Shader.Parsing.AST.Shader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shader.Parsing.AST.Shader;

public class Statement : ShaderToken {}

public class AssignChain : Statement
{
    public AssignChain(Match m)
    {
        Match = m;
    }
}
