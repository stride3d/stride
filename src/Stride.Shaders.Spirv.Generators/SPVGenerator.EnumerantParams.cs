using AngleSharp.Common;
using AngleSharp.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace Stride.Shaders.Spirv.Generators;

public partial class SPVGenerator : IIncrementalGenerator
{

    public void CreateEnumerantParameters(IncrementalGeneratorInitializationContext context, IncrementalValueProvider<SpirvGrammar> grammarProvider)
    {
        context.RegisterImplementationSourceOutput(
            grammarProvider,
            GenerateEnumerantParameters
        );
    }

    public static void GenerateEnumerantParameters(SourceProductionContext spc, SpirvGrammar grammar)
    {
        if (grammar.OperandKinds?.AsDictionary()?.Values?.ToList() is List<OpKind> opkinds)
        {
            spc.AddSource(
                $"EnumerantParameters.gen.cs",
                SourceText.From(
                    SyntaxFactory
                    .ParseCompilationUnit(@$"
                        using static Stride.Shaders.Spirv.Specification;
                        using CommunityToolkit.HighPerformance;
                        using CommunityToolkit.HighPerformance.Buffers;
                        using Stride.Shaders.Spirv.Core.Buffers;

                        namespace Stride.Shaders.Spirv.Core;
                        
                        {string.Join("\n", opkinds.Where(k => (k.Enumerants?.AsList() ?? []).Any(e => (e.Parameters?.AsList() ?? []).Count > 0)).Select(i => GenerateEnumerantParameterSingle(i, grammar)))}
                        
                        public ref partial struct EnumerantParameters
                        {{
                            {string.Join("\n", opkinds.Where(k => (k.Enumerants?.AsList() ?? []).Any(e => (e.Parameters?.AsList() ?? []).Count > 0)).Select(i => GenerateImplicitCasting(i, grammar)))}
                            {string.Join("\n", opkinds.Where(k => (k.Enumerants?.AsList() ?? []).Any(e => (e.Parameters?.AsList() ?? []).Count > 0)).SelectMany(k => k.Enumerants?.AsList() ?? []).Select(e => e.Parameters)
                            .Select(p => new EquatableList<string>(p?.AsList().Select(x => x.Kind.ToCSType()).ToList() ?? []))
                            .Where(p => p.Count > 1)
                            .Distinct()
                            .Select(i => GenerateImplicitTuples(i, grammar)))}
                        }}
                    ")
                    .NormalizeWhitespace()
                    .ToFullString(),
                    Encoding.UTF8
                )
            );
        }
    }

    public static string GenerateImplicitTuples(in EquatableList<string> parameters, in SpirvGrammar grammar)
    {
        var sb = StringBuilderPool.Get();
        StringBuilderPool.Return(sb);
        if (parameters.AsList() is List<string> { Count: > 0 } paramsList)
        {
            sb.AppendLine(@$"
                public static implicit operator EnumerantParameters(({string.Join(", ", paramsList)}) tuple)
                {{
                    Span<int> span = [");
            for (int i = 0; i < paramsList.Count; i++)
            {
                var parameter = paramsList[i];
                var pName = $"tuple.Item{i + 1}";

                if (i > 0)
                    sb.Append(", ");

                if (parameter is "int")
                    sb.Append($"{pName}");
                else if (parameter is "float")
                    sb.Append($"BitConverter.SingleToInt32Bits({pName})");
                else if (parameter is "string")
                    sb.Append($"..{pName}.AsDisposableLiteralValue().Words");
                else
                    sb.Append($"(int){pName}");
            }
            sb.AppendLine("];");
            sb.AppendLine(@"
                    MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
                    span.CopyTo(buffer.Span);
                    var result = new EnumerantParameters(buffer);
                    return result;
                }
                ");
        }
        return sb.ToString();
    }
    public static string GenerateImplicitCasting(in OpKind opkind, in SpirvGrammar grammar)
    {
        var sb = StringBuilderPool.Get();

        foreach (var enumerant in (opkind.Enumerants?.AsList() ?? []).Where(e => (e.Parameters?.AsList() ?? []).Count > 0))
        {
            if (enumerant.Parameters?.AsList() is List<EnumerantParameter> { Count: > 0 } parameters)
            {

                var structName = enumerant.Name switch
                {
                    "FPFastMathMode" => "FPFastMathModeParameter",
                    "FPRoundingMode" => "FPRoundingModeParameter",
                    "BuiltIn" => "BuiltInParameter",
                    _ => enumerant.Name
                };

                sb.AppendLine(@$"
                public static implicit operator EnumerantParameters({opkind.Kind}Params.{structName} parameter)
                {{
                    Span<int> span = [");

                foreach (var parameter in parameters)
                {
                    if (parameter != parameters[0])
                        sb.Append(", ");
                    var typename = parameter.Kind switch
                    {
                        "LiteralString" => "string",
                        "LiteralInteger" or "IdRef" => "int",
                        "LiteralFloat" => "float",
                        "FPFastMathMode" => "FPFastMathModeMask",
                        _ => parameter.Kind
                    };
                    var pName = $"parameter.{parameter.Name?[0..1].ToUpperInvariant()}{parameter.Name?[1..]}";
                    if (parameters.Count == 1 && (parameter.Kind == enumerant.Name || $"{parameter.Name?[0..1].ToUpperInvariant()}{parameter.Name?[1..]}" == enumerant.Name))
                        pName = "parameter.Value";


                    if (parameter.Kind is "LiteralInteger" or "IdRef")
                        sb.Append($"{pName}");
                    else if (parameter.Kind is "LiteralFloat")
                        sb.Append($"BitConverter.SingleToInt32Bits({pName})");
                    else if (parameter.Kind is "LiteralString")
                        sb.Append($"..{pName}.AsDisposableLiteralValue().Words");
                    else
                        sb.Append($"(int){pName}");
                }
                sb.AppendLine("];");
                sb.AppendLine(@"
                    MemoryOwner<int> buffer = MemoryOwner<int>.Allocate(span.Length);
                    span.CopyTo(buffer.Span);
                    var result = new EnumerantParameters(buffer);
                    return result;
                }
                ");


            }
        }
        var result = sb.ToString();
        StringBuilderPool.Return(sb);
        return result;
    }
    public static string GenerateEnumerantParameterSingle(in OpKind opkind, in SpirvGrammar grammar)
    {
        var sb = StringBuilderPool.Get();
        sb.AppendLine($"public static class {opkind.Kind}Params")
            .AppendLine("{");
        foreach (var enumerant in (opkind.Enumerants?.AsList() ?? []).Where(e => (e.Parameters?.AsList() ?? []).Count > 0))
        {


            if (enumerant.Parameters?.AsList() is List<EnumerantParameter> { Count: > 0 } parameters)
            {
                var structName = enumerant.Name switch
                {
                    "FPFastMathMode" => "FPFastMathModeParameter",
                    "FPRoundingMode" => "FPRoundingModeParameter",
                    "BuiltIn" => "BuiltInParameter",
                    _ => enumerant.Name
                };
                sb.Append($"public ref struct {structName}(");
                if (parameters is [{ } op] && (op.Kind == enumerant.Name || $"{op.Name?[0..1].ToUpperInvariant()}{op.Name?[1..]}" == enumerant.Name))
                {
                    var typename = op.Kind.ToCSType();
                    sb.Append($"{typename} value");
                }
                else
                {

                    foreach (var parameter in parameters)
                    {
                        if (parameter != parameters[0])
                            sb.Append(", ");
                        var typename = parameter.Kind.ToCSType();
                        sb.Append($"{typename} {parameter.Name}");

                    }
                }

                sb.AppendLine($") : IEnumerantParameter<{structName}>")
                    .AppendLine("{");



                if (parameters is [{ } onlyParam] && (onlyParam.Kind == enumerant.Name || $"{onlyParam.Name?[0..1].ToUpperInvariant()}{onlyParam.Name?[1..]}" == enumerant.Name))
                {
                    var typename = onlyParam.Kind.ToCSType();
                    sb.AppendLine($"public {typename} Value {{ get; set; }} = value;");
                }
                else
                {

                    foreach (var parameter in parameters)
                    {
                        var typename = parameter.Kind.ToCSType();
                        sb.AppendLine($"\tpublic {typename} {parameter.Name?[0..1].ToUpperInvariant()}{parameter.Name?[1..]} {{ get; set; }} = {parameter.Name};");

                    }
                }


                sb.Append(@$"
                public static {structName} Create(Span<int> words)
                {{
                    var reader = new EnumerantParametersReader(words);
                    var parameter = new {structName}
                    {{");

                foreach (var parameter in parameters)
                {
                    var pname = parameters.Count == 1 && (parameter.Kind == enumerant.Name || $"{parameter.Name?[0..1].ToUpperInvariant()}{parameter.Name?[1..]}" == enumerant.Name)
                        ? "Value"
                        : $"{parameter.Name?[0..1].ToUpperInvariant()}{parameter.Name?[1..]}";
                    var typename = parameter.Kind.ToCSType();
                    sb.AppendLine($"{pname} = reader.Read{typename switch { "int" => "Int", "float" => "Float", "string" => "String", string s => $"Enum<{s}>" }}(),");

                }
                sb.AppendLine(@$"
                    }};
                    return parameter;
                }}
                }}");

            }
        }


        sb.AppendLine("}");
        StringBuilderPool.Return(sb);
        return sb.ToString();
    }



}

file static class KindExtensions
{
    public static string ToCSType(this string kind)
    {
        return kind switch
        {
            "LiteralString" => "string",
            "LiteralInteger" or "IdRef" or "IdScope" => "int",
            "LiteralFloat" => "float",
            "FPFastMathMode" => "FPFastMathModeMask",
            _ => kind
        };
    }
}