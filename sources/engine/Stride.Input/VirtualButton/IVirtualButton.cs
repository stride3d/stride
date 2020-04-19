// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Input
{
    /// <summary>
    /// Interface IVirtualButton
    /// </summary>
    public interface IVirtualButton
    {
        /// <summary>
        /// Gets the value associated with this virtual button from an input manager.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <returns>System.Single.</returns>
        float GetValue(InputManager manager);

        /// <summary>
        /// Indicate if the button is currently down
        /// </summary>
        /// <param name="manager">The input manager</param>
        /// <returns></returns>
        bool IsDown(InputManager manager);

        /// <summary>
        /// Indicate if the button has been pressed since the last frame
        /// </summary>
        /// <param name="manager">The input manager</param>
        /// <returns></returns>
        bool IsPressed(InputManager manager);

        /// <summary>
        /// Indicate if the button has been released since the last frame
        /// </summary>
        /// <param name="manager">The input manager</param>
        /// <returns></returns>
        bool IsReleased(InputManager manager);
    }
}
