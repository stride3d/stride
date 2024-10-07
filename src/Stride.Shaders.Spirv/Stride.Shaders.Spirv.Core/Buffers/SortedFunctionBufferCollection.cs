using Stride.Shaders.Spirv.Core.Parsing;
using System.Collections;

namespace Stride.Shaders.Spirv.Core.Buffers;

/// <summary>
/// A collection of function buffers, usable through the MultiBuffer class
/// </summary>
public class SortedFunctionBufferCollection
{
    bool functionStarted;
    public SortedList<string, SortedWordBuffer> Buffers { get; }
    public SortedWordBuffer? Current => functionStarted ? Buffers.Values[^1] : null;

    public FunctionsInstructions Instructions => new(this);

    public int BuffersLength => Buffers.Sum(static (x) => x.Value.Length);


    public SortedFunctionBufferCollection(FunctionBufferCollection functions)
    {
        Buffers = new(functions.FunctionCount);
        foreach(var func in functions.Buffers)
        {
            Buffers.Add(func.Key, new(func.Value));
        }
    }

    public IEnumerator<KeyValuePair<string,SortedWordBuffer>> GetEnumerator() => Buffers.GetEnumerator();
    
    public struct FunctionsInstructions
    {
        SortedFunctionBufferCollection buffers;
        public FunctionsInstructions(SortedFunctionBufferCollection buffers)
        {
            this.buffers = buffers;
        }


        public Enumerator GetEnumerator() => new(buffers);

        public ref struct Enumerator
        {
            IEnumerator<KeyValuePair<string,SortedWordBuffer>> lastBuffer;
            InstructionEnumerator lastEnumerator;
            bool started;
            public Enumerator(SortedFunctionBufferCollection buffers)
            {
                lastBuffer = buffers.GetEnumerator();
                started = false;
            }

            public Instruction Current => lastEnumerator.Current;

            public bool MoveNext()
            {
                if (!started)
                {
                    started = true;
                    if (!lastBuffer.MoveNext())
                        return false;
                    lastEnumerator = lastBuffer.Current.Value.GetEnumerator();
                    while (!lastEnumerator.MoveNext())
                    {
                        if (!lastBuffer.MoveNext())
                            return false;
                        lastEnumerator = lastBuffer.Current.Value.GetEnumerator();
                    }
                    return true;
                }
                else
                {
                    if (lastEnumerator.MoveNext())
                        return true;
                    else
                    {
                        while (lastBuffer.MoveNext())
                        {
                            lastEnumerator = lastBuffer.Current.Value.GetEnumerator();
                            if (lastEnumerator.MoveNext())
                                return true;
                        }
                    }
                    return false;
                }
            }
        }
    }

    
        
}