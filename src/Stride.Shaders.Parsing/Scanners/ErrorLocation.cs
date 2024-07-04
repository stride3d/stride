using Stride.Shaders.Parsing.SDSL.AST;

namespace Stride.Shaders.Parsing;

public struct ErrorLocation
{
    public ReadOnlyMemory<char> Text { get; private set; }
    public int Position { get; private set; }
    private int leftOffset;
    private int rightOffset;
    private int line;
    private int column;
    public ErrorLocation(Scanner scanner, int position)
    {
        // Getting the line and column at the position given.
        // TODO: Make this a function in scanner
        var pos = scanner.Position;
        scanner.Position = position;
        line = scanner.Line;
        column = scanner.Column;
        scanner.Position = pos;

        // Setting other attributes
        leftOffset = position - 5 > 0 ? 5 : position;
        rightOffset = position + 5 < scanner.Span.Length ? 5 : scanner.Span.Length - position - 1;
        Position = position;
        
        Text = scanner.Memory[(position - leftOffset)..(position + rightOffset)];
    }
    
    public static ErrorLocation Create<TScannable>(Scanner<TScannable> scanner, int position)
        where TScannable : IScannableCode
    {
        var error = new ErrorLocation();
        var pos = scanner.Position;
        scanner.Position = position;
        error.line = scanner.Line;
        error.column = scanner.Column;
        scanner.Position = pos;

        // Setting other attributes
        error.leftOffset = position - 5 > 0 ? 5 : position;
        error.rightOffset = position + 5 < scanner.Span.Length ? 5 : scanner.Span.Length - position - 1;
        error.Position = position;
        
        error.Text = scanner.Memory[(position - error.leftOffset)..(position + error.rightOffset)];
        return error;
    }

    public readonly override string ToString()
    {
        return $"l{line}-c{column} : \n{Text[..5]}>>>{Text[5..]}";
    }
}


