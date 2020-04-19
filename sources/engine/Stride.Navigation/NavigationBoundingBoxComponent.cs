// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Navigation.Processors;

namespace Stride.Navigation
{
    /// <summary>
    /// A three dimensional bounding box  using the scale of the owning entity as the box extent. This is used to limit the area in which navigation meshes are generated
    /// </summary>
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(BoundingBoxProcessor), ExecutionMode = ExecutionMode.All)]
    [Display("Navigation bounding box")]
    [ComponentCategory("Navigation")]
    public class NavigationBoundingBoxComponent : EntityComponent
    {
        /// <summary>
        /// The size of one edge of the bounding box
        /// </summary>
        [DataMember(0)]
        public Vector3 Size { get; set; } = Vector3.One;
    }
}
