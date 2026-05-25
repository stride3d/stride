// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
/// Controls how the loader treats premultiplied vs straight alpha when decoding an image.
/// </summary>
public enum AlphaLoadMode
{
    /// <summary>Return the data as the source encoded it; no conversion.</summary>
    Preserve,

    /// <summary>Ensure the loaded image is premultiplied. No-op if the source is already premultiplied; throws for formats where the source state is unknown.</summary>
    EnsurePremultiplied,

    /// <summary>Ensure the loaded image is straight alpha. No-op if the source is already straight; throws for formats where the source state is unknown.</summary>
    EnsureStraight,
}
