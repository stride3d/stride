using System.ComponentModel.Design.Serialization;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;

namespace Stride.Shaders.Spirv;

public partial class Mixer
{
    public Mixer ComposeWith(string mixinName, string variableName)
    {
        // Foreach instruction in mixin to compose
        // Insert the instruction
        // If the instruction is an OpName for variables, create a new OpName instruction with the same name prefixed by variableName
        // Make sure to offset the Ids.
        var composable = CompositionSourceProvider.Get(mixinName);
        var offset = Buffer.Bound;
        Span<int> nameBuffer = stackalloc int[200];
        foreach (var i in composable)
        {
            if(i.OpCode == SDSLOp.OpName)
            {
                var name = i.GetOperand<LiteralString>("name") ?? throw new Exception("Name is null");
                var newName = $"{variableName}_{name}";
                var buff = nameBuffer[0..(1 + 1 + name.WordCount)];
                buff.Clear();
                var newInstruction = new MutRefInstruction(buff);
                newInstruction.Add(i.ResultId ?? -1);
                newInstruction.Add(newName);

                Buffer.Add(newInstruction);
            }
            else
            {

            }
        }
        throw new NotImplementedException();
        return this;
    }


}