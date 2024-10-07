using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Core.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Spirv;

public sealed class MixinBuffer
{
    public string Name { get; }
    public SortedWordBuffer Declarations { get; }
    public SortedFunctionBufferCollection Functions {get;}
    public MixinGraph Parents { get; }
    public InstructionsWrapper Instructions => new(this);

    public int Bound { 
        get
        {
            var bound = 0;
            foreach(var i in Declarations)
            {
                if (i.ResultId > bound)
                    bound = i.ResultId ?? bound;
            }
            foreach(var (_,f) in Functions)
            foreach (var i in f)
            {
                if (i.ResultId > bound)
                    bound = i.ResultId ?? bound;
            }
            return bound;
        } 
    }

    public MixinBuffer(string name, MultiBuffer buffers)
    {
        Name = name;
        Declarations = new(buffers.Declarations);
        Functions = new(buffers.Functions);
        Parents = new();

        foreach(var i in Declarations)
        {
            if (i.OpCode == Core.SDSLOp.OpSDSLMixinInherit)
                Parents.Add(i.GetOperand<LiteralString>("mixinName")?.Value!);
        }
    }

    public ref struct InstructionsWrapper
    {
        MixinBuffer buffer;
        public InstructionsWrapper(MixinBuffer buffer)
        {
            this.buffer = buffer;
        }


        public Instruction this[int index]
        {
            get
            {
                var e = GetEnumerator();
                for(int i = 0; i < index -1; i++)
                {
                    e.MoveNext();
                }
                return e.MoveNext() ? e.Current : throw new IndexOutOfRangeException();
            }
        }

        public Enumerator GetEnumerator() => new(buffer);

        public ref struct Enumerator
        {
            MixinBuffer buffer;

            InstructionEnumerator declarations;
            SortedFunctionBufferCollection.FunctionsInstructions.Enumerator functions;
            bool finishedDecl;

            public Enumerator(MixinBuffer buffer)
            {
                this.buffer = buffer;
                declarations = buffer.Declarations.GetEnumerator();
                functions = buffer.Functions.Instructions.GetEnumerator();
            }

            public Instruction Current => !finishedDecl ? declarations.Current : functions.Current;

            public bool MoveNext()
            {
                if (declarations.MoveNext())
                    return true;
                else if (functions.MoveNext())
                {
                    if(!finishedDecl)
                        finishedDecl = true;
                    return true;
                }
                else
                    return false;
            }
        }
    }

    public override string ToString()
    {
        return
            new StringBuilder()
            .Append(Disassembler.Disassemble(Declarations))
            .Append(string.Join("\n", Functions.Buffers.Select(x => Disassembler.Disassemble(x.Value))))
            .ToString();
    }
}
