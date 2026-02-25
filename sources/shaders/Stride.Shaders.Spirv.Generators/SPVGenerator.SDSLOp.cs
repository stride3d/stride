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
            .Where(static x => x.OpName is not null)
            .Collect()
            .Select(static (arr, _) => new EquatableList<InstructionData>([.. arr]));

        context.RegisterSourceOutput(
            instructionsProvider,
            ExecuteSDSLOpCreation
        );

    }
    public void ExecuteSDSLOpCreation(SourceProductionContext ctx, EquatableList<InstructionData> instructionArray)
    {

        var members = instructionArray.ToDictionary(x => x.OpName, y => y.OpCode)!;
        int lastnum = 0;

        var code = new StringBuilder();
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
            if (instruction.OpName.Contains("GLSL"))
                continue;
            if (members.TryGetValue(instruction.OpName, out var value))
            {
                if ((instruction.OpName.Contains("SDSL") || instruction.OpName.Contains("SDFX")) && value <= 0)
                    value = ++lastnum;
                code.AppendLine($"    {instruction.OpName} = {value},");
                lastnum = value;
            }
        }
        code.AppendLine("}");

        code.AppendLine("public enum GLSLOp : int")
            .AppendLine("{");
        foreach (var instruction in instructionArray!)
        {
            if (!instruction.OpName.Contains("GLSL"))
                continue;
            if (members.TryGetValue(instruction.OpName, out var value))
                code.AppendLine($"    {instruction.OpName} = {value},");
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
