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

namespace Stride.Shaders.Spirv.Generators
{
    public partial class SPVGenerator
    {
        public void CreateSDSLOp(IncrementalGeneratorInitializationContext context)
        {
            var code = new StringBuilder();
            var nsProvider = context
                .SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: (node, _) => node is NamespaceDeclarationSyntax ns && ns.Name.ToString().StartsWith("Spv"),
                    transform: (node, _) => (NamespaceDeclarationSyntax)node.Node
                );
            context.RegisterImplementationSourceOutput(nsProvider, (ctx, nds) =>
            {
                var eds = nds.ChildNodes().OfType<ClassDeclarationSyntax>().First(x => x.Identifier.Text == "Specification").ChildNodes().OfType<EnumDeclarationSyntax>().First(x => x.Identifier.Text == "Op");
                var members = eds.Members.Where(x => x.Identifier.Text != "Max").ToDictionary(x => x.Identifier.Text, y => ParseInteger(y.EqualsValue.Value.ToString()));
                var lastnum = eds.Members.Where(x => x.Identifier.Text != "Max").Select(x => ParseInteger(x.EqualsValue.Value.ToString())).Max();
                
                foreach (var e in spirvSDSL.RootElement.GetProperty("instructions").EnumerateArray().Select(x => x.GetProperty("opname").GetString()))
                    members.Add(e, ++lastnum);

                code
                    .AppendLine("namespace Stride.Shaders.Spirv.Core;")
                    .AppendLine("")
                    .AppendLine("public enum SDSLOp : int")
                    .AppendLine("{");
                foreach (var e in members)
                    code.Append(e.Key).Append(" = ").Append(e.Value).AppendLine(",");
                code
                    .AppendLine("}");


                ctx.AddSource("SDSLOp.gen.cs", code.ToString());
            });

        }
        public static int ParseInteger(string text)
        {
            if (text.StartsWith("0x"))
                return int.Parse(text.Substring(2), System.Globalization.NumberStyles.HexNumber);
            else
                return int.Parse(text);
        }
    }
}
