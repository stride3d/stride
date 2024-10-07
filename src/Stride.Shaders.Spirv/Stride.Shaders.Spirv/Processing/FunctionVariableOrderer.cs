using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Core;
namespace Stride.Shaders.Spirv.Processing;

/// <summary>
/// Makes sure variables are created in the beginning of a function definition
/// </summary>
public struct FunctionVariableOrderer : INanoPass
{
    public void Apply(MultiBuffer buffer)
    {
        foreach(var (_,f) in buffer.Functions)
        {
            ProcessFunction(new(f.InstructionSpan));
            f.RecomputeLength();
        }
    }
    public static void ProcessFunction(SpirvSpan function)
    {
        using var tmp = new SpirvBuffer(function.Span.Length);
        var enumerator = function.GetEnumerator();
        enumerator.MoveNext();
        var opf = enumerator.Current;
        tmp.Insert(tmp.Length, opf.Words);
        foreach(var i in function)
        {
            if(i.OpCode == SDSLOp.OpFunctionParameter)
                tmp.Insert(tmp.Length, i.Words);
        }
        while(enumerator.Current.OpCode != SDSLOp.OpLabel)
            enumerator.MoveNext();
        
        tmp.Insert(tmp.Length, enumerator.Current.Words);

        foreach (var i in function)
        {
            if(i.OpCode == SDSLOp.OpVariable)
            {
                tmp.Insert(tmp.Length,i.Words);
            }
        }
        while(enumerator.MoveNext())
        {
            var i = enumerator.Current;
            if (i.OpCode != SDSLOp.OpVariable && i.OpCode != SDSLOp.OpFunctionParameter)
            {
                tmp.Insert(tmp.Length, i.Words);
            }
            if (i.OpCode == SDSLOp.OpSDSLVariable)
            {
                var t = 0;
            }
        }
        function.Span.Clear();
        tmp.InstructionSpan.CopyTo(function.Span);
    }
}
