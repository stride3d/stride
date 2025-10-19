using Silk.NET.Maths;

namespace Stride.Graphics.RHI;


public enum FilterKind
{
    Nearest = 0,
    Linear = 1,
    CubicExt = 2,
    CubicImg = 3
}

public enum TextureAddressMode
{
    Repeat = 0,
    Wrap = 1,
    Mirror = 2,
    Clamp = 3,
    Border = 4,
    MirrorOnce = 5
}
public enum ComparisonOperation
{
    Never = 0,
    Less = 1,
    Equal = 2,
    LessEqual = 3,
    Greater = 4,
    NotEqual = 5,
    GreaterEqual = 6,
    Always = 7
}

public struct SamplerDesc
{
    public float MipLODBias { get; set; }
    public float MipLodBias { get; set; }
    public float MinLod { get; set; }
    public float MaxLod { get; set; }
    public float MaxAnisotropy { get; set; }
    public bool UnnormalizedCoordinates { get; set; }
    // VkSamplerMipmapMode     mipmapMode;
    public FilterKind Filter { get; set; }
    public TextureAddressMode AddressU { get; set; }
    public TextureAddressMode AddressV { get; set; }
    public TextureAddressMode AddressW { get; set; }
    public ComparisonOperation CompareOp { get; set; }
    public Vector4D<float> BorderColor { get; set; }

}