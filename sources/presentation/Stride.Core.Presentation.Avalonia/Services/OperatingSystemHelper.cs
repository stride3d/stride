namespace Stride.Core.Presentation.Avalonia.Services;

public static class OperatingSystemHelper
{
    public static readonly bool IsWindows = OperatingSystem.IsWindows();
    public static readonly bool IsLinux = OperatingSystem.IsLinux();
    public static readonly bool IsMacOS = OperatingSystem.IsMacOS();
}
