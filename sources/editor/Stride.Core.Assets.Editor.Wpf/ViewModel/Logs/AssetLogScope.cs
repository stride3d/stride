// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Editor.ViewModel.Logs;

/// <summary>
/// Which assets the asset log shows the messages of.
/// </summary>
public enum AssetLogScope
{
    /// <summary>
    /// The messages of all assets.
    /// </summary>
    AllAssets,

    /// <summary>
    /// The messages of the selected assets and of the assets they use.
    /// </summary>
    SelectedAssets,
}
