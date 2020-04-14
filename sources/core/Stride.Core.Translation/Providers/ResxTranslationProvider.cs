// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Reflection;
using System.Resources;
using Stride.Core.Annotations;

namespace Stride.Core.Translation.Providers
{
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

        public ResxTranslationProvider([NotNull] Assembly assembly)
            : this(assembly.GetName().Name, assembly)
        {
        }

        /// <seealso cref="ResourceManager(string, Assembly)"/>
        private ResxTranslationProvider([NotNull] string baseName, [NotNull] Assembly assembly)
        {
            if (baseName == null) throw new ArgumentNullException(nameof(baseName));
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            resourceManager = new ResourceManager(baseName, assembly);
            BaseName = baseName;
        }

        /// <inheritdoc />
        public string BaseName { get; }

        /// <inheritdoc />
        public string GetString(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            return resourceManager.GetString(text) ?? text;
        }

        /// <inheritdoc />
        /// <remarks>
        /// This method is not supported by this provider and will fallback to returing the translation of <paramref name="text"/>
        /// or <paramref name="textPlural"/> using the English rule for plurals (<paramref name="count"/> &gt; 1).
        /// </remarks>
        public string GetPluralString(string text, string textPlural, long count)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            // Note: plurals not supported by ResourceManager, fallback to the text or textPlural using English rule for plurals
            return (count > 1 ? resourceManager.GetString(textPlural) : resourceManager.GetString(text)) ?? text;
        }

        /// <inheritdoc />
        /// <remarks>
        /// This method is not supported by this provider and will fallback to returing the translation of <paramref name="text"/>.
        /// The context is ignored.
        /// </remarks>
        public string GetParticularString(string context, string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            // Note: context not supported by ResourceManager, fallback to the text
            return resourceManager.GetString(text) ?? text;
        }

        /// <inheritdoc />
        /// <remarks>
        /// This method is not supported by this provider and will fallback to returing the translation of <paramref name="text"/>
        /// or <paramref name="textPlural"/> using the English rule for plurals (<paramref name="count"/> &gt; 1).
        /// The context is ignored.
        /// </remarks>
        public string GetParticularPluralString(string context, string text, string textPlural, long count)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            // Note: context and plurals not supported by ResourceManager, fallback to the text or textPlural using English rule for plurals
            return (count > 1 ? resourceManager.GetString(textPlural) : resourceManager.GetString(text)) ?? text;
        }
    }
}
