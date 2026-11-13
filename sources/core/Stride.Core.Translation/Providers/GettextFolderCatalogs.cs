// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;
using GNU.Gettext;

namespace Stride.Core.Translation.Providers;

/// <summary>
/// The Gettext catalogs of an assembly loaded from memory, read from the folder of its file: <see cref="GettextResourceManager"/>
/// looks for the satellites (<c>&lt;culture&gt;/&lt;name&gt;.Messages.resources.dll</c>) next to <see cref="Assembly.Location"/>,
/// which is empty for such an assembly.
/// </summary>
/// <remarks>
/// It looks a string up as <see cref="GettextResourceManager"/> does: in the catalog of the culture, then of its parents, and
/// falls back to the text itself.
/// </remarks>
internal sealed class GettextFolderCatalogs
{
    private readonly string baseName;
    private readonly Assembly assembly;
    private readonly string directory;
    private readonly ConcurrentDictionary<string, GettextResourceSet?> catalogs = new(StringComparer.OrdinalIgnoreCase);

    public GettextFolderCatalogs(string baseName, Assembly assembly, string directory)
    {
        this.baseName = baseName;
        this.assembly = assembly;
        this.directory = directory;
    }

    public string GetString(string text)
    {
        foreach (var catalog in CatalogsFor(CultureInfo.CurrentUICulture))
        {
            var translation = catalog.GetString(text);
            if (!string.IsNullOrEmpty(translation))
                return translation;
        }
        return text;
    }

    public string GetPluralString(string text, string textPlural, long count)
    {
        foreach (var catalog in CatalogsFor(CultureInfo.CurrentUICulture))
        {
            var translation = catalog.GetPluralString(text, textPlural, count);
            if (!string.IsNullOrEmpty(translation))
                return translation;
        }
        return count == 1 ? text : textPlural;
    }

    public string GetParticularString(string context, string text)
    {
        var contextText = GettextResourceManager.MakeContextMsgid(context, text);
        foreach (var catalog in CatalogsFor(CultureInfo.CurrentUICulture))
        {
            var translation = catalog.GetString(contextText);
            if (!string.IsNullOrEmpty(translation))
                return translation;
        }
        return text;
    }

    public string GetParticularPluralString(string context, string text, string textPlural, long count)
    {
        var contextText = GettextResourceManager.MakeContextMsgid(context, text);
        foreach (var catalog in CatalogsFor(CultureInfo.CurrentUICulture))
        {
            var translation = catalog.GetPluralString(contextText, textPlural, count);
            if (!string.IsNullOrEmpty(translation) && translation != contextText)
                return translation;
        }
        return count == 1 ? text : textPlural;
    }

    private IEnumerable<GettextResourceSet> CatalogsFor(CultureInfo culture)
    {
        for (var current = culture; !current.Equals(CultureInfo.InvariantCulture); current = current.Parent)
        {
            if (catalogs.GetOrAdd(current.Name, _ => Load(current)) is { } catalog)
                yield return catalog;
        }
    }

    private GettextResourceSet? Load(CultureInfo culture)
    {
        var path = Path.Combine(directory, culture.Name, GettextResourceManager.GetSatelliteAssemblyName(baseName));
        if (!File.Exists(path))
            return null;

        // Read in the load context of the assembly and from memory, as the assembly: its file stays free
        var context = AssemblyLoadContext.GetLoadContext(assembly) ?? AssemblyLoadContext.Default;
        using var stream = File.OpenRead(path);
        var satellite = context.LoadFromStream(stream);
        var className = GettextResourceManager.MakeResourceSetClassName(baseName, culture);
        var type = satellite.GetTypes().FirstOrDefault(x => x.Name == className && typeof(GettextResourceSet).IsAssignableFrom(x));
        return type is not null ? (GettextResourceSet?)Activator.CreateInstance(type) : null;
    }
}
