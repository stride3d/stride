using Eto.Parse;

namespace Stride.Shaders.Parsing.AST.Shader;


public class ShaderMethod : ShaderToken
{
    public bool IsStatic { get; set; }
    public bool IsOverride { get; set; }
    public bool IsStaged { get; set; }


    public string Name { get; set; }
    public string ReturnType { get; set; }
    public IEnumerable<ShaderToken> ParameterList { get; set; }
    public IEnumerable<Statement> Statements { get; set; }

    public ShaderMethod(Match m)
    {
        Match = m;
        IsStatic = m["Static"].Success;
        IsOverride = m["Override"].Success;
        IsStaged = m["Stage"].Success;
        Name = m["MethodName"].StringValue;
        ReturnType = m["ReturnType"].StringValue;
        Statements = m["Statements"].Matches.Select(GetToken).Cast<Statement>().ToList();
    }

    public void Generate3Addr()
    {
        foreach (var s in Statements)
        {
            Lowering.LowerToken(s);
        }
    }

    public static ShaderMethod Create(Match m)
    {
        return m["MethodName"].StringValue switch
        {
            "VSMain" => new VSMainMethod(m),
            "PSMain" => new PSMainMethod(m),
            "CSMain" => new CSMainMethod(m),
            "GSMain" => new GSMainMethod(m),
            "DSMain" => new DSMainMethod(m),
            "HSMain" => new HSMainMethod(m),
            _ => new ShaderMethod(m)
        };
    }
}

public abstract class MainMethod : ShaderMethod
{
    public MainMethod(Match m) : base(m) { }

    public IEnumerable<string> GetStreamValuesAssigned()
    {
        return Statements.SelectMany(x => x.GetAssignedStream());
    }
    public IEnumerable<string> GetStreamValuesUsed()
    {
        return Statements.SelectMany(x => x.GetUsedStream());
    }

}


public class VSMainMethod : MainMethod
{
    public VSMainMethod(Match m) : base(m)
    {
        Generate3Addr();
    }
}
public class PSMainMethod : MainMethod
{
    public PSMainMethod(Match m) : base(m)
    {
        Generate3Addr();
    }
}
public class GSMainMethod : MainMethod
{
    public GSMainMethod(Match m) : base(m)
    {
        Generate3Addr();
    }
}
public class CSMainMethod : MainMethod
{
    public CSMainMethod(Match m) : base(m)
    {
        Generate3Addr();
    }
}
public class DSMainMethod : MainMethod
{
    public DSMainMethod(Match m) : base(m)
    {
        Generate3Addr();
    }
}
public class HSMainMethod : MainMethod
{
    public HSMainMethod(Match m) : base(m)
    {
        Generate3Addr();
    }
}