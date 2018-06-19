// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Annotations;

namespace Xenko.Core.Presentation.View
{
    /// <summary>
    /// A default implementation of the <see cref="TemplateProviderComparerBase"/> class that compares <see cref="ITemplateProvider"/> instances by name.
    /// </summary>
    public class DefaultTemplateProviderComparer : TemplateProviderComparerBase
    {
        protected override int CompareProviders([NotNull] ITemplateProvider x, [NotNull] ITemplateProvider y)
        {
            return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
        }
    }
}
