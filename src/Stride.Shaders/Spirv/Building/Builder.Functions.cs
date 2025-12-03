using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;
using System;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Building;

public partial class SpirvBuilder
{
    public SpirvFunction DeclareFunction(SpirvContext context, string name, FunctionType ftype, bool isStage = false)
    {
        var func = context.Bound++;
        foreach (var t in ftype.ParameterTypes)
            context.GetOrRegister(t);
        context.AddName(func, name);
        var result = new SpirvFunction(func, name, ftype) { IsStage = isStage };
        return result;
    }

    public void BeginFunction(SpirvContext context, SpirvFunction function, FunctionControlMask mask = FunctionControlMask.None)
    {
        Buffer.FluentAdd(new OpFunction(context.GetOrRegister(function.FunctionType.ReturnType), function.Id, mask, context.GetOrRegister(function.FunctionType)), out var func);
        Position = Buffer.Count;
        CurrentFunction = function;
    }

    public void EndFunction()
    {
        // If there was no explicit return, add one
        var lastInstruction = Buffer[Position - 1];
        if (!IsBlockTermination(lastInstruction.Op))
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
}