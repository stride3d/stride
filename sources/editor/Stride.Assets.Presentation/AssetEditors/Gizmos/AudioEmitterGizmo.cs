// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    [GizmoComponent(typeof(AudioEmitterComponent), true)]
    public class AudioEmitterGizmo : BillboardingGizmo<AudioEmitterComponent>
    {
        public AudioEmitterGizmo(EntityComponent component)
            : base(component, "AudioEmitter", GizmoResources.AudioEmitterGizmo)
        {
        }
    }
}
