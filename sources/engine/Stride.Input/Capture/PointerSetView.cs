// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace Stride.Input;

/// <summary>
///   A pointer set as the game sees it: pointers that are masked, because they or their device are captured, are left out.
/// </summary>
internal sealed class PointerSetView(DeviceCaptureState capture, Core.Collections.IReadOnlySet<PointerPoint> raw) : Core.Collections.IReadOnlySet<PointerPoint>
{
    public int Count
    {
        get
        {
            int count = 0;
            foreach (var point in raw)
            {
                if (!capture.IsPointerMasked(point.Id))
                    count++;
            }
            return count;
        }
    }

    public bool Contains(PointerPoint item) => !capture.IsPointerMasked(item.Id) && raw.Contains(item);

    public IEnumerator<PointerPoint> GetEnumerator()
    {
        foreach (var point in raw)
        {
            if (!capture.IsPointerMasked(point.Id))
                yield return point;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
