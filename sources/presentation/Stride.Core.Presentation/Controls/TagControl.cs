// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;
using System.Windows.Controls;
using Stride.Core.Presentation.Commands;

namespace Stride.Core.Presentation.Controls
{
    public class TagControl : ContentControl
    {
        /// <summary>
        /// Identifies the <see cref="CloseTagCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CloseTagCommandProperty =
            DependencyProperty.Register("CloseTagCommand", typeof(ICommandBase), typeof(TagControl));

        public ICommandBase CloseTagCommand
        {
            get { return (ICommandBase)GetValue(CloseTagCommandProperty); }
            set { SetValue(CloseTagCommandProperty, value); }
        }

        static TagControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TagControl), new FrameworkPropertyMetadata(typeof(TagControl)));
        }
    }
}
