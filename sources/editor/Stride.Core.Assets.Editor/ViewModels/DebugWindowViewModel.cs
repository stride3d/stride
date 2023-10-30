// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

public sealed class DebugWindowViewModel : ViewModelBase
{
    public DebugWindowViewModel(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    public ObservableList<IDebugPage> Pages { get; } = [];
}
