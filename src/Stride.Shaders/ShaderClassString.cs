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
    public string Code { get; set; }
    public string ClassName { get; set; }
    public ShaderProgram? AST { get; set; }
    string[] EntryPointNames = {
        "PSMain",
        "VSMain",
        "GSMain",
        "HSMain",
        "DSMain",
        "CSMain"
    };

    ShaderMixinParser Parser { get; set; }

    public ShaderClassString(string code, ShaderMixinParser parser)
    {
        Code = code;
        Parser = parser;
    }

    public void Parse()
    {
        AST = (ShaderProgram)Parser.Parse(Code);
    }

    public IEnumerable<ShaderValueDeclaration> GetStreamValues()
    {
        return
            from e in AST?.Body
            where e is ShaderValueDeclaration v
            && v.IsStream
            select e as ShaderValueDeclaration;
    }

    public ShaderMethod GetEntryPoint(EntryPoints entry)
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
