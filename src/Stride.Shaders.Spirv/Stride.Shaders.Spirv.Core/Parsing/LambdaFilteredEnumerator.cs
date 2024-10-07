using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Spirv.Core.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Spv.Specification;

namespace Stride.Shaders.Spirv.Core.Parsing;
/// <summary>
/// An enumerator to filter instructions with a lambda
/// </summary>
/// <typeparam name="T"></typeparam>
public ref struct LambdaFilteredEnumerator<T>
    where T : ISpirvBuffer
{ 
    T buffer;

    string? classFilter;
    Func<Instruction, bool> filter;
    InstructionEnumerator enumerator;


    public LambdaFilteredEnumerator(T buff, Func<Instruction, bool> filter)
    {
        buffer = buff;
        this.filter = filter;
        enumerator = new(buffer);
    }
    public Instruction Current => enumerator.Current;
    public bool MoveNext()
    {
        while(enumerator.MoveNext())
        {
            if (filter.Invoke(Current) == true)
                return true;
        }
        return false;
    }

}
