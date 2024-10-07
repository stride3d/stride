using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Spv.Specification;

namespace Stride.Shaders.Spirv.Core.Parsing;

/// <summary>
/// Spirv version wrapper to interact through string/integers
/// </summary>
public struct SpirvVersion
{
    public int Version { get; }

    internal SpirvVersion(int word)
    {
        Version = word;
    }

    public SpirvVersion(int major, int minor)
    {
        Version = major << 16 | minor << 8;
    }
    public SpirvVersion(string version)
    {
        if(version.Length == 3 && char.IsDigit(version[0]) && version[1] == '.' && char.IsDigit(version[2]))
        {
            Version = version[0] - '0' << 16 | version[1] - '0' << 8;
        }
    }

    public static implicit operator int(SpirvVersion v) => v.Version;
    public static implicit operator SpirvVersion(int v) => new(v);
    public static implicit operator SpirvVersion(string v) => new(v);
}

/// <summary>
/// Spirv Header struct for spirv assembling
/// </summary>
public struct SpirvHeader
{
    public uint MagicNumber { get; init; }
    public SpirvVersion VersionNumber { get; init; }
    public int GeneratorMagicNumber { get; init; }
    public int Bound { get; init; }
    public int Schema { get; init; }

    public string Version => $"{VersionNumber >> 16}.{(VersionNumber >> 8) & 0x00FF}";

    public SpirvHeader(string version, int generator, int bound, int schema = 0)
    {
        MagicNumber = Spv.Specification.MagicNumber;
        VersionNumber = version;
        GeneratorMagicNumber = generator;
        Bound = bound;
        Schema = schema;
    }
    public SpirvHeader(SpirvVersion version, int generator, int bound, int schema = 0)
    {
        MagicNumber = Spv.Specification.MagicNumber;
        VersionNumber = version;
        GeneratorMagicNumber = generator;
        Bound = bound;
        Schema = schema;
    }

    public void WriteTo(Span<int> words)
    {
        words[0] = unchecked((int)MagicNumber);
        words[1] = VersionNumber.Version;
        words[2] = GeneratorMagicNumber;
        words[3] = Bound;
        words[4] = Schema;
    }

    public static SpirvHeader Read(Span<int> words)
    {
        return new SpirvHeader
        {
            MagicNumber = (uint)words[0],
            VersionNumber = words[1],
            GeneratorMagicNumber = words[2],
            Bound = words[3],
            Schema = words[4]
        };
    }

    public bool IsValidMagic => MagicNumber == Spv.Specification.MagicNumber;

}
