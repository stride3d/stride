using Stride.Rendering.Materials;

namespace Stride.BepuPhysics.Definitions.Heightfield.Assets;

/// <summary>
/// The runtime representation for the layer of an <see cref="Heightfield"/>,
/// prepares an <see cref="IHeightfieldFunction"/> for evaluation by the physics engine at runtime 
/// </summary>
public interface IHeightfieldRuntimeLayer
{
    /// <summary>
    /// Returns the actual function called by the physics engine to evaluate the heights of an <see cref="Heightfield"/>
    /// </summary>
    IHeightfieldFunction BuildHeightfieldFunction();

    /// <summary>
    /// Returns the material-side displacement matching what <see cref="BuildHeightfieldFunction"/> would output for the physics-side
    /// </summary>
    IComputeScalar BuildGPUSideSampler(MaterialGeneratorContext context);
}
