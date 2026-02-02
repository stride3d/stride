using Stride.Shaders.Core;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;
using System;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Building;

public partial class SpirvBuilder
{
    public static SpirvFunction DeclareFunction(SpirvContext context, string name, FunctionType ftype, bool isStage = false)
    {
        var func = context.Bound++;
        foreach (var t in ftype.ParameterTypes)
            context.GetOrRegister(t.Type);
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
            if (CurrentFunction.Value.FunctionType.ReturnType != ScalarType.Void)
                throw new InvalidOperationException("No function termination, but a return value is expected");

            Return(null);
        }

        Buffer.Insert(Position++, new OpFunctionEnd());
    }

    public SpirvValue EmitFunctionParameter(SpirvContext context, string name, SymbolType type)
    {
        var p = Buffer.Insert(Position++, new OpFunctionParameter(context.GetOrRegister(type), context.Bound++));
        context.AddName(p, name);
        var value = new SpirvValue(p.ResultId, p.ResultType, name);
        CurrentFunction!.Value.Parameters.Add(name, value);
        return value; 
    }

    public static OpFunctionParameter GetFunctionParameter(NewSpirvBuffer buffer, Symbol method, int functionParameterIndex)
    {
        // Find OpFunctionParameter
        var functionParameterCurrent = 0;
        (var start, var end) = FindMethodBounds(buffer, method.IdRef);
        for (int index = start; index < end; ++index)
        {
            var i = buffer[index];
            if (i.Op == Op.OpFunctionParameter && functionParameterCurrent++ == functionParameterIndex && (OpFunctionParameter)i is {} functionParameter)
            {
                return functionParameter;
            }
        }

        throw new InvalidOperationException();
    }
    
    public static void FunctionRemoveArgument(SpirvContext context, NewSpirvBuffer buffer, Symbol method, int argIndex)
    {
        var methodType = (FunctionType)method.Type;
        method.Type = methodType with { ParameterTypes = methodType.ParameterTypes[0..^1] };
        
        // Find OpFunctionParameter and remove it
        var functionParameter = GetFunctionParameter(buffer, method, argIndex);
        SetOpNop(functionParameter.InstructionMemory.Span);
    }

    public static void FunctionReplaceArgument(SpirvContext context, NewSpirvBuffer buffer, Symbol method, int argIndex, SymbolType newType)
    {
        var methodType = (FunctionType)method.Type;
        var parameterTypes = new List<FunctionParameter>(methodType.ParameterTypes);
        parameterTypes[argIndex] = parameterTypes[argIndex] with { Type = newType };
        method.Type = methodType with { ParameterTypes = parameterTypes };
        
        // Find OpFunctionParameter and remove it
        var functionParameter = GetFunctionParameter(buffer, method, argIndex);
        functionParameter.ResultType = context.GetOrRegister(newType);
    }
    
    public static (int Start, int End) FindMethodBounds(NewSpirvBuffer buffer, int functionId)
    {
        int? start = null;
        for (var index = 0; index < buffer.Count; index++)
        {
            var instruction = buffer[index];
            if (instruction.Op is Op.OpFunction && ((OpFunction)instruction).ResultId == functionId)
                start = index;
            if (instruction.Op is Op.OpFunctionEnd && start is int startIndex)
                return (startIndex, index + 1);
        }
        throw new InvalidOperationException($"Could not find start of method {functionId}");
    }

}