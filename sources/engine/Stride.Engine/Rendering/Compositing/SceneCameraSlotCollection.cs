// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

using Stride.Core;
using Stride.Core.Collections;
using Stride.Engine;

namespace Stride.Rendering.Compositing
{
    /// <summary>
    /// A collection of <see cref="CameraComponent"/>.
    /// </summary>
    [DataContract("SceneCameraSlotCollection")]
    public sealed class SceneCameraSlotCollection : FastTrackingCollection<SceneCameraSlot>
    {
        /// <summary>
        /// Property key to access the current collection of <see cref="CameraComponent"/> from <see cref="RenderContext.Tags"/>.
        /// </summary>
        public static readonly PropertyKey<SceneCameraSlotCollection> Current = new PropertyKey<SceneCameraSlotCollection>("SceneCameraSlotCollection.Current", typeof(SceneCameraSlotCollection));
    }
}
