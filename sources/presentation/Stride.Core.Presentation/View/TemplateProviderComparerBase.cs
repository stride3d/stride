// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

namespace Stride.Core.Presentation.View
{
    /// <summary>
    /// A base class to compare instances of <see cref="ITemplateProvider"/> in order to determine which template to use when multiple template providers match the same object.
    /// </summary>
    public abstract class TemplateProviderComparerBase : IComparer<ITemplateProvider>
    {
        public int Compare(ITemplateProvider x, ITemplateProvider y)
        {
            return CompareProviders(x, y);
        }

        /// <summary>
        /// Compares two <see cref="ITemplateProvider"/> to determine which template has the greatest priority.
        /// </summary>
        /// <param name="x">The first <see cref="ITemplateProvider"/> to compare.</param>
        /// <param name="y">The second <see cref="ITemplateProvider"/> to compare.</param>
        /// <returns><c>-1</c> if <see cref="x"/> has a greater priority than <see cref="y"/>, <c>0</c> if they have the same priority, <c>1</c> if <see cref="y"/> has a greater priority than <see cref="x"/>.</returns>
        protected abstract int CompareProviders(ITemplateProvider x, ITemplateProvider y);
    }
}
