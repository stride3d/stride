namespace Stride.Shaders.Parsing.AST.Shader.Analysis;

public interface ISymbol { }


public partial class SymbolTable : Stack<Dictionary<string, ISymbol>>
{
    static readonly SymbolTable empty = new();
    public static SymbolTable Empty { get => empty; }

    public ErrorList Errors { get; set; } = new();
    public Dictionary<string, ISymbol> CurrentScope => Peek();
    public Dictionary<string, ISymbol> GlobalScope => this.First();
    public SymbolTable()
    {
        Push(new());
    }

    public void AddScope() => Push(new());
    public void AddError(Eto.Parse.Match match, string title) => Errors.Add(new(match, title));


    public IEnumerable<ISymbolType> GetAllStructTypes() => this.SelectMany(x => x.Select(y => y.Value).OfType<CompositeType>());


    public void PushVar(Declaration a)
    {
        foreach (var d in this)
            if (d.ContainsKey(a.VariableName))
                throw new Exception("Variable already declared at " + a.Match);
        a.TypeCheck(this, ScalarType.VoidType);
        CurrentScope.Add(a.VariableName, new SymbolVariable { Declaration = a, Name = a.VariableName, Type = a.TypeName });
    }
    public void PushStreamType(IEnumerable<ShaderVariableDeclaration> variables)
    {
        CurrentScope["STREAM"] = new CompositeType("STREAM", new(variables.ToDictionary(v => v.Name, v => v.Type)), new(variables.ToDictionary(v => v.Name, v => v.Semantic)));
    }
    public void PushType(string name, CompositeType structDef)
    {
        CurrentScope[name] = structDef;
    }
    public void PushVar(string name, string type)
    {
        if (TryGetType(type, out var t))
            CurrentScope[name] = new SymbolVariable() { Name = name, Type = t };
    }
    public ISymbolType PushType(string name, Eto.Parse.Match type)
    {
        if (!GlobalScope.ContainsKey(name))
            GlobalScope[name] = Tokenize(type);
        return (ISymbolType)GlobalScope[name];
    }
    public ISymbolType PushScalarType(string name)
    {
        if (!GlobalScope.ContainsKey(name))
            GlobalScope[name] = TokenizeScalar(name);
        return (ISymbolType)GlobalScope[name];
    }

    public void SetType(string variableName, ISymbolType type)
    {
        foreach (var d in this)
            if (d.TryGetValue(variableName, out var v))
            {
                if (v is SymbolVariable sv)
                {
                    sv.Type = type;
                    if (sv.Declaration is not null)
                        sv.Type = type;
                }
            }
    }
    public bool TryGetType(string variableName, out ISymbolType type)
    {
        type = ScalarType.VoidType;
        foreach (var d in this)
        {
            if (d.TryGetValue(variableName, out var v) && v is ISymbolType sv)
            {
                type = sv;
                return true;
            }
        }
        return false;
    }
    public bool TryGetVarType(string variableName, out ISymbolType type)
    {
        foreach (var scope in this)
        {
            if (scope.TryGetValue(variableName, out var t))
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
        if (s is Declaration d)
            PushVar(d);
        else if (s is BlockStatement b)
        {
            AddScope();
            foreach (var bs in b.Statements)
                Analyse(bs);
            Pop();
        }
        else CheckVar(s);
    }
}