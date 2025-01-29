// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace xunit.runner.stride.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanging, INotifyPropertyChanged
{
    public event PropertyChangingEventHandler? PropertyChanging;
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Sets the value of a field to the given value. Both values are compared with the default <see cref="EqualityComparer{T}"/>, and if they are equals,
    /// this method does nothing. If they are different, the <see cref="PropertyChanging"/> event will be raised first, then the field value will be modified,
    /// and finally the <see cref="PropertyChanged"/> event will be raised.
    /// </summary>
    /// <typeparam name="T">The type of the field.</typeparam>
    /// <param name="field">A reference to the field to set.</param>
    /// <param name="value">The new value to set.</param>
    /// <param name="propertyName">The name of the property that must be notified as changing/changed. Can use <see cref="CallerMemberNameAttribute"/>.</param>
    /// <returns><c>True</c> if the field was modified and events were raised, <c>False</c> if the new value was equal to the old one and nothing was done.</returns>
    protected bool SetProperty<T>([NotNullIfNotNull(nameof(value))] ref T field, T value, [CallerMemberName] string propertyName = null!)
    {
        if (EqualityComparer<T>.Default.Equals(field, value) == false)
        {
            OnPropertyChanging(propertyName);
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        return false;
    }

    /// <summary>
    /// This method will raise the <see cref="PropertyChanging"/> for the provided <paramref name="propertyName"/>.
    /// </summary>
    /// <param name="propertyName">The name of the property that is changing.</param>
    protected virtual void OnPropertyChanging(string propertyName)
    {
        PropertyChanging?.Invoke(this, new(propertyName));
    }

    /// <summary>
    /// This method will raise the <see cref="PropertyChanged"/> for the provided <paramref name="propertyName"/>.
    /// </summary>
    /// <param name="propertyName">The name of the property that has changed.</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new(propertyName));
    }
}
