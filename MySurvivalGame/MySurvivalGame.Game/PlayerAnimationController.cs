// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
// Adapted for MySurvivalGame.

using Stride.Engine;
using Stride.Core.Mathematics; // For Vector3
using Stride.Physics;         // For CharacterComponent
using Stride.Animations;      // For AnimationComponent, PlayingAnimation, AnimationClip etc.
using System.Linq;            // For LINQ operations like FirstOrDefault

namespace MySurvivalGame.Game
{
    public class PlayerAnimationController : SyncScript
    {
        public AnimationComponent TargetAnimationComponent { get; set; } // MODIFIED: Renamed property
        public CharacterComponent Character { get; set; }

        // Conceptual names for animations that would be in TargetAnimationComponent.Animations dictionary
        // For this test, we'll use "RifleIdle" for both base idle and additive upper body,
        // and "WalkForward" for movement.
        private const string IdleAnim = "RifleIdle";       // MODIFIED: Conceptual name
        private const string WalkAnim = "WalkForward";     // MODIFIED: Conceptual name

        public override void Start()
        {
            // Ensure components are linked, preferrably via the editor
            if (TargetAnimationComponent == null) // MODIFIED: Changed to TargetAnimationComponent
            {
                TargetAnimationComponent = Entity.Get<AnimationComponent>(); // MODIFIED: Changed to TargetAnimationComponent
                if (TargetAnimationComponent == null) // MODIFIED: Changed to TargetAnimationComponent
                    Log.Error("PlayerAnimationController: TargetAnimationComponent not found on entity or not assigned.");
            }

            if (Character == null)
            {
                // CharacterComponent is expected on the parent entity ("Player")
                Character = Entity.GetParent()?.Get<CharacterComponent>();
                if (Character == null)
                    Log.Error("PlayerAnimationController: CharacterComponent not found on parent entity or not assigned.");
            }

            // --- Initial Animation State & Investigation into Layering/Masking ---
            // Stride's AnimationComponent plays AnimationClips. A PlayingAnimation is an instance of an AnimationClip being played.
            // The AnimationComponent has a list of PlayingAnimations.
            //
            // **Primary Question for Investigation:** How to make one PlayingAnimation affect only upper body bones,
            // and another PlayingAnimation affect only lower body bones simultaneously?
            //
            // **1. Bone Masking per PlayingAnimation:**
            //    - Ideal Scenario: `AnimComponent.Play("Walk_Forward", new AnimationPlayParameters { BoneMask = lowerBodyBoneNamesList });`
            //    - Stride's `PlayingAnimation` class does NOT seem to have a direct `BoneMask` property or equivalent
            //      that takes a list of bone names to include/exclude for that specific animation instance *at runtime* when playing.
            //    - `AnimationClip` itself doesn't store a bone mask that `PlayingAnimation` would inherit for a specific playback.
            //
            // **2. Animation Layers (Unity-style):**
            //    - Unity has a concept of layers in its Animator, where each layer can have a weight and an Avatar Mask.
            //    - Stride's `AnimationComponent` has a `BlendOperation` (Additive, Linear) but this applies globally to how
            //      multiple currently playing animations are combined, not to layering with distinct masks per layer.
            //    - There isn't an obvious "Layers" collection on `AnimationComponent` where each layer could have its own mask.
            //
            // **3. Additive Animations & `AnimationBlendOperation.Additive`:**
            //    - This is supported. If "Rifle_Idle_Upper" is a true additive animation (designed to only contain rotations relative
            //      to a base pose, typically the T-pose or another idle), then playing it additively might work IF the lower body
            //      animation ("Walk_Forward") correctly provides the base pose for all bones, and "Rifle_Idle_Upper" only has
            //      meaningful animation data for upper body bones.
            //    - If "Rifle_Idle_Upper" is a standard animation (not additive), playing it additively might lead to undesirable
            //      results (e.g., doubled rotations).
            //
            // **4. `IAnimationBlender` & Custom Blenders:**
            //    - `AnimationComponent` uses an `IAnimationBlender` (default is `AnimationBlender`).
            //    - It might be possible to create a custom `IAnimationBlender` that, during its `Blend` method,
            //      selectively applies bone transformations based on some criteria (e.g., animation name conventions
            //      or custom properties attached to `PlayingAnimation.StateObject` if that's usable).
            //    - This is an advanced approach and would require deep understanding of Stride's animation evaluation pipeline.
            //
            // **5. Pre-Processing Animations:**
            //    - The most straightforward way, if runtime masking is not directly available, is to ensure animations are
            //      authored/exported correctly:
            //        a) "Walk_Forward" should ideally only contain keyframes for lower body bones.
            //        b) "Rifle_Idle_Upper" should ideally only contain keyframes for upper body bones.
            //    - If animations are full-body, they would need to be split in a DCC tool (e.g., Blender) into separate
            //      clips for upper and lower body.
            //
            // **6. Stride `SkeletonUpdater` and `BoneMask` (Low-level internal type):**
            //    - Digging into Stride's source, there's `Stride.Animations.SkeletonUpdater` which uses a `BoneMask`.
            //    - However, this `BoneMask` (a struct of bitfields) seems to be computed internally based on which bones an
            //      `AnimationClip` actually has animation data for. It's not something a user typically sets per `PlayingAnimation`
            //      to restrict a full-body animation to a subset of bones at runtime.
            //
            // **Conclusion for Initial Setup (based on public API) & Investigation Summary:**
            // - Stride's `AnimationComponent` plays `AnimationClip` assets.
            // - For distinct upper/lower body animations from full-body clips, animations generally need to be pre-split
            //   in a DCC tool (e.g., Blender) or authored specifically as additive layers.
            // - Stride does not appear to offer a high-level, built-in runtime bone masking feature on `PlayingAnimation`
            //   to restrict a full-body clip to certain bones for a specific playback instance without custom `IAnimationBlender` development.
            // - Current blending will rely on `AnimationBlendOperation.Additive` for upper body layers,
            //   assuming the animation clips are authored appropriately (either as true additive clips or pre-split).
            // - The system seems to rely more on animations being authored for the parts they should affect.
            // - For this script, we will assume animations *would be* correctly authored (e.g., "Idle_Upper_Additive" only affects upper bones).
            //   Then, `BlendOperation.Additive` for the upper body animation is the most promising approach.

            // Example: Try to play a base idle and an upper body additive idle if available
            // This assumes "Idle_Upper_Additive" is a true additive animation or only affects upper bones.
            // And "Idle_Lower" (or a full body idle) provides the base.
            
            // Ensure animations are present before trying to play
            // Play base idle animation (linear blend)
            if (TargetAnimationComponent?.Animations.ContainsKey(IdleAnim) ?? false)
            {
                 TargetAnimationComponent.Play(IdleAnim, new AnimationPlayParameters { Loop = true, BlendOperation = AnimationBlendOperation.Linear, Key = "BaseLayer" }); // MODIFIED: Using new const and key
            }
            else
            {
                // Log if conceptual "IdleAnim" is not in the TargetAnimationComponent.Animations dictionary
            }

            // Play upper body idle animation (additive blend)
            // For this test, we use the same "RifleIdle" animation conceptually for the additive layer.
            // In a real scenario, this would likely be a separate "RifleIdle_UpperAdditive" clip.
            if (TargetAnimationComponent?.Animations.ContainsKey(IdleAnim) ?? false)
            {
                TargetAnimationComponent.Play(IdleAnim, new AnimationPlayParameters { Loop = true, BlendOperation = AnimationBlendOperation.Additive, Key = "UpperLayer" }); // MODIFIED: Using new const and key
            }
             else
            {
                // Log if conceptual "IdleAnim" for upper body is not in the TargetAnimationComponent.Animations dictionary
            }
        }

        public override void Update()
        {
            if (TargetAnimationComponent == null || Character == null)
                return;

            bool isMoving = Character.Velocity.LengthSquared() > 0.1f;

            // --- Conceptual Animation Blending Logic ---
            // This logic assumes "WalkForward" would animate lower body (or be full body base)
            // and "RifleIdle" would animate upper body additively.

            var currentBaseAnimation = TargetAnimationComponent.PlayingAnimations.FirstOrDefault(pa => pa.Key == "BaseLayer");

            if (isMoving)
            {
                // If "WalkForward" is available and not already the primary base animation
                if (TargetAnimationComponent.Animations.ContainsKey(WalkAnim))
                {
                    if (currentBaseAnimation == null || currentBaseAnimation.Name != WalkAnim || !currentBaseAnimation.Enabled)
                    {
                        currentBaseAnimation?.Stop(); // Stop whatever was playing on base layer
                        TargetAnimationComponent.Play(WalkAnim, new AnimationPlayParameters { Loop = true, BlendOperation = AnimationBlendOperation.Linear, Key = "BaseLayer" });
                        // Log.Info("Switched to WalkForward (conceptual)");
                    }
                }
                else
                {
                    // Log.WarningOnce("Player is moving but 'WalkForward' animation is not available.");
                }
            }
            else // Player is Idle
            {
                // If "RifleIdle" is available and not already the primary base animation
                if (TargetAnimationComponent.Animations.ContainsKey(IdleAnim))
                {
                     if (currentBaseAnimation == null || currentBaseAnimation.Name != IdleAnim || !currentBaseAnimation.Enabled)
                    {
                        currentBaseAnimation?.Stop(); // Stop whatever was playing on base layer
                        TargetAnimationComponent.Play(IdleAnim, new AnimationPlayParameters { Loop = true, BlendOperation = AnimationBlendOperation.Linear, Key = "BaseLayer" });
                        // Log.Info("Switched to RifleIdle (conceptual base)");
                    }
                }
                else
                {
                    // Log.WarningOnce("'RifleIdle' (for base) animation is not available for idle state.");
                }
            }

            // Ensure upper body additive animation ("RifleIdle") is playing.
            // This was started in Start() and should be looping. If it somehow stopped, restart it.
            var currentUpperAnimation = TargetAnimationComponent.PlayingAnimations.FirstOrDefault(pa => pa.Key == "UpperLayer");
            if (TargetAnimationComponent.Animations.ContainsKey(IdleAnim))
            {
                 if(currentUpperAnimation == null || !currentUpperAnimation.Enabled) {
                    TargetAnimationComponent.Play(IdleAnim, new AnimationPlayParameters { Loop = true, BlendOperation = AnimationBlendOperation.Additive, Key = "UpperLayer" });
                 }
            }

            // --- Further Investigation Notes (already present in Start(), reiterated here for clarity in report) ---
            // Stride's `AnimationComponent` plays `AnimationClip` assets.
            // For distinct upper/lower body animations from full-body clips, animations generally need to be pre-split
            // in a DCC tool (e.g., Blender) or authored specifically as additive layers.
            // Stride does not appear to offer a high-level, built-in runtime bone masking feature on `PlayingAnimation`
            // to restrict a full-body clip to certain bones for a specific playback instance without custom `IAnimationBlender` development.
            // Current blending will rely on `AnimationBlendOperation.Additive` for upper body layers,
            // assuming the animation clips are authored appropriately (either as true additive clips or pre-split).
        }
    }
}
