// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Analysis;

public static class AssetPartsAnalysis
{
    /// <summary>
    /// Assigns new unique identifiers for base part <see cref="BasePart.InstanceId"/> in the given <paramref name="hierarchy"/>.
    /// </summary>
    /// <typeparam name="TAssetPartDesign"></typeparam>
    /// <typeparam name="TAssetPart">The underlying type of part.</typeparam>
    /// <param name="hierarchy">The hierarchy which part groups should have new identifiers.</param>
    public static void GenerateNewBaseInstanceIds<TAssetPartDesign, TAssetPart>(AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart> hierarchy)
        where TAssetPartDesign : class, IAssetPartDesign<TAssetPart>
        where TAssetPart : class, IIdentifiable
    {
        var baseInstanceMapping = new Dictionary<Guid, Guid>();
        foreach (var part in hierarchy.Parts.Values)
        {
            if (part.Base == null)
                continue;

            if (!baseInstanceMapping.TryGetValue(part.Base.InstanceId, out var newInstanceId))
            {
                newInstanceId = Guid.NewGuid();
                baseInstanceMapping.Add(part.Base.InstanceId, newInstanceId);
            }
            part.Base = new BasePart(part.Base.BasePartAsset, part.Base.BasePartId, newInstanceId);
        }
    }
}
