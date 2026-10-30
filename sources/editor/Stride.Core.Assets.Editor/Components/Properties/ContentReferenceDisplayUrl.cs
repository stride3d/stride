// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Assets.Editor.Components.Properties;

/// <summary>
/// The URL of a referenced asset split for the unfocused display of a reference field in the property grid: the dimmed
/// location, then the name of the asset.
/// </summary>
public static class ContentReferenceDisplayUrl
{
    /// <summary>
    /// Splits the URL of the asset referenced by the given value.
    /// </summary>
    /// <param name="value">The value of the reference node: an <see cref="IReference"/>, a proxy object of an attached reference, <see cref="NodeViewModel.DifferentValues"/>, or <c>null</c>.</param>
    /// <param name="session">The session to look for the referenced asset in.</param>
    /// <param name="differentValues">The text to show for <see cref="NodeViewModel.DifferentValues"/>.</param>
    /// <returns>
    /// The location without the trailing slash (directories, and the package root when the namespace of the asset isn't in
    /// scope for the session), and the name of the asset. The location is empty for an empty or a broken reference, whose
    /// name is the text to show.
    /// </returns>
    public static (string Location, string Name) Split(object? value, SessionViewModel session, string differentValues)
    {
        if (value is null)
            return (string.Empty, ContentReferenceHelper.EmptyReference);

        if (value == NodeViewModel.DifferentValues)
            return (string.Empty, differentValues);

        var contentReference = value as IReference ?? AttachedReferenceManager.GetAttachedReference(value);
        var asset = contentReference is not null && contentReference.Id != AssetId.Empty ? session.GetAssetById(contentReference.Id) : null;
        if (asset is null)
            return (string.Empty, ContentReferenceHelper.BrokenReference);

        // In-scope namespaces resolve by unqualified URL for the open game; display them unqualified too
        var url = asset.Url;
        if (asset.AssetItem.Package?.Container is { AssetNamespace: { } assetNamespace } container
            && session.IsAssetNamespaceInScope(assetNamespace))
        {
            url = container.Unqualify(url).FullPath;
        }

        var slash = url.LastIndexOf('/');
        return slash < 0 ? (string.Empty, url) : (url[..slash], url[(slash + 1)..]);
    }
}
