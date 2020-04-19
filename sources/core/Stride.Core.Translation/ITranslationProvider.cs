// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Annotations;

namespace Stride.Core.Translation
{
    public interface ITranslationProvider
    {
        [NotNull]
        string BaseName { get; }

        /// <summary>
        /// Gets the translation of <paramref name="text"/> in the current culture.
        /// </summary>
        /// <param name="text">The text to translate.</param>
        /// <returns>The translation of <paramref name="text"/> in the current culture; or <paramref name="text"/> if no translation is found.</returns>
        [NotNull]
        string GetString([NotNull] string text);

        /// <summary>
        /// Gets the translation of <paramref name="text"/> and/or <paramref name="textPlural"/> in the current culture,
        /// choosing the right plural form depending on the <paramref name="count"/>.
        /// </summary>
        /// <param name="text">The text to translate.</param>
        /// <param name="textPlural">The plural version of the text to translate.</param>
        /// <param name="count">An integer used to determine the plural form.</param>
        /// <returns>The translation of <paramref name="text"/> in the current culture; or <paramref name="text"/> if no translation is found.</returns>
        [NotNull]
        string GetPluralString([NotNull] string text, string textPlural, long count);

        /// <summary>
        /// Gets the translation of <paramref name="text"/> in the provided <paramref name="context"/> in the current culture.
        /// </summary>
        /// <param name="context">The particular context for the translation.</param>
        /// <param name="text">The text to translate.</param>
        /// <returns>The translation of <paramref name="text"/> in the current culture; or <paramref name="text"/> if no translation is found.</returns>
        [NotNull]
        string GetParticularString(string context, [NotNull] string text);

        /// <summary>
        /// Gets the translation of <paramref name="text"/> and/or <paramref name="textPlural"/> in the provided <paramref name="context"/> in the current culture,
        /// choosing the right plural form depending on the <paramref name="count"/>.
        /// </summary>
        /// <param name="context">The particular context for the translation.</param>
        /// <param name="text">The text to translate.</param>
        /// <param name="textPlural">The plural version of the text to translate.</param>
        /// <param name="count">An integer used to determine the plural form.</param>
        /// <returns>The translation of <paramref name="text"/> in the current culture; or <paramref name="text"/> if no translation is found.</returns>
        [NotNull]
        string GetParticularPluralString(string context, [NotNull] string text, string textPlural, long count);
    }
}
