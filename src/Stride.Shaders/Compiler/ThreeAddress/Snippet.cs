namespace Stride.Shaders.ThreeAddress;
public class Snippet
{
    Dictionary<string, int> LookUp {get;set;} = new();
    List<Register> IntermediateCode {get;set;} = new();
    
    
    public void Add(Register r)
    {
        IntermediateCode.Add(r);
        if(r.Name is null) r.Name = $"Stride.T{IntermediateCode.Count}";
        LookUp[r.Name] = IntermediateCode.Count;
    }

}
