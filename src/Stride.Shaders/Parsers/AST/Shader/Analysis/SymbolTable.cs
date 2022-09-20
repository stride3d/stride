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
        a.TypeCheck(this,ScalarType.VoidType);
        CurrentScope.Add(a.VariableName, new SymbolVariable{Declaration = a, Name = a.VariableName, Type = a.TypeName});
    }
    public void PushStreamType(IEnumerable<ShaderVariableDeclaration> variables)
    {
        CurrentScope["STREAM"] = new CompositeType("STREAM", variables.ToDictionary(v => v.Name, v => v.Type));
    }
    public void PushStreamVar()
    {
        if(TryGetType("STREAM", out var type))
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
    public bool TryGetVarType(string variableName, out ISymbolType type)
    {
        foreach(var scope in this)
        {
            if(scope.TryGetValue(variableName, out var t))
            {
                type = ((SymbolVariable)t).Type;
                return true;
            }
        }
        type = ScalarType.VoidType;
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