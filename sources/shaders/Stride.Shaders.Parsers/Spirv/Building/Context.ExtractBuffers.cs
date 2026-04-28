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

            // For dedup: normalize GenericReference → GenericParameter so it matches existing entries.
            // Also detect raw GenericParameter leaked from parent contexts.
            var isGenericLike = NormalizeGenericForDedup(ref iData, out var isGenericReference);

            // Note: we try to avoid duplicating the last (constant) instruction if there is a desired ID (so that it keeps its name/identity)
            if ((TypeDuplicateHelper.OpCheckDuplicateForTypesAndImport(iData.Op) || TypeDuplicateHelper.OpCheckDuplicateForConstant(iData.Op) || isGenericLike)
                && typeDuplicateInserter.CheckForDuplicates(iData, out var existingData)
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
                remapIds.Add(iData.IdResult!.Value, existingData.Data.IdResult!.Value);
                lastResultId = existingData.Data.IdResult.Value;
            }
            else
            {
                // After dedup failed, restore generic instructions to GenericReference
                // so they're properly marked as parent-context references.
                if (isGenericLike)
                    RestoreGenericReference(ref iData);

                if (iData.IdResult.HasValue)
                {
                    // Make sure to remap last instruction (which we assume is the actual constant) with the desired result ID
                    var resultId = index == lastResultIndex && desiredResultId != null
                        ? desiredResultId.Value
                        : bound++;

                    remapIds.Add(iData.IdResult.Value, resultId);
                    iData.IdResult = resultId;
                    typeDuplicateInserter.InsertInstruction(instructionIndex++, iData);

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

    /// <summary>
    /// For dedup: temporarily convert GenericReference (and leaked raw GenericParameter from
    /// parent contexts) to GenericParameter so they match existing entries during CheckForDuplicates.
    /// </summary>
    /// <returns>True if the instruction is a generic-like instruction that participates in dedup.</returns>
    private static bool NormalizeGenericForDedup(ref OpData iData, out bool isGenericReference)
    {
        isGenericReference = iData.Op == Specification.Op.OpGenericReferenceSDSL;
        var isRawGenericParameter = !isGenericReference && iData.Op == Specification.Op.OpGenericParameterSDSL;
        if (isGenericReference)
            iData.Memory.Span[0] = (int)(iData.Memory.Span[0] & 0xFFFF0000) | (int)Specification.Op.OpGenericParameterSDSL;
        return isGenericReference || isRawGenericParameter;
    }

    /// <summary>
    /// After a non-deduplicated generic instruction is inserted, restore its opcode
    /// to GenericReference so it's properly marked as a parent-context reference.
    /// </summary>
    private static void RestoreGenericReference(ref OpData iData)
    {
        iData.Memory.Span[0] = (int)(iData.Memory.Span[0] & 0xFFFF0000) | (int)Specification.Op.OpGenericReferenceSDSL;
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
