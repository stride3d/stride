// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Updater
{
    /// <summary>
    /// Describes how to access an object member so that it can be updated by the <see cref="UpdateEngine"/>.
    /// </summary>
    public abstract class UpdatableMember
    {
        /// <summary>
        /// Gets the type of the member.
        /// </summary>
        public abstract Type MemberType { get; }

        /// <summary>
        /// Called by <see cref="UpdateEngine.Compile"/> to generate additional checks when entering an object (typically out of bound checks).
        /// </summary>
        /// <returns>The created enter checker (or null if not needed).</returns>
        public virtual EnterChecker CreateEnterChecker()
        {
            return null;
        }
    }
}
