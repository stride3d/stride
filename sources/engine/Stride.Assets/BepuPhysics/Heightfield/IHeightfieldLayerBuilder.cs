using System.Collections.Generic;
using Stride.Core.Assets;
using Stride.Core.BuildEngine;
using Stride.Core.Serialization.Contents;

namespace Stride.BepuPhysics.Definitions.Heightfield.Assets;

/// <summary>
/// A compile-time representation for the layer of an <see cref="Heightfield"/>,
/// prepares an <see cref="IHeightfieldFunction"/> for evaluation by the physics engine at runtime 
/// </summary>
public interface IHeightfieldLayerBuilder
{
    /// <summary>
    /// Returns the assets this layer requires
    /// </summary>
    IEnumerable<ObjectUrl> GetInputFiles();

    /// <summary>
    /// Builds the heightfield data that will be used at runtime
    /// </summary>
    /// <param name="size">The <see cref="Heightfield.Size"/> this layer will run on</param>
    /// <param name="subdivision">The <see cref="Heightfield.Subdivision"/> this layer will run on</param>
    /// <param name="commandContext">Context of the asset compiler</param>
    /// <param name="assetFinder">Asset finder</param>
    /// <param name="function">The output of the build process, the data to be used at runtime</param>
    /// <param name="minHeight">
    /// The lower bound, or rock bottom of the height <see cref="function"/> would output.
    /// Used to compute the bounding box.
    /// </param>
    /// <param name="maxHeight">
    /// The higher bound, or apex of the height <see cref="function"/> would output.
    /// Used to compute the bounding box.
    /// </param>
    void BuildRuntimeRepresentation(float size, int subdivision, ICommandContext commandContext, IAssetFinder assetFinder, out IHeightfieldRuntimeLayer function, out float minHeight, out float maxHeight);
}
