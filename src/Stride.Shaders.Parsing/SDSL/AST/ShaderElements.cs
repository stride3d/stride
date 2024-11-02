namespace Stride.Shaders.Parsing.SDSL.AST;


public abstract class ShaderElement(TextLocation info) : Node(info);


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

public static class ShaderVariableInformationExtensions
{
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
    public TypeName Type { get; set; } = type;
    public Identifier Name { get; set; } = name;
    public Expression? Value { get; set; } = value;
    public StorageClass StorageClass { get; set; } = StorageClass.None;
    public TypeModifier TypeModifier { get; set; } = TypeModifier.None;
    public override string ToString()
    {
        return $"{(StorageClass != StorageClass.None ? $"{StorageClass} " :"")}{(TypeModifier != TypeModifier.None ? $"{TypeModifier} " :"")}{Type} {Name} = {Value}";
    }
}

public class TypeDef(TypeName type, Identifier name, TextLocation info) : ShaderElement(info)
{
    public Identifier Name { get; set; } = name;
    public TypeName Type { get; set; } = type;

    public override string ToString()
    {
        return $"typedef {Type} {Name}";
    }
}

public abstract class ShaderBuffer(List<Identifier> name, TextLocation info) : ShaderElement(info)
{
    public List<Identifier> Name { get; set; } = name;
}

public class ShaderStructMember(TypeName typename, Identifier identifier, TextLocation info) : Node(info)
{
    public TypeName TypeName { get; set; } = typename;
    public Identifier Name { get; set; } = identifier;
    public List<ShaderAttribute> Attributes { get; set; } = [];

    public override string ToString()
    {
        return $"{TypeName} {Name}";
    }
}

public class ShaderStruct(Identifier typename, TextLocation info) : ShaderElement(info)
{
    public Identifier TypeName { get; set; } = typename;
    public List<ShaderStructMember> Members { get; set; } = [];

    public override string ToString()
    {
        return $"struct {TypeName} ({string.Join(", ", Members)})";
    }
}


public sealed class CBuffer(List<Identifier> name, TextLocation info) : ShaderBuffer(name, info)
{
    public List<ShaderMember> Members { get; set; } = [];
}
public sealed class RGroup(List<Identifier> name, TextLocation info) : ShaderBuffer(name, info)
{
    public List<ShaderMember> Members { get; set; } = [];
}
public sealed class TBuffer(List<Identifier> name, TextLocation info) : ShaderBuffer(name, info)
{
    public List<ShaderMember> Members { get; set; } = [];
}
