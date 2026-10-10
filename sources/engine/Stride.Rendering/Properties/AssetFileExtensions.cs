// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Serialization;

// The asset files of this package and the type their compiled content loads as, for the asset URL constants generator
[assembly: AssetFileExtension(".sdm3d", typeof(Stride.Rendering.Model))]
[assembly: AssetFileExtension(".sdmat", typeof(Stride.Rendering.Material))]
[assembly: AssetFileExtension(".sdprefabmodel", typeof(Stride.Rendering.Model))]
[assembly: AssetFileExtension(".sdpromodel", typeof(Stride.Rendering.Model))]
[assembly: AssetFileExtension(".sdskel", typeof(Stride.Rendering.Skeleton))]
[assembly: AssetFileExtension(".sdsky", typeof(Stride.Rendering.Skyboxes.Skybox))]
