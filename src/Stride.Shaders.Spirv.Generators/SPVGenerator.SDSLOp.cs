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
            .SelectMany(static (grammar, _) => grammar.Instructions!.Value)
            .Where(static x => x.OpName is not null && !x.OpName.Contains("GLSL"))
            .Collect()
            .Select(static (arr, _) => new EquatableArray<InstructionData>([.. arr]));

        context.RegisterImplementationSourceOutput(
            instructionsProvider,
            (ctx, instructionArray) =>
            {
                var code = new StringBuilder();
                code
                        .AppendLine("using static Spv.Specification;")
                        .AppendLine("")
                        .AppendLine("namespace Stride.Shaders.Spirv.Core;")
                        .AppendLine("")
                        .AppendLine("public enum SDSLOp : int")
                        .AppendLine("{");
                try
                {
                    Dictionary<string, int> members = instructionArray.Where(x => !x.OpName.Contains("SDSL")).ToDictionary(x => x.OpName, y => y.OpCode);
                    int lastnum = members.Values.Max();
                    foreach (var instruction in instructionArray)
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
                }
                catch (Exception ex)
                {
                    code.AppendLine($"/*{ex.Message}\n\n {ex.StackTrace} */");
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
            }

        );

    }
}
