using Stride.Shader.Parsing.AST.Shader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shader.Parsing;

public class ShaderMixin
{
    public string Code { get; set; }
    public string MixinName { get => AST != null ? AST.Name : string.Empty; }
    public ShaderProgram? AST { get; set; }
    Eto.Parse.Grammar EntryPointMatcher = new Eto.Parse.Grammar(
        Eto.Parse.Terminals.Literal("PSMain")
        | "VSMain"
        | "GSMain"
        | "HSMain"
        | "DSMain"
        | "CSMain");

    SDSLParser Parser { get; set; } = new();

    public ShaderMixin(string code)
    {
        Code = code;
    }

    public void Parse()
    {
        AST = (ShaderProgram)Parser.Parse(Code);
    }

    public IEnumerable<ShaderValueDeclaration> GetStreamValues()
    {
        if (AST is not null)
            return
                AST.Body
                .Where(x => x is ShaderValueDeclaration)
                .Cast<ShaderValueDeclaration>()
                .Where(x => x.IsStream);
        else
            throw new Exception("AST is null");
    }
    public IEnumerable<ShaderMethod> GetEntryPoints()
    {
        if (AST is not null)
            return
                AST.Body
                .Where(x => x is ShaderMethod)
                .Cast<ShaderMethod>()
                .Where(x => EntryPointMatcher.Match(x.Name).Success);
        else
            throw new Exception("AST is null");
    }
    public IEnumerable<ShaderMethod> GetMethods()
    {
        if (AST is not null)
            return
                AST.Body
                .Where(x => x is ShaderMethod)
                .Cast<ShaderMethod>()
                .Where(x => !EntryPointMatcher.Match(x.Name).Success);
        else
            throw new Exception("AST is null");
    }


}
