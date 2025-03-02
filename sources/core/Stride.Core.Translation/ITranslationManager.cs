// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using System.Reflection;

namespace Stride.Core.Translation;

public interface ITranslationManager : ITranslationProvider
{
    /// <summary>
    /// Gets or sets the current culture used by this Translation Manager to look up culture-specific resources at run time.
    /// </summary>
    CultureInfo CurrentLanguage { get; set; }

    event EventHandler LanguageChanged;

    /// <summary>
    /// Gets the translation of <paramref name="text"/> in the current culture.
    /// </summary>
    /// <param name="text">The text to translate.</param>
    /// <param name="assembly">The main assembly to lookup the translation.</param>
    /// <returns>The translation of <paramref name="text"/> in the current culture; or <paramref name="text"/> if no translation is found.</returns>
    string GetString(string text, Assembly assembly);

    /// <summary>
    /// Gets the translation of <paramref name="text"/> and/or <paramref name="textPlural"/> in the current culture,
    /// choosing the right plural form depending on the <paramref name="count"/>.
    /// </summary>
    /// <param name="text">The text to translate.</param>
    /// <param name="textPlural">The plural version of the text to translate.</param>
    /// <param name="count">An integer used to determine the plural form.</param>
    /// <param name="assembly">The main assembly to lookup the translation.</param>
    /// <returns>The translation of <paramref name="text"/> in the current culture; or <paramref name="text"/> if no translation is found.</returns>
    string GetPluralString(string text, string textPlural, long count, Assembly assembly);

    /// <summary>
    /// Gets the translation of <paramref name="text"/> in the provided <paramref name="context"/> in the current culture.
    /// </summary>
    /// <param name="context">The particular context for the translation.</param>
    /// <param name="text">The text to translate.</param>
    /// <param name="assembly">The main assembly to lookup the translation.</param>
    /// <returns>The translation of <paramref name="text"/> in the current culture; or <paramref name="text"/> if no translation is found.</returns>
    string GetParticularString(string context, string text, Assembly assembly);

    /// <summary>
    /// Gets the translation of <paramref name="text"/> and/or <paramref name="textPlural"/> in the provided <paramref name="context"/> in the current culture,
    /// choosing the right plural form depending on the <paramref name="count"/>.
    /// </summary>
    /// <param name="context">The particular context for the translation.</param>
    /// <param name="text">The text to translate.</param>
    /// <param name="textPlural">The plural version of the text to translate.</param>
    /// <param name="count">An integer used to determine the plural form.</param>
    /// <param name="assembly">The main assembly to lookup the translation.</param>
    /// <returns>The translation of <paramref name="text"/> in the current culture; or <paramref name="text"/> if no translation is found.</returns>
    string GetParticularPluralString(string context, string text, string textPlural, long count, Assembly assembly);

    void RegisterProvider(ITranslationProvider provider);
}
