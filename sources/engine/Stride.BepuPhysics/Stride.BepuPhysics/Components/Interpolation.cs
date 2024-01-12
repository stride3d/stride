namespace Stride.BepuPhysics.Components
{
    public enum Interpolation
    {
        /// <summary>
        /// No interpolation, the body will be moved on every physics update and left alone during normal updates
        /// </summary>
        None,
        /// <summary>
        /// The body will move from the previous physics pose to the current physics pose,
        /// introducing one physics update of latency but should be very smooth
        /// </summary>
        Interpolated,
        /// <summary>
        /// The body will move from the current physics pose to a predicted one,
        /// reducing the latency but introducing imprecise or jerky motion when the pose changes significantly
        /// </summary>
        Extrapolated
    }
}