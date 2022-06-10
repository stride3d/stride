using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.AST.Shader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders;

public class ShaderClassString
{
    static string[] EntryPointNames = {
        "PSMain",
        "VSMain",
        "GSMain",
        "HSMain",
        "DSMain",
        "CSMain"
    };


    public string Code { get; set; }
    public string ClassName => AST is null ? "" : AST.Name;
    public ShaderProgram? AST { get; set; }

    public ShaderClassString(string code)
    {
        Code = code;
        AST = ShaderMixinParser.ParseShader(code);
    }

    public void Parse()
    {
        AST = ShaderMixinParser.ParseShader(Code);
    }

    public IEnumerable<ShaderVariableDeclaration> GetStreamValues()
    {
        return
            from e in AST?.Body
            where e is ShaderVariableDeclaration v
            && v.IsStream
            select e as ShaderVariableDeclaration;
    }

    public ShaderMethod? GetEntryPoint(EntryPoints entry)
    {
        return AST?.Body.First(e => e is ShaderMethod m && m.Name == entry.ToString()) as ShaderMethod;
    }
    public IEnumerable<ShaderMethod> GetEntryPoints()
    {
        return
                from e in AST?.Body
                where e is ShaderMethod method
                && EntryPointNames.Contains(method.Name)
                select e as ShaderMethod;
    }
    public IEnumerable<ShaderMethod> GetMethods()
    {
        return
            from e in AST?.Body
            where e is ShaderMethod method
            && !EntryPointNames.Contains(method.Name)
            select e as ShaderMethod;
    }


}
