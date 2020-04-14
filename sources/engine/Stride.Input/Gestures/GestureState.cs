// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Input
{
    /// <summary>
    /// The different possible states of a gestures.
    /// </summary>
    public enum GestureState
    {
        /// <summary>
        /// A discrete gesture has occurred.
        /// </summary>
        Occurred,
        /// <summary>
        /// A continuous gesture has started.
        /// </summary>
        Began,
        /// <summary>
        /// A continuous gesture parameters changed.
        /// </summary>
        Changed,
        /// <summary>
        /// A continuous gesture has stopped.
        /// </summary>
        Ended,
    }
}
