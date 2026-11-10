// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Presentation.Services;

/// <summary>
/// What the asset view models need from the preview of the editor: show an asset again after it changed (for example after a rename).
/// </summary>
/// <remarks>
/// The editor's preview service (<c>IAssetPreviewService</c>) implements it. The asset view models find it in the service provider of the
/// session, and do nothing when the editor has no preview.
/// </remarks>
public interface IAssetPreviewUpdater
{
    /// <summary>
    /// Shows <paramref name="asset"/> in the preview, or clears the preview when it is <c>null</c>.
    /// </summary>
    void SetAssetToPreview(AssetViewModel? asset);
}
