// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Keys;

internal static class CameraSlotData
{
    public const string SceneCameraSlots = nameof(SceneCameraSlots);
    public const string UpdateCameraSlotIndex = nameof(UpdateCameraSlotIndex);

    public static readonly PropertyKey<List<SceneCameraSlot>> CameraSlotsKey = new(SceneCameraSlots, typeof(CameraSlotData));
}
