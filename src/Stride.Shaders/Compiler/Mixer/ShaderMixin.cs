using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.AST.Shader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Mixer;

public abstract class ShaderMixin : ShaderSource
{
    static readonly string[] EntryPointNames = {
        "PSMain",
        "VSMain",
        "GSMain",
        "HSMain",
        "DSMain",
        "CSMain"
    };

    public abstract string Code { get; }
    public string? ClassName => AST?.Name;
    public override IEnumerable<string> MixinNames => AST.Mixins.Select(x => x.Name).ToList();

    public ShaderProgram AST { get; set; }

    public IEnumerable<ShaderVariableDeclaration> GetStreamValues()
    {
        if (AST is not null)
            return
                from e in AST.Body
                where e is ShaderVariableDeclaration v 
                && v.IsStream
                select e as ShaderVariableDeclaration;
        else
            throw new Exception("AST is null");
    }
    public IEnumerable<ShaderMethod> GetEntryPoints()
    {
        if (AST is not null)
            return
                from e in AST.Body
                where e is ShaderMethod method
                && EntryPointNames.Contains(method.Name)
                select e as ShaderMethod;
        else
            throw new Exception("AST is null");
    }
    public IEnumerable<ShaderMethod> GetMethods()
    {
        if (AST is not null)
            return
                from e in AST.Body
                where e is ShaderMethod method
                && !EntryPointNames.Contains(method.Name)
                select e as ShaderMethod;
        else
            throw new Exception("AST is null");
    }
}
