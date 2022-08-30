namespace Stride.Shaders.Parsing.AST.Shader.Analysis;

public class SymbolTable
{
    Stack<Dictionary<string, VariableNameLiteral>> Symbols;

    public SymbolTable()
    {
        Symbols = new();
        Symbols.Push(new());
    }
    public SymbolTable(SymbolTable previous)
    {
        Symbols = new();
        foreach(var s in previous.Symbols)
            Symbols.Push(s);
        Symbols.Push(new());
    }

    public void AddVariable(VariableNameLiteral v)
    {
        foreach(var d in Symbols)
        {
            if(d.ContainsKey(v.Name))
                throw new Exception("Variable already declared at " + v.Match);
        }
        Symbols.Peek().Add(v.Name, v);
    }

    public void SetType(string variableName, string type)
    {
        foreach(var d in Symbols)
        {
            if(d.TryGetValue(variableName, out var v))
                v.InferredType = type;
        }
    }
}