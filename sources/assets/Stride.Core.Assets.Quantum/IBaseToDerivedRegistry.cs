// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Annotations;

namespace Stride.Core.Assets.Quantum
{
    public interface IBaseToDerivedRegistry
    {
        void RegisterBaseToDerived([CanBeNull] IAssetNode baseNode, [NotNull] IAssetNode derivedNode);

        [CanBeNull]
        IIdentifiable ResolveFromBase([CanBeNull] object baseObjectReference, [NotNull] IAssetNode derivedReferencerNode);
    }
}
