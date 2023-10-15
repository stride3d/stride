// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using CommunityToolkit.Mvvm.ComponentModel;

namespace Stride.Core.Presentation.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    protected ViewModelBase()
    {
        ServiceProvider = ViewModelServiceProvider.NullServiceProvider;
    }

    protected ViewModelBase(IViewModelServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    /// <summary>
    /// An <see cref="IViewModelServiceProvider"/> that allows to retrieve various service objects.
    /// </summary>
    public IViewModelServiceProvider ServiceProvider { get; init; }
}
