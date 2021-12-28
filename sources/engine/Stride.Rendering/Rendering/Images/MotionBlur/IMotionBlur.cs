using Stride.Rendering.Images;

namespace Stride.Rendering.Rendering.Images.MotionBlur
{
    public interface IMotionBlur : IImageEffect
    {
        bool RequiresDepthBuffer { get; }
        bool RequiresVelocityBuffer { get; }
    }
}
