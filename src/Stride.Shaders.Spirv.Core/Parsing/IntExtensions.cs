namespace Stride.Shaders.Spirv.Core.Parsing;

public static class IntExtensions
{
    public static bool HasEndString(this int i)
    {
        return
            (char)(i >> 24) == '\0'
            || (char)(i >> 16 & 0XFF) == '\0'
            || (char)(i >> 8 & 0xFF) == '\0'
            || (char)(i & 0xFF) == '\0';
    }
}