// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Presentation.Dirtiables
{
    public interface IDirtiable
    {
        /// <summary>
        /// Gets the dirty state of this object.
        /// </summary>
        bool IsDirty { get; }

        /// <summary>
        /// Updates the <see cref="IsDirty"/> property to the given value.
        /// </summary>
        /// <param name="value">The new value for the dirty flag.</param>
        void UpdateDirtiness(bool value);
    }
}
