// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Presentation.ViewModels;

/// <summary>
/// How long a service registered with a factory lives (<see cref="IViewModelServiceProvider.Register{TService}"/>).
/// </summary>
public enum ViewModelServiceLifetime
{
    /// <summary>
    /// One instance, made the first time it is asked for, with the provider it is registered in. It goes when that provider is
    /// disposed or the registration is removed.
    /// </summary>
    Singleton,

    /// <summary>
    /// One instance per scope: the provider it is registered in and each child provider (the scope of a session, of an editor)
    /// that asks for it get their own, made with that provider. It goes when that provider is disposed.
    /// </summary>
    PerScope,
}
