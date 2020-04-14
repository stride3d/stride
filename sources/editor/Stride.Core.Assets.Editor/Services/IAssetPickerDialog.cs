// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Assets.Editor.Services
{
    /// <summary>
    /// This interface represents a dialog that can pick assets from the projects.
    /// </summary>
    public interface IAssetPickerDialog : IModalDialog
    {
        /// <summary>
        /// Gets or sets the message of the asset picker.
        /// </summary>
        string Message { get; set; }
        
        /// <summary>
        /// Gets or sets the initial location selected in the asset picker.
        /// </summary>
        DirectoryBaseViewModel InitialLocation { get; set; }

        /// <summary>
        /// Gets or sets the asset that is initially selected when the asset picked is displayed.
        /// </summary>
        AssetViewModel InitialAsset { get; set; }

        /// <summary>
        /// Gets or sets whether multi-selection is allowed.
        /// </summary>
        bool AllowMultiSelection { get; set; }

        /// <summary>
        /// Gets the list of accepted asset type. If this list is empty, all types are accepted.
        /// </summary>
        List<Type> AcceptedTypes { get; }

        /// <summary>
        /// Gets or sets the filter to apply to the asset collection displayed in the asset picker.
        /// </summary>
        Func<AssetViewModel, bool> Filter { get; set; }

        /// <summary>
        /// Gets the assets selected by the user when the asset picking has been validated.
        /// </summary>
        /// <remarks>This property returns an empty enumerable if accessed before the user pressed ok or </remarks>
        IReadOnlyCollection<AssetViewModel> SelectedAssets { get; }
    }
}
