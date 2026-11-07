// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;

namespace Stride.Core.Assets.Editor;

/// <summary>
/// The languages of the editor (the <see cref="Settings.EditorSettings.Language"/> setting).
/// </summary>
public enum SupportedLanguage
{
    [Display("Default")]
    MachineDefault = 0,
    English,
    French,
    Japanese,
    Russian,
    German,
    Spanish,
    [Display("Chinese (Simplified)")]
    ChineseSimplified,
    Italian,
    Korean,
}

public static class SupportedLanguageExtensions
{
    /// <summary>
    /// The culture of a language of the editor; <see cref="SupportedLanguage.MachineDefault"/> is the language of the machine.
    /// </summary>
    public static CultureInfo GetCulture(this SupportedLanguage language)
    {
        return language switch
        {
            SupportedLanguage.MachineDefault => CultureInfo.InstalledUICulture,
            SupportedLanguage.English => new CultureInfo("en-US"),
            SupportedLanguage.French => new CultureInfo("fr-FR"),
            SupportedLanguage.Japanese => new CultureInfo("ja-JP"),
            SupportedLanguage.Russian => new CultureInfo("ru-RU"),
            SupportedLanguage.German => new CultureInfo("de-DE"),
            SupportedLanguage.Spanish => new CultureInfo("es-ES"),
            SupportedLanguage.ChineseSimplified => new CultureInfo("zh-Hans"),
            SupportedLanguage.Italian => new CultureInfo("it-IT"),
            SupportedLanguage.Korean => new CultureInfo("ko-KR"),
            _ => throw new ArgumentException("Invalid language option", nameof(language)),
        };
    }
}
