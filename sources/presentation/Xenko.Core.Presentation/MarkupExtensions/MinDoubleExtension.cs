// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows.Markup;
using Xenko.Core.Annotations;

namespace Xenko.Core.Presentation.MarkupExtensions
{
    [MarkupExtensionReturnType(typeof(double))]
    public sealed class MinDoubleExtension : MarkupExtension
    {
        [NotNull]
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return double.MinValue;
        }
    }
}
