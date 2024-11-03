using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing;

public struct ErrorLocation
{
    public int Position { get; private set; }
    private int leftOffset;
    private int rightOffset;
    public int Line { get; private set;}
    public int Column { get; private set;}
    public ErrorLocation(Scanner scanner, int position)
    {
        // Getting the line and column at the position given.
        // TODO: Make this a function in scanner
        var pos = scanner.Position;
        scanner.Position = position;
        Line = scanner.Line;
        Column = scanner.Column;
        scanner.Position = pos;

        // Setting other attributes
        leftOffset = Math.Max(0, position - 5);
        rightOffset = Math.Min(scanner.Memory.Length, position);
        Position = position;
    }

    public static ErrorLocation Create<TScannable>(Scanner<TScannable> scanner, int position)
        where TScannable : IScannableCode
    {
        var error = new ErrorLocation();
        var pos = scanner.Position;
        scanner.Position = position;
        error.Line = scanner.Line;
        error.Column = scanner.Column;
        scanner.Position = pos;

        // Setting other attributes
        error.leftOffset = Math.Max(0, position - 5);
        error.rightOffset = Math.Min(scanner.Memory.Length, position);
        error.Position = position;
        return error;
    }

    public override readonly string ToString()
    {
        return $"[{Line}, {Column}]";
    }
}


