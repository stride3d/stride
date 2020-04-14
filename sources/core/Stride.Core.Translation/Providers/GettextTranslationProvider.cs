// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Reflection;
using GNU.Gettext;
using Stride.Core.Annotations;

namespace Stride.Core.Translation.Providers
{
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
        public GettextTranslationProvider([NotNull] Assembly assembly)
            : this(assembly.GetName().Name, assembly)
        {
        }

        /// <seealso cref="GettextResourceManager(string, Assembly)"/>
        private GettextTranslationProvider([NotNull] string baseName, [NotNull] Assembly assembly)
        {
            if (baseName == null) throw new ArgumentNullException(nameof(baseName));
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            resourceManager = new GettextResourceManager(baseName, assembly);
            BaseName = baseName;
        }

        public string BaseName { get; }

        /// <inheritdoc />
        /// <seealso cref="GettextResourceManager.GetString(string)"/>
        public string GetString(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            return resourceManager.GetString(text);
        }

        /// <inheritdoc />
        /// <seealso cref="GettextResourceManager.GetPluralString(string,string,long)"/>
        public string GetPluralString(string text, [NotNull] string textPlural, long count)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (textPlural == null) throw new ArgumentNullException(nameof(textPlural));
            return resourceManager.GetPluralString(text, textPlural, count);
        }

        /// <inheritdoc />
        /// <seealso cref="GettextResourceManager.GetParticularString(string,string)"/>
        public string GetParticularString([NotNull] string context, string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (context == null) throw new ArgumentNullException(nameof(context));
            return resourceManager.GetParticularString(context, text);
        }

        /// <inheritdoc />
        /// <seealso cref="GettextResourceManager.GetParticularPluralString(string,string,string,long)"/>
        public string GetParticularPluralString([NotNull] string context, string text, [NotNull] string textPlural, long count)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (textPlural == null) throw new ArgumentNullException(nameof(textPlural));
            if (context == null) throw new ArgumentNullException(nameof(context));
            return resourceManager.GetParticularPluralString(context, text, textPlural, count);
        }
    }
}
