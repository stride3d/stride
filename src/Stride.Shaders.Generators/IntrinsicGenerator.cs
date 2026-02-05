using System.Collections.Frozen;
using System.Diagnostics;
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
        context.RegisterSourceOutput(file, GenerateIntrinsicsCall);

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
            foreach (var intrinsicGroup in ns.Intrinsics.Items.GroupBy(i => i.Name.Name).Where(x => x.Key is not "printf"))
            {
                builder.AppendLine($"[\"{intrinsicGroup.Key}\"] = [");
                foreach (var overload in intrinsicGroup)
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
                        Matching m => builder.Append($", new({m.LayoutIndex}, {m.BaseTypeIndex})"),
                        _ => builder.Append(", null")
                    };
                    builder.AppendLine("), ");
                    // Parameters
                    builder.AppendLine("[");
                    
                    foreach (var param in overload.Parameters.Items.Where(p => p.Name.Name != "..."))
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
                            Matching m => builder.Append($", new({m.LayoutIndex}, {m.BaseTypeIndex})"),
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

    static string GenerateParameters(List<IntrinsicParameter> parameters, bool optional = false)
    {
        return string.Concat(parameters.Where(p => p.Name.Name != "...").Select(p => optional ? $", SpirvValue? {p.Name.Name} = null" : $", SpirvValue {p.Name.Name}"));
    }

    static string GenerateArguments(List<IntrinsicParameter> parameters)
    {
        return string.Concat(parameters.Where(p => p.Name.Name != "...").Select((p, i) => $", new SpirvValue(compiledParams[{i}], context.GetOrRegister(functionType.ParameterTypes[{i}].Type))"));
    }
    
    static string CapitalizeFirstLetter(string s) => char.ToUpper(s[0]) + s[1..];

    // Group of intrinsics with same parameter names (parameter types might differ)
    record IntrinsicOverloadGroup(string Name, List<IntrinsicParameter> Parameters, List<IntrinsicDeclaration> Overloads)
    {
        public TrieNode<IntrinsicOverloadGroup> TrieNode { get; set; }
    }
    
    static void GenerateIntrinsicsCall(SourceProductionContext spc, EquatableList<NamespaceDeclaration> namespaces)
    {
        var builder = new StringBuilder();

        builder.AppendLine("""
                           namespace Stride.Shaders.Parsing.SDSL;

                           using System.Collections.Frozen;
                           using Stride.Shaders.Core;
                           using Stride.Shaders.Spirv.Building;
                           using Stride.Shaders.Parsing.Analysis;
                           
                           """);

        foreach (var ns in namespaces.Items)
        {
            var intrinsicGroups = new Dictionary<string, Trie<IntrinsicOverloadGroup>>();
            
            foreach (var intrinsicGroup in ns.Intrinsics.Items
                         .Where(x => x.Parameters.Items.All(p => p.Name.Name != "..."))
                         .GroupBy(i => i.Name.Name))
            {
                var trie = new Trie<IntrinsicOverloadGroup>();
                foreach (var overload in intrinsicGroup.GroupBy(x => GenerateParameters(x.Parameters.Items)))
                {
                    var parameters = overload.First().Parameters.Items.Where(p => p.Name.Name != "...").ToList();
                    var intrinsicOverloadGroup = new IntrinsicOverloadGroup(intrinsicGroup.Key, parameters, overload.ToList());
                    intrinsicOverloadGroup.TrieNode = trie.Insert(parameters.Select(p => p.Name.Name).ToList(), intrinsicOverloadGroup);
                }
                
                // Try to attach method definition to a parent definition with optional parameter (only if one option)
                // i.e. (a,b) and (a,b,c) will be grouped into (a,b,c?)
                // however (a,b) (a,b,c) and (a,b,d) won't be merged as (a,b) has two possible optional parameter branches
            
                // To do that, we will simplify node with only a single leaves
                trie.SimplifySingleLeaves();
                
                intrinsicGroups.Add(intrinsicGroup.Key, trie);
            }
            
            
            builder.AppendLine($"public abstract class {ns.Name.Name}Declarations");
            builder.AppendLine("{");

            foreach (var intrinsicGroup in intrinsicGroups)
            {
                foreach (var intrinsicOverloadGroup in intrinsicGroup.Value.EnumerateNodes())
                {
                    if (intrinsicOverloadGroup.Values.Count == 0)
                        continue;

                    // Get parameters of first and last overload (the ones with the less and most parameters)
                    var mandatoryParameters1 = intrinsicOverloadGroup.Values.First().Parameters;
                    var mandatoryParameters2 = intrinsicOverloadGroup.Values.Last().Parameters;
                    var optionalParameters = mandatoryParameters2.GetRange(mandatoryParameters1.Count, mandatoryParameters2.Count - mandatoryParameters1.Count);
                    builder.AppendLine($"public virtual SpirvValue Compile{CapitalizeFirstLetter(intrinsicGroup.Key)}(SpirvContext context, SpirvBuilder builder, FunctionType functionType{GenerateParameters(mandatoryParameters1)}{GenerateParameters(optionalParameters, true)}) => throw new NotImplementedException();");
                }
            }
            
            builder.AppendLine("public SpirvValue CompileIntrinsic(SymbolTable table, CompilerUnit compiler, string name, FunctionType functionType, Span<int> compiledParams) {");
            builder.AppendLine("var (builder, context) = compiler;");
            builder.AppendLine("return (name, compiledParams.Length) switch {");
            foreach (var intrinsicGroup in intrinsicGroups)
            {
                foreach (var intrinsicOverloadGroup in intrinsicGroup.Value.EnumerateNodes())
                {
                    foreach (var overload in intrinsicOverloadGroup.Values)
                    {
                        builder.AppendLine($"(\"{overload.Name}\", {overload.Parameters.Count}) => Compile{CapitalizeFirstLetter(overload.Name)}(context, builder, functionType{GenerateArguments(overload.Parameters)}),");
                    }
                }
            }

            builder.AppendLine("};");
            builder.AppendLine("}");
            builder.AppendLine("}");
        }

        spc.AddSource(
            "IntrinsicsDeclarations.g.cs",
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