using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spv.Generator;
using Stride.Shaders.Mixer;
using Stride.Shaders.Parsing.AST.Shader;
using static Spv.Specification;

namespace Stride.Shaders.Spirv;

public partial class SpirvEmitter : Module
{
    public void MainMethod(EntryPoints entry, ShaderProgram p)
    {
        (entry switch
        {
            EntryPoints.VSMain => (Action<ShaderProgram>)VSMethod,
            EntryPoints.PSMain => (Action<ShaderProgram>)PSMethod,
            EntryPoints.CSMain => (Action<ShaderProgram>)CSMethod,
            EntryPoints.GSMain => (Action<ShaderProgram>)GSMethod,
            EntryPoints.HSMain => (Action<ShaderProgram>)HSMethod,
            EntryPoints.DSMain => (Action<ShaderProgram>)DSMethod,
            _ => throw new NotImplementedException()
        })(p);
    }
    public void VSMethod(ShaderProgram p)
    {
        
    }
    public void PSMethod(ShaderProgram p)
    {
        
    }
    public void GSMethod(ShaderProgram p)
    {
        
    }
    public void CSMethod(ShaderProgram p)
    {
        
    }
    public void HSMethod(ShaderProgram p)
    {
        
    }
    public void DSMethod(ShaderProgram p)
    {
        
    }
}
