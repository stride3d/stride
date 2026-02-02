using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Stride.Shaders.Generators.Intrinsics;

namespace Stride.Shaders.Generators;

[Generator]
internal class IntrinsicsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var file =
            context
            .AdditionalTextsProvider
            .Where(x => x.Path.EndsWith("gen_intrin_main.txt"))
            .Select(ParseInstrinsics);

        context.RegisterSourceOutput(file, GenerateIntrinsicsData);
    }


    static void GenerateIntrinsicsData(SourceProductionContext spc, EquatableList<NamespaceDeclaration> ns)
    {
        var builder = new StringBuilder();

        builder.AppendLine("""
        namespace Stride.Shaders.Core;

        internal static partial class IntrinsicsDefinitions
        {
        """);

        if (ns.Items.Count == 0)
            builder.AppendLine("// No intrinsics parsed");
        foreach (var n in ns)
        {
            builder.AppendLine($"internal static Dictionary<string, IntrinsicDefinition[]> {n.Name.Name} = new()")
            .AppendLine("{");
            foreach (var intrin in n.Intrinsics)
                builder.AppendLine($"[\"{intrin.Name.Name}\"] = [],");
            builder.AppendLine("};");
        }
        builder.AppendLine("}");

        spc.AddSource(
            "IntrinsicsData.g.cs", 
            SourceText.From(
                SyntaxFactory.ParseCompilationUnit(builder.ToString())
                .NormalizeWhitespace()
                .ToFullString(),
                Encoding.UTF8
            )
        );
    }


    internal static EquatableList<NamespaceDeclaration> ParseInstrinsics(AdditionalText text, CancellationToken ct)
    {
        if (IntrinParser.ProcessAndParse(text.GetText()?.ToString() ?? "", out var ns))
            return ns;
        else return [];
    }
}