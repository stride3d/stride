using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using static Spv.Specification;

namespace Stride.Shaders.Spirv.Building;

public partial class SpirvBuilder
{
    public SpirvFunction CreateFunction(SpirvContext context, string name, FunctionType ftype, FunctionControlMask mask = FunctionControlMask.MaskNone)
    {
        foreach(var t in ftype.ParameterTypes)
            context.GetOrRegister(t);
        var func = Buffer.AddOpFunction(context.Bound++, context.GetOrRegister(ftype.ReturnType), mask, context.GetOrRegister(ftype));
        context.AddName(func, name);
        var result = new SpirvFunction(func.ResultId!.Value, name, ftype);
        Buffer.AddOpFunctionEnd();
        CurrentFunction = result;
        return result;
    }

    public SpirvValue AddFunctionParameter(SpirvContext context, string name, SymbolType type)
    {
        var p = Buffer.InsertOpFunctionParameter(Position, context.Bound++, context.GetOrRegister(type));
        Position += p.WordCount;
        context.AddName(p, name);
        CurrentFunction!.Value.Parameters.Add(name, new(p, name));
        return new(p, name);
    }
    public SpirvFunction CreateEntryPoint(SpirvContext context, ExecutionModel execModel, string name, FunctionType type, ReadOnlySpan<Symbol> variables, FunctionControlMask mask = FunctionControlMask.MaskNone)
    {
        var func = Buffer.AddOpFunction(context.Bound++, context.GetOrRegister(type.ReturnType), mask, context.GetOrRegister(type));
        context.AddName(func, name);
        context.SetEntryPoint(execModel, func, name, variables);
        var result = new SpirvFunction(func.ResultId!.Value, name, type);
        if(!variables.IsEmpty)
            foreach(var p in variables)
                context.AddName(context.Variables[p.Id.Name], p.Id.Name);
        Position += Buffer.InsertOpFunctionEnd(Position).WordCount;
        CurrentFunction = result;
        return result;
    }
    
    
}