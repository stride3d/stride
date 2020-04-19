// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Stride.Core.Presentation.Collections
{
    /// <summary>
    /// This interface regroups the <see cref="ICollection{T}"/> interface, the
    /// <see cref="INotifyPropertyChanged"/> interface, and the <see cref="INotifyCollectionChanged"/>
    /// interface. It has no additional members.
    /// </summary>
    /// <typeparam name="T">The type of item contained in the collection.</typeparam>
    public interface IObservableCollection<T> : ICollection<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
    }
}
