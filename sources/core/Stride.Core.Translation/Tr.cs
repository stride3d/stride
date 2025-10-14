// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace Stride.Core.Translation;

// ReSharper disable InconsistentNaming
public static class Tr
{
    /// <inheritdoc cref="ITranslationProvider.GetString"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string _(string text)
    {
        return TranslationManager.Instance.GetString(text, Assembly.GetCallingAssembly());
    }

    /// <inheritdoc cref="ITranslationProvider.GetPluralString"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string _n(string text, string textPlural, long count)
    {
        return TranslationManager.Instance.GetPluralString(text, textPlural, count, Assembly.GetCallingAssembly());
    }

    /// <inheritdoc cref="ITranslationProvider.GetParticularString"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string _p(string context, string text)
    {
        return TranslationManager.Instance.GetParticularString(context, text, Assembly.GetCallingAssembly());
    }

    /// <inheritdoc cref="ITranslationProvider.GetParticularPluralString"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string _pn(string context, string text, string textPlural, long count)
    {
        return TranslationManager.Instance.GetParticularPluralString(context, text, textPlural, count, Assembly.GetCallingAssembly());
    }
}
