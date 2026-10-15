using System;
using System.IO;
using System.Linq;
using Stride.Editor;

namespace MyTemplate.Editor;

/// <summary>
/// The Game Studio's entry point into this package: gizmos and previews declared in this assembly are found through it.
/// </summary>
public sealed class MyTemplateEditorPlugin : StrideAssetsPlugin
{
    /// <summary>
    /// Reads an embedded resource of this assembly by file name.
    /// </summary>
    internal static byte[] LoadResource(string fileName)
    {
        var assembly = typeof(MyTemplateEditorPlugin).Assembly;
        var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(x => x.EndsWith("." + fileName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded resource [{fileName}] not found in {assembly.GetName().Name}.");
        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
