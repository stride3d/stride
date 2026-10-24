// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Serialization;

// The asset files of this package and the type their compiled content loads as, for the asset URL constants generator
[assembly: AssetFileExtension(".sdhmap", typeof(Stride.Physics.Heightmap))]
[assembly: AssetFileExtension(".sdphy", typeof(Stride.Physics.PhysicsColliderShape))]
