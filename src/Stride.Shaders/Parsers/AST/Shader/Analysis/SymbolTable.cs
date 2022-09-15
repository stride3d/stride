namespace Stride.Shaders.Parsing.AST.Shader.Analysis;

public interface ISymbol{}


public partial class SymbolTable : Stack<Dictionary<string, ISymbol>>
{
    public Dictionary<string,ISymbol> CurrentScope => Peek();
    public Dictionary<string,ISymbol> GlobalScope => this.First();
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
        a.Value.TypeCheck(this, a.TypeName);
        CurrentScope.Add(a.VariableName, new SymbolVariable{Declaration = a});
    }
    public void PushStreamType(IEnumerable<ShaderVariableDeclaration> variables)
    {
        CurrentScope["STREAM"] = new CompositeType("STREAM", variables.ToDictionary(v => v.Name, v => v.Type));
    }
    public void PushStreamVar()
    {
        if(TryGetType("streams", out var type))
            CurrentScope["streams"] = new SymbolVariable(){Name = "streams", Type = type};
    }
    public ISymbolType PushType(string name, Eto.Parse.Match type)
    {
        if(!GlobalScope.ContainsKey(name))
            GlobalScope[name] = Tokenize(type);
        return (ISymbolType)GlobalScope[name];
    }
    public ISymbolType PushScalarType(string name)
    {
        if(!GlobalScope.ContainsKey(name))
            GlobalScope[name] = TokenizeScalar(name);
        return (ISymbolType)GlobalScope[name];
    }

    public void SetType(string variableName, ISymbolType type)
    {
        foreach(var d in this)
            if(d.TryGetValue(variableName, out var v))
            {
                if(v is SymbolVariable sv)
                {
                    sv.Type = type;
                    if(sv.Declaration is not null)
                        sv.Type = type;
                }
            }
    }
    public bool TryGetType(string variableName, out ISymbolType type)
    {
        type = ScalarType.VoidType;
        foreach(var d in this)
        {
            if(d.TryGetValue(variableName, out var v) && v is ISymbolType sv)
            {
                type = sv;
                return true;
            }
        }
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