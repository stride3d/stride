// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace xunit.runner.stride.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanging, INotifyPropertyChanged
{
    protected readonly Dictionary<string, string[]> DependentProperties = [];

    protected bool SetValue<T>([NotNullIfNotNull(nameof(value))] ref T field, T value, [CallerMemberName] string propertyName = null!)
        => SetValue(ref field, value, null, [propertyName]);

    protected bool SetValue<T>([NotNullIfNotNull(nameof(value))] ref T field, T value, params string[] propertyNames)
        => SetValue(ref field, value, null, propertyNames);

    protected bool SetValue<T>([NotNullIfNotNull(nameof(value))] ref T field, T value, Action? updateAction, [CallerMemberName] string propertyName = null!)
        => SetValue(ref field, value, updateAction, [propertyName]);

    protected virtual bool SetValue<T>([NotNullIfNotNull(nameof(value))] ref T field, T value, Action? updateAction, params string[] propertyNames)
    {
        if (propertyNames.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(propertyNames), "This method must be invoked with at least one property name.");

        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            OnPropertyChanging(propertyNames);
            field = value;
            updateAction?.Invoke();
            OnPropertyChanged(propertyNames);
            return true;
        }

        return false;
    }

    protected bool SetValue(Action? updateAction, [CallerMemberName] string propertyName = null!)
        => SetValue(null, updateAction, [propertyName]);

    protected bool SetValue(Action? updateAction, params string[] propertyNames)
        => SetValue(null, updateAction, propertyNames);

    protected bool SetValue(Func<bool>? hasChangedFunction, Action? updateAction, [CallerMemberName] string propertyName = null!)
        => SetValue(hasChangedFunction, updateAction, [propertyName]);

    protected bool SetValue(bool hasChanged, Action? updateAction, [CallerMemberName] string propertyName = null!)
        => SetValue(() => hasChanged, updateAction, [propertyName]);

    protected bool SetValue(bool hasChanged, Action? updateAction, params string[] propertyNames)
        => SetValue(() => hasChanged, updateAction, propertyNames);

    protected virtual bool SetValue(Func<bool>? hasChangedFunction, Action? updateAction, params string[] propertyNames)
    {
        if (propertyNames.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(propertyNames), "This method must be invoked with at least one property name.");

        var hasChanged = hasChangedFunction?.Invoke() ?? true;
        if (hasChanged)
        {
            OnPropertyChanging(propertyNames);
            updateAction?.Invoke();
            OnPropertyChanged(propertyNames);
        }
        return hasChanged;
    }

    protected virtual void OnPropertyChanging(params string[] propertyNames)
    {
        var propertyChanging = PropertyChanging;
        foreach (var propertyName in propertyNames)
        {
            propertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
            if (DependentProperties.TryGetValue(propertyName, out var dependentProperties))
                OnPropertyChanging(dependentProperties);
        }
    }

    protected virtual void OnPropertyChanged(params string[] propertyNames)
    {
        var propertyChanged = PropertyChanged;
        for (var i = 0; i < propertyNames.Length; ++i)
        {
            var propertyName = propertyNames[propertyNames.Length - 1 - i];
            if (DependentProperties.TryGetValue(propertyName, out var dependentProperties))
            {
                var reverseList = new string[dependentProperties.Length];
                for (var j = 0; j < dependentProperties.Length; ++j)
                    reverseList[j] = dependentProperties[dependentProperties.Length - 1 - j];
                OnPropertyChanged(reverseList);
            }
            propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public event PropertyChangingEventHandler? PropertyChanging;
    public event PropertyChangedEventHandler? PropertyChanged;
}
