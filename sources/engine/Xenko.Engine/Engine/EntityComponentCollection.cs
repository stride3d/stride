// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xenko.Core;
using Xenko.Core.Collections;
using Xenko.Core.Diagnostics;

namespace Xenko.Engine
{
    /// <summary>
    /// A collection of <see cref="EntityComponent"/> managed exclusively by the <see cref="Entity"/>.
    /// </summary>
    [DataContract("EntityComponentCollection")]
    [DebuggerTypeProxy(typeof(CollectionDebugView))]
    [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
    public sealed class EntityComponentCollection : FastCollection<EntityComponent>
    {
        private readonly Entity entity;

        public EntityComponentCollection()
        {
        }

        internal EntityComponentCollection(Entity entity)
        {
            this.entity = entity;
        }

        /// <summary>
        /// This property is only used when merging
        /// </summary>
        /// <remarks>
        /// NOTE: This property set to true internally in some very rare case (merging)
        /// </remarks>
        internal bool AllowReplaceForeignEntity { get; set; }

        /// <summary>
        /// Gets the first component of the specified type or derived type.
        /// </summary>
        /// <typeparam name="T">Type of the component</typeparam>
        /// <returns>The first component or null if it was not found</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>() where T : EntityComponent
        {
            for (int i = 0; i < Count; i++)
            {
                var item = this[i] as T;
                if (item != null)
                {
                    return item;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the index'th component of the specified type or derived type.
        /// </summary>
        /// <typeparam name="T">Type of the component</typeparam>
        /// <param name="index">Index of the component of the same type</param>
        /// <returns>The component or null if it was not found</returns>
        /// <remarks>
        /// <ul>
        /// <li>If index &gt; 0, it will take the index'th component of the specified <typeparamref name="T"/>.</li>
        /// <li>An index == 0 is equivalent to calling <see cref="Get{T}()"/></li>
        /// <li>if index &lt; 0, it will start from the end of the list to the beginning. A value of -1 means the first last component.</li>
        /// </ul>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>(int index) where T : EntityComponent
        {
            if (index < 0)
            {
                for (int i = Count - 1; i >= 0; i--)
                {
                    var item = this[i] as T;
                    if (item != null && ++index == 0)
                    {
                        return item;
                    }
                }
            }
            else
            {
                for (int i = 0; i < Count; i++)
                {
                    var item = this[i] as T;
                    if (item != null && index-- == 0)
                    {
                        return item;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Removes the first component of the specified type or derived type.
        /// </summary>
        /// <typeparam name="T">Type of the component</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove<T>() where T : EntityComponent
        {
            for (int i = 0; i < Count; i++)
            {
                var item = this[i] as T;
                if (item != null)
                {
                    RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Removes all components of the specified type or derived type.
        /// </summary>
        /// <typeparam name="T">Type of the component</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAll<T>() where T : EntityComponent
        {
            for (int i = Count - 1; i >= 0; i--)
            {
                var item = this[i] as T;
                if (item != null)
                {
                    RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Gets all the components of the specified type or derived type.
        /// </summary>
        /// <typeparam name="T">Type of the component</typeparam>
        /// <returns>An iterator on the component matching the specified type</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T> GetAll<T>() where T : EntityComponent
        {
            for (int i = 0; i < Count; i++)
            {
                var item = this[i] as T;
                if (item != null)
                {
                    yield return item;
                }
            }
        }

        protected override void ClearItems()
        {
            for (int i = Count - 1; i >= 0; i--)
            {
                RemoveItem(i);
            }
            base.ClearItems();
        }

        protected override void InsertItem(int index, EntityComponent item)
        {
            ValidateItem(index, item, false);

            base.InsertItem(index, item);

            // Notify the entity about this component being updated
            entity?.OnComponentChanged(index, null, item);
        }

        protected override void RemoveItem(int index)
        {
            var item = this[index];
            if (item is TransformComponent)
            {
                entity.TransformValue = null;
            }
            item.Entity = null;

            base.RemoveItem(index);

            // Notify the entity about this component being updated
            entity?.OnComponentChanged(index, item, null);
        }

        protected override void SetItem(int index, EntityComponent item)
        {
            var oldItem = ValidateItem(index, item, true);

            if (item != oldItem)
            {
                // Detach entity from previous item only when it's different from the new item.
                oldItem.Entity = null;
            }

            base.SetItem(index, item);

            // Notify the entity about this component being updated
            entity?.OnComponentChanged(index, oldItem, item);
        }

        private EntityComponent ValidateItem(int index, EntityComponent item, bool isReplacing)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item), @"Cannot add a null component");
            }

            var componentType = item.GetType();
            var attributes = EntityComponentAttributes.Get(componentType);

            var onlySingleComponent = !attributes.AllowMultipleComponents;

            EntityComponent previousItem = null;
            for (int i = 0; i < Count; i++)
            {
                var existingItem = this[i];
                if (index == i && isReplacing)
                {
                    previousItem = existingItem;
                }
                else
                {
                    if (ReferenceEquals(existingItem, item))
                    {
                        throw new InvalidOperationException($"Cannot add a same component multiple times. Already set at index [{i}]");
                    }

                    if (onlySingleComponent && componentType == existingItem.GetType())
                    {
                        throw new InvalidOperationException($"Cannot add a component of type [{componentType}] multiple times");
                    }
                }
            }

            if (!AllowReplaceForeignEntity && entity != null && item.Entity != null)
            {
                throw new InvalidOperationException($"This component is already attached to entity [{item.Entity}] and cannot be attached to [{entity}]");
            }

            if (entity != null)
            {
                var transform = item as TransformComponent;
                if (transform != null)
                {
                    entity.TransformValue = transform;
                }
                else if (previousItem is TransformComponent)
                {
                    // If previous item was a transform component but we are actually replacing it, we should 
                    entity.TransformValue = null;
                }

                item.Entity = entity;
            }

            return previousItem;
        }
    }
}
