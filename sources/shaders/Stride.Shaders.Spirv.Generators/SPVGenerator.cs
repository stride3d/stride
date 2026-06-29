// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Stride.Shaders.Spirv.Generators;

public partial class SPVGenerator
{
    static readonly JsonSerializerOptions options = new();

    public static void Run(IReadOnlyList<SpvInputFile> files, ISpvOutput output)
    {
        if (!options.Converters.Any(x => x is EquatableListJsonConverter<OperandData>))
            options.Converters.Add(new EquatableListJsonConverter<OperandData>());
        if (!options.Converters.Any(x => x is EquatableListJsonConverter<InstructionData>))
            options.Converters.Add(new EquatableListJsonConverter<InstructionData>());
        if (!options.Converters.Any(x => x is EquatableListJsonConverter<OpKind>))
            options.Converters.Add(new EquatableListJsonConverter<OpKind>());
        if (!options.Converters.Any(x => x is EquatableListJsonConverter<Enumerant>))
            options.Converters.Add(new EquatableListJsonConverter<Enumerant>());
        if (!options.Converters.Any(x => x is EquatableListJsonConverter<string>))
            options.Converters.Add(new EquatableListJsonConverter<string>());
        if (!options.Converters.Any(x => x is EquatableListJsonConverter<EnumerantParameter>))
            options.Converters.Add(new EquatableListJsonConverter<EnumerantParameter>());

        var filtered = files.Where(IsSpirvSpecification).ToImmutableArray();
        var presentNames = new HashSet<string>(filtered.Select(f => Path.GetFileName(f.Path)));
        var missing = RequiredFiles.Where(r => !presentNames.Contains(r)).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException(
                $"Missing SPIR-V specification files: {string.Join(", ", missing)}. "
                + "Check the fetch list in Program.cs.");

        var gen = new SPVGenerator();
        var grammar = gen.PreProcessGrammar(filtered, default);
        grammar = gen.PreProcessEnumerants(grammar, default);
        grammar = gen.PreProcessInstructions(grammar, default);

        GenerateEnumerantParameters(output, grammar);
        GenerateKinds(output, grammar);
        GenerateInstructionInformation(output, grammar);
        ExecuteSDSLOpCreation(output, grammar);
        GenerateInstructionStructs(output, grammar);
        GenerateSDSLSpecification(output, grammar);
    }
}
