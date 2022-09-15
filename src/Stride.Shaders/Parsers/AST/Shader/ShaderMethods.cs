using Eto.Parse;
using Stride.Shaders.Parsing.AST.Shader.Analysis;

namespace Stride.Shaders.Parsing.AST.Shader;



public class ShaderMethod : ShaderToken
{
    public bool IsStatic { get; set; }
    public bool IsOverride { get; set; }
    public bool IsStaged { get; set; }


    public string Name { get; set; }
    public ISymbolType ReturnType { get; set; }
    public IEnumerable<ShaderToken> ParameterList { get; set; }
    public IEnumerable<Statement> Statements { get; set; }

    public ShaderMethod(Match m, SymbolTable symbols)
    {
        Match = m;
        IsStatic = m["Static"].Success;
        IsOverride = m["Override"].Success;
        IsStaged = m["Stage"].Success;
        Name = m["MethodName"].StringValue;
        ReturnType = symbols.PushType(m["ReturnType"].StringValue, m["ReturnType"]);
        Statements = m["Statements"].Matches.Select(x => GetToken(x, symbols)).Cast<Statement>().ToList();
    }

    public static ShaderMethod Create(Match m, SymbolTable s)
    {
        return m["MethodName"].StringValue switch
        {
            "VSMain" => new VSMainMethod(m, s),
            "PSMain" => new PSMainMethod(m, s),
            "CSMain" => new CSMainMethod(m, s),
            "GSMain" => new GSMainMethod(m, s),
            "DSMain" => new DSMainMethod(m, s),
            "HSMain" => new HSMainMethod(m, s),
            _ => new ShaderMethod(m, s)
        };
    }
}

public abstract class MainMethod : ShaderMethod, IStreamCheck
{
    public MainMethod(Match m, SymbolTable s) : base(m, s) { }

    public bool CheckStream(SymbolTable s)
    {
        return Statements.OfType<IStreamCheck>().Any(x => x.CheckStream(s));
    }

    public IEnumerable<string> GetAssignedStream()
    {
        return Statements.OfType<IStreamCheck>().SelectMany(x => x.GetAssignedStream());
    }

    public IEnumerable<string> GetUsedStream()
    {
        return Statements.OfType<IStreamCheck>().SelectMany(x => x.GetUsedStream());
    }

    public void VariableChecking(SymbolTable sym)
    {
        if(CheckStream(sym))
            sym.PushStreamVar();
        foreach (var s in Statements)
            sym.Analyse(s);
    }

}


public class VSMainMethod : MainMethod
{
    public VSMainMethod(Match m, SymbolTable s) : base(m, s)
    {

    }
}
public class PSMainMethod : MainMethod
{
    public PSMainMethod(Match m, SymbolTable s) : base(m, s)
    {

    }
}
public class GSMainMethod : MainMethod
{
    public GSMainMethod(Match m, SymbolTable s) : base(m, s)
    {

    }
}
public class CSMainMethod : MainMethod
{
    public CSMainMethod(Match m, SymbolTable s) : base(m, s)
    {

    }
}
public class DSMainMethod : MainMethod
{
    public DSMainMethod(Match m, SymbolTable s) : base(m, s)
    {

    }
}
public class HSMainMethod : MainMethod
{
    public HSMainMethod(Match m, SymbolTable s) : base(m, s)
    {

    }
}