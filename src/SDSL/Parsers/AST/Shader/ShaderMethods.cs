using Eto.Parse;
using SDSL.Parsing.AST.Shader.Analysis;
using SDSL.ThreeAddress;

namespace SDSL.Parsing.AST.Shader;



public class ShaderMethod : ShaderToken
{
    public bool IsStatic { get; set; }
    public bool IsOverride { get; set; }
    public bool IsStaged { get; set; }


    public string Name { get; set; }
    public SymbolType ReturnType { get; set; }
    public List<ShaderToken>? ParameterList { get; set; }
    public List<Statement>? Statements { get; set; }

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
    protected string prefix;
    // public TAC IL { get; set; }

    public MainMethod(Match m, SymbolTable s) : base(m, s)
    {
        prefix = "NONE";
        // IL = new(s);
    }

    public bool CheckStream(SymbolTable s)
    {
        if (Statements == null) return true;
        return Statements.OfType<IStreamCheck>().Any(x => x.CheckStream(s));
    }

    public IEnumerable<string>? GetAssignedStream()
    {
        return Statements?.OfType<IStreamCheck>().SelectMany(x => x.GetAssignedStream() ?? Enumerable.Empty<string>());
    }

    public IEnumerable<string>? GetUsedStream()
    {
        return Statements?.OfType<IStreamCheck>().SelectMany(x => x.GetUsedStream() ?? Enumerable.Empty<string>());
    }

    public void VariableChecking(SymbolTable sym)
    {
        // if(CheckStream(sym))
        //     sym.PushVar("streams","STREAM");
        // foreach (var s in Statements)
        //     sym.Analyse(s);
        throw new NotImplementedException();
    }
    public void CreateInOutStream(SymbolTable sym)
    {
        // if(sym.TryGetType("STREAM", out var t))
        // {
        //     var used = GetUsedStream();
        //     var assigned = GetAssignedStream();
        //     var i = ((CompositeType)t).SubType(prefix + "_STREAM_IN",used);
        //     var o = ((CompositeType)t).SubType(prefix + "_STREAM_OUT",assigned);
        //     sym.PushType(i.Name, i);
        //     sym.PushType(o.Name, o);
        // }
        throw new NotImplementedException();
    }

    internal void GenerateIl(SymbolTable symbols)
    {
        CreateInOutStream(symbols);
        throw new NotImplementedException();
        // symbols.PushVar("streams", "STREAM");
        // symbols.PushVar("streams_in", prefix + "_STREAM_IN");
        // symbols.PushVar("streams_out", prefix + "_STREAM_OUT");
        // IL.Construct(Statements);
    }
}


public class VSMainMethod : MainMethod
{
    public VSMainMethod(Match m, SymbolTable s) : base(m, s)
    {
        prefix = "VS";
    }
}
public class PSMainMethod : MainMethod
{
    public PSMainMethod(Match m, SymbolTable s) : base(m, s)
    {
        prefix = "PS";
    }
}
public class GSMainMethod : MainMethod
{
    public GSMainMethod(Match m, SymbolTable s) : base(m, s)
    {
        prefix = "GS";
    }
}
public class CSMainMethod : MainMethod
{
    public CSMainMethod(Match m, SymbolTable s) : base(m, s)
    {
        prefix = "CS";
    }
}
public class DSMainMethod : MainMethod
{
    public DSMainMethod(Match m, SymbolTable s) : base(m, s)
    {
        prefix = "DS";
    }
}
public class HSMainMethod : MainMethod
{
    public HSMainMethod(Match m, SymbolTable s) : base(m, s)
    {
        prefix = "HS";
    }
}