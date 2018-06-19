// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core;
using Xenko.Rendering.Compositing;

namespace Xenko.Assets.Presentation.NodePresenters.Keys
{
    public static class CameraSlotData
    {
        public const string SceneCameraSlots = nameof(SceneCameraSlots);
        public const string UpdateCameraSlotIndex = nameof(UpdateCameraSlotIndex);

        public static readonly PropertyKey<List<SceneCameraSlot>> CameraSlotsKey = new PropertyKey<List<SceneCameraSlot>>(SceneCameraSlots, typeof(CameraSlotData));
    }
}
