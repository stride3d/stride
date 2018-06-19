namespace Xenko.Rendering.Materials
{
    /// <summary>
    /// Base interface for specular models supporting energy conservation.
    /// </summary>
    public interface IEnergyConservativeDiffuseModelFeature : IMaterialDiffuseModelFeature
    {
        /// <summary>
        /// A value indicating whether this instance is energy conservative.
        /// </summary>
        bool IsEnergyConservative { get; set; }
    }
}