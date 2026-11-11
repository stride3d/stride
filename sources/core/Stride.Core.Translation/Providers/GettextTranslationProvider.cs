// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using GNU.Gettext;

namespace Stride.Core.Translation.Providers;

/// <summary>
/// Translation provider using the Gettext library.
/// </summary>
public sealed class GettextTranslationProvider : ITranslationProvider
{
    private readonly GettextResourceManager resourceManager;
    private readonly GettextFolderCatalogs? folderCatalogs;

    /// <seealso cref="GettextResourceManager()"/>
    public GettextTranslationProvider()
        : this(Assembly.GetCallingAssembly())
    {
    }

    /// <seealso cref="GettextResourceManager(Assembly)"/>
    public GettextTranslationProvider(Assembly assembly)
        : this(assembly.GetName().Name!, assembly)
    {
    }

    /// <summary>
    /// Creates a provider for an assembly that may have been loaded from memory (a plugin): its catalogs are then read from the
    /// folder of <paramref name="assemblyPath"/>, the file it was loaded from.
    /// </summary>
    /// <param name="assembly">The assembly whose strings are translated.</param>
    /// <param name="assemblyPath">The file the assembly was loaded from, or <c>null</c> if unknown. Used only when the assembly
    /// has no <see cref="Assembly.Location"/>.</param>
    public GettextTranslationProvider(Assembly assembly, string? assemblyPath)
        : this(assembly.GetName().Name!, assembly)
    {
        if (string.IsNullOrEmpty(assembly.Location) && Path.GetDirectoryName(assemblyPath) is { Length: > 0 } directory)
            folderCatalogs = new GettextFolderCatalogs(BaseName, assembly, directory);
    }

    /// <seealso cref="GettextResourceManager(string, Assembly)"/>
    private GettextTranslationProvider(string baseName, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(baseName);
        ArgumentNullException.ThrowIfNull(assembly);
        resourceManager = new GettextResourceManager(baseName, assembly);
        BaseName = baseName;
    }

    public string BaseName { get; }

    /// <inheritdoc />
    /// <seealso cref="GettextResourceManager.GetString(string)"/>
    public string GetString(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return folderCatalogs?.GetString(text) ?? resourceManager.GetString(text);
    }

    /// <inheritdoc />
    /// <seealso cref="GettextResourceManager.GetPluralString(string,string,long)"/>
    public string GetPluralString(string text, string textPlural, long count)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(textPlural);
        return folderCatalogs?.GetPluralString(text, textPlural, count) ?? resourceManager.GetPluralString(text, textPlural, count);
    }

    /// <inheritdoc />
    /// <seealso cref="GettextResourceManager.GetParticularString(string,string)"/>
    public string GetParticularString(string context, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(context);
        return folderCatalogs?.GetParticularString(context, text) ?? resourceManager.GetParticularString(context, text);
    }

    /// <inheritdoc />
    /// <seealso cref="GettextResourceManager.GetParticularPluralString(string,string,string,long)"/>
    public string GetParticularPluralString(string context, string text, string textPlural, long count)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(textPlural);
        ArgumentNullException.ThrowIfNull(context);
        return folderCatalogs?.GetParticularPluralString(context, text, textPlural, count) ?? resourceManager.GetParticularPluralString(context, text, textPlural, count);
    }
}
