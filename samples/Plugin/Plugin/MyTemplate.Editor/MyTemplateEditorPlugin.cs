using System;
using System.IO;
using System.Linq;
using Stride.Core.Assets.Editor.Services;
using Stride.Editor;

namespace MyTemplate.Editor;

/// <summary>
/// The Game Studio's entry point into this package: gizmos and previews declared in this assembly are found through it.
/// </summary>
public sealed class MyTemplateEditorPlugin : StrideAssetsPlugin
{
    /// <inheritdoc />
    public override void Initialize(IEditorPluginContext context)
    {
        base.Initialize(context);

        // Register here what the editor gets from this plugin besides what the assembly declares with attributes: images
        // (context.TypeImages, context.EnumImages), primitive types, copy and paste processors, profile settings, ...
        // The editor removes it all when the plugin leaves (its assembly is reloaded or the session closes).
    }

    /// <inheritdoc />
    public override void InitializeSession(ISessionPluginContext context)
    {
        base.InitializeSession(context);

        // Register here the services, property grid commands and updaters of the session (context.Services,
        // context.NodePresenterCommands, context.NodePresenterUpdaters), and with context.OnClose what to clean up.
    }

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
