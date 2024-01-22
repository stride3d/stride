// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Windows.Markup;
using Stride.Core.Presentation.Internal;

namespace Stride.Core.Presentation.MarkupExtensions
{
    [MarkupExtensionReturnType(typeof(bool))]
    public sealed class FalseExtension : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return BooleanBoxes.FalseBox;
        }
    }
}
