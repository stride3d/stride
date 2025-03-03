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
        return resourceManager.GetString(text);
    }

    /// <inheritdoc />
    /// <seealso cref="GettextResourceManager.GetPluralString(string,string,long)"/>
    public string GetPluralString(string text, string textPlural, long count)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(textPlural);
        return resourceManager.GetPluralString(text, textPlural, count);
    }

    /// <inheritdoc />
    /// <seealso cref="GettextResourceManager.GetParticularString(string,string)"/>
    public string GetParticularString(string context, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(context);
        return resourceManager.GetParticularString(context, text);
    }

    /// <inheritdoc />
    /// <seealso cref="GettextResourceManager.GetParticularPluralString(string,string,string,long)"/>
    public string GetParticularPluralString(string context, string text, string textPlural, long count)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(textPlural);
        ArgumentNullException.ThrowIfNull(context);
        return resourceManager.GetParticularPluralString(context, text, textPlural, count);
    }
}
