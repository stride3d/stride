// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Annotations;
using Stride.Core.Quantum;

namespace Stride.Core.Presentation.Quantum.Presenters
{
    public interface INodePresenterFactory
    {
        INodePresenter CreateNodeHierarchy([NotNull] IObjectNode rootNode, [NotNull] GraphNodePath rootNodePath, [CanBeNull] IPropertyProviderViewModel propertyProvider = null);

        bool IsPrimitiveType(Type type);
    }
}
