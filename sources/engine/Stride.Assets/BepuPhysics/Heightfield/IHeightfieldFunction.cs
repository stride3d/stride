namespace Stride.BepuPhysics.Definitions.Heightfield.Assets;

/// <summary>
/// An <see cref="IHeightfieldSampler"/> that can be combined with other <see cref="IHeightfieldSampler"/>
/// </summary>
public interface IHeightfieldFunction : IHeightfieldSampler
{
    /// <summary>
    /// Appends this <see cref="IHeightfieldFunction"/> to the <see cref="IFunctionCollector"/> provided
    /// </summary>
    void AppendTo(IFunctionCollector solution);
}
