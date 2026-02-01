namespace Stride.Shaders.Generators.Intrinsics;

using System;

internal record struct TextLocation(string Code, Range Range);

internal ref struct Scanner(string code)
{
    internal readonly string Code { get; } = code;
    internal int Position { get; private set; } = 0;
    internal readonly bool EOF => Position >= Code.Length;

    internal readonly char Peek => Position < Code.Length ? Code[Position] : '\0';

    internal Stack<TextLocation?> Errors { get; } = [];

    internal void Advance(int size = 1) => Position += size;
    internal bool Backtrack(int position)
    {
        Position = position;
        return false;
    }
    internal bool Backtrack<T>(int position, out T value)
    {
        Position = position;
        value = default!;
        return false;
    }

    internal readonly bool Success()
    {
        return true;
    }

    internal bool Match(ReadOnlySpan<char> expected, bool advance = false)
    {
        if (Code.AsSpan(Position).StartsWith(expected, StringComparison.Ordinal))
        {
            if (advance)
                Advance(expected.Length);
            return true;
        }
        return false;
    }
    internal bool MatchLetter(bool advance = false)
    {
        if (Position < Code.Length && char.IsLetter(Peek))
        {
            if (advance)
                Advance(1);
            return true;
        }
        return false;
    }
    internal bool MatchDigit(bool advance = false) => MatchDigit(0..9, advance);
    internal bool MatchDigit(Range range, bool advance = false)
    {
        if (Position < Code.Length && char.IsDigit(Peek))
        {
            if (advance)
                Advance(1);
            return true;
        }
        return false;
    }
    internal bool MatchLetterOrDigit(bool advance = false)
    {
        if (MatchLetter(advance))
            return true;
        else if (MatchDigit(advance))
            return true;
        else return false;
    }

    internal bool MatchWhiteSpace(int minimum = 0, bool advance = false)
    {
        var start = Position;
        while (Position < Code.Length && char.IsWhiteSpace(Peek))
            Position++;
        if (Position - start >= minimum)
        {
            if (!advance)
                Backtrack(start);
            return true;
        }
        else return Backtrack(start);
    }

    internal bool FollowedBy(ReadOnlySpan<char> expected, bool advance = false)
    {
        var start = Position;
        if (MatchWhiteSpace(advance: true) && Match(expected, advance: false))
        {
            if (advance)
                Advance(expected.Length);
            else
                Position = start;
            return true;
        }
        return Backtrack(start);
    }

    internal bool AnyUntil(ReadOnlySpan<char> expected, bool advance = false)
    {
        var start = Position;
        while (Position < Code.Length && !Match(expected, advance: false))
            Advance(1);
        if (Position < Code.Length)
        {
            if (advance)
                Advance(expected.Length);
            return true;
        }
        return Backtrack(start);
    }
    internal bool AnyOf(out string matched, bool advance = false, params ReadOnlySpan<string> expected)
    {
        var start = Position;
        foreach (var exp in expected)
        {
            if (Match(exp, advance))
            {
                matched = exp;
                return true;
            }
        }
        return Backtrack(start, out matched);
    }
}



internal static class ParsersExtensions
{
    internal static bool Identifier(this ref Scanner scanner, out Identifier identifier)
    {
        var position = scanner.Position;
        if (scanner.MatchLetter() || scanner.Match("_"))
        {
            scanner.Advance();
            while (scanner.MatchLetterOrDigit() || scanner.Match("_"))
                scanner.Advance();

            var name = scanner.Code[position..scanner.Position];
            identifier = new Identifier(name.ToString(), new TextLocation(scanner.Code, position..scanner.Position));
            return scanner.Success();
        }
        return scanner.Backtrack(position, out identifier);
    }

    internal static bool ArgumentQualifier(this ref Scanner scanner, out ArgumentQualifier qualifier)
    {
        var position = scanner.Position;
        if (scanner.AnyOf(out var matched, advance: true, "in", "out", "inout"))
        {
            var tmp = scanner.Position;
            scanner.MatchWhiteSpace();
            if (scanner.AnyOf(out var optionalQualifier, advance: true, "row_major", "col_major"))
            {
                qualifier = new ArgumentQualifier(matched, new TextLocation(scanner.Code, position..scanner.Position), optionalQualifier);
                return scanner.Success();
            }
            else scanner.Backtrack(tmp);
            qualifier = new ArgumentQualifier(matched, new TextLocation(scanner.Code, position..scanner.Position));
            return scanner.Success();
        }
        return scanner.Backtrack(position, out qualifier);
    }

    internal static bool TypeMatching(this ref Scanner scanner, out Matching typeInfo)
    {
        var position = scanner.Position;
        if (scanner.Match("$classT", true))
        {
            typeInfo = new ClassTMatch(new TextLocation(scanner.Code, position..scanner.Position));
            return scanner.Success();
        }
        else if (scanner.Match("$funcT", true))
        {
            typeInfo = new FuncMatch(new TextLocation(scanner.Code, position..scanner.Position));
            return scanner.Success();
        }
        else if (scanner.Match("$funcT2", true))
        {
            typeInfo = new Func2Match(new TextLocation(scanner.Code, position..scanner.Position));
            return scanner.Success();
        }
        else if (scanner.Match("$type", true))
        {
            var tmpPos = scanner.Position;
            while (scanner.MatchDigit(true)) ;
            int component = int.Parse(scanner.Code[tmpPos..scanner.Position]);
            typeInfo = new TypeMatch(component, new TextLocation(scanner.Code, position..scanner.Position));
            return scanner.Success();
        }
        else if (scanner.Match("$match<", true))
        {
            var tmpPos1 = scanner.Position;
            while (scanner.MatchDigit(true)) ;
            int componentA = int.Parse(scanner.Code[tmpPos1..scanner.Position]);
            scanner.MatchWhiteSpace();
            if (scanner.Match(",", true))
            {
                scanner.MatchWhiteSpace();
                var tmpPos2 = scanner.Position;
                while (scanner.MatchDigit(true)) ;
                int componentB = int.Parse(scanner.Code[tmpPos2..scanner.Position]);
                scanner.MatchWhiteSpace();
                if (scanner.Match(">", true))
                {
                    typeInfo = new Matching(componentA, componentB, new TextLocation(scanner.Code, position..scanner.Position));
                    return scanner.Success();
                }
                else return scanner.Backtrack(position, out typeInfo);
            }
            else return scanner.Backtrack(position, out typeInfo);
        }
        else return scanner.Backtrack(position, out typeInfo);
    }

    internal static bool TypeInfo(this ref Scanner scanner, out TypeInfo typeInfo)
    {
        var position = scanner.Position;
        if (scanner.TypeMatching(out var typematch))
        {
            if(typematch is ClassTMatch or FuncMatch or Func2Match)
            {
                typeInfo = new TypeInfo(new Typename("void", null, new TextLocation(scanner.Code, position..scanner.Position)), new TextLocation(scanner.Code, position..scanner.Position), typematch);
                return scanner.Success();
            }
            else if(typematch is TypeMatch tm)
            {
                typeInfo = new TypeInfo(new Typename("$to_resolve", null, new TextLocation(scanner.Code, position..scanner.Position)), new TextLocation(scanner.Code, position..scanner.Position), typematch);
                return scanner.Success();
            }
        }
        return scanner.Backtrack(position, out typeInfo);
    }

    internal static bool LayoutSize(this ref Scanner scanner, out Layout layout)
    {
        var position = scanner.Position;
        if (scanner.Match("<", true))
        {
            scanner.MatchWhiteSpace();
            var size1Pos = scanner.Position;
            while (scanner.MatchLetterOrDigit(true)) ;
            var size1 = scanner.Code[size1Pos..scanner.Position].ToString();
            scanner.MatchWhiteSpace();
            if (scanner.Match(",", true))
            {
                scanner.MatchWhiteSpace();
                var size2Pos = scanner.Position;
                while (scanner.MatchLetterOrDigit(true)) ;
                var size2 = scanner.Code[size2Pos..scanner.Position].ToString();
                scanner.MatchWhiteSpace();
            }
            if (scanner.Match(">", true))
            {
                layout = new Layout(size1, null, new TextLocation(scanner.Code, position..scanner.Position));
                return true;
            }
            else return scanner.Backtrack(position, out layout);
        }
        return scanner.Backtrack(position, out layout);
    }
    internal static bool Typename(this ref Scanner scanner, out Typename typename)
    {
        var position = scanner.Position;
        if (scanner.Identifier(out var id))
        {
            scanner.MatchWhiteSpace();
            if (scanner.LayoutSize(out var layout))
            {
                typename = new Typename(id.Name, layout, new TextLocation(scanner.Code, position..scanner.Position));
                return true;
            }
            else
            {
                typename = new Typename(id.Name, null, new TextLocation(scanner.Code, position..scanner.Position));
                return true;
            }
        }
        typename = null!;
        return false;
    }

    internal static bool IntrinsicOp(this ref Scanner scanner, out IntrinsicOp intrinsicOp)
    {
        var position = scanner.Position;
        if (scanner.Match(":", true))
        {
            scanner.MatchWhiteSpace();
            if (scanner.Identifier(out var id) && scanner.FollowedBy(";", advance: true))
            {
                intrinsicOp = new IntrinsicOp(id.Name, new TextLocation(scanner.Code, position..scanner.Position));
                return true;
            }
            else
                return scanner.Backtrack(position, out intrinsicOp);
        }
        return scanner.Backtrack(position, out intrinsicOp);
    }

    internal static bool Attribute(this ref Scanner scanner, out Attributes attributes)
    {
        var position = scanner.Position;
        if (scanner.Match("[[", true))
        {
            var attributeStart = scanner.Position;
            scanner.MatchWhiteSpace();
            if (scanner.AnyUntil("]]", true))
            {
                attributes = new(scanner.Code[attributeStart..(scanner.Position - 2)].ToString().Split(','), new(scanner.Code, position..scanner.Position));
            }
            else if (scanner.Match("]]", true))
            {
                attributes = new(Array.Empty<string>(), new(scanner.Code, position..scanner.Position));
                return true;
            }
            else return scanner.Backtrack(position, out attributes);

        }
        return scanner.Backtrack(position, out attributes);
    }
}

static class Machin
{
    static void Main()
    {
        var scanner = new Scanner("hello world");


    }
}