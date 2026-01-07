// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.ReferenceCounting;

namespace Stride.Graphics;

/// <summary>
///   <para>
///     A <strong>Pipeline State</strong> object encapsulates the complete pipeline configuration,
///     including Shaders, input layout, Render States, and output settings.
///     It represents an atomic, immutable collection of states that can be efficiently bound and unbound as
///     a single unit during rendering operations.
///   </para>
///   <para>
///     An instance of this class can represent either the state of the <strong>graphics pipeline</strong>,
///     or the state of the <strong>compute pipeline</strong> (in the platforms that support compute).
///   </para>
/// </summary>
public partial class PipelineState : GraphicsResourceBase
{
    // TODO: Unused? Vulkan backend has 'inputBindingCount', but does not write to this property.
    public int InputBindingCount { get; private set; }


    /// <summary>
    ///   Creates a new <strong>Pipeline State</strong> object from the provided description.
    /// </summary>
    /// <param name="graphicsDevice">The Graphics Device.</param>
    /// <param name="pipelineStateDescription">A description of the desired graphics pipeline configuration.</param>
    /// <returns>A new instance of <see cref="PipelineState"/>.</returns>
    public static PipelineState New(GraphicsDevice graphicsDevice, PipelineStateDescription pipelineStateDescription)
    {
        // Hash the current state
        var hashedState = new PipelineStateDescriptionWithHash(pipelineStateDescription);

        // Store SamplerState in a cache (D3D seems to have quite bad concurrency when using CreateSampler while rendering)
        PipelineState pipelineState;
        lock (graphicsDevice.CachedPipelineStates)
        {
            if (graphicsDevice.CachedPipelineStates.TryGetValue(hashedState, out pipelineState))
            {
                // TODO: Appropriate destroy
                pipelineState.AddReferenceInternal();
            }
            else
            {
                pipelineState = new PipelineState(graphicsDevice, pipelineStateDescription);
                graphicsDevice.CachedPipelineStates.Add(hashedState, pipelineState);
            }
        }
        return pipelineState;
    }
}
