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
        var typefunc = TypeFunction(TypeVoid());
        var func = Function(TypeVoid(), FunctionControlMask.MaskNone, typefunc);
        AddLabel(Label());
        // foreach(var s in ((VSMainMethod)p.Body.First(x => x is VSMainMethod)).Statements)
        // {

        // }
        Return();
        FunctionEnd();
        AddEntryPoint(ExecutionModel.Vertex, func, "VSMain", input,output);

        // AddExecutionMode(func, ExecutionMode.OriginLowerLeft);
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
