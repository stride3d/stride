using AngleSharp.Dom;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.Json;
using AngleSharp;
using System.Net.Http.Headers;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Stride.Shaders.Spirv.Generators;


[Generator]
public partial class SPVGenerator : IIncrementalGenerator
{
    static readonly JsonSerializerOptions options = new();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        if (!options.Converters.Any(x => x is EquatableArrayJsonConverter<OperandData>))
            options.Converters.Add(new EquatableArrayJsonConverter<OperandData>());
        if (!options.Converters.Any(x => x is EquatableArrayJsonConverter<InstructionData>))
            options.Converters.Add(new EquatableArrayJsonConverter<InstructionData>());
        if (!options.Converters.Any(x => x is EquatableArrayJsonConverter<OpKind>))
            options.Converters.Add(new EquatableArrayJsonConverter<OpKind>());
        if (!options.Converters.Any(x => x is EquatableArrayJsonConverter<Enumerant>))
            options.Converters.Add(new EquatableArrayJsonConverter<Enumerant>());
        if (!options.Converters.Any(x => x is EquatableArrayJsonConverter<string>))
            options.Converters.Add(new EquatableArrayJsonConverter<string>());

        
        

        var grammarData =
            context
            .AdditionalTextsProvider
            .Where(IsSpirvSpecification)
            .Collect()
            .Select(PreProcessGrammar)
            .Select(PreProcessInstructions);

        context.SyntaxProvider.CreateSyntaxProvider(
            predicate: (s, _) => s is ClassDeclarationSyntax or EnumDeclarationSyntax or StructDeclarationSyntax or MemberDeclarationSyntax,
            transform: (ctx, _) => ctx
        ).Combine(grammarData);

        CreateInfo(context, grammarData);
        CreateSDSLOp(context, grammarData);
        GenerateStructs(context, grammarData);

        context.RegisterImplementationSourceOutput(
            grammarData,
            BufferGeneration
        );


    }

    public static void BufferGeneration(SourceProductionContext spc, SpirvGrammar source)
    {
        var operandKinds = source.OperandKinds!.Value.AsArray().ToDictionary(x => x.Kind, x => x);
        var code = new StringBuilder();
        code
            .AppendLine("using static Spv.Specification;")
            .AppendLine("using Stride.Shaders.Spirv.Core.Buffers;")
            .AppendLine("")
            .AppendLine("namespace Stride.Shaders.Spirv.Core;")
            .AppendLine("")
            .AppendLine("public static class SpirvBufferExtensions")
            .AppendLine("{");
        foreach (var instruction in source.Instructions!.Value.AsArray()!)
        {
            if (instruction.OpName.StartsWith("Op"))
                CreateOperation(instruction, code, operandKinds);
            else
                CreateGlslOperation(instruction, code, operandKinds);
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


}
