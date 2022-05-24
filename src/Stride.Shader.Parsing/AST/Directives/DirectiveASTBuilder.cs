using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Parse.Ast;

namespace Stride.Shader.Parsing.AST.Directives
{
    public class DirectiveASTBuilder : AstBuilder<DirectiveToken>
    {
        public DirectiveASTBuilder()
        {
            var token = new Builder<DirectiveToken>();

            var shaderProgram = Create("shader", () => new Shader());
            shaderProgram.Children().HasMany<Shader, DirectiveToken>().Builders.Add(
                token
            );

            //token.Create("CodeSnippet", () => new CodeSnippet()).Property<string>((o, v) => o.Snippet = v);
            //token.Create("Define", () => new Define()).Property<>

        }
    }
}
