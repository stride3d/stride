// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;

public static class CollectionData
{
    //public const string ReorderCollectionItem = nameof(ReorderCollectionItem);
    public const string ReadOnlyCollection = nameof(ReadOnlyCollection);
    //public static readonly PropertyKey<ReorderCollectionItemViewModel> ReorderCollectionItemKey = new(ReorderCollectionItem, typeof(CollectionData));
    public static readonly PropertyKey<bool> ReadOnlyCollectionKey = new(ReadOnlyCollection, typeof(CollectionData));
}
