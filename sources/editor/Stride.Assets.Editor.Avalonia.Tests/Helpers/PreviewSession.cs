// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Assets.Editor.Avalonia.Tests.Helpers;

/// <summary>
/// The session of the preview view models in the view tests: they only take its service provider (for their commands),
/// so every other member returns its default value.
/// </summary>
/// <remarks>Not sealed: <see cref="DispatchProxy"/> generates a type derived from it.</remarks>
public class PreviewSession : DispatchProxy
{
    private IViewModelServiceProvider serviceProvider = null!;

    public static ISessionViewModel Create()
    {
        var session = Create<ISessionViewModel, PreviewSession>();
        ((PreviewSession)(object)session).serviceProvider = new ViewModelServiceProvider([new NullDispatcherService()]);
        return session;
    }

    /// <inheritdoc />
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod?.Name == "get_" + nameof(ISessionViewModel.ServiceProvider))
            return serviceProvider;

        return targetMethod?.ReturnType is { IsValueType: true } type && type != typeof(void) ? Activator.CreateInstance(type) : null;
    }
}
