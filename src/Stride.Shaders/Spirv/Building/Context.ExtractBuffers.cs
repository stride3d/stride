using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Processing;

namespace Stride.Shaders.Spirv.Building;

public partial class SpirvContext
{
    public int InsertWithoutDuplicates(int? desiredResultId, NewSpirvBuffer source)
    {
        var index = Buffer.Count;
        return InsertWithoutDuplicates(ref index, desiredResultId, source);
    }

    public int InsertWithoutDuplicates(ref int instructionIndex, int? desiredResultId, NewSpirvBuffer source)
    {
        // Import in current buffer (without duplicate)
        var typeDuplicateInserter = new TypeDuplicateHelper(this);
        var remapIds = new Dictionary<int, int>();
        int lastResultId = -1;
        
        var lastResultIndex = -1;
        if (desiredResultId != null)
        {
            // Find last index returning a value (that's the value we want remapped to desiredResultId)
            for (int index = 0; index < source.Count; ++index)
            {
                var i = source[index];
                if (i.Data.IdResult is not null)
                    lastResultIndex = index;
            }
        }

        for (int index = 0; index < source.Count; ++index)
        {
            var i = source[index];
            SpirvBuilder.RemapIds(remapIds, ref i.Data);

            //// If it's a generic reference, remap to OpSDSLGenericParameter which has to match during typeDuplicateInserter.CheckForDuplicates()
            var isGenericReference = i.Op == Specification.Op.OpSDSLGenericReference;
            if (isGenericReference)
                i.Data.Memory.Span[0] = (int)(i.Data.Memory.Span[0] & 0xFFFF0000) | (int)Specification.Op.OpSDSLGenericParameter;

            // Note: we try to avoid duplicating the last (constant) instruction if there is a desired ID (so that it keeps its name/identity) 
            if ((TypeDuplicateHelper.OpCheckDuplicateForTypesAndImport(i.Op) || TypeDuplicateHelper.OpCheckDuplicateForConstant(i.Op) || isGenericReference)
                && typeDuplicateInserter.CheckForDuplicates(i.Data, out var existingData)
                && (index != lastResultIndex || desiredResultId == null))
            {
                // Make sure this data is declared at current index, otherwise move it.
                // Note: it should be safe to do so as the source buffer has all the dependencies and they should have been inserted in previous loops
                if (existingData.Index > instructionIndex)
                {
                    var existingDataCopy = new OpData(existingData.Data.Memory);
                    typeDuplicateInserter.RemoveInstructionAt(existingData.Index, false);
                    existingData = typeDuplicateInserter.InsertInstruction(instructionIndex++, existingDataCopy);
                }
                remapIds.Add(i.Data.IdResult.Value, existingData.Data.IdResult.Value);
                lastResultId = existingData.Data.IdResult.Value;
            }
            else
            {
                if (isGenericReference)
                    i.Data.Memory.Span[0] = (int)(i.Data.Memory.Span[0] & 0xFFFF0000) | (int)Specification.Op.OpSDSLGenericReference;

                if (i.Data.IdResult.HasValue)
                {
                    // Make sure to remap last instruction (which we assume is the actual constant) with the desired result ID
                    var resultId = index == lastResultIndex && desiredResultId != null
                        ? desiredResultId.Value
                        : bound++;

                    remapIds.Add(i.Data.IdResult.Value, resultId);
                    i.Data.IdResult = resultId;
                    typeDuplicateInserter.InsertInstruction(instructionIndex++, i.Data);

                    lastResultId = resultId;
                }
            }
        }

        if (lastResultId == -1)
            throw new InvalidOperationException("Could not find any instruction with a value");

        // Note: we made sure to not copy last instruction which should have the constant we want, so this case shouldn't happen anymore
        if (desiredResultId != null && lastResultId != desiredResultId)
            throw new InvalidOperationException();
            // Note: if we were to readd this, we would also need to process the main buffer 
            //SpirvBuilder.RemapIds(Buffer, 0, Buffer.Count, new Dictionary<int, int> { { lastResultId, desiredResultId.Value } });

        return lastResultId;
    }

    public NewSpirvBuffer ExtractConstantAsSpirvBuffer(int constantId)
    {
        // First, run a simplification pass
        // TODO: separate simplification from computing value?
        TryGetConstantValue(constantId, out _, out _, true);

        // Go backward and find any reference
        var newBuffer = new NewSpirvBuffer();
        var referenced = new HashSet<int> { constantId };
        var instructions = new List<OpData>();
        for (int index = Buffer.Count - 1; index >= 0; --index)
        {
            var i = Buffer[index];
            if (i.Data.IdResult is int resultId && referenced.Remove(resultId))
            {
                var i2 = new OpData(i.Data.Memory.Span);

                // Then add IdRef operands to next requested instructions or types
                foreach (var op in i2)
                {
                    if (op.Kind == OperandKind.IdRef
                        || op.Kind == OperandKind.IdResultType
                        || op.Kind == OperandKind.PairIdRefIdRef)
                    {
                        foreach (ref var word in op.Words)
                        {
                            referenced.Add(word);
                        }
                    }
                    else if (op.Kind == OperandKind.PairLiteralIntegerIdRef
                             || op.Kind == OperandKind.PairIdRefLiteralInteger)
                    {
                        throw new NotImplementedException();
                    }
                }

                instructions.Add(i2);
            }
        }

        // Since we went backward, reverse the list
        instructions.Reverse();
        foreach (var i in instructions)
            newBuffer.Add(i);
        return newBuffer;
    }
}