// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Engine;
using FirstPersonShooter.Building.Defenses; // For TurretTargetingSystem, TurretWeaponSystem
using FirstPersonShooter.Core; // For MaterialType

namespace FirstPersonShooter.Building.Pieces
{
    public class TurretPiece : BaseBuildingPiece
    {
        private float health = 400f;
        private MaterialType structureMaterialType = MaterialType.Metal;

        public override float Health { get => health; set => health = value; }
        public override MaterialType StructureMaterialType { get => structureMaterialType; set => structureMaterialType = value; }

        public TurretTargetingSystem TargetingSystem { get; set; }
        public TurretWeaponSystem WeaponSystem { get; set; }
        public Entity TurretYawPart { get; set; }  // Rotates around Y-axis
        public Entity TurretPitchPart { get; set; } // Rotates around X-axis (local)
        public float RotationSpeed { get; set; } = 90f; // Degrees per second

        /// <summary>
        /// Determines if the turret has power and can operate.
        /// In a full system, this would be managed by a power grid or generator connection.
        /// </summary>
        public bool IsPowered { get; set; } = true;

        public TurretPiece()
        {
            // Turrets are typically not ground pieces themselves, they sit on foundations/ceilings.
            this.IsGroundPiece = false; 
        }

        public override void InitializeSnapPoints()
        {
            SnapPoints.Clear();
            // Example: A single snap point at its base to connect to a "FoundationTopCenter" or "CeilingTopCenter"
            // Assumes origin of the TurretPiece entity is at its very base center.
            SnapPoints.Add(new SnapPoint
            {
                LocalOffset = Vector3.Zero, // Snaps from its own origin
                LocalRotation = Quaternion.Identity,
                Type = "TurretBase" // Compatible with "FoundationTopCenter", "CeilingTopCenter" etc.
            });
        }

        public override void Start()
        {
            base.Start(); // Calls InitializeSnapPoints

            if (TargetingSystem == null)
            {
                Log.Error($"TurretPiece '{Entity?.Name ?? "Unnamed"}' has no TargetingSystem assigned.");
            }
            if (WeaponSystem == null)
            {
                Log.Error($"TurretPiece '{Entity?.Name ?? "Unnamed"}' has no WeaponSystem assigned.");
            }
            if (TurretYawPart == null)
            {
                Log.Error($"TurretPiece '{Entity?.Name ?? "Unnamed"}' has no TurretYawPart assigned.");
            }
            if (TurretPitchPart == null)
            {
                Log.Error($"TurretPiece '{Entity?.Name ?? "Unnamed"}' has no TurretPitchPart assigned.");
            }
            else if (TurretYawPart != null && TurretPitchPart.GetParent() != TurretYawPart)
            {
                 Log.Warning($"TurretPiece '{Entity?.Name ?? "Unnamed"}': TurretPitchPart is ideally a child of TurretYawPart for correct combined rotation.");
            }
        }

        public override void Update()
        {
            base.Update(); // BaseBuildingPiece.Update() if it ever has logic

            if (!IsPowered)
            {
                if (TargetingSystem != null && TargetingSystem.CurrentTarget != null)
                {
                    TargetingSystem.CurrentTarget = null; // Stop targeting
                    Log.Info($"TurretPiece '{Entity?.Name ?? "Unnamed"}' lost power, clearing target.");
                }
                // Optionally, add logic here to return turret to an idle/default rotation.
                // For now, it will just stop where it is.
                return; // Stop all turret operations if not powered
            }

            if (TargetingSystem?.CurrentTarget == null || WeaponSystem == null || TurretYawPart == null || TurretPitchPart == null)
            {
                // Optionally, return turret to a default orientation if no target and powered
                return;
            }

            var targetEntity = TargetingSystem.CurrentTarget;
            Vector3 targetPosition;
            var targetable = targetEntity.Get<ITargetable>();
            if (targetable != null)
            {
                targetPosition = targetable.GetTargetPosition();
            }
            else
            {
                targetPosition = targetEntity.Transform.Position; // Fallback
            }

            float deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            float maxRotationThisFrame = MathUtil.DegreesToRadians(RotationSpeed * deltaTime);

            // --- Yaw Rotation (TurretYawPart around Y-axis) ---
            Vector3 directionToTargetYaw = targetPosition - TurretYawPart.Transform.Position;
            directionToTargetYaw.Y = 0; // Project onto XZ plane for yaw
            if (directionToTargetYaw.LengthSquared() > 0.001f)
            {
                directionToTargetYaw.Normalize();
                Quaternion targetYawRotation = Quaternion.LookRotation(directionToTargetYaw, Vector3.UnitY);
                TurretYawPart.Transform.Rotation = Quaternion.Slerp(TurretYawPart.Transform.Rotation, targetYawRotation, maxRotationThisFrame);
                // Stride Slerp uses amount for interpolation factor, not max angle.
                // A more accurate Slerp by max angle would be:
                // Quaternion.Slerp(TurretYawPart.Transform.Rotation, targetYawRotation, RotationSpeed * deltaTime / Quaternion.Angle(TurretYawPart.Transform.Rotation, targetYawRotation));
                // For simplicity, the direct slerp will often look okay or can be tuned with RotationSpeed.
                // Proper way:
                Quaternion currentYaw = TurretYawPart.Transform.Rotation;
                float angleDiffYaw = Quaternion.Angle(currentYaw, targetYawRotation);
                if (angleDiffYaw > 0.001f) // Check for non-zero angle
                {
                     TurretYawPart.Transform.Rotation = Quaternion.Slerp(currentYaw, targetYawRotation, Math.Min(1.0f, maxRotationThisFrame / angleDiffYaw));
                }
            }

            // --- Pitch Rotation (TurretPitchPart around its local X-axis) ---
            // Pitch should be relative to the YawPart's current orientation.
            // Transform target position into YawPart's local space to isolate pitch.
            Matrix yawPartWorldToLocal = TurretYawPart.Transform.WorldMatrix;
            yawPartWorldToLocal.Invert();
            Vector3 targetPosInYawLocalSpace = Vector3.TransformCoordinate(targetPosition, yawPartWorldToLocal);
            
            Vector3 directionToTargetPitchLocal = targetPosInYawLocalSpace - TurretPitchPart.Transform.Position; // Position is local to YawPart

            if (directionToTargetPitchLocal.LengthSquared() > 0.001f)
            {
                // We want to rotate around the local X-axis of TurretPitchPart.
                // The directionToTargetPitchLocal gives us X and Y components in YawPart's space (Z is forward).
                // We need to find the angle for pitch.
                float pitchAngle = MathF.Atan2(directionToTargetPitchLocal.Y, directionToTargetPitchLocal.Z); // Angle in YZ plane (local to YawPart)
                Quaternion targetPitchLocalRotation = Quaternion.RotationX(pitchAngle);
                
                Quaternion currentPitch = TurretPitchPart.Transform.Rotation; // This is local rotation
                float angleDiffPitch = Quaternion.Angle(currentPitch, targetPitchLocalRotation);
                 if (angleDiffPitch > 0.001f)
                {
                    TurretPitchPart.Transform.Rotation = Quaternion.Slerp(currentPitch, targetPitchLocalRotation, Math.Min(1.0f, maxRotationThisFrame / angleDiffPitch));
                }
            }

            // --- Firing Logic ---
            // Check if turret is aimed (e.g., dot product of forward vector and target direction)
            Vector3 muzzleForward;
            if (WeaponSystem.MuzzlePointEntity != null)
            {
                muzzleForward = WeaponSystem.MuzzlePointEntity.Transform.WorldMatrix.Forward;
            }
            else // Fallback to TurretPitchPart or YawPart if muzzle not set
            {
                muzzleForward = TurretPitchPart.Transform.WorldMatrix.Forward; 
            }
            
            Vector3 directionToActualTarget = Vector3.Normalize(targetPosition - (WeaponSystem.MuzzlePointEntity?.Transform.WorldMatrix.TranslationVector ?? TurretPitchPart.Transform.WorldMatrix.TranslationVector));
            
            if (Vector3.Dot(muzzleForward, directionToActualTarget) > 0.95f) // Reasonably aimed
            {
                WeaponSystem.FireAt(targetEntity);
            }
        }
    }
}
