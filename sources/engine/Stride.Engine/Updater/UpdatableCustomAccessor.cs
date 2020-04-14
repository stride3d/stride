// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Reflection;

namespace Stride.Updater
{
    /// <summary>
    /// Provide a custom implementation to access a member by the <see cref="UpdateEngine"/>.
    /// </summary>
    public abstract class UpdatableCustomAccessor : UpdatablePropertyBase
    {
        /// <summary>
        /// Gets a reference object from a property.
        /// </summary>
        /// <param name="obj">The object encoded as a native pointer (<see cref="UpdateEngine"/> will make sure it is pinned).</param>
        /// <returns>The object value from the property.</returns>
        public abstract object GetObject(IntPtr obj);

        /// <summary>
        /// Sets a reference object from a property.
        /// </summary>
        /// <param name="obj">The object encoded as a native pointer (<see cref="UpdateEngine"/> will make sure it is pinned).</param>
        /// <param name="data">The object value to set.</param>
        public abstract void SetObject(IntPtr obj, object data);

        /// <inheritdoc/>
        internal override UpdateOperationType GetSetOperationType()
        {
            if (MemberType.GetTypeInfo().IsValueType)
            {
                if (BlittableHelper.IsBlittable(MemberType))
                    return UpdateOperationType.ConditionalSetBlittablePropertyBase;

                return UpdateOperationType.ConditionalSetStructPropertyBase;
            }
            else
            {
                return UpdateOperationType.ConditionalSetObjectCustom;
            }
        }

        /// <inheritdoc/>
        internal override UpdateOperationType GetEnterOperationType()
        {
            if (MemberType.GetTypeInfo().IsValueType)
            {
                return UpdateOperationType.EnterStructPropertyBase;
            }
            else
            {
                return UpdateOperationType.EnterObjectCustom;
            }
        }
    }
}
