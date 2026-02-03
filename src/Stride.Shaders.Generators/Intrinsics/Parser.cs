namespace Stride.Shaders.Generators.Intrinsics;

using System;
using System.Text.Json;

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
        if (scanner.AnyOf(out var matched, advance: true, "inout", "in", "out", "ref"))
        {
            var tmp = scanner.Position;
            if (!scanner.MatchWhiteSpace(1, true))
                return scanner.Backtrack(position, out qualifier);
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
        else if (scanner.Match("$funcT2", true))
        {
            typeInfo = new Func2Match(new TextLocation(scanner.Code, position..scanner.Position));
            return scanner.Success();
        }
        else if (scanner.Match("$funcT", true))
        {
            typeInfo = new FuncMatch(new TextLocation(scanner.Code, position..scanner.Position));
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
            scanner.Match("-", true);
            while (scanner.MatchDigit(true)) ;
            int componentA = int.Parse(scanner.Code[tmpPos1..scanner.Position]);
            scanner.MatchWhiteSpace(advance: true);
            if (scanner.Match(",", true))
            {
                scanner.MatchWhiteSpace(advance: true);
                var tmpPos2 = scanner.Position;
                scanner.Match("-", true);
                while (scanner.MatchDigit(true)) ;
                int componentB = int.Parse(scanner.Code[tmpPos2..scanner.Position]);
                scanner.MatchWhiteSpace(advance: true);
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
            if (typematch is ClassTMatch or FuncMatch or Func2Match)
            {
                typeInfo = new TypeInfo(new Typename("void", null, new TextLocation(scanner.Code, position..scanner.Position)), new TextLocation(scanner.Code, position..scanner.Position), typematch);
                return scanner.Success();
            }
            else if (typematch is TypeMatch tm)
            {
                typeInfo = new TypeInfo(new Typename("$to_resolve", null, new TextLocation(scanner.Code, position..scanner.Position)), new TextLocation(scanner.Code, position..scanner.Position), typematch);
                return scanner.Success();
            }
            else if (typematch is Matching m)
            {
                scanner.MatchWhiteSpace(advance: true);
                if (scanner.Typename(out var typename))
                {
                    typeInfo = new TypeInfo(typename, new TextLocation(scanner.Code, position..scanner.Position), typematch);
                    return scanner.Success();
                }
                else return scanner.Backtrack(position, out typeInfo);
            }
        }
        else if (scanner.Typename(out var typename))
        {
            typeInfo = new TypeInfo(typename, new TextLocation(scanner.Code, position..scanner.Position), null);
            return scanner.Success();
        }
        return scanner.Backtrack(position, out typeInfo);
    }

    internal static bool LayoutSize(this ref Scanner scanner, out Layout layout)
    {
        var position = scanner.Position;
        if (scanner.Match("<", true))
        {
            scanner.MatchWhiteSpace(advance: true);
            if(scanner.Match(">", true))
            {
                layout = new Layout("any", "any", new TextLocation(scanner.Code, position..scanner.Position));
                return true;
            }
            var size1Pos = scanner.Position;
            while (scanner.MatchLetterOrDigit(true)) ;
            var size1 = scanner.Code[size1Pos..scanner.Position].ToString();
            string? size2 = null;
            scanner.MatchWhiteSpace(advance: true);
            if (scanner.Match(",", true))
            {
                scanner.MatchWhiteSpace(advance: true);
                var size2Pos = scanner.Position;
                while (scanner.MatchLetterOrDigit(true)) ;
                size2 = scanner.Code[size2Pos..scanner.Position].ToString();
                scanner.MatchWhiteSpace(advance: true);
            }
            if (scanner.Match(">", true))
            {
                layout = new Layout(size1, size2, new TextLocation(scanner.Code, position..scanner.Position));
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
            var tmpPos = scanner.Position;
            scanner.MatchWhiteSpace(advance: true);
            if (scanner.LayoutSize(out var layout))
            {
                typename = new Typename(id.Name, layout, new TextLocation(scanner.Code, position..scanner.Position));
                return true;
            }
            else
            {
                scanner.Backtrack(tmpPos);
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
            scanner.MatchWhiteSpace(advance: true);
            if (scanner.Identifier(out var id))
            {
                intrinsicOp = new IntrinsicOp(id.Name, new TextLocation(scanner.Code, position..scanner.Position));
                return true;
            }
            else
                return scanner.Backtrack(position, out intrinsicOp);
        }
        return scanner.Backtrack(position, out intrinsicOp);
    }

    internal static bool Attributes(this ref Scanner scanner, out Attributes attributes)
    {
        var position = scanner.Position;
        if (scanner.Match("[[", true))
        {
            var attributeStart = scanner.Position;
            scanner.MatchWhiteSpace(advance: true);
            if (scanner.AnyUntil("]]"))
            {
                if (scanner.EOF || !scanner.Match("]]", true))
                    return scanner.Backtrack(position, out attributes);
                attributes = new(scanner.Code[attributeStart..(scanner.Position - 2)].ToString().Split(','), new(scanner.Code, position..scanner.Position));
                return scanner.Success();
            }
        }
        return scanner.Backtrack(position, out attributes);
    }

    internal static bool IntrinsicParameter(this ref Scanner scanner, out IntrinsicParameter parameter)
    {
        var position = scanner.Position;
        parameter = new(null, null!, null!, new());
        if(scanner.Match("...", true))
        {
            parameter = parameter with { Name = new Identifier("...", new TextLocation(scanner.Code, position..scanner.Position)), Location = new TextLocation(scanner.Code, position..scanner.Position) };
            return scanner.Success();
        }
        if (scanner.ArgumentQualifier(out var qualifier))
        {
            parameter = parameter with { Qualifier = qualifier };
            if (!scanner.MatchWhiteSpace(1, advance: true))
                return scanner.Backtrack(position, out parameter);
        }
        if (scanner.TypeInfo(out var typeInfo))
        {
            parameter = parameter with { TypeInfo = typeInfo };
            if (!scanner.MatchWhiteSpace(1, advance: true))
                return scanner.Backtrack(position, out parameter);
            if (scanner.Identifier(out var name))
            {
                parameter = parameter with { Name = name, Location = new TextLocation(scanner.Code, position..scanner.Position) };
                return scanner.Success();
            }
            else return scanner.Backtrack(position, out parameter);
        }
        else return scanner.Backtrack(position, out parameter);
    }

    internal static bool IntrinsicDeclaration(this ref Scanner scanner, out IntrinsicDeclaration intrinsic)
    {
        var position = scanner.Position;
        intrinsic = new(null!, null!, [], new());
        if (scanner.TypeInfo(out var returnType))
        {
            intrinsic = intrinsic with { ReturnType = returnType };
            if (!scanner.MatchWhiteSpace(1, true))
                return scanner.Backtrack(position, out intrinsic);

            if (scanner.Attributes(out var attributes))
            {
                intrinsic = intrinsic with { Attributes = attributes };
                if (!scanner.MatchWhiteSpace(1, true))
                    return scanner.Backtrack(position, out intrinsic);
            }

            if (scanner.Identifier(out var name))
            {
                intrinsic = intrinsic with { Name = name };
                scanner.MatchWhiteSpace(advance: true);
                if (scanner.Match("(", true))
                {
                    do
                    {
                        scanner.MatchWhiteSpace(advance: true);
                        scanner.IntrinsicParameter(out var parameter);
                        scanner.MatchWhiteSpace(advance: true);
                        intrinsic.Parameters.Items.Add(parameter);
                    }
                    while (!scanner.EOF && scanner.Match(",", true));
                    if (scanner.EOF || !scanner.Match(")", true))
                        return scanner.Backtrack(position, out intrinsic);
                    scanner.MatchWhiteSpace(advance: true);
                    if (scanner.IntrinsicOp(out var op))
                        intrinsic = intrinsic with { Location = new TextLocation(scanner.Code, position..scanner.Position), Operator = op };
                    if (!scanner.FollowedBy(";", advance: true))
                        return scanner.Backtrack(position, out intrinsic);
                    return scanner.Success();
                }
            }
        }
        return scanner.Backtrack(position, out intrinsic);
    }

    internal static bool NamespaceDeclaration(this ref Scanner scanner, out NamespaceDeclaration namespaceDecl)
    {
        var position = scanner.Position;
        namespaceDecl = new(null!, [], new());
        if (scanner.Match("namespace", true))
        {
            scanner.MatchWhiteSpace(1, true);
            if (scanner.Identifier(out var name))
            {
                scanner.MatchWhiteSpace(advance: true);
                if (scanner.Match("{", true))
                {
                    scanner.MatchWhiteSpace(advance: true);
                    while (!scanner.EOF && scanner.IntrinsicDeclaration(out var intrinsic))
                    {
                        namespaceDecl.Intrinsics.Items.Add(intrinsic);
                        scanner.MatchWhiteSpace(advance: true);
                    }
                    if (scanner.EOF || !(scanner.Match("}", true) && scanner.FollowedBy("namespace", advance: true)))
                        return scanner.Backtrack(position, out namespaceDecl);
                    namespaceDecl = namespaceDecl with { Name = name, Location = new TextLocation(scanner.Code, position..scanner.Position) };
                    return scanner.Success();
                }
            }
        }
        return scanner.Backtrack(position, out namespaceDecl);
    }
    internal static bool IntrinsicFile(this ref Scanner scanner, out EquatableList<NamespaceDeclaration> namespaces)
    {
        var position = scanner.Position;
        namespaces = [];
        scanner.MatchWhiteSpace(advance: true);
        while (!scanner.EOF && scanner.NamespaceDeclaration(out var namespaceDecl))
        {
            namespaces.Items.Add(namespaceDecl);
            scanner.MatchWhiteSpace(advance: true);
        }
        return scanner.Success();
    }
}

internal static class IntrinParser
{

    internal static bool ProcessAndParse(string code, out EquatableList<NamespaceDeclaration> result)
        => Parse(PreProcess(code), out result);
    
    internal static string PreProcess(string code)
        => string.Join("\n", code.Split('\n').Where(line => !line.TrimStart().StartsWith("//")));
    internal static bool Parse(string code, out EquatableList<NamespaceDeclaration> result)
    {
        var scanner = new Scanner(code);
        if(scanner.IntrinsicFile(out var ns))
        {
            foreach(var n in ns)
            {
                for(int i = 0; i < n.Intrinsics.Items.Count; i++)
                {
                    var intrinsic = n.Intrinsics.Items[i];
                    if(intrinsic.ReturnType is { Typename.Name: "$to_resolve" })
                    {
                        intrinsic = intrinsic with
                        {
                            ReturnType = intrinsic.Parameters.Items[intrinsic.ReturnType.Match is TypeMatch tm ? tm.ComponentA - 1 : 0].TypeInfo
                        };
                    }
                    for(int j = 0; j < intrinsic.Parameters.Items.Count; j++)
                    {
                        var parameter = intrinsic.Parameters.Items[j];
                        if(parameter is not null && parameter.TypeInfo is { Typename.Name: "$to_resolve", Match: TypeMatch {ComponentA : >= 0} tm})
                        {
                            parameter = tm switch
                            {
                                { ComponentA: 0 } => parameter with { TypeInfo = intrinsic.ReturnType },
                                _ => parameter with { TypeInfo = intrinsic.Parameters.Items[tm.ComponentA - 1].TypeInfo }
                            };
                            
                            intrinsic.Parameters.Items[j] = parameter;
                        }
                    }
                    
                    n.Intrinsics.Items[i] = intrinsic;
                    
                }
            }
        }
        
        if (!scanner.EOF)
        {
            result = [];
            return false;
        }
        result = ns;
        return true;
    }
}