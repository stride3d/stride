// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Services;

/// <summary>
/// Constructor injection for the types a plugin creates from a context (a panel, a toolbar widget, a command handler): the
/// constructor can ask for the context, for any service of the session's scope, and takes the given arguments.
/// </summary>
internal static class ContextInjection
{
    public static T CreateInstance<T>(IViewModelServiceProvider services, object context, object[] arguments)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(arguments);
        return ActivatorUtilities.CreateInstance<T>(new WithContext(services, context), arguments);
    }

    // The scope, plus the context for a constructor that asks for it; a constructor that doesn't still fits
    private sealed class WithContext(IViewModelServiceProvider services, object context) : IServiceProvider, IServiceProviderIsService
    {
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IServiceProviderIsService))
                return this;
            return serviceType.IsInstanceOfType(context) ? context : services.GetService(serviceType);
        }

        public bool IsService(Type serviceType)
        {
            if (serviceType.IsInstanceOfType(context))
                return true;
            return services.GetService(typeof(IServiceProviderIsService)) is IServiceProviderIsService check
                ? check.IsService(serviceType)
                : services.GetService(serviceType) is not null;
        }
    }
}
