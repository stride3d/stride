using Microsoft.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace Stride.Shaders.Spirv.Generators;


[Generator]
public partial class SPVGenerator : IIncrementalGenerator
{
    static readonly JsonSerializerOptions options = new();

    internal static readonly DiagnosticDescriptor MissingSpecDiagnostic = new(
        id: "SPV0001",
        title: "Missing SPIR-V specification files",
        messageFormat: "Missing SPIR-V specification files: {0}. Run 'git submodule update --init --recursive'.",
        category: "SpirvGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
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
        var files =
            context
            .AdditionalTextsProvider
            .Where(IsSpirvSpecification)
            .Collect();

        // Emit SPV0001 if any required grammar file is missing (submodule not fetched).
        context.RegisterSourceOutput(files, static (spc, texts) =>
        {
            var fileNames = new HashSet<string>(texts.Select(f => Path.GetFileName(f.Path)));
            var missing = RequiredFiles.Where(r => !fileNames.Contains(r)).ToArray();
            if (missing.Length > 0)
                spc.ReportDiagnostic(Diagnostic.Create(MissingSpecDiagnostic, Location.None, string.Join(", ", missing)));
        });

        var grammarData =
            files
            .Select(PreProcessGrammar)
            .Select(PreProcessEnumerants)
            .Select(PreProcessInstructions);

        CreateEnumerantParameters(context, grammarData);
        CreateInfo(context, grammarData);
        CreateSDSLOp(context, grammarData);
        GenerateStructs(context, grammarData);
        CreateSpecification(context, grammarData);

        // context.RegisterImplementationSourceOutput(
        //     grammarData,
        //     BufferGeneration
        // );


    }

    public static void BufferGeneration(SourceProductionContext spc, SpirvGrammar source)
    {

        try
        {
            var code = new StringBuilder();
            code
                .AppendLine("using static Stride.Shaders.Spirv.Specification;")
                .AppendLine("using Stride.Shaders.Spirv.Core.Buffers;")
                .AppendLine("")
                .AppendLine("namespace Stride.Shaders.Spirv.Core;")
                .AppendLine("")
                .AppendLine("public static class SpirvBufferExtensions")
                .AppendLine("{");
            foreach (var instruction in source.Instructions?.AsList() ?? [])
            {
                if (instruction.OpName.StartsWith("Op"))
                    CreateOperation(instruction, code, source.OperandKinds?.AsDictionary() ?? []);
                else
                    CreateGlslOperation(instruction, code, source.OperandKinds?.AsDictionary() ?? []);
            }
            code
                .AppendLine("}");

            spc.AddSource("SpirvBufferExtensions.gen.cs",
                SourceText.From(
                    SyntaxFactory
                    .ParseCompilationUnit(code.ToString())
                    .NormalizeWhitespace()
                    .ToFullString(),
                    Encoding.UTF8
                )
            );
        }
        catch (Exception ex)
        {
            spc.AddSource("SpirvBufferExtensions.gen.cs",
                SourceText.From(
                    SyntaxFactory
                    .ParseCompilationUnit($"/* Error generating SpirvBufferExtensions: {ex.Message} */")
                    .NormalizeWhitespace()
                    .ToFullString(),
                    Encoding.UTF8
                )
            );
        }

    }


}
