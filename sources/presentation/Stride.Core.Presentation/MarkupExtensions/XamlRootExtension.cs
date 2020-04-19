// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Xaml;
using System.Windows.Markup;

namespace Stride.Core.Presentation.MarkupExtensions
{
    /// <summary>
    /// Finds and returns the root object of the current XAML document.
    /// </summary>
    public sealed class XamlRootExtension : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var provider = serviceProvider.GetService(typeof(IRootObjectProvider)) as IRootObjectProvider;
            return provider?.RootObject;
        }
    }
}
