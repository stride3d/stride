using Stride.Shaders.Spirv.Core.Parsing;
using System.Transactions;

namespace Stride.Shaders.Spirv.Core.Buffers;



public sealed partial class MultiBuffer
{
    public MultiBufferLocalVariables LocalVariables => new(this);

    /// <summary>
    /// Representation of local variables of the current function being written.
    /// </summary>
    public ref struct MultiBufferLocalVariables
    {
        MultiBuffer buffer;
        public MultiBufferLocalVariables(MultiBuffer buffer)
        {
            this.buffer = buffer;
        }

        public Instruction this[string name]
        {
            get
            {
                if(TryGet(name, out var instruction))
                    return instruction;
                throw new Exception($"Variable {name} not found");
            }
        }
        /// <summary>
        /// Finds the last varibale with a specific name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="instruction"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public readonly bool TryGet(string name, out Instruction instruction)
        {
            var found = false;
            instruction = Instruction.Empty;
            if (buffer.Functions.Current == null)
                throw new Exception("Not in function scope");
            var filtered = new LambdaFilteredEnumerator<WordBuffer>(buffer.Functions.Current, static (i) => i.OpCode == SDSLOp.OpSDSLVariable || i.OpCode == SDSLOp.OpSDSLFunctionParameter);
            while (filtered.MoveNext())
            {
                var vname = filtered.Current.GetOperand<LiteralString>("name");
                if (vname?.Value == name)
                {
                    instruction = filtered.Current;
                    found = true;
                }
            }
            return found;
        }
    }
}