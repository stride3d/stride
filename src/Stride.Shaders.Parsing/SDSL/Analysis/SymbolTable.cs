using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing.SDSL.Analysis;



public partial class SymbolTable
{
    public Dictionary<string, SymbolType> DeclaredTypes { get; } = [];
    public Stack<Dictionary<string, Symbol>> Symbols { get; } = [];

    public void Process(ShaderClass sclass, Dictionary<string, Symbol>? globalSymbols = null)
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

    [GeneratedRegex(@"^((s?byte)|(u?(short|int|long))|(half|float|double))$")]
    private static partial Regex ScalarPattern();
    [GeneratedRegex(@"^((s?byte)|(u?(short|int|long))|(half|float|double))([2-4])$")]
    private static partial Regex VectorPattern();
    [GeneratedRegex(@"^((s?byte)|(u?(short|int|long))|(half|float|double))([2-4])x([2-4])$")]
    private static partial Regex MatrixPattern();
    [GeneratedRegex(@"^((s?byte)|(u?(short|int|long))|(half|float|double))[\s\n]*\[[\s\n]*([0-9]+)?[\s\n]*\]$")]
    private static partial Regex ArrayPattern();
    public SymbolType ParseType(string typename)
    {
        if (ScalarPattern().IsMatch(typename))
            return new Scalar(typename);
        else if (VectorPattern().IsMatch(typename))
        {
            var matches = VectorPattern().Match(typename);
            var size = int.Parse(matches.Groups[6].ValueSpan);
            var baseType = matches.Groups[1].Value;
            return new Vector(new Scalar(baseType), size);
        }
        else if (MatrixPattern().IsMatch(typename))
        {
            var matches = MatrixPattern().Match(typename);
            var width = int.Parse(matches.Groups[6].ValueSpan);
            var length = int.Parse(matches.Groups[7].ValueSpan);
            var baseType = matches.Groups[1].Value;
            return new Matrix(new Scalar(baseType), width, length);
        }
        else if (ArrayPattern().IsMatch(typename))
        {
            var matches = ArrayPattern().Match(typename);
            return new Array(ParseType(matches.Groups[1].Value), int.Parse(matches.Groups[6].ValueSpan));
        }
        else throw new NotImplementedException();
    }

    
}