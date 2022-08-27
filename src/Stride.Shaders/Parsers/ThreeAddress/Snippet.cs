using Stride.Shaders.Parsing.AST.Shader;

namespace Stride.Shaders.ThreeAddress;
public partial class Snippet
{
    Dictionary<string, int> LookUp {get;set;} = new();
    List<Register> IntermediateCode {get;set;} = new();
    
    
    public void Add(Register r)
    {
        IntermediateCode.Add(r);
        if(r.Name is null) r.Name = $"Stride.T{IntermediateCode.Count}";
        LookUp[r.Name] = IntermediateCode.Count;
    }
    public void Construct(params Statement[] statements)
    {
        Construct(statements.AsEnumerable());
    }
    public void Construct(IEnumerable<Statement> statements)
    {
        foreach(var s in statements)
        {
            LowerToken(s);
        }
    }
}
