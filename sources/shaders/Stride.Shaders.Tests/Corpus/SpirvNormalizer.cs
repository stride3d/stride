// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Tools;
using static Stride.Shaders.Spirv.Specification;
using Spv = Stride.Shaders.Spirv.Tools.Spv;

namespace Stride.Shaders.Parsers.Tests.Corpus;

/// <summary>
/// Stable text form of a SPIR-V module, so that two compiles can be compared: debug instructions removed (source, lines,
/// and names unless kept for reading) and IDs renumbered in order of first use, so that IDs allocated by unrelated code
/// don't show up as changes.
/// </summary>
static class SpirvNormalizer
{
    public static string Normalize(ReadOnlySpan<int> words, bool keepNames)
    {
        using var input = SpirvBytecode.CreateFromSpan(words.ToArray().AsSpan());
        using var output = new SpirvBuffer();

        var remap = new Dictionary<int, int>();
        var names = new List<OpData>();
        foreach (var i in input.Buffer)
        {
            if (i.Op is Op.OpName or Op.OpMemberName)
            {
                if (keepNames)
                    names.Add(new OpData(i.Data.Memory.Span));
                continue;
            }
            if (IsDebugInstruction(i.Op))
                continue;

            var data = new OpData(i.Data.Memory.Span);
            ForEachId(data, (ref int id) =>
            {
                if (!remap.TryGetValue(id, out var newId))
                    remap.Add(id, newId = remap.Count + 1);
                id = newId;
            });
            output.Add(data);
        }

        // Names go after the capabilities, entry points and execution modes, like in a real module; names of removed
        // instructions are dropped
        var insertIndex = 0;
        while (insertIndex < output.Count && output[insertIndex].Op is Op.OpCapability or Op.OpExtension or Op.OpExtInstImport
                   or Op.OpMemoryModel or Op.OpEntryPoint or Op.OpExecutionMode or Op.OpExecutionModeId)
            insertIndex++;
        var namedIds = new HashSet<int>();
        foreach (var name in names)
        {
            ref var target = ref name.Memory.Span[1];
            if (remap.TryGetValue(target, out var newTarget))
            {
                target = newTarget;
                if (name.Op == Op.OpName)
                    namedIds.Add(newTarget);
                output.Insert(insertIndex++, name);
            }
            else
            {
                name.Dispose();
            }
        }

        // Types and constants are named after their content (int, float4, ptr_Uniform_float2, int_4): a type that moves is
        // then one moved line, instead of a new ID in every instruction using it. These names go last, and their lines are
        // cut from the text.
        var structuralNames = StructuralNames(output);
        var addedNames = 0;
        foreach (var (id, name) in structuralNames)
        {
            if (namedIds.Contains(id))
                continue;
            output.Add(CreateName(id, name));
            addedNames++;
        }

        // Dis(SpirvBuffer) writes a fixed header, so the text only depends on the instructions; it formats numbers with
        // the current culture
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        string text;
        try
        {
            text = Spv.Dis(output, DisassemblerFlags.Name);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }

        // Split leaves an empty string after the final newline. The padding is removed: Dis aligns on the longest name, so
        // one longer name would change every line.
        var lines = text.Split('\n');
        return string.Join('\n', lines[..^(addedNames + 1)].Select(x => x.TrimStart())) + "\n";
    }

    /// <summary>
    /// Content-based names of the types and constants of a module, in definition order. Structs keep their number.
    /// </summary>
    private static List<(int Id, string Name)> StructuralNames(SpirvBuffer buffer)
    {
        var result = new List<(int, string)>();
        var names = new Dictionary<int, string>();
        var constants = new Dictionary<int, string>();
        var scalarTypes = new Dictionary<int, (Op Op, int Width, int Signed)>();
        string NameOf(int id) => names.TryGetValue(id, out var name) ? name : $"id{id}";

        foreach (var i in buffer)
        {
            var w = i.Data.Memory.Span;
            if (i.Op is Op.OpTypeInt)
                scalarTypes[w[1]] = (i.Op, w[2], w[3]);
            else if (i.Op is Op.OpTypeFloat)
                scalarTypes[w[1]] = (i.Op, w[2], 0);
            string? name = i.Op switch
            {
                Op.OpTypeVoid => "void",
                Op.OpTypeBool => "bool",
                Op.OpTypeInt => (w[3] != 0 ? "int" : "uint") + (w[2] != 32 ? w[2].ToString(CultureInfo.InvariantCulture) : ""),
                Op.OpTypeFloat => w[2] switch { 16 => "half", 32 => "float", 64 => "double", _ => $"float{w[2]}" },
                Op.OpTypeVector => $"{NameOf(w[2])}{w[3]}",
                Op.OpTypeMatrix => $"{NameOf(w[2])}x{w[3]}",
                Op.OpTypePointer => $"ptr_{(StorageClass)w[2]}_{NameOf(w[3])}",
                Op.OpTypeArray => $"{NameOf(w[2])}_arr_{(constants.TryGetValue(w[3], out var length) ? length : NameOf(w[3]))}",
                Op.OpTypeRuntimeArray => $"{NameOf(w[2])}_rarr",
                Op.OpTypeSampler => "sampler",
                Op.OpTypeImage => $"image_{NameOf(w[2])}_{(Dim)w[3]}_{string.Join("_", w[4..8].ToArray())}_{(ImageFormat)w[8]}",
                Op.OpTypeSampledImage => $"sampled_{NameOf(w[2])}",
                Op.OpTypeFunction => $"fn_{NameOf(w[2])}" + string.Concat(w[3..].ToArray().Select(x => "_" + NameOf(x))),
                Op.OpConstantTrue => "true",
                Op.OpConstantFalse => "false",
                Op.OpConstantNull => $"null_{NameOf(w[1])}",
                Op.OpUndef => $"undef_{NameOf(w[1])}",
                Op.OpConstant => $"{NameOf(w[1])}_{ConstantValue(scalarTypes, w)}",
                Op.OpConstantComposite when w.Length <= 7 => $"{NameOf(w[1])}" + string.Concat(w[3..].ToArray().Select(x => "_" + NameOf(x))),
                _ => null,
            };
            if (name == null)
                continue;

            var id = i.Op is Op.OpConstant or Op.OpConstantTrue or Op.OpConstantFalse or Op.OpConstantNull or Op.OpConstantComposite or Op.OpUndef ? w[2] : w[1];
            if (i.Op == Op.OpConstant)
                constants[id] = ConstantValue(scalarTypes, w);
            names[id] = name;
            result.Add((id, name));
        }
        return result;
    }

    private static string ConstantValue(Dictionary<int, (Op Op, int Width, int Signed)> scalarTypes, Span<int> w)
    {
        if (scalarTypes.TryGetValue(w[1], out var type))
        {
            if (type.Op == Op.OpTypeFloat)
            {
                return type.Width switch
                {
                    32 => BitConverter.Int32BitsToSingle(w[3]).ToString("R", CultureInfo.InvariantCulture),
                    64 => BitConverter.Int64BitsToDouble((uint)w[3] | ((long)w[4] << 32)).ToString("R", CultureInfo.InvariantCulture),
                    16 => ((float)BitConverter.Int16BitsToHalf((short)w[3])).ToString("R", CultureInfo.InvariantCulture),
                    _ => string.Join("_", w[3..].ToArray()),
                };
            }
            if (type.Op == Op.OpTypeInt && type.Width == 32)
                return type.Signed != 0 ? w[3].ToString(CultureInfo.InvariantCulture) : ((uint)w[3]).ToString(CultureInfo.InvariantCulture);
        }
        return string.Join("_", w[3..].ToArray());
    }

    private static OpData CreateName(int target, string name)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(name);
        var stringWords = bytes.Length / 4 + 1;
        var words = new int[2 + stringWords];
        words[0] = (words.Length << 16) | (int)Op.OpName;
        words[1] = target;
        Buffer.BlockCopy(bytes, 0, words, 8, bytes.Length);
        return new OpData(words.AsSpan());
    }

    private static bool IsDebugInstruction(Op op) => op is Op.OpNop or Op.OpString or Op.OpSource or Op.OpSourceContinued
        or Op.OpSourceExtension or Op.OpLine or Op.OpNoLine or Op.OpModuleProcessed;

    private delegate void IdVisitor(ref int id);

    private static void ForEachId(OpData data, IdVisitor visitor)
    {
        foreach (var operand in data)
        {
            switch (operand.Kind)
            {
                case OperandKind.IdRef or OperandKind.IdResult or OperandKind.IdResultType or OperandKind.IdScope
                    or OperandKind.IdMemorySemantics or OperandKind.PairIdRefIdRef:
                    for (int i = 0; i < operand.Words.Length; ++i)
                        visitor(ref operand.Words[i]);
                    break;
                case OperandKind.PairIdRefLiteralInteger:
                    for (int i = 0; i < operand.Words.Length; i += 2)
                        visitor(ref operand.Words[i]);
                    break;
                case OperandKind.PairLiteralIntegerIdRef:
                    for (int i = 1; i < operand.Words.Length; i += 2)
                        visitor(ref operand.Words[i]);
                    break;
            }
        }
    }
}
