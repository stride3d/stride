using System.Collections.Frozen;

namespace Stride.Shaders.Core;


public partial record Scalar
{
    public static string[] names = [
        "bool",
        "byte",
        "sbyte",
        "short",
        "ushort",
        "half",
        "int",
        "uint",
        "float",
        "long",
        "ulong",
        "double"
    ];
    public static Scalar From(string s) => Types[s];
    public static FrozenDictionary<string, Scalar> Types { get; } = Init();

    // static Scalar()
    // {
    //     var arr = new KeyValuePair<string, Scalar>[names.Length + 1];
    //     arr[0] = new("void", new("void"));
    //     for(int i = 1; i < names.Length; i++)
    //         arr[i] = new(names[i], new(names[i]));
    //     Types = FrozenDictionary.ToFrozenDictionary(arr); 
    // }
    internal static FrozenDictionary<string, Scalar> Init()
    {
        var arr = new KeyValuePair<string, Scalar>[names.Length + 1];
        arr[0] = new("void", new("void"));
        for(int i = 1; i < names.Length + 1; i++)
            arr[i] = new(names[i - 1], new(names[i - 1]));
        return arr.ToFrozenDictionary();
    }
}

public partial record Vector
{
    public static Vector From(string s) => Types[s];
    public static FrozenDictionary<string, Vector> Types { get; } = Init();

    internal static FrozenDictionary<string, Vector> Init()
    {
        var arr = new KeyValuePair<string, Vector>[Scalar.names.Length * 4];
        for(int i = 0; i < Scalar.names.Length; i++)
            for(int x = 1; x < 5; x++)
                arr[i * 4 + (x - 1)] = new($"{Scalar.names[i]}{x}", new(Scalar.From(Scalar.names[i]),x));
        return arr.ToFrozenDictionary();
    }
}


public partial record Matrix
{
    public static Matrix From(string s) => Types[s];
    public static FrozenDictionary<string, Matrix> Types { get; } = Init();
    internal static FrozenDictionary<string, Matrix> Init()
    {
        var arr = new KeyValuePair<string, Matrix>[Scalar.names.Length * 4 * 4];
        for(int i = 0; i < Scalar.names.Length; i++)
            for(int x = 1; x < 5; x++)
            for(int y = 1; y < 5; y++)
                arr[i * 16 + (x - 1) * 4 + (y - 1) * 4] = new($"{Scalar.names[i]}{x}x{y}", new(Scalar.From(Scalar.names[i]),x,y));
        return arr.ToFrozenDictionary(); 
    }
}