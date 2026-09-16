// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Stride.Core.Translation;

public static class TranslationManager
{
    private static readonly Lazy<ITranslationManager> Lazy = new(() => new TranslationManagerImpl());

    /// <summary>
    /// Gets the instance of the <see cref="ITranslationManager"/>.
    /// </summary>
    public static ITranslationManager Instance => Lazy.Value;

    /// <summary>
    /// Implementation of <see cref="ITranslationManager"/>.
    /// </summary>
    private sealed class TranslationManagerImpl : ITranslationManager
    {
        private readonly Dictionary<string, ITranslationProvider> translationProviders = [];

        /// <inheritdoc />
        public CultureInfo CurrentLanguage
        {
            get;
            set
            {
                if (value is null || Equals(field, value))
                    return;

                field = CultureInfo.CurrentUICulture = CultureInfo.DefaultThreadCurrentUICulture = value;
                OnLanguageChanged();
            }
        } = CultureInfo.CurrentUICulture;

        /// <inheritdoc />
        public event EventHandler? LanguageChanged;

        /// <inheritdoc />
        string ITranslationProvider.BaseName => nameof(TranslationManager);

        /// <inheritdoc />
        public string GetString(string text)
        {
            EnsureUICulture();
            return GetString(text, Assembly.GetCallingAssembly());
        }

        /// <inheritdoc />
        public string GetString(string text, Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            EnsureUICulture();
            return GetProvider(assembly)?.GetString(text) ?? text;
        }

        /// <inheritdoc />
        public string GetPluralString(string text, string textPlural, long count)
        {
            EnsureUICulture();
            return GetPluralString(text, textPlural, count, Assembly.GetCallingAssembly());
        }

        /// <inheritdoc />
        public string GetPluralString(string text, string textPlural, long count, Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            EnsureUICulture();
            return GetProvider(assembly)?.GetPluralString(text, textPlural, count) ?? text;
        }

        /// <inheritdoc />
        public string GetParticularString(string context, string text)
        {
            EnsureUICulture();
            return GetParticularString(context, text, Assembly.GetCallingAssembly());
        }

        /// <inheritdoc />
        public string GetParticularString(string context, string text, Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            EnsureUICulture();
            return GetProvider(assembly)?.GetParticularString(context, text) ?? text;
        }

        /// <inheritdoc />
        public string GetParticularPluralString(string context, string text, string textPlural, long count)
        {
            EnsureUICulture();
            return GetParticularPluralString(context, text, textPlural, count, Assembly.GetCallingAssembly());
        }

        /// <inheritdoc />
        public string GetParticularPluralString(string context, string text, string textPlural, long count, Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);
            EnsureUICulture();
            return GetProvider(assembly)?.GetParticularPluralString(context, text, textPlural, count) ?? text;
        }

        /// <inheritdoc />
        public void RegisterProvider(ITranslationProvider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);
            translationProviders.Add(provider.BaseName, provider);
        }

        private ITranslationProvider? GetProvider(Assembly assembly)
        {
            translationProviders.TryGetValue(assembly.GetName().Name!, out var provider);
            return provider;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureUICulture()
        {
#if DEBUG
            if (CultureInfo.CurrentUICulture != CurrentLanguage)
                System.Diagnostics.Debugger.Break();
#endif // DEBUG
            CultureInfo.CurrentUICulture = CurrentLanguage;
        }

        private void OnLanguageChanged()
        {
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
