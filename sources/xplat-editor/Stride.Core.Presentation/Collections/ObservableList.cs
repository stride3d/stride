// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Collections;

public class ObservableList<T> : ObservableCollection<T>, IObservableList<T>, IReadOnlyObservableList<T>
{
    [CollectionAccess(CollectionAccessType.UpdatedContent)]
    public void AddRange(IEnumerable<T> items)
    {
        var index = Count;
        foreach (var item in items)
        {
            base.InsertItem(index++, item);
        }
    }

    /// <inheritdoc/>
    [CollectionAccess(CollectionAccessType.None)]
    public override string ToString()
    {
        return $"{{ObservableList}} Count = {Count}";
    }
}
