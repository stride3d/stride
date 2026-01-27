// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.ReferenceCounting;

namespace Stride.Graphics;

/// <summary>
///   A graphics object that describes a <strong>Sampler State</strong>, which determines
///   how to sample Texture data.
/// </summary>
public partial class SamplerState : GraphicsResourceBase
{
    /// <summary>
    ///   The description of the Sampler State.
    /// </summary>
    public readonly SamplerStateDescription Description;


    /// <summary>
    ///   Creates a new <see cref="SamplerState"/>.
    /// </summary>
    /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
    /// <param name="description">
    ///   A <see cref="SamplerStateDescription"/> structure describing the Sampler State
    ///   object to create.
    /// </param>
    /// <param name="name">An optional name that can be used to identify the Sampler State.</param>
    /// <returns>A new Sampler State object.</returns>
    public static SamplerState New(GraphicsDevice device, ref readonly SamplerStateDescription description, string? name = null)
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
            else
            {
                samplerState = new SamplerState(device, in description, name);
                device.CachedSamplerStates.Add(description, samplerState);
            }
        }
        return samplerState;
    }

    /// <summary>
    ///   Creates a new fake <see cref="SamplerState"/> for serialization.
    /// </summary>
    /// <param name="description">
    ///   A <see cref="SamplerStateDescription"/> structure describing the Sampler State
    ///   object to create.
    /// </param>
    /// <returns>A new fake Sampler State object.</returns>
    public static SamplerState NewFake(SamplerStateDescription description)
    {
        return new SamplerState(description);
    }
    private SamplerState(SamplerStateDescription description)
    {
        Description = description;
    }

    /// <inheritdoc/>
    protected override void Destroy()
    {
        lock (GraphicsDevice.CachedSamplerStates)
        {
            GraphicsDevice.CachedSamplerStates.Remove(Description);
        }

        base.Destroy();
    }
}
