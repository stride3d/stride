using Stride.Core;

namespace Stride.Engine.Splines.Components.Models
{
    /// <summary>
    /// State control for the Spline traveller
    /// </summary>
    [DataContract]
    public enum StateControl
    {
        /// <summary>
        /// The state is active and currently playing
        /// </summary>
        Play,

        /// <summary>
        /// The state is active, but currently not playing (paused)
        /// </summary>
        Pause,

        /// <summary>
        /// The state is inactive
        /// </summary>
        Stop,
    }
}
