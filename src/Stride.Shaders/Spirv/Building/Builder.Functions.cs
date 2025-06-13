using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Building;

public partial class SpirvBuilder
{
    public SpirvFunction CreateFunction(SpirvContext context, string name, FunctionType ftype, FunctionControlMask mask = FunctionControlMask.None)
    {
        foreach(var t in ftype.ParameterTypes)
            context.GetOrRegister(t);
        var func = Buffer.AddOpFunction(context.Bound++, context.GetOrRegister(ftype.ReturnType), mask, context.GetOrRegister(ftype));
        Position = Buffer.Instructions.Count;
        context.AddName(func, name);
        var result = new SpirvFunction(func.ResultId!.Value, name, ftype);
        CurrentFunction = result;
        context.Module.Functions.Add(name, result);
        return result;
    }

    public void EndFunction(SpirvContext context)
    {
        Buffer.InsertOpFunctionEnd(Position++);
    }

    public SpirvValue AddFunctionParameter(SpirvContext context, string name, SymbolType type)
    {
        var p = Buffer.InsertOpFunctionParameter(Position++, context.Bound++, context.GetOrRegister(type));
        context.AddName(p, name);
        CurrentFunction!.Value.Parameters.Add(name, new(p, name));
        return new(p, name);
    }
    public SpirvFunction CreateEntryPoint(SpirvContext context, ExecutionModel execModel, string name, FunctionType type, ReadOnlySpan<Symbol> variables, FunctionControlMask mask = FunctionControlMask.None)
    {
        var func = Buffer.AddOpFunction(context.Bound++, context.GetOrRegister(type.ReturnType), mask, context.GetOrRegister(type));
        context.AddName(func, name);
        context.SetEntryPoint(execModel, func, name, variables);
        var result = new SpirvFunction(func.ResultId!.Value, name, type);
        if(!variables.IsEmpty)
            foreach(var p in variables)
                context.AddName(context.Variables[p.Id.Name].Id, p.Id.Name);
        CurrentFunction = result;
        return result;
    }
    
    
}