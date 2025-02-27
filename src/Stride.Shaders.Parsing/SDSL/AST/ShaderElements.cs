using Stride.Shaders.Core;
using Stride.Shaders.Core.Analysis;
using Stride.Shaders.Parsing.Analysis;

namespace Stride.Shaders.Parsing.SDSL.AST;


public abstract class ShaderElement(TextLocation info) : Node(info)
{
    public SymbolType? Type { get; set; }
}


public enum StorageClass
{
    None,
    Extern,
    NoInterpolation,
    Precise,
    Shared,
    GroupShared,
    Static,
    Uniform,
    Volatile
}

public enum TypeModifier
{
    None,
    Const,
    RowMajor,
    ColumnMajor
}
public enum InterpolationModifier
{
    None,
    Linear,
    Centroid,
    NoInterpolation,
    NoPerspective,
    Sample
}

public enum StreamKind
{
    None,
    Stream,
    PatchStream
}

public static class ShaderVariableInformationExtensions
{
    public static StreamKind ToStreamKind(this string str)
    {
        return str switch
        {
            "stream" => StreamKind.Stream,
            "patchstream" => StreamKind.PatchStream,
            _ => StreamKind.None
        };
    }
    public static InterpolationModifier ToInterpolationModifier(this string str)
    {
        return str switch
        {
            "linear" => InterpolationModifier.Linear,
            "centroid" => InterpolationModifier.Centroid,
            "nointerpolation" => InterpolationModifier.NoInterpolation,
            "noperspective" => InterpolationModifier.NoPerspective,
            "sample" => InterpolationModifier.Sample,
            _ => InterpolationModifier.None
        };
    }
    public static StorageClass ToStorageClass(this string str)
    {
        return str switch
        {
            "extern" => StorageClass.Extern,
            "nointerpolation" => StorageClass.NoInterpolation,
            "precise" => StorageClass.Precise,
            "shared" => StorageClass.Shared,
            "groupshared" => StorageClass.GroupShared,
            "static" => StorageClass.Static,
            "uniform" => StorageClass.Uniform,
            "volatile" => StorageClass.Volatile,
            _ => StorageClass.None
        };
    }

    public static TypeModifier ToTypeModifier(this string str)
    {
        return str switch
        {
            "const" => TypeModifier.Const,
            "row_major" => TypeModifier.RowMajor,
            "column_major" => TypeModifier.ColumnMajor,
            _ => TypeModifier.None
        };
    }
}

public class ShaderVariable(TypeName type, Identifier name, Expression? value, TextLocation info) : ShaderElement(info)
{
    public TypeName TypeName { get; set; } = type;
    public Identifier Name { get; set; } = name;
    public Expression? Value { get; set; } = value;
    public StorageClass StorageClass { get; set; } = StorageClass.None;
    public TypeModifier TypeModifier { get; set; } = TypeModifier.None;
    public override string ToString()
    {
        return $"{(StorageClass != StorageClass.None ? $"{StorageClass} " :"")}{(TypeModifier != TypeModifier.None ? $"{TypeModifier} " :"")}{TypeName} {Name} = {Value}";
    }
}

public class TypeDef(TypeName type, Identifier name, TextLocation info) : ShaderElement(info)
{
    public Identifier Name { get; set; } = name;
    public TypeName TypeName { get; set; } = type;

    public override string ToString()
    {
        return $"typedef {TypeName} {Name}";
    }
}

public abstract class ShaderBuffer(List<Identifier> name, TextLocation info) : ShaderElement(info)
{
    public List<Identifier> Name { get; set; } = name;
    public List<ShaderMember> Members { get; set; } = [];

    public override void ProcessSymbol(SymbolTable table)
    {
        var sym = new Symbol(new(Name.ToString() ?? "", SymbolKind.CBuffer), new BufferSymbol(Name.ToString() ?? "", []));

        table.DeclaredTypes.TryAdd(sym.ToString(), sym.Type);
        var kind = this switch
        {
            CBuffer => SymbolKind.CBuffer,
            TBuffer => SymbolKind.TBuffer,
            RGroup => SymbolKind.RGroup,
            _ => throw new NotSupportedException()
        };
        table.RootSymbols.Add(new(Name.ToString() ?? "", kind), sym);
        foreach (var cbmem in Members)
        {
            var msym = cbmem.TypeName.ToSymbol();
            table.DeclaredTypes.TryAdd(sym.ToString(), sym.Type);
            cbmem.Type = msym;
        }
    }
}

public class ShaderStructMember(TypeName typename, Identifier identifier, TextLocation info) : Node(info)
{
    public TypeName TypeName { get; set; } = typename;
    public SymbolType? Type { get; set; }
    public Identifier Name { get; set; } = identifier;
    public List<ShaderAttribute> Attributes { get; set; } = [];

    public override string ToString()
    {
        if(Type is not null)
            return $"{Type} {Name}";
        else return $"{TypeName} {Name}";
    }
}

public class ShaderStruct(Identifier typename, TextLocation info) : ShaderElement(info)
{
    public Identifier TypeName { get; set; } = typename;
    public List<ShaderStructMember> Members { get; set; } = [];

    public override void ProcessSymbol(SymbolTable table)
    {
        var sym = new Symbol(new(TypeName.ToString() ?? "", SymbolKind.Struct), new Struct(TypeName.ToString() ?? "", []));
        table.DeclaredTypes.TryAdd(sym.ToString(), sym.Type);
        table.RootSymbols.Add(new(TypeName.ToString() ?? "", SymbolKind.Struct), sym);
        foreach (var smem in Members)
        {
            var msym = smem.TypeName.ToSymbol();
            table.DeclaredTypes.TryAdd(sym.ToString(), sym.Type);
            smem.Type = msym;
        }
    }

    public override string ToString()
    {
        return $"struct {TypeName} ({string.Join(", ", Members)})";
    }
}


public sealed class CBuffer(List<Identifier> name, TextLocation info) : ShaderBuffer(name, info);
public sealed class RGroup(List<Identifier> name, TextLocation info) : ShaderBuffer(name, info);
public sealed class TBuffer(List<Identifier> name, TextLocation info) : ShaderBuffer(name, info);
