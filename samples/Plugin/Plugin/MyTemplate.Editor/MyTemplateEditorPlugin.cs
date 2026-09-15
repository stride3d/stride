using System;
using System.IO;
using System.Linq;
using MyTemplate;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Editor;

// The component's icon in the property grid and the scene editor, an embedded resource of this assembly
[assembly: TypeImage(typeof(MyTemplateComponent), "MyTemplateComponent.png")]

namespace MyTemplate.Editor;

/// <summary>
/// The Game Studio's entry point into this package: gizmos, icons and previews declared in this assembly are found
/// through it.
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
