// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Quantum;

namespace Stride.Core.Presentation.Quantum.Presenters;

public interface INodePresenterFactoryInternal : INodePresenterFactory
{
    IReadOnlyCollection<INodePresenterCommand> AvailableCommands { get; }

    void CreateChildren(IInitializingNodePresenter parentPresenter, IObjectNode? objectNode, IPropertyProviderViewModel? propertyProvider);
}
