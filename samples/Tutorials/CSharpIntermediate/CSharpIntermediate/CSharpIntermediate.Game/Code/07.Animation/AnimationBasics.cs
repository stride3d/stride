using System;
using Stride.Animations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace CSharpIntermediate.Code
{
    public class AnimationBasics : SyncScript
    {

        public float AnimationSpeed = 1.0f;
        private AnimationComponent animation;
        private PlayingAnimation latestAnimation;

        public override void Start()
        {
            animation = Entity.Get<AnimationComponent>();

            // Set the default animation
            latestAnimation = animation.Play("Idle");
        }

        public override void Update()
        {
            StopOrResumeAnimations();

            AdjustAnimationSpeed();

            DebugText.Print("I to start playing Idle", new Int2(320, 80));
            if (Input.IsKeyPressed(Keys.I))
            {
                latestAnimation = animation.Play("Idle");
                latestAnimation.TimeFactor = AnimationSpeed;
            }

            DebugText.Print("R to crossfade to Run", new Int2(320, 100));
            if (Input.IsKeyPressed(Keys.R))
            {
                latestAnimation = animation.Crossfade("Run", TimeSpan.FromSeconds(0.5));
                latestAnimation.TimeFactor = AnimationSpeed;
            }

            // We can crossfade to a punch animation, but onyl if it is not already playing
            DebugText.Print("P to crossfade to Punch and play it once", new Int2(320, 120));
            if (Input.IsKeyPressed(Keys.P) && !animation.IsPlaying("Punch"))
            {
                latestAnimation = animation.Crossfade("Punch", TimeSpan.FromSeconds(0.1));
                latestAnimation.RepeatMode = AnimationRepeatMode.PlayOnce;
                latestAnimation.TimeFactor = AnimationSpeed;
            }
            // When de punch animation is the latest animation, but it is no longer playing, we set a new animation
            if (latestAnimation.Name == "Punch" && !animation.IsPlaying("Punch"))
            {
                latestAnimation = animation.Play("Idle");
                latestAnimation.RepeatMode = AnimationRepeatMode.LoopInfinite;
                latestAnimation.TimeFactor = AnimationSpeed;
            }
        }

        private void StopOrResumeAnimations()
        {
            DebugText.Print($"S to pause or resume animations", new Int2(320, 60));
            if (Input.IsKeyPressed(Keys.S))
            {
                foreach (var playingAnimation in animation.PlayingAnimations)
                {
                    playingAnimation.Enabled = !playingAnimation.Enabled;
                }
            }
        }

        private void AdjustAnimationSpeed()
        {
            DebugText.Print($"Q and E for speed {AnimationSpeed:0.0}", new Int2(320, 40));
            if (Input.IsKeyPressed(Keys.E))
            {
                AnimationSpeed += 0.1f;
                latestAnimation.TimeFactor = AnimationSpeed;
            }
            if (Input.IsKeyPressed(Keys.Q))
            {
                AnimationSpeed -= 0.1f;
                latestAnimation.TimeFactor = AnimationSpeed;
            }
        }
    }
}
