// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Reflection;
using Stride.Core.Annotations;

namespace Stride.Engine
{
    /// <summary>
    /// Allow to query attributes used on an <see cref="EntityComponent"/>
    /// </summary>
    public struct EntityComponentAttributes
    {
        private static readonly Dictionary<Type, EntityComponentAttributes> ComponentAttributes = new Dictionary<Type, EntityComponentAttributes>();

        private EntityComponentAttributes(bool allowMultipleComponents)
        {
            AllowMultipleComponents = allowMultipleComponents;
        }

        /// <summary>
        /// Gets a boolean indicating whether the <see cref="EntityComponent"/> is supporting multiple components of the same type on an entity.
        /// </summary>
        public readonly bool AllowMultipleComponents;

        /// <summary>
        /// Gets the attributes for the specified type.
        /// </summary>
        /// <typeparam name="T">Type of the component</typeparam>
        /// <returns>The attributes for the specified type.</returns>
        public static EntityComponentAttributes Get<T>() where T : EntityComponent
        {
            return GetInternal(typeof(T));
        }

        /// <summary>
        /// Gets the attributes for the specified type.
        /// </summary>
        /// <param name="type">The type of the component.</param>
        /// <returns>The attributes for the specified type.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentException">The type must be of EntityComponent;type</exception>
        public static EntityComponentAttributes Get([NotNull] Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (!typeof(EntityComponent).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo())) throw new ArgumentException(@"The type must be of EntityComponent", nameof(type));
            return GetInternal(type);
        }

        private static EntityComponentAttributes GetInternal([NotNull] Type type)
        {
            EntityComponentAttributes attributes;
            lock (ComponentAttributes)
            {
                if (!ComponentAttributes.TryGetValue(type, out attributes))
                {
                    attributes = new EntityComponentAttributes(type.GetTypeInfo().GetCustomAttribute<AllowMultipleComponentsAttribute>() != null);
                    ComponentAttributes.Add(type, attributes);
                }
            }
            return attributes;
        }
    }
}
