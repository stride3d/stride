// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Presentation.ViewModels;

public abstract class SessionObjectViewModel : DispatcherViewModel
{
    protected SessionObjectViewModel(ISessionViewModel session)
        : base(session.ServiceProvider)
    {
        Session = session;
    }

    public abstract string Name { get; set; }

    /// <summary>
    /// Gets the session in which this object is currently in.
    /// </summary>
    public ISessionViewModel Session { get; }
}
