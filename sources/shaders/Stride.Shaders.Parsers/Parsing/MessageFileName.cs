namespace Stride.Shaders.Parsing;

// Also compiled into Stride.Shaders.Compilers (linked file), so that both name files the same way in their messages.
internal static class MessageFileName
{
    /// <summary>
    /// The file as a message shows it: a file on disk with the separators of the system, so that it can be copied and opened;
    /// a virtual path, such as one of the asset database, as it is.
    /// </summary>
    public static string? Of(string? filename)
        => filename != null && Path.IsPathFullyQualified(filename) ? filename.Replace('/', Path.DirectorySeparatorChar) : filename;
}
