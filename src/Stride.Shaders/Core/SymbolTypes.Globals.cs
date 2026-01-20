using System.Collections.Frozen;

namespace Stride.Shaders.Core;


public partial record ScalarType
{
    internal static KeyValuePair<string, ScalarType>[] names = [
        new("void", Void),
        new("bool", Boolean),
        new("int", Int),
        new("uint", UInt),
        new("long", Int64),
        new("ulong", UInt64),
        new("float", Float),
        new("double", Double),
    ];
    public static ScalarType From(string s) => Types[s];
    public static FrozenDictionary<string, ScalarType> Types { get; } = Init();

    internal static FrozenDictionary<string, ScalarType> Init() =>
        FrozenDictionary.Create<string, ScalarType>(names);
}

public partial record VectorType
{
    public static VectorType From(string s) => Types[s];
    public static FrozenDictionary<string, VectorType> Types { get; } = Init();

    internal static FrozenDictionary<string, VectorType> Init()
    {
        var arr = new KeyValuePair<string, VectorType>[ScalarType.names.Length * 3];
        for(int i = 0; i < ScalarType.names.Length; i++)
            for(int x = 2; x <= 4; x++)
                arr[i * 3 + (x - 2)] = new($"{ScalarType.names[i].Key}{x}", new(ScalarType.names[i].Value,x));
        return arr.ToFrozenDictionary();
    }
}


public partial record MatrixType
{
    public static MatrixType From(string s) => Types[s];
    public static FrozenDictionary<string, MatrixType> Types { get; } = Init();
    internal static FrozenDictionary<string, MatrixType> Init()
    {
        var arr = new List<KeyValuePair<string, MatrixType>>(ScalarType.names.Length * 3 * 3);
        for(int i = 0; i < ScalarType.names.Length; i++)
            for(int x = 2; x <= 4; x++)
                for(int y = 2; y <= 4; y++)
                    // Note: this is HLSL-style so Rows/Columns meaning is swapped
                    arr.Add(new($"{ScalarType.names[i].Key}{y}x{x}", new(ScalarType.names[i].Value,x,y)));
        return arr.ToFrozenDictionary(); 
    }
}