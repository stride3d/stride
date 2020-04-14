// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows;
using Stride.Core.Annotations;

// http://www.thomaslevesque.com/2011/03/21/wpf-how-to-bind-to-data-when-the-datacontext-is-not-inherited/

namespace Stride.Core.Presentation.Core
{
    /// <summary>
    /// A class that serves as a proxy for data binding. As a freezable, its <see cref="Data"/> dependency property can inherit data context from a container <see cref="DependencyObject"/>.
    /// </summary>
    public class BindingProxy : Freezable
    {
        /// <summary>
        /// Identifies the <see cref="Data"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy));

        /// <summary>
        /// Gets or sets the data contained in this <see cref="BindingProxy"/>.
        /// </summary>
        public object Data { get { return GetValue(DataProperty); } set { SetValue(DataProperty, value); } }

        /// <inheritdoc/>
        [NotNull]
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }
    }
}
