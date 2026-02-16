namespace Stride.Shaders.Core;


public enum EntryPoint : uint
{
    None = 0,
    VertexShader = 1,
    PixelShader = 1 << 1,
    ComputeShader = 1 << 2,
    GeometryShader = 1 << 3,
    /// <summary>
    /// Tesselation control
    /// </summary>
    HullShader = 1 << 4,
    /// <summary>
    /// Tesselation evaluation
    /// </summary>
    DomainShader = 1 << 5,
    TaskNV = 1 << 6,
    MeshNV = 1 << 7,
    RayGeneration = 1 << 8,
    Intersection = 1 << 9,
    AnyHit = 1 << 10,
    ClosestHit = 1 << 11,
    Miss = 1 << 12,
    Callable = 1 << 13,
    TaskEXT = 1 << 14,
    MeshEXT = 1 << 15,
}