// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Engine;
using Stride.Engine.Gizmos;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    [GizmoComponent(typeof(AudioListenerComponent), true)]
    public class AudioListenerGizmo : BillboardingGizmo<AudioListenerComponent>
    {
        public AudioListenerGizmo(EntityComponent component)
            : base(component, "AudioListener", GizmoResources.AudioListenerGizmo)
        {
        }
    }
}
