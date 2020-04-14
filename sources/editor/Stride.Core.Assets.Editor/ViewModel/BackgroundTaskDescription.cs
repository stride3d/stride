// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using System.Windows.Data;

namespace Stride.Core.Assets.Editor.ViewModel
{
    public abstract class BackgroundTaskDescription : DependencyObject, IDisposable
    {
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register("Description", typeof(string), typeof(BackgroundTaskDescription));

        public string Description { get { return (string)GetValue(DescriptionProperty); } set { SetValue(DescriptionProperty, value); } }

        public void Dispose()
        {
            BindingOperations.ClearAllBindings(this);
        }

        protected static void PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var description = (BackgroundTaskDescription)d;
            description.Description = description.UpdateDescription();
        }

        protected abstract string UpdateDescription();
    }
}
