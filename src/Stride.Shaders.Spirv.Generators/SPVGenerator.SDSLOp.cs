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
            .Select((x, _) => x!.Instructions!.Value);
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
                        if (instruction.OpName is null)
                            continue;
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

        // var code = new StringBuilder();
        // var nsProvider = context
        //     .SyntaxProvider
        //     .CreateSyntaxProvider(
        //         predicate: (node, _) => node is NamespaceDeclarationSyntax ns && ns.Name.ToString().StartsWith("Spv"),
        //         transform: (node, _) => (NamespaceDeclarationSyntax)node.Node
        //     );
        // context.RegisterImplementationSourceOutput(nsProvider, (ctx, nds) =>
        // {
        //     var eds = nds.ChildNodes().OfType<ClassDeclarationSyntax>().First(x => x.Identifier.Text == "Specification").ChildNodes().OfType<EnumDeclarationSyntax>().First(x => x.Identifier.Text == "Op");
        //     var members = eds.Members.Where(x => x.Identifier.Text != "Max").ToDictionary(x => x.Identifier.Text, y => ParseInteger(y.EqualsValue!.Value.ToString()));
        //     var lastnum = eds.Members.Where(x => x.Identifier.Text != "Max").Select(x => ParseInteger(x.EqualsValue!.Value.ToString())).Max();

        //     foreach (var e in spirvSDSL!.Instructions.Select(x => x.OpName))
        //         members.Add(e!, ++lastnum);

        //     code
        //         .AppendLine("namespace Stride.Shaders.Spirv.Core;")
        //         .AppendLine("")
        //         .AppendLine("public enum SDSLOp : int")
        //         .AppendLine("{");
        //     foreach (var e in members)
        //         code.Append(e.Key).Append(" = ").Append(e.Value).AppendLine(",");
        //     code
        //         .AppendLine("}");


        //     ctx.AddSource("SDSLOp.gen.cs", code.ToSourceText());
        // });

    }
    public static int ParseInteger(string text)
    {
        if (text.StartsWith("0x"))
            return int.Parse(text.Substring(2), System.Globalization.NumberStyles.HexNumber);
        else
            return int.Parse(text);
    }
}
