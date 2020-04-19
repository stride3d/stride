// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows.Input;
using System.Windows.Markup;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.MarkupExtensions
{
    /// <summary>
    /// This markup extension allows to create a <see cref="Key"/> instance from a string representing the key.
    /// </summary>
    public class KeyExtension : MarkupExtension
    {
        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        public Key Key { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyExtension"/> class with a string representing the key.
        /// </summary>
        /// <param name="key">A string representing the key.</param>
        public KeyExtension([NotNull] string key)
        {
            Key = (Key)Enum.Parse(typeof(Key), key, true);
        }

        /// <inheritdoc/>
        [NotNull]
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Key;
        }
    }
}
