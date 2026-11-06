// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Components.Properties
{
    /// <summary>
    /// The part of a properties panel of the session (<c>SessionObjectPropertiesViewModel</c>) that the session objects and the plugins use.
    /// </summary>
    public interface ISessionObjectPropertiesViewModel
    {
        /// <summary>
        /// Generates the properties of the given objects and displays them.
        /// </summary>
        Task<GraphViewModel> GenerateSelectionPropertiesAsync(IEnumerable<IPropertyProviderViewModel> selectedObjects);

        /// <summary>
        /// Regenerates the properties of the objects currently displayed.
        /// </summary>
        Task RefreshSelectedPropertiesAsync();

        /// <summary>
        /// Registers a node presenter updater.
        /// </summary>
        void RegisterNodePresenterUpdater(INodePresenterUpdater nodeUpdater);

        /// <summary>
        /// Unregisters a node presenter updater.
        /// </summary>
        void UnregisterNodePresenterUpdater(INodePresenterUpdater nodeUpdater);
    }
}
