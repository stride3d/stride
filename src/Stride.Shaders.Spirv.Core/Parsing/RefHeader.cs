namespace Stride.Shaders.Spirv.Core.Parsing;

/// <summary>
/// A spirv header parser
/// </summary>
public readonly ref struct RefHeader
{
    internal Span<int> Words { get; init; }
    public uint MagicNumber { get => unchecked((uint)Words[0]); set => Words[0] = unchecked((int)value); }
    public SpirvVersion VersionNumber { get => Words[1]; set => Words[1] = value; }
    public int GeneratorMagicNumber { get => Words[2]; set => Words[2] = value; }
    public int Bound { get => Words[3]; set => Words[3] = value; }
    public int Schema { get => Words[4]; set => Words[4] = value; }

    public string Version => $"{VersionNumber >> 16}.{(VersionNumber >> 8) & 0x00FF}";

    public RefHeader(Span<int> words)
    {
        if (words.Length != 5)
            throw new ArgumentException("There should be 5 words");
        Words = words;
    }

    public bool IsValidMagic => MagicNumber == Spv.Specification.MagicNumber;

}
