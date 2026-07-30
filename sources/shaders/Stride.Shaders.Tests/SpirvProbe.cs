// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using System.Text;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// Minimal SPIR-V reader extracting what the Vulkan test renderer needs:
/// entry points and stage input locations/semantics.
/// </summary>
public class SpirvProbe
{
    public const uint ExecutionModelVertex = 0;
    public const uint ExecutionModelTessellationControl = 1;
    public const uint ExecutionModelTessellationEvaluation = 2;
    public const uint ExecutionModelGeometry = 3;
    public const uint ExecutionModelFragment = 4;
    public const uint ExecutionModelGLCompute = 5;

    private const uint OpEntryPoint = 15;
    private const uint OpVariable = 59;
    private const uint OpDecorate = 71;
    private const uint OpDecorateString = 5632;

    private const uint DecorationBuiltIn = 11;
    private const uint DecorationLocation = 30;
    private const uint DecorationUserSemantic = 5635;

    private const uint StorageClassInput = 1;

    public record EntryPointInfo(uint ExecutionModel, string Name, uint[] Interface);

    public List<EntryPointInfo> EntryPoints { get; } = [];
    public Dictionary<uint, uint> Locations { get; } = [];
    public Dictionary<uint, string> UserSemantics { get; } = [];
    public HashSet<uint> BuiltIns { get; } = [];
    public Dictionary<uint, uint> VariableStorageClasses { get; } = [];

    public SpirvProbe(ReadOnlySpan<byte> spirv)
    {
        var words = MemoryMarshal.Cast<byte, uint>(spirv);
        if (words.Length < 5 || words[0] != 0x07230203)
            throw new InvalidOperationException("Not a SPIR-V module");

        for (int i = 5; i < words.Length;)
        {
            var wordCount = (int)(words[i] >> 16);
            var opcode = words[i] & 0xFFFF;
            if (wordCount == 0)
                throw new InvalidOperationException("Invalid SPIR-V instruction");
            var operands = words.Slice(i + 1, wordCount - 1);

            switch (opcode)
            {
                case OpEntryPoint:
                {
                    var name = ReadString(operands.Slice(2), out var nameWords);
                    EntryPoints.Add(new EntryPointInfo(operands[0], name, operands.Slice(2 + nameWords).ToArray()));
                    break;
                }
                case OpVariable:
                    VariableStorageClasses[operands[1]] = operands[2];
                    break;
                case OpDecorate:
                    switch (operands[1])
                    {
                        case DecorationLocation:
                            Locations[operands[0]] = operands[2];
                            break;
                        case DecorationBuiltIn:
                            BuiltIns.Add(operands[0]);
                            break;
                    }
                    break;
                case OpDecorateString:
                    if (operands[1] == DecorationUserSemantic)
                        UserSemantics[operands[0]] = ReadString(operands.Slice(2), out _);
                    break;
            }

            i += wordCount;
        }
    }

    public EntryPointInfo? FindEntryPoint(uint executionModel)
        => EntryPoints.FirstOrDefault(x => x.ExecutionModel == executionModel);

    /// <summary>
    /// Stage inputs (user variables with a location) of the given entry point: (Location, Semantic).
    /// </summary>
    public IEnumerable<(uint Location, string Semantic)> GetStageInputs(EntryPointInfo entryPoint)
    {
        foreach (var id in entryPoint.Interface)
        {
            if (VariableStorageClasses.TryGetValue(id, out var storageClass) && storageClass == StorageClassInput
                && !BuiltIns.Contains(id) && Locations.TryGetValue(id, out var location))
            {
                UserSemantics.TryGetValue(id, out var semantic);
                yield return (location, semantic ?? "");
            }
        }
    }

    private static string ReadString(ReadOnlySpan<uint> words, out int wordsRead)
    {
        var bytes = MemoryMarshal.Cast<uint, byte>(words);
        var length = bytes.IndexOf((byte)0);
        wordsRead = length / 4 + 1;
        return Encoding.UTF8.GetString(bytes.Slice(0, length));
    }
}
