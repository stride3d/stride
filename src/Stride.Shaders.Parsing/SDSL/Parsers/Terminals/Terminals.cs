
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Stride.Shaders.Parsing.SDSL;


public static class Terminals
{
    public static bool AnyChar<TScanner>(ref TScanner scanner)
        where TScanner : struct, IScanner
        => !scanner.IsEof;

    public static CharTerminalParser Char(char c) => new(c);
    public static bool Char<TScanner>(char c, ref TScanner scanner, bool advance = false)
        where TScanner : struct, IScanner
         => new CharTerminalParser(c).Match(ref scanner, advance);
    public static SetTerminalParser Set(string set) => new(set);
    public static bool Set<TScanner>(string set, ref TScanner scanner, bool advance = false)
        where TScanner : struct, IScanner
        => new SetTerminalParser(set).Match(ref scanner, advance);
    
    public static bool Set<TScanner>(string set, ref TScanner scanner, out char chosen, bool advance = false)
        where TScanner : struct, IScanner
    {
        chosen = '\0';
        foreach(var c in set)
            if(Char(c, ref scanner, advance: advance))
            {
                chosen = c;
                return true;
            }
        return false;
    }
    public static LiteralTerminalParser Literal(string literal) => new(literal);
    public static bool Literal<TScanner>(string c, ref TScanner scanner, bool advance = false)
        where TScanner : struct, IScanner
        => new LiteralTerminalParser(c).Match(ref scanner, advance);
    public static bool AnyOf<TScanner>(ReadOnlySpan<string> literals, ref TScanner scanner, out string matched, bool advance = false)
        where TScanner : struct, IScanner
    {
        matched = null!;
        foreach(var l in literals)
            if(new LiteralTerminalParser(l).Match(ref scanner, advance))
            {
                matched = l;
                return true;
            }
        return false;
    }
    public static DigitTerminalParser Digit(DigitRange? mode = null) => new(mode ?? DigitRange.All);
    public static bool Digit<TScanner>(ref TScanner scanner, DigitRange? mode = null, bool advance = false)
        where TScanner : struct, IScanner
        => new DigitTerminalParser(mode ?? DigitRange.All).Match(ref scanner, advance);
    public static LetterTerminalParser Letter() => new();
    public static bool Letter<TScanner>(ref TScanner scanner, bool advance = false)
        where TScanner : struct, IScanner
        => new LetterTerminalParser().Match(ref scanner, advance);
    public static LetterOrDigitTerminalParser LetterOrDigit() => new();
    public static bool LetterOrDigit<TScanner>(ref TScanner scanner, bool advance = false)
        where TScanner : struct, IScanner
        => new LetterOrDigitTerminalParser().Match(ref scanner, advance);
    public static bool IdentifierFirstChar<TScanner>(ref TScanner scanner, bool advance = false)
        where TScanner : struct, IScanner
        => Letter(ref scanner, advance) || Char('_', ref scanner, advance);
    public static bool EOL<TScanner>(ref TScanner scanner, bool advance = false)
        where TScanner : struct, IScanner
        => new EOLTerminalParser().Match(ref scanner, advance);
    public static bool EOF<TScanner>(ref TScanner scanner)
        where TScanner : struct, IScanner
        => new EOFTerminalParser().Match(ref scanner, false);


    
    public static bool FloatSuffix<TScanner>(ref TScanner scanner, out Suffix? suffix, bool advance = false)
        where TScanner : struct, IScanner
    {
        suffix = null;
        if (AnyOf(["f16", "h", "f32", "f", "f64", "d"], ref scanner, out var matched, advance: advance))
        {
            suffix = matched switch
            {
                "f16" or "h" => new(16, true, true),
                "f32" or "f" => new(32, true, true),
                "f64" or "d" => new(64, true, true),
                _ => throw new NotImplementedException()
            };
            return true;
        }
        else return false;
    }
    public static bool IntSuffix<TScanner>(ref TScanner scanner, out Suffix? suffix, bool advance = false)
        where TScanner : struct, IScanner
    {
        suffix = null;
        if (AnyOf(["u32", "u", "U", "i64", "l", "L", "u64", "ul", "UL"], ref scanner, out var matched, advance: advance))
        {
            suffix = matched switch
            {
                "u32" or "u" or "U" => new(32, false, false),
                "i64" or "l" or "L" => new(64, false, true),
                "u64" or "ul" or "UL" => new(64, false, false),
                _ => throw new NotImplementedException()
            };
            return true;
        }
        else return false;
    }
}

public interface ITerminal
{
    public bool Match<TScanner>(ref TScanner scanner, bool advance)
        where TScanner : struct, IScanner;
}

public record struct CharTerminalParser(char Character) : ITerminal
{
    public readonly bool Match<TScanner>(ref TScanner scanner, bool advance)
        where TScanner : struct, IScanner
    {
        if (scanner.Peek() == Character)
        {
            if(advance)
                scanner.Advance(1);
            return true;
        }
        return false;
    }
    public static implicit operator CharTerminalParser(char c) => new(c);
}

public struct DigitRange
{
    static string allChars = "0123456789";
    public static DigitRange All { get; } = new(0..9);
    public static DigitRange ExceptZero { get; } = new(1..9);
    public static DigitRange OnlyZero { get; } = new(0);
    public string Chars { get; set; }
    public DigitRange(Range range)
    {
        var (o, l) = range.GetOffsetAndLength(allChars.Length);
        Chars = allChars[o..Math.Min(allChars.Length,o+l+1)];
    }
    public DigitRange(int digit)
    {
        Chars = $"{(char)(digit + '0')}";
    }
    public DigitRange(string chars)
    {
        foreach(var e in chars)
            if(!char.IsDigit(e))
                throw new ArgumentException($"Cannot use {chars} as a list of digit");
        Chars = chars;
    }

    public static implicit operator DigitRange(Range range) => new(range);
    public static implicit operator DigitRange(int number) => new(number);
    public static implicit operator DigitRange(string numbers) => new(numbers);
}

public record struct DigitTerminalParser(DigitRange Mode) : ITerminal
{
    public readonly bool Match<TScanner>(ref TScanner scanner, bool advance)
        where TScanner : struct, IScanner
    {
        bool found = false;
        if (Mode.Chars.Contains((char)scanner.Peek()))
            found = true;
        if (advance && found)
            scanner.Advance(1);
        return found;
    }
}

public record struct LetterTerminalParser() : ITerminal
{
    public readonly bool Match<TScanner>(ref TScanner scanner, bool advance)
        where TScanner : struct, IScanner
    {
        if (scanner.Peek() > 0 && char.IsLetter((char)scanner.Peek()))
        {
            if (advance)
                scanner.Advance(1);
            return true;
        }
        return false;
    }
}
public record struct LetterOrDigitTerminalParser() : ITerminal
{
    public readonly bool Match<TScanner>(ref TScanner scanner, bool advance)
        where TScanner : struct, IScanner
    {
        if (scanner.Peek() > 0 && char.IsLetterOrDigit((char)scanner.Peek()))
        {
            if (advance)
                scanner.Advance(1);
            return true;
        }
        return false;
    }
}

public record struct LiteralTerminalParser(string Literal, bool CaseSensitive = true) : ITerminal
{
    public readonly bool Match<TScanner>(ref TScanner scanner, bool advance)
        where TScanner : struct, IScanner
    {
        if (scanner.ReadString(Literal, CaseSensitive))
        {
            if (advance)
                scanner.Advance(Literal.Length);
            return true;
        }
        return false;
    }
    public static implicit operator LiteralTerminalParser(string lit) => new(lit);
}


public record struct SetTerminalParser(string Set) : ITerminal
{
    public readonly bool Match<TScanner>(ref TScanner scanner, bool advance)
        where TScanner : struct, IScanner
    {
        if (scanner.Peek() > 0 && Set.Contains((char)scanner.Peek()))
        {
            if (advance)
                scanner.Advance(1);
            return true;
        }
        return false;
    }

    public static implicit operator SetTerminalParser(string set) => new(set);
}

public record struct EOFTerminalParser() : ITerminal
{
    public readonly bool Match<TScanner>(ref TScanner scanner, bool advance)
        where TScanner : struct, IScanner
    {
        return scanner.IsEof;
    }
}
public record struct EOLTerminalParser() : ITerminal
{
    public readonly bool Match<TScanner>(ref TScanner scanner, bool advance)
        where TScanner : struct, IScanner
    {
        var position = scanner.Position;
        while (scanner.Peek() == ' ')
            scanner.Advance(1);
        var result = Terminals.Char('\n', ref scanner, advance) || Terminals.Literal("\r\n", ref scanner, advance);
        if (!advance && result)
            scanner.Position = position;
        return result;
    }
}

