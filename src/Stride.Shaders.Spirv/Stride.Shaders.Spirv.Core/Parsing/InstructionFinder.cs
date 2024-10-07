using Stride.Shaders.Spirv.Core.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Spirv.Core.Parsing;

/// <summary>
/// A utility struct to find and look for specific instructions
/// </summary>
/// <typeparam name="T"></typeparam>
public ref struct InstructionFinder<T> where T : ISpirvBuffer
{
    T buffer;
    //LambdaFilteredEnumerator<T> enumerator;

    public InstructionFinder(T buffer) 
    {
        this.buffer = buffer;
    }

    public readonly Instruction First(Func<Instruction,bool> filter)
    {
        var enumerator = new LambdaFilteredEnumerator<T>(buffer, filter);
        if (enumerator.MoveNext())
            return enumerator.Current;
        else
            throw new Exception("No matching instructions found");
    }
    public readonly Instruction Last(Func<Instruction, bool> filter)
    {
        var enumerator = new LambdaFilteredEnumerator<T>(buffer, filter);
        Instruction? result = null;
        if (enumerator.MoveNext())
            result = enumerator.Current;
                
        return result != null ? result .Value: throw new Exception("No matching instructions found");
    }

}
