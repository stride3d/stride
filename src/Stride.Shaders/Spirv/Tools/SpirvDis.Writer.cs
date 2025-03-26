using System.Text;

namespace Stride.Shaders.Spirv.Tools;



public struct DisWriter()
{
    public bool WriteToConsole { get; set; }
    readonly StringBuilder builder = new();

    public readonly DisWriter Append(char value, int repeatCount, ConsoleColor? color = null)
    {
        builder.Append(value, repeatCount);
        if(WriteToConsole)
        {
            if(color is ConsoleColor c)
                Console.ForegroundColor = c;
            Console.Write(new string(value, repeatCount));
            Console.ResetColor();
        }
        return this;
    }
    public readonly DisWriter Append<T>(T value, ConsoleColor? color = null)
    {
        builder.Append(value);
        if(WriteToConsole)
        {
            if(color is ConsoleColor c)
                Console.ForegroundColor = c;
            Console.Write(value);
            Console.ResetColor();
        }
        return this;
    }
    public readonly DisWriter AppendLine()
    {
        builder.AppendLine();
        if(WriteToConsole)
            Console.WriteLine();
        return this;
    }
    public readonly DisWriter AppendLine(string machin, ConsoleColor? color = null)
    {
        builder.AppendLine(machin);
        if(WriteToConsole)
        {
            if(color is ConsoleColor c)
                Console.ForegroundColor = c;
            Console.WriteLine(machin);
            Console.ResetColor();
        }
        return this;
    }

    public readonly void Clear() => builder.Clear();
    public readonly override string ToString() => builder.ToString();
}