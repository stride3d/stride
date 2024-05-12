// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Presentation.ViewModels;

public abstract class SessionObjectViewModel : ViewModelBase
{
    protected SessionObjectViewModel(ISessionViewModel session)
    {
        Session = session;
    }

    public abstract string Name { get; set; }

    public ISessionViewModel Session { get; }
}
