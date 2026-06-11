// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
/// Controls how the loader treats premultiplied vs straight alpha when decoding an image.
/// </summary>
/// <remarks>
/// On iOS the underlying ImageIO decoder always premultiplies during decode; to honor the
/// <see cref="Preserve"/> contract the loader undoes that premul, which is slightly lossy for
/// translucent pixels. Callers that don't need the source encoding can ask for
/// <see cref="EnsurePremultiplied"/> on iOS to avoid the lossy step.
/// </remarks>
public enum AlphaLoadMode
{
    /// <summary>Return the data as the source encoded it; convert only if the platform decoder produced a different state.</summary>
    Preserve,

    /// <summary>Ensure the loaded image is premultiplied. No-op if the source is already premultiplied; throws for formats where the source state is unknown.</summary>
    EnsurePremultiplied,

    /// <summary>Ensure the loaded image is straight alpha. No-op if the source is already straight; throws for formats where the source state is unknown.</summary>
    EnsureStraight,
}
