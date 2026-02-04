using System.Collections.Frozen;
using System.Text;
using System.Text.Json;
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

        // context.RegisterSourceOutput(file, (spc, ns) => 
        // {
        //     spc.AddSource("IntrinsicsParsedData.cs", $"/*{JsonSerializer.Serialize(ns, new JsonSerializerOptions { WriteIndented = true })}*/");
        // });
    }


    static void GenerateIntrinsicsData(SourceProductionContext spc, EquatableList<NamespaceDeclaration> namespaces)
    {
        var builder = new StringBuilder();

        builder.AppendLine("""
        namespace Stride.Shaders.Core;

        using System.Collections.Frozen;

        public static partial class IntrinsicsDefinitions
        {
        """);

        if (namespaces.Items.Count == 0)
            builder.AppendLine("// No intrinsics parsed");

        foreach (var ns in namespaces)
        {
            builder.AppendLine($"public static FrozenDictionary<string, IntrinsicDefinition[]> {ns.Name.Name} {{ get; }} = new Dictionary<string, IntrinsicDefinition[]>()")
            .AppendLine("{");
            foreach (var intrinsicGroup in ns.Intrinsics.Items.GroupBy(i => i.Name.Name).Where(x => x.Key is not null && x.Key is not "printf"))
            {
                builder.AppendLine($"[\"{intrinsicGroup.Key}\"] = [");
                foreach (var overload in intrinsicGroup.Where(i => i is not null))
                {
                    builder.Append("new(");
                    // Return type
                    builder.AppendLine($"new(\"{overload.ReturnType.Typename.Name}\"");
                    _ = overload.ReturnType.Typename switch
                    {
                        { Size: { Size1: string, Size2: string } } => builder.Append($", new(\"{overload.ReturnType.Typename.Size.Size1}\", \"{overload.ReturnType.Typename.Size.Size2}\")"),
                        { Size.Size1: string } => builder.Append($", new(\"{overload.ReturnType.Typename.Size.Size1}\")"),
                        _ => builder.Append(", null")
                    };

                    _ = overload.ReturnType.Match switch
                    {
                        Matching m => builder.Append($", new({m.ComponentA}, {m.ComponentB})"),
                        _ => builder.Append(", null")
                    };
                    builder.AppendLine("), ");
                    // Parameters
                    builder.AppendLine("[");
                    
                    if(overload is not null && overload.Parameters.Items is not null)
                    foreach (var param in overload.Parameters.Items.Where(p => p is not null && p.Name.Name != "..."))
                    {
                        builder.Append("new(");
                        // Qualifier
                        _ = param.Qualifier switch
                        {
                            { Qualifier: string q, OptionalQualifier: string oq } => builder.Append($"FromString(\"{q}\"), FromStringOptional(\"{oq}\"), "),
                            { Qualifier: string q } => builder.Append($"FromString(\"{q}\"), null, "),
                            _ => builder.Append("null, null, ")
                        };
                        
                        // Type
                        builder.Append($"new(\"{param.TypeInfo.Typename.Name}\"");
                        _ = param.TypeInfo.Typename switch
                        {
                            {Size : {Size1 : string, Size2 : string}} => builder.Append($", new(\"{param.TypeInfo.Typename.Size.Size1}\", \"{param.TypeInfo.Typename.Size.Size2}\")"),
                            { Size.Size1: string } => builder.Append($", new(\"{param.TypeInfo.Typename.Size.Size1}\")"),
                            _ => builder.Append(", null")
                        };
                        _ = param.TypeInfo.Match switch
                        {
                            Matching m => builder.Append($", new({m.ComponentA}, {m.ComponentB})"),
                            _ => builder.Append(", null")
                        };
                        builder.Append($"), \"{param.Name.Name}\"");
                        builder.Append("), ");
                    }
                    builder.AppendLine("]), ");
                }
                builder.AppendLine("],");
            }
            builder.AppendLine("}.ToFrozenDictionary();");
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