// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using Stride.Core;
using Stride.Engine;
using Stride.Rendering;

namespace Stride.Assets.Presentation.SceneEditor
{
    /// <summary>
    /// Result of a the <see cref="PickingRenderFeature"/>
    /// </summary>
    public struct EntityPickingResult
    {
        /// <summary>
        /// The entity picked. May be null if not found.
        /// </summary>
        public Entity Entity;

        /// <summary>
        /// The component identifier
        /// </summary>
        public int ComponentId;

        /// <summary>
        /// The mesh node index
        /// </summary>
        public int MeshNodeIndex;

        /// <summary>
        /// The material index
        /// </summary>
        public int MaterialIndex;

        /// <summary>
        /// The instance index
        /// </summary>
        public int InstanceId;

        /// <summary>
        /// Gets the component.
        /// </summary>
        public EntityComponent Component
        {
            get
            {
                if (Entity != null)
                {
                    int component = ComponentId;
                    return Entity.Components.First(x => component == RuntimeIdHelper.ToRuntimeId(x));
                }
                return null;
            }
        }

        public override string ToString()
        {
            return $"ComponentId: {ComponentId}, MeshNodeIndex: {MeshNodeIndex}, MaterialIndex: {MaterialIndex}";
        }
    }
}
