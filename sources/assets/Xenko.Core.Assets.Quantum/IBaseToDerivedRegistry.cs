// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;
using Xenko.Core.Annotations;

namespace Xenko.Core.Assets.Quantum
{
    public interface IBaseToDerivedRegistry
    {
        void RegisterBaseToDerived([CanBeNull] IAssetNode baseNode, [NotNull] IAssetNode derivedNode);

        [CanBeNull]
        IIdentifiable ResolveFromBase([CanBeNull] object baseObjectReference, [NotNull] IAssetNode derivedReferencerNode);
    }
}
