using Eto.Parse;

namespace Stride.Shaders.Parsing.AST.Shader.Analysis;

public interface ISymbolType
{
    public bool IsAccessorValid(string accessor);
    public bool IsIndexingValid(string index);
}

public class ArrayType : ISymbolType
{
    public ISymbolType TypeName {get;set;}

    public bool IsAccessorValid(string accessor)
    {
        return false;
    }

    public bool IsIndexingValid(string index)
    {
        return true;
    }
}

public class CompositeType : ISymbolType
{
    public Dictionary<string,ISymbolType> Fields {get;set;} = new();

    public bool IsAccessorValid(string accessor)
    {
        return Fields.ContainsKey(accessor);
    }

    public bool IsIndexingValid(string index)
    {
        return false;
    }
}

public class VectorType : ISymbolType
{
    public int Size{get;set;}
    public string TypeName {get;set;}
    static string[] accessors = new string[]{"x","y","z","w"};
    public bool IsAccessorValid(string accessor)
    {
        return accessor.All(accessor[0..Size].Contains);
    }

    public bool IsIndexingValid(string index)
    {
        return false;
    }
}
public class MatrixType : ISymbolType
{
    public int SizeX{get;set;}
    public int SizeY{get;set;}
    public string TypeName {get;set;}

    static readonly Grammar accessorGrammar = new(
      Terminals.Literal("_")
      .Then("m" & Terminals.Set("0123") & Terminals.Set("0123"))  
      .Or(Terminals.Set("1234") & Terminals.Set("1234"))
      .WithName("accessor")
    );

    public bool IsAccessorValid(string accessor)
    {
        return accessorGrammar.Match(accessor).Success;
    }

    public bool IsIndexingValid(string index)
    {
        return false;
    }
}
public class ScalarType : ISymbolType
{
    public string TypeName {get;set;}

    public bool IsAccessorValid(string accessor)
    {
        return false;
    }

    public bool IsIndexingValid(string index)
    {
        return false;
    }
}