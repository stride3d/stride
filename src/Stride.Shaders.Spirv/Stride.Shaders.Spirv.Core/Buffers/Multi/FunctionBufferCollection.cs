using Stride.Shaders.Spirv.Core.Parsing;
using System.Collections;

namespace Stride.Shaders.Spirv.Core.Buffers;

/// <summary>
/// A collection of function buffers, usable through the MultiBuffer class
/// </summary>
public class FunctionBufferCollection
{
    bool functionStarted;
    public SortedList<string, WordBuffer> Buffers { get; }
    public WordBuffer? Current => functionStarted ? Buffers.Values[^1] : null;

    public FunctionsInstructions Instructions => new(this);

    public int BuffersLength => Buffers.Sum(static (x) => x.Value.Length);
    public int FunctionCount => Buffers.Count;

    public WordBuffer this[string name] => Buffers[name];

    public FunctionBufferCollection()
    {
        functionStarted = false;
        Buffers = new();
    }

    public IEnumerator<KeyValuePair<string,WordBuffer>> GetEnumerator() => Buffers.GetEnumerator();


    public Instruction Insert(MutRefInstruction instruction, string? functionName = null)
    {
        if(!functionStarted)
        {
            if (instruction.OpCode != SDSLOp.OpFunction || functionName == null)
                throw new Exception("A function should be started with SDSLOp.OpFunction");
            Buffers.Add(functionName, new());
            functionStarted = true;
        }
        Instruction? result = Current?.Add(instruction);
        if(instruction.OpCode == SDSLOp.OpFunctionEnd)
        {
            functionStarted = false;
        }
        return result ?? throw new Exception("The instruction was not inserted");
    }

    public void Add(string name, WordBuffer function)
    {
        Buffers.Add(name, function);
    }

    public struct FunctionsInstructions
    {
        FunctionBufferCollection buffers;
        public FunctionsInstructions(FunctionBufferCollection buffers)
        {
            this.buffers = buffers;
        }


        public Enumerator GetEnumerator() => new(buffers);

        public ref struct Enumerator
        {
            FunctionBufferCollection buffers;
            IEnumerator<KeyValuePair<string,WordBuffer>> lastBuffer;
            InstructionEnumerator lastEnumerator;
            bool started;
            public Enumerator(FunctionBufferCollection buffers)
            {
                this.buffers = buffers;
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
                    lastEnumerator = new(lastBuffer.Current.Value);
                    while (!lastEnumerator.MoveNext())
                    {
                        if (!lastBuffer.MoveNext())
                            return false;
                        lastEnumerator = new(lastBuffer.Current.Value);
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
                            lastEnumerator = new(lastBuffer.Current.Value);
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