// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Assets;
using Stride.Core.Mathematics;
using Stride.Core.Reflection;
using Stride.Navigation;
using Stride.Physics;

namespace Stride.Assets.Navigation
{
    /// <summary>
    /// Default factory for navigation meshes
    /// </summary>
    public class DefaultNavigationMeshFactory : AssetFactory<NavigationMeshAsset>
    {
        public override NavigationMeshAsset New()
        {
            // Initialize build settings
            return new NavigationMeshAsset
            {
                BuildSettings = ObjectFactoryRegistry.NewInstance<NavigationMeshBuildSettings>(),
                IncludedCollisionGroups = CollisionFilterGroupFlags.AllFilter,
            };
        }
    }
}
