using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Processing;

namespace Stride.Shaders.Spirv.Building;

public partial class SpirvContext
{
    public int InsertWithoutDuplicates(int? desiredResultId, SpirvBuffer source)
    {
        ThrowIfFrozen();
        var index = Buffer.Count;
        return InsertWithoutDuplicates(ref index, desiredResultId, source);
    }

    public int InsertWithoutDuplicates(ref int instructionIndex, int? desiredResultId, SpirvBuffer source, IReadOnlyDictionary<int, string>? sourceNames = null)
    {
        ThrowIfFrozen();
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
            // Copy instruction data so we never modify the source buffer in-place.
            // The source may be a frozen cached buffer shared across compilations.
            var iData = new OpData(source[index].Data.Memory.Span);
            SpirvBuilder.RemapIds(remapIds, ref iData);

            //// If it's a generic reference, remap to OpSDSLGenericParameter which has to match during typeDuplicateInserter.CheckForDuplicates()
            var isGenericReference = iData.Op == Specification.Op.OpSDSLGenericReference;
            // Also detect raw OpSDSLGenericParameter from parent contexts — these leaked through
            // ExtractConstantAsSpirvBuffer and must be converted to references so they don't get
            // mis-counted as the current shader's own generics.
            var isRawGenericParameter = !isGenericReference && iData.Op == Specification.Op.OpSDSLGenericParameter;
            if (isGenericReference)
                iData.Memory.Span[0] = (int)(iData.Memory.Span[0] & 0xFFFF0000) | (int)Specification.Op.OpSDSLGenericParameter;

            // For OpTypeImage: find the UserTypeGOOGLE decoration in the source buffer so CheckForDuplicates
            // can distinguish e.g. Texture2D<float2> vs Texture2D<float4> (same binary, different return type).
            // IdResult is still the original source ID here because RemapIds does not remap the current instruction's own result.
            string? sourceUserTypeGOOGLE = null;
            if (iData.Op == Specification.Op.OpTypeImage && iData.IdResult.HasValue)
            {
                var originalId = iData.IdResult.Value;
                foreach (var inst in source)
                {
                    if (inst.Op == Specification.Op.OpDecorateString)
                    {
                        Spirv.Core.OpDecorateString dec = inst;
                        if (dec.Decoration == Specification.Decoration.UserTypeGOOGLE && dec.Target == originalId)
                        {
                            sourceUserTypeGOOGLE = dec.Value;
                            break;
                        }
                    }
                }
            }

            // Note: we try to avoid duplicating the last (constant) instruction if there is a desired ID (so that it keeps its name/identity)
            if ((TypeDuplicateHelper.OpCheckDuplicateForTypesAndImport(iData.Op) || TypeDuplicateHelper.OpCheckDuplicateForConstant(iData.Op) || isGenericReference || isRawGenericParameter)
                && typeDuplicateInserter.CheckForDuplicates(iData, sourceUserTypeGOOGLE, out var existingData)
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
                remapIds.Add(iData.IdResult.Value, existingData.Data.IdResult.Value);
                lastResultId = existingData.Data.IdResult.Value;
            }
            else
            {
                if (isGenericReference || isRawGenericParameter)
                    iData.Memory.Span[0] = (int)(iData.Memory.Span[0] & 0xFFFF0000) | (int)Specification.Op.OpSDSLGenericReference;

                if (iData.IdResult.HasValue)
                {
                    // Make sure to remap last instruction (which we assume is the actual constant) with the desired result ID
                    var resultId = index == lastResultIndex && desiredResultId != null
                        ? desiredResultId.Value
                        : bound++;

                    remapIds.Add(iData.IdResult.Value, resultId);
                    iData.IdResult = resultId;
                    typeDuplicateInserter.InsertInstruction(instructionIndex++, iData);

                    // For OpTypeImage: also insert the UserTypeGOOGLE decoration so it stays
                    // paired with the type during future deduplication in the mixer.
                    if (sourceUserTypeGOOGLE != null)
                    {
                        var dec = new OpDecorateString(resultId, Specification.Decoration.UserTypeGOOGLE, sourceUserTypeGOOGLE);
                        typeDuplicateInserter.InsertInstruction(instructionIndex++, new OpData(dec.InstructionMemory));
                    }

                    lastResultId = resultId;
                }
            }
        }

        if (lastResultId == -1)
            throw new InvalidOperationException("Could not find any instruction with a value");

        // Note: we made sure to not copy last instruction which should have the constant we want, so this case shouldn't happen anymore
        if (desiredResultId != null && lastResultId != desiredResultId)
            throw new InvalidOperationException();

        // Carry over names from source context for all remapped IDs.
        // We must also emit OpName instructions into the buffer so that downstream consumers
        // (e.g. MergeClassInBuffers) that read names from the buffer can find them.
        if (sourceNames != null)
        {
            foreach (var (sourceId, destId) in remapIds)
            {
                if (sourceNames.TryGetValue(sourceId, out var name) && Names.TryAdd(destId, name))
                    Buffer.Add(new Spirv.Core.OpName(destId, name));
            }
        }

        // Carry over OpName entries from the source buffer using remapped IDs.
        // OpName has no IdResult so it is skipped in the main loop above.
        // Process them here in a second pass so remapIds is fully populated.
        foreach (var inst in source)
        {
            if (inst.Op == Specification.Op.OpName)
            {
                Spirv.Core.OpName nameInst = inst;
                if (remapIds.TryGetValue(nameInst.Target, out var remappedTarget) && Names.TryAdd(remappedTarget, nameInst.Name))
                    Buffer.Add(new Spirv.Core.OpName(remappedTarget, nameInst.Name));
            }
        }

        return lastResultId;
    }

    public SpirvBuffer ExtractConstantAsSpirvBuffer(int constantId)
    {
        // First, run a simplification pass
        // TODO: separate simplification from computing value?
        TryGetConstantValue(constantId, out _, out _, true);

        return ExtractConstantFromBuffer(constantId, Buffer);
    }

    /// <summary>
    /// Extracts a constant and all its transitive dependencies from a buffer into a new standalone buffer.
    /// Works on any buffer (not just this context's buffer).
    /// </summary>
    public static SpirvBuffer ExtractConstantFromBuffer(int constantId, SpirvBuffer source)
    {
        // Go backward and find any reference
        var newBuffer = new SpirvBuffer();
        var referenced = new HashSet<int> { constantId };
        var instructions = new List<OpData>();
        for (int index = source.Count - 1; index >= 0; --index)
        {
            var i = source[index];
            if (i.Data.IdResult is int resultId && referenced.Remove(resultId))
            {
                var i2 = new OpData(i.Data.Memory.Span);

                // Then add IdRef operands to next requested instructions or types
                foreach (var op in i2)
                {
                    if (op.Kind == OperandKind.IdRef
                        || op.Kind == OperandKind.IdResultType
                        || op.Kind == OperandKind.IdScope
                        || op.Kind == OperandKind.IdMemorySemantics
                        || op.Kind == OperandKind.PairIdRefIdRef
                        || op.Kind == OperandKind.IdScope
                        || op.Kind == OperandKind.IdMemorySemantics)
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
