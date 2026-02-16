using CommunityToolkit.HighPerformance.Buffers;
using Stride.Shaders.Spirv.Core.Parsing;
using System.Runtime.InteropServices;
using System.Text;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Spirv.Core;


public readonly struct LiteralString : ISpirvElement, IFromSpirv<LiteralString>
{
    readonly static StringPool pool = new();

    public MemoryOwner<int> Memory { get; init; }
    public string Value { get; init; }
    public readonly int Length => Value.Length + 1;

    public int WordCount => (Length / 4) + (HasRest ? 1 : 0);
    internal bool HasRest => Length % 4 > 0;
    internal int RestSize => Length % 4;

    public ReadOnlySpan<int> Words => Memory.Span;

    public LiteralString(string value)
    {
        Value = pool.GetOrAdd(value);
        Memory = MemoryOwner<int>.Allocate(WordCount);
        
    }
    public LiteralString(Span<int> words)
    {
        Memory = MemoryOwner<int>.Allocate(WordCount);
        words.CopyTo(Memory.Span);
        Span<char> chars = stackalloc char[words.Length * 4];
        for (int i = 0; i < words.Length; i++)
        {
            chars[i * 4] = (char)(words[i] & 0xFF);
            chars[i * 4 + 1] = (char)(words[i] >> 8 & 0xFF);
            chars[i * 4 + 2] = (char)(words[i] >> 16 & 0xFF);
            chars[i * 4 + 3] = (char)(words[i] >> 24 & 0xFF);
        };
        var real = chars[..chars.IndexOf('\0')];
        Value = pool.GetOrAdd(real);
    }
    public static implicit operator LiteralString(string s) => new(s);


    public void WriteTo(Span<int> slice)
    {
        for (int i = 0; i < Length; i++)
        {
            var pos = i / 4;
            var shift = 8 * (i % 4);
            var value = i < Value.Length ? Value[i] : '\0';
            slice[pos] |= value << shift;
        }
    }

    public static string Parse(Span<int> input)
    {
        var lit = new LiteralString(input);
        return lit.Value;
    }

    public static LiteralString From(Span<int> words)
    {
        return new(words);
    }

    public static LiteralString From(string value) => value;

    public void Dispose()
    {
        Memory.Dispose();
    }
}