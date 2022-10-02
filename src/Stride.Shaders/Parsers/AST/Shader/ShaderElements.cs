using Eto.Parse;
using Stride.Shaders.Parsing.AST.Shader.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Shaders.Parsing.AST.Shader;

public class StructField : ShaderToken
{
    public ISymbolType Type {get;set;}
    public string Name {get;set;}
    

    public StructField(Match m, SymbolTable s)
    {
        Match = m;
        Type = s.Tokenize(m["ValueTypes"]);
        Name = m["Name"].StringValue;
    }
}

public class StructDefinition : ShaderToken
{
    public string StructName {get;set;}
    public List<StructField> Fields {get;set;}

    public CompositeType Type {get;set;}

    public StructDefinition(Match m, SymbolTable s)
    {
        Match = m;
        StructName = Match["StructName"].StringValue;
        Fields = Match["Fields"].Matches.Select(x => new StructField(x,s)).ToList();
        Type = new CompositeType(StructName, new(Fields.ToDictionary(x => x.Name, x => s.Tokenize(x.Match["ValueTypes"]))));
    }
}

public class ResourceGroup : ShaderToken
{
    public IEnumerable<ShaderToken> Variables {get;set;}

    public ResourceGroup(Match m, SymbolTable s)
    {
        Match = m;
        throw new NotImplementedException();
        // Variables = m["Variables"].Matches.Select(GetToken).ToList();
        
    }
}
public class ConstantBuffer : ShaderToken
{
    public IEnumerable<ShaderToken> Variables {get;set;}

    public ConstantBuffer(Match m, SymbolTable s)
    {
        Match = m;
        throw new NotImplementedException();
        // Variables = m["Variables"].Matches.Select(GetToken).ToList();

    }
}

public class ShaderVariableDeclaration : ShaderToken
{
    public bool IsStream {get;set;}
    public bool IsStaged {get;set;}
    public string Name {get;set;}
    public ISymbolType Type {get;set;}
    public string? Semantic { get; set; }
    public ShaderToken Expression {get;set;}

    public ShaderVariableDeclaration(Match m, SymbolTable s)
    {
        Match = m;
        IsStream = m["Stream"].Success;
        IsStaged = m["Stage"].Success;
        Semantic = m["Semantic"].Success ? m["Semantic"].StringValue : null;
        Type = s.PushType(m["ValueTypes"].StringValue,m["ValueTypes"]);
        Name = m["Identifier"].StringValue;
    }
}
public class Generics : ShaderToken
{
    public string Type { get; set; }
    public string Name { get; set; }
}

public class ShaderGenerics : ShaderToken
{
    public string Name { get; set; }
    public IEnumerable<Generics> Generics { get; set; }
}

public class MixinToken : ShaderToken
{
    public string Name { get; set; }
    public List<string> GenericsValues { get; set; }

    public MixinToken(Match m)
    {
        Match = m;
        Name = m["Name"].StringValue;
        GenericsValues = m["Generics"].Matches.Select(x => x.StringValue).ToList();
    }
}