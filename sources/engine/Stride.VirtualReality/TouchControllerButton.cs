// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.VirtualReality
{
    public enum TouchControllerButton
    {
        /// <summary>
        /// Oculus: Thumbstick
        /// Vive: Thumb trackpad
        /// Windows Mixed Reality: Thumbstick
        /// </summary>
        Thumbstick,

        /// <summary>
        /// Oculus: Thumbstick
        /// Vive: Thumb trackpad
        /// Windows Mixed Reality: Touchpad
        /// </summary>
        Touchpad,
        /// <summary>
        /// Oculus: A
        /// Windows Mixed Reality: Right half of right touchpad
        /// </summary>
        A,

        /// <summary>
        /// Oculus: B
        /// Windows Mixed Reality: Left half of right touchpad
        /// </summary>
        B,

        /// <summary>
        /// Oculus: X
        /// Windows Mixed Reality: Left half of left touchpad
        /// </summary>
        X,

        /// <summary>
        /// Oculus: Y
        /// Windows Mixed Reality: Right half of left touchpad
        /// </summary>
        Y,

        /// <summary>
        /// Oculus: Trigger
        /// Vive : Trigger
        /// Windows Mixed Reality: Trigger
        /// </summary>
        Trigger,

        /// <summary>
        /// Oculus: Grip
        /// Vive: Grip
        /// Windows Mixed Reality: Grip
        /// </summary>
        Grip,

        /// <summary>
        /// Oculus: Left controller menu button
        /// Vive: Both controllers menu button
        /// Windows Mixed Reality: Both controllers menu button
        /// </summary>
        Menu,
    }
}
