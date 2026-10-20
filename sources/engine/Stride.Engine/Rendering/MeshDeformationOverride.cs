// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Rendering;

/// <summary>
/// Forces a deformation path and thread group, for benchmarks and tests. See <see cref="ModelRenderProcessor.DeformationOverride"/>.
/// </summary>
/// <param name="Mode">The deformation path for every deformed mesh.</param>
/// <param name="ThreadGroup">The compute thread group shape, or <c>null</c> to choose it per batch.</param>
internal sealed record MeshDeformationOverride(MeshDeformationMode Mode, DeformationThreadGroup? ThreadGroup = null);
