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

    static string GenerateArguments(List<IntrinsicParameter> parameters, bool optional = false, int startIndex = 0)
    {
        return string.Concat(parameters.Where(p => p.Name.Name != "...").Select((p, i) => $", {(optional ? $"{p.Name.Name}:" : "")}new SpirvValue(compiledParams[{startIndex + i}], context.GetOrRegister(functionType.ParameterTypes[{startIndex + i}].Type))"));
    }
    
    static string CapitalizeFirstLetter(string s) => char.ToUpper(s[0]) + s[1..];
    static string UncapitalizeFirstLetter(string s) => char.ToLower(s[0]) + s[1..];

    // Group of intrinsics with same parameter names (parameter types might differ)
    record IntrinsicOverloadGroup(string Name, List<IntrinsicParameter> MandatoryParameters, List<IntrinsicParameter> OptionalParameters, List<(string DeclaringNamespace, IntrinsicDeclaration Declaration)> Overloads)
    {
        public string Name { get; set; } = Name;
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

        builder.AppendLine("public interface IIntrinsicCompiler");
        builder.AppendLine("{");
        builder.AppendLine("    SpirvValue CompileIntrinsic(SymbolTable table, CompilerUnit compiler, string? @namespace, string name, FunctionType functionType, SpirvValue? thisValue, Span<int> compiledParams);");
        builder.AppendLine("}");

        static string IntrinsicDeclarationKey(NamespaceDeclaration arg)
        {
            var key = arg.Name.Name;
            
            // Merge RW and non-RW methods in same type
            if (key.StartsWith("RW"))
                key = key.Substring("RW".Length);
                
            
            // Merge all texture methods in same type
            if (key.StartsWith("Texture"))
                return "TextureMethods";
            
            return key;
        }

        static bool DecodeThisType(string @namespace, out string typeName)
        {
            if (@namespace.EndsWith("Methods"))
            {
                typeName = @namespace.Substring(0, @namespace.Length - "Methods".Length);
                return true;
            }

            typeName = string.Empty;
            return false;
        }

        static string NormalizeParameters(string @namespace, string methodName, string parameterName)
        {
            return (@namespace, methodName, parameterName) switch
            {
                ("TextureMethods", "SampleCmp" or "SampleCmpLevelZero", "c") => "compareValue",
                _ => parameterName,
            };
        }
        
        foreach (var ns in namespaces.Items.GroupBy(IntrinsicDeclarationKey))
        {
            bool hasThis = DecodeThisType(ns.Key, out var thisType);
            var thisParam = hasThis ? $", SpirvValue {UncapitalizeFirstLetter(thisType)}" : "";
            var thisArg = hasThis ? ", thisValue.Value" : "";
            
            var intrinsicGroups = new Dictionary<string, IntrinsicOverloadGroup>();
            
            foreach (var intrinsicGroup in ns.SelectMany(x => x.Intrinsics.Items.Select(y => (DeclaringNamespace: x.Name.Name, Declaration: y)))
                         .Where(x => x.Declaration.Parameters.Items.All(p => p.Name.Name != "..."))
                         .GroupBy(i => i.Declaration.Name.Name))
            {
                // Normalize parameters
                foreach (var x in intrinsicGroup)
                {
                    var parameters = x.Declaration.Parameters.Items.Select(p => p with { Name = p.Name with { Name = NormalizeParameters(ns.Key, x.Declaration.Name.Name, p.Name.Name) } }).ToList();
                    x.Declaration.Parameters.Items.Clear();
                    x.Declaration.Parameters.Items.AddRange(parameters);
                }
                
                // Find common parameters
                var maxParameterCount = intrinsicGroup.Min(x => x.Declaration.Parameters.Items.Count);
                var mandatoryParameters = intrinsicGroup.First().Declaration.Parameters.Items.GetRange(0, maxParameterCount);
                for (int i = 0; i < maxParameterCount; ++i)
                {
                    if (!intrinsicGroup.All(x => x.Declaration.Parameters.Items[i].Name.Name == mandatoryParameters[i].Name.Name))
                    {
                        mandatoryParameters = mandatoryParameters.GetRange(0, i);
                        break;
                    }
                }
                
                var optionalParameters = intrinsicGroup
                    .SelectMany(x => x.Declaration.Parameters.Items.Skip(mandatoryParameters.Count))
                    .GroupBy(x => x.Name.Name)
                    .Select(x => x.First())
                    .ToList();

                intrinsicGroups[intrinsicGroup.Key] = new IntrinsicOverloadGroup(intrinsicGroup.Key, mandatoryParameters, optionalParameters, intrinsicGroup.ToList());
            }
            
            builder.AppendLine($"public abstract class {ns.Key}Declarations : IIntrinsicCompiler");
            builder.AppendLine("{");
            
            foreach (var intrinsicGroup in intrinsicGroups)
            {
                // Get parameters of first and last overload (the ones with the less and most parameters)
                var mandatoryParameters = intrinsicGroup.Value.MandatoryParameters;

                var optionalParameters = intrinsicGroup.Value.OptionalParameters;
                builder.AppendLine($"public virtual SpirvValue Compile{CapitalizeFirstLetter(intrinsicGroup.Key)}(SpirvContext context, SpirvBuilder builder, FunctionType functionType{thisParam}{GenerateParameters(mandatoryParameters)}{GenerateParameters(optionalParameters, true)}) => throw new NotImplementedException();");
            }
            
            builder.AppendLine("public SpirvValue CompileIntrinsic(SymbolTable table, CompilerUnit compiler, string? @namespace, string name, FunctionType functionType, SpirvValue? thisValue, Span<int> compiledParams) {");
            builder.AppendLine("var (builder, context) = compiler;");
            builder.AppendLine("return (@namespace, name, compiledParams.Length) switch {");
            foreach (var intrinsicGroup in intrinsicGroups)
            {
                var mandatoryParameters = intrinsicGroup.Value.MandatoryParameters;
                builder.AppendLine($"// group {intrinsicGroup.Key}");

                // We split by parameter count then parameters to know if we really need to include switch case in namepsace
                foreach (var intrinsicOverloadsByParamCount in intrinsicGroup.Value.Overloads
                             .GroupBy(x => x.Declaration.Parameters.Items.Count))
                {
                    var intrinsicOverloadGroupsByParameters = intrinsicOverloadsByParamCount.GroupBy(x => GenerateParameters(x.Declaration.Parameters.Items)).ToList();
                    foreach (var intrinsicOverloadGroups in intrinsicOverloadGroupsByParameters)
                    {
                        var optionalParameters = intrinsicOverloadGroups.First().Declaration.Parameters.Items.GetRange(mandatoryParameters.Count, intrinsicOverloadGroups.First().Declaration.Parameters.Items.Count - mandatoryParameters.Count);

                        // If only one parameter signature for all overloads with same number of parameters, skip namespace
                        var declaredInNamespaces = intrinsicOverloadGroupsByParameters.Count > 1
                            ? intrinsicOverloadGroups.Select(x => $"\"{x.DeclaringNamespace}\"").Distinct().ToArray()
                            : ["_"];
                        foreach (var @namespace in declaredInNamespaces)
                        {
                            builder.AppendLine($"({@namespace}, \"{intrinsicGroup.Key}\", {intrinsicOverloadGroups.First().Declaration.Parameters.Items.Count}) => Compile{CapitalizeFirstLetter(intrinsicGroup.Key)}(context, builder, functionType{thisArg}{GenerateArguments(mandatoryParameters)}{GenerateArguments(optionalParameters, true, mandatoryParameters.Count)}),");
                        }
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