// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters
{
    public interface IAssetNodePresenter : INodePresenter
    {
        new IAssetNodePresenter this[string childName] { get; }

        bool HasBase { get; }

        bool IsInherited { get; }

        bool IsOverridden { get; }

        AssetViewModel Asset { get; }

        new AssetNodePresenterFactory Factory { get; }

        event EventHandler<EventArgs> OverrideChanging;

        event EventHandler<EventArgs> OverrideChanged;

        /// <summary>
        /// Indicates if the given value, when assigned to this node, would be an object reference.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <returns>True if the given value is an object reference, False otherwise.</returns>
        bool IsObjectReference(object value);

        void ResetOverride();
    }
}
