using System.Runtime.CompilerServices;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;

namespace Stride.Shaders.Spirv;




/// <summary>
/// Mixin object
/// </summary>
public partial struct Mixin
{
    public static readonly Mixin Empty = new("", SortedWordBuffer.Empty);

    public string Name { get; init; }
    public int Bound { get; init; }
    public int ResultIdCount { get; init; }

    internal SortedWordBuffer Buffer { get; }
    public MixinInstructions Instructions => new(this);
    public FullMixinInstructions FullInstructions => new(this);
    public MixinParents Parents => new(this);

    public bool IsEmpty => Buffer.IsEmpty;


    public Mixin(string name, SortedWordBuffer wordBuffer)
    {
        Name = name;
        Buffer = wordBuffer;
        ResultIdCount = 0;
        Bound = 0;
        foreach (var i in Instructions)
        {
            if (i.ResultId != null)
            {
                ResultIdCount += 1;
                if(i.ResultId > Bound)
                    Bound = i.ResultId.Value;
            }
        }
    }

    public string Disassemble()
    {
        var words = new WordBuffer();
        foreach(var e in FullInstructions)
        {
            words.Insert(e.Instruction);
        }
        return Disassembler.Disassemble(new UnsortedWordBuffer(words));
    }


    public override string ToString()
    {
        return Disassembler.Disassemble(Buffer.Memory);
    }
}