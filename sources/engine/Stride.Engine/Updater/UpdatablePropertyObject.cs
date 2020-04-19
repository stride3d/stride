// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Updater
{
    /// <summary>
    /// Defines how to set and get values from a property of a given reference type for the <see cref="UpdateEngine"/>.
    /// </summary>
    /// <typeparam name="T">The property type.</typeparam>
    public class UpdatablePropertyObject<T> : UpdatableProperty
    {
        public UpdatablePropertyObject(IntPtr getter, bool virtualDispatchGetter, IntPtr setter, bool virtualDispatchSetter) : base(getter, virtualDispatchGetter, setter, virtualDispatchSetter)
        {
        }

        /// <inheritdoc/>
        public override Type MemberType
        {
            get { return typeof(T); }
        }

        /// <inheritdoc/>
        public override void GetBlittable(IntPtr obj, IntPtr data)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override void SetBlittable(IntPtr obj, IntPtr data)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override void SetStruct(IntPtr obj, object data)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override IntPtr GetStructAndUnbox(IntPtr obj, object data)
        {
            throw new NotImplementedException();
        }
    }
}
