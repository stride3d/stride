// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Updater
{
    /// <summary>
    /// Provides a way to perform additional checks when entering an object (typically out of bounds checks).
    /// </summary>
    public abstract class EnterChecker
    {
        /// <summary>
        /// Called by <see cref="UpdateEngine.Run"/> to perform additional checks when entering an object (typically out of bounds checks).
        /// </summary>
        /// <param name="obj">The object being entered.</param>
        /// <returns>True if checks succeed, false otherwise.</returns>
        public abstract bool CanEnter(object obj);
    }
}
