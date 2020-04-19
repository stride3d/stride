// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Updater
{
    /// <summary>
    /// Shared class between <see cref="UpdatableProperty"/> and <see cref="UpdatableCustomAccessor"/>.
    /// </summary>
    public abstract class UpdatablePropertyBase : UpdatableMember
    {
        /// <summary>
        /// Gets a blittable property (from its pointer).
        /// </summary>
        /// <param name="obj">The container object.</param>
        /// <param name="data">The struct data.</param>
        public abstract void GetBlittable(IntPtr obj, IntPtr data);

        /// <summary>
        /// Sets a blittable property (from its pointer).
        /// </summary>
        /// <param name="obj">The container object.</param>
        /// <param name="data">The struct data.</param>
        public abstract void SetBlittable(IntPtr obj, IntPtr data);

        /// <summary>
        /// Sets a non-blittable struct property (given in boxed form).
        /// </summary>
        /// <param name="obj">The container object.</param>
        /// <param name="data">The new value to unbox and set</param>
        public abstract void SetStruct(IntPtr obj, object data);

        /// <summary>
        /// Gets and stores a non-blittable struct property into pre-allocated data, and return pointer to its start.
        /// </summary>
        /// <param name="obj">The container object.</param>
        /// <param name="data">The pre-allocated boxed struct.</param>
        /// <returns></returns>
        public abstract IntPtr GetStructAndUnbox(IntPtr obj, object data);

        /// <summary>
        /// Internally used to know type of set operation to use.
        /// </summary>
        internal abstract UpdateOperationType GetSetOperationType();

        /// <summary>
        /// Internally used to know type of enter operation to use.
        /// </summary>
        internal abstract UpdateOperationType GetEnterOperationType();
    }
}
