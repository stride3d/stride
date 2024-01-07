using SDSL.Symbols;

namespace SDSL.TAC;

public record struct Operand(string Value, Kind Kind, SymbolType? Type = null)
{
    public readonly bool IsNone => Value == null && Kind == Kind.Undefined;
    public static Operand None => new(null!, Kind.Undefined);
    public static implicit operator Operand(in ValueTuple<string, Kind> o) => new(o.Item1, o.Item2);
}