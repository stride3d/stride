// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Navigation
{
    /// <summary>
    /// Represents cached data for a static collider component on an entity
    /// </summary>
    [DataContract]
    internal class NavigationMeshCachedObject
    {
        /// <summary>
        /// Guid of the collider
        /// </summary>
        public Guid Guid;

        /// <summary>
        /// Hash obtained with <see cref="NavigationMeshBuildUtils.HashEntityCollider"/>
        /// </summary>
        public int ParameterHash;

        /// <summary>
        /// Cached vertex data
        /// </summary>
        public NavigationMeshInputBuilder InputBuilder;

        /// <summary>
        /// List of infinite planes contained on this object
        /// </summary>
        public List<Plane> Planes = new List<Plane>();
    }
}
