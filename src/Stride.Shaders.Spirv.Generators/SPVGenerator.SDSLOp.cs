using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using AngleSharp.Common;
using Microsoft.CodeAnalysis.Text;
using System.Dynamic;

namespace Stride.Shaders.Spirv.Generators;

public partial class SPVGenerator
{
    public void CreateSDSLOp(IncrementalGeneratorInitializationContext context, IncrementalValueProvider<SpirvGrammar> grammarProvider)
    {

        var instructionsProvider =
            grammarProvider
            .SelectMany(static (grammar, b) => grammar.Instructions?.AsList() ?? [])
            .Where(static x => x.OpName is not null && !x.OpName.Contains("GLSL"))
            .Collect()
            .Select(static (arr, _) => new EquatableList<InstructionData>([.. arr]));

        context.RegisterImplementationSourceOutput(
            instructionsProvider,
            ExecuteSDSLOpCreation


        );

    }
    public void ExecuteSDSLOpCreation(SourceProductionContext ctx, EquatableList<InstructionData> instructionArray)
    {

        var code = new StringBuilder();
        code
                .AppendLine("using static Stride.Shaders.Spirv.Specification;")
                .AppendLine("")
                .AppendLine("namespace Stride.Shaders.Spirv.Core;")
                .AppendLine("")
                .AppendLine("public enum SDSLOp : int")
                .AppendLine("{");

        Dictionary<string, int> members = instructionArray.Where(x => !x.OpName.Contains("SDSL")).ToDictionary(x => x.OpName, y => y.OpCode)!;
        int lastnum = members.Values.Max();
        foreach (var instruction in instructionArray!)
        {
            if (members.TryGetValue(instruction.OpName, out var value))
            {
                if (instruction.OpName.Contains("SDSL") && value <= 0)
                    value = ++lastnum;
                code.AppendLine($"    {instruction.OpName} = {value},");
            }
            else
            {
                members.Add(instruction.OpName, ++lastnum);
                code.AppendLine($"    {instruction.OpName} = {lastnum},");
            }
        }


        code.AppendLine("}");
        ctx.AddSource("SDSLOp.gen.cs",
            SourceText.From(
                SyntaxFactory
                .ParseCompilationUnit(code.ToString())
                .NormalizeWhitespace()
                .ToFullString(),
                Encoding.UTF8
         )
        );
        code.Clear();
        code
                .AppendLine("using static Stride.Shaders.Spirv.Specification;")
                .AppendLine("")
                .AppendLine("namespace Stride.Shaders.Spirv;")
                .AppendLine("")
                .AppendLine("public static partial class Specification")
                .AppendLine("{")
                .AppendLine("public enum Op : int")
                .AppendLine("{");

        foreach (var instruction in instructionArray!)
        {
            if (members.TryGetValue(instruction.OpName, out var value))
            {
                if (instruction.OpName.Contains("SDSL") && value <= 0)
                    value = ++lastnum;
                code.AppendLine($"    {instruction.OpName} = {value},");
            }
            else
            {
                members.Add(instruction.OpName, ++lastnum);
                code.AppendLine($"    {instruction.OpName} = {lastnum},");
            }
        }
        code.AppendLine("}}");

        ctx.AddSource("SpecificationOp.gen.cs",
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
