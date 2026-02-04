
using System.Collections;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Stride.Shaders.Generators.Intrinsics;

//   <type> \[\[[attr]\]\] <name>([<qual> <type> <name> [, ... ]]) [ : <op>]

internal abstract record Node([property:JsonIgnore]TextLocation Location);

internal record Identifier(string Name, TextLocation Location) : Node(Location)
{
    public override string ToString() => Name;
}


internal record Attributes(string[] Values, TextLocation Location) : Node(Location);


internal record Layout(string Size1, string? Size2, TextLocation Location) : Node(Location);

internal record Typename(string Name, Layout? Size, TextLocation Location) : Node(Location)
{
    public override string ToString() => Size switch
    {
        null => Name,
        _ when Size.Size2 is null => $"{Name}{Size.Size1}",
        _ => $"{Name}{Size.Size1}x{Size.Size2}",
    };
}
// internal record NumericType(Layout Size, TextLocation Location) : Typename("numeric", Size, Location);

internal record Matching(int LayoutIndex, int BaseTypeIndex, TextLocation Location) : Node(Location);
internal record ClassTMatch(TextLocation Location) : Matching(-1, 0,Location);
internal record FuncMatch(TextLocation Location) : Matching(-3, 0, Location);
internal record Func2Match(TextLocation Location) : Matching(-3, 0, Location);
internal record TypeMatch(int Index, TextLocation Location) : Matching(Index, Index, Location);


internal record TypeInfo(Typename Typename, TextLocation Location, Matching? Match = null) : Node(Location);
internal record IntrinsicOp(string Operator, TextLocation Location) : Node(Location);
internal record ArgumentQualifier(string Qualifier, TextLocation Location, string? OptionalQualifier = null) : Node(Location);

internal record IntrinsicParameter(ArgumentQualifier? Qualifier, TypeInfo TypeInfo, Identifier Name, TextLocation Location) : Node(Location);

internal record IntrinsicDeclaration(Identifier Name, TypeInfo ReturnType, EquatableList<IntrinsicParameter> Parameters, TextLocation Location, Attributes? Attributes = null, IntrinsicOp? Operator = null) : Node(Location);

internal record NamespaceDeclaration(Identifier Name, EquatableList<IntrinsicDeclaration> Intrinsics, TextLocation Location) : Node(Location);


static class EquatableListBuilder
{
    public static EquatableList<T> Create<T>(ReadOnlySpan<T> items) => new([..items]);
}

[CollectionBuilder(typeof(EquatableListBuilder), "Create")]
internal readonly struct EquatableList<T>(List<T> items)
{
    public List<T> Items { get; } = items;

    public readonly override bool Equals(object? obj)
    {
        if (obj is EquatableList<T> other)
            return Items.SequenceEqual(other.Items);
        return false;
    }

    public readonly override int GetHashCode()
    {
        int hash = 17;
        foreach (var item in Items)
            hash = hash * 31 + (item?.GetHashCode() ?? 0);
        return hash;
    }
    public void Deconstruct(out List<T> items) => items = Items;

    public List<T>.Enumerator GetEnumerator()
        => Items.GetEnumerator();
}
