// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Presentation.ViewModels;

public abstract class SessionObjectViewModel : ViewModelBase
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
