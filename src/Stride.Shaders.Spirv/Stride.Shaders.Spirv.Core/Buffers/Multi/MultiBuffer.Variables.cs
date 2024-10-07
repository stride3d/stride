using Stride.Shaders.Spirv.Core.Parsing;
using System.Transactions;

namespace Stride.Shaders.Spirv.Core.Buffers;



public sealed partial class MultiBuffer
{
    public MultiBufferGlobalVariables GlobalVariables => new(this);

    public ref struct MultiBufferGlobalVariables
    {
        MultiBuffer buffer;
        public MultiBufferGlobalVariables(MultiBuffer buffer)
        {
            this.buffer = buffer;
        }

        public Instruction this[string name]
        {
            get
            {
                if(TryGet(name, out var instruction))
                    return instruction;
                throw new Exception($"Variable {name} does not exist");
            }
        }

        public readonly bool TryGet(string name, out Instruction instruction)
        {
            var filtered = new LambdaFilteredEnumerator<WordBuffer>(buffer.Declarations, static (i) => i.OpCode == SDSLOp.OpSDSLVariable || i.OpCode == SDSLOp.OpSDSLIOVariable);
            while (filtered.MoveNext())
            {
                if (filtered.Current.GetOperand<LiteralString>("name")?.Value == name)
                {
                    instruction = filtered.Current;
                    return true;
                }
            }
            instruction = Instruction.Empty;
            return false;
        }
    }
}