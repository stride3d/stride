// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Assets.Editor.Quantum.ViewModels
{
    /// <summary>
    /// Interface representing protected properties of <see cref="IAssetNodeViewModel"/> instances.
    /// </summary>
    /// <remarks>
    /// This interface is purely internal and exists only because implementations of <see cref="IAssetNodeViewModel"/>
    /// all inherit from generic classes and do not have a common non-generic base.
    /// </remarks>
    internal interface IInternalAssetNodeViewModel
    {
        void ChildOverrideChanging();
        void ChildOverrideChanged();
    }
}
