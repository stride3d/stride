namespace Stride.Shaders.Spirv.Core;

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
    public static int LengthOfString(this Span<int> ints)
    {
        for (int i = 0; i < ints.Length; i++)
        {
            if (ints[i].HasEndString())
                return i + 1;
        }
        return ints.Length;
    }
    public static int GetWordCount(this string s)
    {
        var length = System.Text.Encoding.UTF8.GetByteCount(s) + 1; // +1 for the null terminator
        return (length + 3) / 4;
    }
}
