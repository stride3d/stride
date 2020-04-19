// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Reflection;
using System.Runtime.CompilerServices;
using Stride.Core.Annotations;

namespace Stride.Core.Translation
{
    // ReSharper disable InconsistentNaming
    public static class Tr
    {
        /// <inheritdoc cref="ITranslationProvider.GetString"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NotNull]
        public static string _([NotNull] string text)
        {
            return TranslationManager.Instance.GetString(text, Assembly.GetCallingAssembly());
        }

        /// <inheritdoc cref="ITranslationProvider.GetPluralString"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NotNull]
        public static string _n([NotNull] string text, string textPlural, long count)
        {
            return TranslationManager.Instance.GetPluralString(text, textPlural, count, Assembly.GetCallingAssembly());
        }

        /// <inheritdoc cref="ITranslationProvider.GetParticularString"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NotNull]
        public static string _p(string context, [NotNull] string text)
        {
            return TranslationManager.Instance.GetParticularString(context, text, Assembly.GetCallingAssembly());
        }

        /// <inheritdoc cref="ITranslationProvider.GetParticularPluralString"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining), NotNull]
        public static string _pn(string context, [NotNull] string text, string textPlural, long count)
        {
            return TranslationManager.Instance.GetParticularPluralString(context, text, textPlural, count, Assembly.GetCallingAssembly());
        }
    }
}
