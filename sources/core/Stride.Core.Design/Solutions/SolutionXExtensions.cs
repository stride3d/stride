namespace Stride.Core.Solutions
{
    /// <summary>
    /// Helper class to encapsulate logic helpers from `.Slnx` support.
    /// </summary>
    internal static class SolutionXExtensions
    {
        /// <summary>
        /// Determines whether the specified file path is a Visual Studio XML based solution file (<c>.slnx</c>).
        /// </summary>
        public static bool IsSolutionX(this string filePath)
        {
            return string.Equals(
                Path.GetExtension(filePath),
                ".slnx",
                StringComparison.OrdinalIgnoreCase
            );
        }
    }
}
