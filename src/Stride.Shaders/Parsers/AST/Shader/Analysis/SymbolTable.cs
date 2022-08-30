namespace Stride.Shaders.Parsing.AST.Shader.Analysis;

public class Symbol
{
    public string? Name {get;set;}
    public string? Type {get;set;}
    public DeclareAssign? Declaration {get;set;}
}

public class SymbolTable : Stack<Dictionary<string, Declaration>>
{
    public Dictionary<string,Declaration> CurrentScope => Peek();
    public SymbolTable()
    {
        Push(new());
    }

    public void AddScope() => Push(new());

    public void PushVar(Declaration a)
    {
        foreach(var d in this)
            if(d.ContainsKey(a.VariableName))
                throw new Exception("Variable already declared at " + a.Match);
        a.Value.TypeCheck(this);
        CurrentScope.Add(a.VariableName, a);
    }

    public void SetType(string variableName, string type)
    {
        foreach(var d in this)
            if(d.TryGetValue(variableName, out var v))
                v.TypeName = type;
    }
    public bool TryGetType(string variableName, out string? type)
    {
        type = null;
        foreach(var d in this)
            if(d.TryGetValue(variableName, out var v))
            {
                type = v.TypeName;
                return true;
            }
        return false;
    }
}