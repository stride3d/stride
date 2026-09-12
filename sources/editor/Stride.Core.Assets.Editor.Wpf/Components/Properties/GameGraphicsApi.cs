// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Editor.Components.Properties
{
    // Graphics API a game executable builds against; Default = no explicit choice (SDK resolves the platform default).
    public enum GameGraphicsApi
    {
        Default,
        Direct3D11,
        Direct3D12,
        Vulkan,
    }
}
