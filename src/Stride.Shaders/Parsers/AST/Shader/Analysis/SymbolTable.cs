namespace Stride.Shaders.Parsing.AST.Shader.Analysis;

public class Symbol
{
    public string? Name {get;set;}
    public string? Type {get;set;}
    public Declaration? Declaration {get;set;}
}

public partial class SymbolTable : Stack<Dictionary<string, Symbol>>
{
    public Dictionary<string,Symbol> CurrentScope => Peek();
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
        a.Value.TypeCheck(this, a.TypeName ?? "");
        CurrentScope.Add(a.VariableName, new Symbol{Declaration = a});
    }
    public void PushStream()
    {
        CurrentScope["streams"] = new Symbol{Name = "streams", Type = "STREAM"};
    }

    public void SetType(string variableName, string type)
    {
        foreach(var d in this)
            if(d.TryGetValue(variableName, out var v))
            {
                v.Type = type;
                if(v.Declaration is not null)
                    v.Type = type;
            }
    }
    public bool TryGetType(string variableName, out string? type)
    {
        type = null;
        foreach(var d in this)
            if(d.TryGetValue(variableName, out var v))
                type = v.Type;
            
        return false;
    }
    public void Analyse(Statement s)
    {
        if(s is Declaration d)
            PushVar(d);
        else if(s is BlockStatement b)
        {
            AddScope();
            foreach(var bs in b.Statements)
                Analyse(bs);
            Pop();
        }
        else CheckVar(s);
    }
}