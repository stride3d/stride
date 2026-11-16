// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Compilers.SDSL;

/// <summary>
/// What <see cref="ShaderMixer"/> needs from the instructions of a loaded class to merge it, read once per class instead of at each merge.
/// </summary>
/// <remarks>
/// Kept as long as the class buffer lives. The buffers of a loaded class don't change; Debug builds check it.
/// </remarks>
internal sealed class ClassLinkInfo
{
    private static readonly ConditionalWeakTable<SpirvBuffer, ClassLinkInfo> Cache = new();

    private ClassLinkInfo(ShaderBuffers shaderBuffers)
    {
        Context = new BufferLinkInfo(shaderBuffers.Context.GetBuffer(), Names);
        Buffer = new BufferLinkInfo(shaderBuffers.Buffer, Names);
    }

    /// <summary>
    /// The instructions of the class context.
    /// </summary>
    public BufferLinkInfo Context { get; }

    /// <summary>
    /// The instructions of the class buffer.
    /// </summary>
    public BufferLinkInfo Buffer { get; }

    /// <summary>
    /// The name of each ID that an <c>OpName</c> names, in both buffers.
    /// </summary>
    public Dictionary<int, string> Names { get; } = [];

    public static ClassLinkInfo Get(ShaderBuffers shaderBuffers)
    {
        var info = Cache.GetValue(shaderBuffers.Buffer, _ => new ClassLinkInfo(shaderBuffers));
#if DEBUG
        info.Context.CheckCurrent(shaderBuffers.Context.GetBuffer());
        info.Buffer.CheckCurrent(shaderBuffers.Buffer);
#endif
        return info;
    }
}

/// <summary>
/// What the merge reads from each instruction of one buffer of a class.
/// </summary>
internal sealed class BufferLinkInfo
{
    /// <summary>
    /// The positions of the ID words of all instructions, in order: <see cref="InstructionLinkInfo.IdStart"/> to <see cref="InstructionLinkInfo.IdEnd"/> for each one.
    /// </summary>
    public readonly int[] IdPositions;

    public readonly InstructionLinkInfo[] Instructions;

    /// <summary>
    /// The value of each <c>OpString</c>, null for other instructions.
    /// </summary>
    public readonly string?[] Strings;

#if DEBUG
    private readonly SpirvBuffer buffer;
    private readonly long checksum;
#endif

    public BufferLinkInfo(SpirvBuffer buffer, Dictionary<int, string> names)
    {
        var count = buffer.Count;
        Instructions = new InstructionLinkInfo[count];
        Strings = new string?[count];
        var idPositions = new List<int>();
        var functionStart = -1;
        for (var index = 0; index < count; index++)
        {
            var i = buffer[index];
            ref var link = ref Instructions[index];
            link.IdStart = idPositions.Count;
            SpirvBuilder.CollectIdPositions(i.Data, idPositions);
            link.IdEnd = idPositions.Count;
            link.ResultId = i.Data.IdResult ?? 0;

            switch (i.Op)
            {
                case Op.OpName:
                    var name = (OpName)i;
                    names.Add(name.Target, name.Name);
                    break;
                case Op.OpSourceHashSDSL:
                    link.Kind = InstructionLinkKind.SourceHash;
                    break;
                case Op.OpString:
                    link.Kind = InstructionLinkKind.String;
                    Strings[index] = ((OpString)i).Value;
                    break;
                case Op.OpSource:
                    link.Kind = InstructionLinkKind.Source;
                    // OpSource layout: [wordcount|op, sourcelanguage, version, file?, source?]
                    link.SourceFile = i.Words.Length > 3 ? i.Words[3] : 0;
                    break;
                // Only a function with its SDSL metadata can be a stage or non-stage member
                case Op.OpFunction when index + 1 < count && buffer[index + 1].Op == Op.OpFunctionMetadataSDSL:
                    var function = (OpFunction)i;
                    var functionInfo = (OpFunctionMetadataSDSL)buffer[index + 1];
                    link.Kind = InstructionLinkKind.Function;
                    link.IsStage = (functionInfo.Flags & FunctionFlagsMask.Stage) != 0;
                    link.FunctionType = function.FunctionType;
                    functionStart = index;
                    break;
                case Op.OpFunctionEnd when functionStart >= 0:
                    Instructions[functionStart].FunctionEnd = index;
                    functionStart = -1;
                    break;
                case Op.OpVariableSDSL:
                    var variable = (OpVariableSDSL)i;
                    link.Kind = InstructionLinkKind.Variable;
                    link.IsStage = (variable.Flags & VariableFlagsMask.Stage) != 0;
                    break;
                case Op.OpTypeStruct:
                    link.Kind = InstructionLinkKind.Struct;
                    link.IsStage = true;
                    break;
            }
        }
        IdPositions = [.. idPositions];
#if DEBUG
        this.buffer = buffer;
        checksum = Checksum(buffer);
#endif
    }

#if DEBUG
    public void CheckCurrent(SpirvBuffer current)
    {
        if (!ReferenceEquals(current, buffer) || Checksum(current) != checksum)
            throw new InvalidOperationException("The buffer of a loaded class changed after the mixer read it.");
    }

    private static long Checksum(SpirvBuffer buffer)
    {
        long hash = buffer.Count;
        for (var index = 0; index < buffer.Count; index++)
        {
            foreach (var word in buffer.GetWords(index))
                hash = hash * 31 + word;
        }
        return hash;
    }
#endif
}

internal enum InstructionLinkKind : byte
{
    None,
    SourceHash,
    String,
    Source,
    Function,
    Variable,
    Struct,
}

internal struct InstructionLinkInfo
{
    public int IdStart;
    public int IdEnd;
    /// <summary>
    /// The result ID, 0 when the instruction has none.
    /// </summary>
    public int ResultId;
    public InstructionLinkKind Kind;
    /// <summary>
    /// For a function or a variable: whether it is a stage member. Always true for a struct.
    /// </summary>
    public bool IsStage;
    public int FunctionType;
    /// <summary>
    /// For a function: the index of its <c>OpFunctionEnd</c>.
    /// </summary>
    public int FunctionEnd;
    /// <summary>
    /// For an <c>OpSource</c>: the ID of its file string, 0 when it has none.
    /// </summary>
    public int SourceFile;
}
