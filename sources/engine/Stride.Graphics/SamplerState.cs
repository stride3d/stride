// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.ReferenceCounting;

        /// <summary>
        /// Gets the sampler state description.
        /// </summary>
namespace Stride.Graphics;

public partial class SamplerState : GraphicsResourceBase
{
    public readonly SamplerStateDescription Description;


    public static SamplerState New(GraphicsDevice device, SamplerStateDescription description)
    {
        // Store SamplerState in a cache (D3D seems to have quite bad concurrency when using CreateSampler while rendering)
        SamplerState samplerState;
        lock (device.CachedSamplerStates)
        {
            if (device.CachedSamplerStates.TryGetValue(description, out samplerState))
            {
                // TODO: Appropriate destroy
                samplerState.AddReferenceInternal();
            }
        /// <summary>
        /// Create a new fake sampler state for serialization.
        /// </summary>
        /// <param name="description">The description of the sampler state</param>
        /// <returns>The fake sampler state</returns>
            else
            {
                samplerState = new SamplerState(device, description);
                device.CachedSamplerStates.Add(description, samplerState);
            }
        }
        return samplerState;
    }

    public static SamplerState NewFake(SamplerStateDescription description)
    {
        return new SamplerState(description);
    }
    private SamplerState(SamplerStateDescription description)
    {
        Description = description;
    }

    protected override void Destroy()
    {
        lock (GraphicsDevice.CachedSamplerStates)
        {
            GraphicsDevice.CachedSamplerStates.Remove(Description);
        }

        base.Destroy();
    }
}
