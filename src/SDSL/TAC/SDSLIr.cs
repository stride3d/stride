namespace SDSL.TAC;

public class Ir
{
    public List<Quadruple> Code { get; set; } = [];

    public void Something()
    {
        Code = [
            (
                Operator.Plus,
                ("3", OperandType.Int),
                Operand.None,
                ("a", OperandType.Variable)
            ),
            (
                Operator.Equals,
                ("a", OperandType.Variable),
                ("5", OperandType.Int),
                ("compare", OperandType.Variable)
            )
        ];
    }
}