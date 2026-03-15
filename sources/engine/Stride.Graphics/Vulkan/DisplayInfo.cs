using Stride.Core.Mathematics;
using Vortice.Vulkan;

namespace Stride.Graphics
{
    public record DisplayInfo(
        uint DisplayId,
        nint Handle,
        string DisplayName,
        Rectangle Bounds,
        DisplayMode? CurrentMode,
        DisplayMode[] SupportedModes,
        VkSurfaceFormatKHR[] SupportedFormats);
}
