using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Building;

public partial class SpirvBuilder
{
    public SpirvFunction CreateFunction(SpirvContext context, string name, FunctionType ftype, FunctionControlMask mask = FunctionControlMask.None)
    {
        foreach(var t in ftype.ParameterTypes)
            context.GetOrRegister(t);
        Buffer.FluentAdd(new OpFunction(context.GetOrRegister(ftype.ReturnType), context.Bound++, mask, context.GetOrRegister(ftype)), out var func);
        Position = Buffer.Count;
        context.AddName(func, name);
        var result = new SpirvFunction(func.ResultId, name, ftype);
        CurrentFunction = result;
        context.Module.Functions.Add(name, result);
        return result;
    }

    public void EndFunction()
    {
        // If there was no explicit return, add one
        var lastInstruction = Buffer[Position];
        if (lastInstruction.Op == Op.OpUnreachable)
        {
            if (CurrentFunction.Value.FunctionType.ReturnType != ScalarType.From("void"))
                throw new InvalidOperationException("No function termination, but a return value is expected");

            Return(null);
        }

        Buffer.Insert(Position++, new OpFunctionEnd());
    }

    public SpirvValue AddFunctionParameter(SpirvContext context, string name, SymbolType type)
    {
        var p = Buffer.Insert(Position++, new OpFunctionParameter(context.GetOrRegister(type), context.Bound++));
        context.AddName(p, name);
        var value = new SpirvValue(p.ResultId, p.ResultType, name);
        CurrentFunction!.Value.Parameters.Add(name, value);
        return value; 
    }
    public SpirvFunction CreateEntryPoint(SpirvContext context, ExecutionModel execModel, string name, FunctionType type, ReadOnlySpan<Symbol> variables, FunctionControlMask mask = FunctionControlMask.None)
    {
        Buffer.FluentAdd(new OpFunction(context.GetOrRegister(type.ReturnType), context.Bound++, mask, context.GetOrRegister(type)), out var func);
        context.AddName(func, name);
        context.SetEntryPoint(execModel, func, name, variables);
        var result = new SpirvFunction(func.ResultId, name, type);
        if(!variables.IsEmpty)
            foreach(var p in variables)
                context.AddName(context.Variables[p.Id.Name].Id, p.Id.Name);
        CurrentFunction = result;
        return result;
    }
    
    
}