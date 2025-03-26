using System.Collections.Frozen;

namespace Stride.Shaders.Core;


public partial record ScalarType
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
    public static ScalarType From(string s) => Types[s];
    public static FrozenDictionary<string, ScalarType> Types { get; } = Init();

    // static Scalar()
    // {
    //     var arr = new KeyValuePair<string, Scalar>[names.Length + 1];
    //     arr[0] = new("void", new("void"));
    //     for(int i = 1; i < names.Length; i++)
    //         arr[i] = new(names[i], new(names[i]));
    //     Types = FrozenDictionary.ToFrozenDictionary(arr); 
    // }
    internal static FrozenDictionary<string, ScalarType> Init()
    {
        var arr = new KeyValuePair<string, ScalarType>[names.Length + 1];
        arr[0] = new("void", new("void"));
        for(int i = 1; i < names.Length + 1; i++)
            arr[i] = new(names[i - 1], new(names[i - 1]));
        return arr.ToFrozenDictionary();
    }
}

public partial record VectorType
{
    public static VectorType From(string s) => Types[s];
    public static FrozenDictionary<string, VectorType> Types { get; } = Init();

    internal static FrozenDictionary<string, VectorType> Init()
    {
        var arr = new KeyValuePair<string, VectorType>[ScalarType.names.Length * 4];
        for(int i = 0; i < ScalarType.names.Length; i++)
            for(int x = 1; x < 5; x++)
                arr[i * 4 + (x - 1)] = new($"{ScalarType.names[i]}{x}", new(ScalarType.From(ScalarType.names[i]),x));
        return arr.ToFrozenDictionary();
    }
}


public partial record MatrixType
{
    public static MatrixType From(string s) => Types[s];
    public static FrozenDictionary<string, MatrixType> Types { get; } = Init();
    internal static FrozenDictionary<string, MatrixType> Init()
    {
        var arr = new List<KeyValuePair<string, MatrixType>>(ScalarType.names.Length * 4 * 4);
        for(int i = 0; i < ScalarType.names.Length; i++)
            for(int x = 1; x < 5; x++)
            for(int y = 1; y < 5; y++)
                arr.Add(new($"{ScalarType.names[i]}{x}x{y}", new(ScalarType.From(ScalarType.names[i]),x,y)));
        return arr.ToFrozenDictionary(); 
    }
}