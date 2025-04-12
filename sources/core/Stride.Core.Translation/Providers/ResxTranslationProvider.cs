// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using System.Resources;

namespace Stride.Core.Translation.Providers;

/// <summary>
/// Translation provider using the standard Resource Manager.
/// </summary>
public sealed class ResxTranslationProvider : ITranslationProvider
{
    private readonly ResourceManager resourceManager;

    public ResxTranslationProvider()
        : this(Assembly.GetCallingAssembly())
    {
    }

    public ResxTranslationProvider(Assembly assembly)
        : this(assembly.GetName().Name!, assembly)
    {
    }

    /// <seealso cref="ResourceManager(string, Assembly)"/>
    private ResxTranslationProvider(string baseName, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(baseName);
        ArgumentNullException.ThrowIfNull(assembly);
        resourceManager = new ResourceManager(baseName, assembly);
        BaseName = baseName;
    }

    /// <inheritdoc />
    public string BaseName { get; }

    /// <inheritdoc />
    public string GetString(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return resourceManager.GetString(text) ?? text;
    }

    /// <inheritdoc />
    /// <remarks>
    /// This method is not supported by this provider and will fallback to returing the translation of <paramref name="text"/>
    /// or <paramref name="textPlural"/> using the English rule for plurals (<paramref name="count"/> &gt; 1).
    /// </remarks>
    public string GetPluralString(string text, string textPlural, long count)
    {
        ArgumentNullException.ThrowIfNull(text);
        // Note: plurals not supported by ResourceManager, fallback to the text or textPlural using English rule for plurals
        return (count > 1 ? resourceManager.GetString(textPlural) : resourceManager.GetString(text)) ?? text;
    }

    /// <inheritdoc />
    /// <remarks>
    /// This method is not supported by this provider and will fallback to returing the translation of <paramref name="text"/>.
    /// The context is ignored.
    /// </remarks>
    public string GetParticularString(string? context, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        // Note: context not supported by ResourceManager, fallback to the text
        return resourceManager.GetString(text) ?? text;
    }

    /// <inheritdoc />
    /// <remarks>
    /// This method is not supported by this provider and will fallback to returing the translation of <paramref name="text"/>
    /// or <paramref name="textPlural"/> using the English rule for plurals (<paramref name="count"/> &gt; 1).
    /// The context is ignored.
    /// </remarks>
    public string GetParticularPluralString(string? context, string text, string textPlural, long count)
    {
        ArgumentNullException.ThrowIfNull(text);
        // Note: context and plurals not supported by ResourceManager, fallback to the text or textPlural using English rule for plurals
        return (count > 1 ? resourceManager.GetString(textPlural) : resourceManager.GetString(text)) ?? text;
    }
}
