namespace SDSL.TAC;

public record struct Operand(string Value, OperandType Type)
{
    public readonly bool IsNone => Value == null && Type == OperandType.Undefined;
    public static Operand None => new(null!, OperandType.Undefined);
    public static implicit operator Operand(ValueTuple<string, OperandType> o) => new(o.Item1, o.Item2);
}