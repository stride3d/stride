// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Graphics
{
    /// <summary>
    /// Defines flags that describe the relationship between the adapter refresh rate and the rate at which Present operations are completed.
    /// </summary>
    public enum PresentInterval
    {
        // NOTE: Values should not be changed, as they are directly used by Present method (0: no frame to wait, 1: one frame...etc.)

        /// <summary>
        /// The runtime updates the window client area immediately, and might do so more than once during the adapter refresh period. Present operations might be affected immediately. This option is always available for both windowed and full-screen swap chains.
        /// </summary>
        Immediate = 0,

        /// <summary>
        /// Equivalent to setting One.
        /// </summary>
        Default = One,

        /// <summary>
        /// The driver waits for the vertical retrace period (the runtime will beam trace to prevent tearing). Present operations are not affected more frequently than the screen refresh rate; the runtime completes one Present operation per adapter refresh period, at most. This option is always available for both windowed and full-screen swap chains.
        /// </summary>
        One = 1,
        /// <summary>
        /// The driver waits for the vertical retrace period. Present operations are not affected more frequently than every second screen refresh.
        /// </summary>
        Two = 2,
    }
}
