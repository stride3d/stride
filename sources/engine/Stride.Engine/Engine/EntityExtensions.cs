// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Engine.Design;

namespace Stride.Engine
{
    /// <summary>
    /// Extensions for <see cref="Entity"/>
    /// </summary>
    public static class EntityExtensions
    {
        /// <summary>
        /// Deep clone of this entity.
        /// </summary>
        /// <param name="entity">The entity to clone</param>
        /// <returns>The cloned entity</returns>
        public static Entity Clone(this Entity entity)
        {
            return EntityCloner.Clone(entity);
        }

        /// <summary>
        /// Gets the children of this entity.
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <returns>An iteration on the child entity</returns>
        public static IEnumerable<Entity> GetChildren(this Entity entity)
        {
            var transformationComponent = entity.Transform;
            if (transformationComponent != null)
            {
                foreach (var child in transformationComponent.Children)
                {
                    yield return child.Entity;
                }
            }
        }

        /// <summary>
        /// Enables or disables components of the specified type.
        /// </summary>
        /// <typeparam name="T">Type of the component</typeparam>
        /// <param name="entity">The entity to apply this method.</param>
        /// <param name="enabled">If set to <c>true</c>, all components of {T} will be enabled; otherwise they will be disabled</param>
        /// <param name="applyOnChildren">Recursively apply this method to the children of this entity</param>
        public static void Enable<T>(this Entity entity, bool enabled = true, bool applyOnChildren = false) where T : ActivableEntityComponent
        {
            // NOTE: This method is recursive. That might not be the best solution in case of deep entities.
            for (var i = 0; i < entity.Components.Count; i++)
            {
                var component = entity.Components[i] as T;
                if (component != null)
                {
                    component.Enabled = enabled;
                }
            }

            if (!applyOnChildren) return;

            var transformationComponent = entity.Transform;

            if (transformationComponent == null) return;

            var children = transformationComponent.Children;
            for (var i = 0; i < children.Count; i++)
            {
                Enable<T>(children[i].Entity, enabled, true);
            }
        }

        /// <summary>
        /// Enables or disables all <see cref="ActivableEntityComponent"/>.
        /// </summary>
        /// <param name="entity">The entity to apply this method.</param>
        /// <param name="enabled">If set to <c>true</c>, all <see cref="ActivableEntityComponent"/> will be enabled; otherwise they will be disabled</param>
        /// <param name="applyOnChildren">Recursively apply this method to the children of this entity</param>
        public static void EnableAll(this Entity entity, bool enabled = true, bool applyOnChildren = false)
        {
            Enable<ActivableEntityComponent>(entity, enabled, applyOnChildren);
        }
    }
}
