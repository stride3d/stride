using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL.Analysis;



public class SymbolTable
{
    public Dictionary<string, SymbolType> DeclaredTypes { get; } = [];
    public Stack<Dictionary<string, Symbol>> Symbols { get; } = [];

    public void Process(ShaderClass sclass)
    {
        DeclaredTypes.Add(sclass.Name.Name, new MixinSymbol(sclass));
        foreach (var e in sclass.Elements)
        {
            if(e is ShaderMember member)
            {
                if (!DeclaredTypes.TryGetValue(member.Type.Name, out var mt))
                {
                    // mt = new SymbolType()
                }
            }
        }
    }
}