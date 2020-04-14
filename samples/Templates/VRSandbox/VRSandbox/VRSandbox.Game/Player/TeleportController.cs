// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Engine;
using Stride.Particles.Components;
using Stride.Particles.Initializers;
using Stride.VirtualReality;

namespace Player
{
    public class TeleportController : SyncScript
    {
        // Declared public member fields and properties will show in the game studio
        public Entity Player { get; set; }

        public Entity Pointer { get; set; }

        public Entity Target { get; set; }

        public ParticleSystemComponent Arc { get; set; }

        public RectangleF Constraints { get; set; }

        private VRDeviceSystem vrDeviceSystem;

        private bool wasOn = false;

        public override void Start()
        {
            base.Start();

            vrDeviceSystem = Services.GetService<VRDeviceSystem>();
        }

        public override void Update()
        {
            // We need a valid controller, otherwise ignore teleport effects
            var vrController = vrDeviceSystem.Device?.RightHand;

            if (vrController == null) return;

            if (vrController.State == DeviceState.Invalid) return;

            // Do stuff every new frame
            if (Pointer == null || Target == null)
                return;

            Vector3 position = Pointer.Transform.WorldMatrix.TranslationVector;
            var arcHeightBias = (position.Y > 0) ? position.Y : 0;
            // Shorten the pointer
            if (position.Y < 0)
            {
                var distanceToTarget = Entity.Transform.WorldMatrix.TranslationVector - position;
                var coefficient = -position.Y / distanceToTarget.Y;
                position.X += distanceToTarget.X * coefficient;
                position.Z += distanceToTarget.Z * coefficient;
            }

            position.Y = 0.1f;
            position.X = Math.Min(Math.Max(Constraints.Left, position.X), Constraints.Right);
            position.Z = Math.Min(Math.Max(Constraints.Top, position.Z), Constraints.Bottom);
            Target.Transform.Position = position;

            if (Arc == null)
                return;

            var arcInitializer = Arc.ParticleSystem?.Emitters[0].Initializers[2] as InitialPositionArc;
            if (arcInitializer == null)
                return;

            var distance = arcHeightBias + (Arc.Entity.Transform.WorldMatrix.TranslationVector - position).Length();
            arcInitializer.ArcHeight = distance * 0.25f;

            // Turn the arc on and off
            var isOn = (vrController.IsPressed(TouchControllerButton.A) || (vrController.IsPressed(TouchControllerButton.Thumbstick)));
            Arc.Enabled = isOn;
            Target.Get<ModelComponent>().Enabled = isOn;

            // Teleport
            if ((wasOn && !isOn) && (Player != null))
            {
                Player.Transform.Position.X = position.X;
                Player.Transform.Position.Z = position.Z;
            }
            wasOn = isOn;
        }
    }
}
