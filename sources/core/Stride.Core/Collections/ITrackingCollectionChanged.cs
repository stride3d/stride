using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.Core.Collections;
public interface ITrackingCollectionChanged<TValue>
{
    /// <summary>
    /// Occurs when [collection changed].
    /// </summary>
    /// Called as is when adding an item, and in reverse-order when removing an item.
    event EventHandler<TrackingCollectionChangedEventArgs<TValue>> CollectionChanged;
}
