// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace GameMenu
{
    /// <summary>
    /// Orbits the entity around a target, always looking at it. Space pauses and resumes the orbit.
    /// </summary>
    public class OrbitCamera : SyncScript
    {
        /// <summary>
        /// The entity to orbit around.
        /// </summary>
        public Entity Target { get; set; }

        /// <summary>
        /// The distance from the target.
        /// </summary>
        public float Distance { get; set; } = 6f;

        /// <summary>
        /// The height above the target.
        /// </summary>
        public float Height { get; set; } = 1f;

        /// <summary>
        /// The orbit speed, in degrees per second.
        /// </summary>
        public float DegreesPerSecond { get; set; } = 20f;

        /// <summary>
        /// The largest angle away from facing the target, in degrees, so a flat panel stays readable.
        /// </summary>
        public float MaximumAngle { get; set; } = 50f;

        private float time;
        private bool paused;

        public override void Update()
        {
            if (Target == null)
                return;

            if (Input.IsKeyPressed(Keys.Space))
                paused = !paused;

            if (!paused)
                time += (float)Game.UpdateTime.Elapsed.TotalSeconds;

            // Swing back and forth in front of the target instead of going all the way around, so the panel's front stays visible
            var period = 4f * MaximumAngle / DegreesPerSecond;
            var angle = MathUtil.DegreesToRadians(MaximumAngle * MathF.Sin(MathUtil.TwoPi * time / period));

            var center = Target.Transform.WorldMatrix.TranslationVector;
            var offset = new Vector3(MathF.Sin(angle) * Distance, Height, MathF.Cos(angle) * Distance);
            Entity.Transform.Position = center + offset;

            // A camera looks along -Z, so turning it by the orbit angle faces the target; pitch down to look at its centre
            var pitch = -MathF.Atan2(Height, Distance);
            Entity.Transform.Rotation = Quaternion.RotationYawPitchRoll(angle, pitch, 0f);
        }
    }
}
