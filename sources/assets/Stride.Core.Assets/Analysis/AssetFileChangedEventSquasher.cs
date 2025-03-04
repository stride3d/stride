// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Analysis;

/// <summary>
/// Used to squash a list of <see cref="AssetFileChangedEvent"/>.
/// </summary>
internal class AssetFileChangedEventSquasher
{
    private static readonly ComparerPackageAndLocation ComparerPackageAndLocationInstance = new();
    private readonly Dictionary<AssetFileChangedEvent, AssetFileChangedEvent> filteredAssetFileChangedEvents = new(ComparerPackageAndLocationInstance);

    /// <summary>
    /// Squashes the list of events and returned a compact form of it. This method guaranty that for a specific file, there will be only a single event.
    /// So for example, if there is a Added + Changed + Deleted event, there will be only a Deleted event in final.
    /// </summary>
    /// <param name="currentAssetFileChangedEvents">The current asset file changed events.</param>
    /// <returns>An enumeration of events.</returns>
    public IEnumerable<AssetFileChangedEvent> Squash(List<AssetFileChangedEvent> currentAssetFileChangedEvents)
    {
        if (currentAssetFileChangedEvents.Count == 0)
            return [];

        // Compute the list of AssetFileChangedEvent in reverse order
        // and squash them per Package/AssetLocation.
        // The original list of currentAssetFileChangedEvents is not squashed
        // so it means that AssetFileChangedEvent.ChangeType in this list are single flags (e.g. AssetFileChangedType.Added)
        // Here we are squashing individual events into a single one for the same URL.
        // Though there are few cases:
        // - If the new event is Added or Deleted, than It will completely replace previous change types
        // - If the new event is Added, it will keep previous AssetFileChangedType.SourceXXX
        filteredAssetFileChangedEvents.Clear();
        var eventsCopy = new List<AssetFileChangedEvent>();
        foreach (var currentAssetEvent in currentAssetFileChangedEvents)
        {
            if (filteredAssetFileChangedEvents.TryGetValue(currentAssetEvent, out var previousEvent))
            {
                var sourceEventTypes = previousEvent.ChangeType & AssetFileChangedType.SourceEventMask;
                // If new event is added or deleted, then it replace completely previous
                // squash
                if (currentAssetEvent.ChangeType == AssetFileChangedType.Added ||
                    currentAssetEvent.ChangeType == AssetFileChangedType.Deleted)
                {
                    previousEvent.ChangeType = currentAssetEvent.ChangeType;

                    // Force source events, we keep them in case of Added
                    if (currentAssetEvent.ChangeType == AssetFileChangedType.Added)
                    {
                        previousEvent.ChangeType |= sourceEventTypes;
                    }
                }
                else
                {
                    // In case of a SourceDeleted event, delete previous (SourceAdded  event if any)
                    if (currentAssetEvent.ChangeType == AssetFileChangedType.SourceDeleted)
                    {
                        previousEvent.ChangeType &= ~AssetFileChangedType.SourceEventMask;
                    }

                    // Else we can merge the event into a single one
                    previousEvent.ChangeType |= currentAssetEvent.ChangeType;
                }
            }
            else
            {
                eventsCopy.Add(currentAssetEvent);
                filteredAssetFileChangedEvents.Add(currentAssetEvent, currentAssetEvent);
            }

        }
        filteredAssetFileChangedEvents.Clear();

        return eventsCopy;
    }

    private class ComparerPackageAndLocation : IEqualityComparer<AssetFileChangedEvent>
    {
        public bool Equals(AssetFileChangedEvent? x, AssetFileChangedEvent? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;

            return x.Package == y?.Package && x.AssetLocation == y.AssetLocation;
        }

        public int GetHashCode(AssetFileChangedEvent obj)
        {
            ArgumentNullException.ThrowIfNull(obj);
            return HashCode.Combine(obj.Package, obj.AssetLocation);
        }
    }
}
