// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Definitions.Heightfield;

/// <summary>
/// A query passed to <see cref="IHeightfieldSampler"/> who will write the <see cref="Height"/> of the <see cref="SampleCoord"/> provided
/// </summary>
public record struct Sample(Int2 SampleCoord, float Height);
