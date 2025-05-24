// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using FirstPersonShooter.Core; // For MaterialType // MOVED TO TOP

namespace FirstPersonShooter.World
{
    public class TreeResource : ScriptComponent, IResourceNode
    {
        /// <summary>
        /// The current health or remaining resources of the tree.
        /// </summary>
        public float Health { get; set; } = 100f;

        /// <summary>
        /// The type of resource this tree provides.
        /// </summary>
        public string ResourceType { get; set; } = "Wood";

        /// <summary>
        /// The material type of this tree for impact effects.
        /// Can be configured in the editor if desired, defaults to Wood.
        /// </summary>
        public MaterialType TreeMaterialType { get; set; } = MaterialType.Wood; // From duplicated block

        private bool depleted = false;

        #region IResourceNode Implementation

        /// <summary>
        /// Gets the type of resource this node provides.
        /// </summary>
        public string GetResourceType()
        {
            return ResourceType;
        }

        /// <summary>
        /// Gets a value indicating whether this resource node is depleted.
        /// </summary>
        public bool IsDepleted => depleted;

        /// <summary>
        /// Gets the material type of this resource node for impact effects.
        /// </summary>
        public MaterialType HitMaterial => TreeMaterialType;

        /// <summary>
        /// Gets the entity associated with this resource node.
        /// </summary>
        public Entity GetEntity()
        {
            return this.Entity;
        }

        /// <summary>
        /// Attempts to harvest resources from this node.
        /// </summary>
        /// <param name="gatherAmount">The amount of resource to attempt to gather (e.g., damage dealt by the tool).</param>
        /// <param name="harvester">The entity performing the harvest.</param>
        /// <returns>True if resources were successfully harvested or if the node was depleted by this harvest, false otherwise (e.g., already depleted).</returns>
        public bool Harvest(float gatherAmount, Entity harvester)
        {
            if (depleted)
            {
                // Log.Info($"{Entity?.Name ?? "TreeResource"} is already depleted."); // Optional: can be verbose
                return false;
            }

            Health -= gatherAmount;
            Log.Info($"{harvester?.Name ?? "Harvester"} harvested {gatherAmount} from {Entity?.Name ?? "TreeResource"}. Health remaining: {Health}");

            if (Health <= 0)
            {
                Health = 0;
                depleted = true;
                Log.Info($"{Entity?.Name ?? "TreeResource"} has been depleted.");

                // Optional: Disable the entity or its model to visually show depletion.
                // This could be disabling a specific ModelComponent or the entire entity.
                // For example, to hide the model component if it's the first child:
                // Entity.GetChild(0)?.Get<ModelComponent>()?.Enabled = false;
                // Or to disable the entire entity (making it non-interactive and invisible):
                // this.Entity.EnableAll(false, true); // Disables this entity and its children.
                // The specific action might depend on game design (e.g., switch to a "stump" model).
            }

            return true; // Successfully harvested this turn, or depleted it this turn.
        }

        #endregion
    }
}
