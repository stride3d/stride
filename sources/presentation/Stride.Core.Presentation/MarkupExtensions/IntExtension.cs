// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows.Markup;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.MarkupExtensions
{
    [MarkupExtensionReturnType(typeof(int))]
    public class IntExtension : MarkupExtension
    {
        public int Value { get; set; }

        public IntExtension(object value)
        {
            Value = Convert.ToInt32(value);
        }

        [NotNull]
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Value;
        }
    }
}
