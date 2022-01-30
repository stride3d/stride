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
        private PlayingAnimation currentAnimation;
        private PlayingAnimation previousAnimation;


        public override void Start()
        {
            animation = Entity.Get<AnimationComponent>();

            // Set the default animation
            currentAnimation = animation.Play("Idle");
        }

        public override void Update()
        {
            StopOrResumeAnimations();

            AdjustAnimationSpeed();

            DebugText.Print("I to start playing Idle", new Int2(300, 60));
            if (Input.IsKeyPressed(Keys.I))
            {
                currentAnimation = animation.Play("Idle");
                currentAnimation.TimeFactor = AnimationSpeed;
            }

            DebugText.Print("R to crossfade to Run", new Int2(300, 80));
            if (Input.IsKeyPressed(Keys.R))
            {
                currentAnimation = animation.Crossfade("Run", TimeSpan.FromSeconds(0.5));
                currentAnimation.TimeFactor = AnimationSpeed;
            }

            DebugText.Print("P to crossfade to Punch and play it once", new Int2(300, 100));
            if (Input.IsKeyPressed(Keys.P) && !animation.IsPlaying("Punch"))
            {
                previousAnimation = currentAnimation;
                currentAnimation = animation.Crossfade("Punch", TimeSpan.FromSeconds(1));
                currentAnimation.RepeatMode = AnimationRepeatMode.PlayOnce;
                currentAnimation.TimeFactor = AnimationSpeed;
            }
            if(currentAnimation.Name == "Punch" && currentAnimation.CurrentTime <= 0)
            {
                currentAnimation = animation.Play(previousAnimation.Name);
                currentAnimation.TimeFactor = AnimationSpeed;
            }
        }

        private void StopOrResumeAnimations()
        {
            DebugText.Print($"S to pause or resume animations", new Int2(300, 40));
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
            DebugText.Print($"Q and E for speed {AnimationSpeed:0.0}", new Int2(300, 20));
            if (Input.IsKeyPressed(Keys.E))
            {
                AnimationSpeed += 0.1f;
                currentAnimation.TimeFactor = AnimationSpeed;
            }
            if (Input.IsKeyPressed(Keys.Q))
            {
                AnimationSpeed -= 0.1f;
                currentAnimation.TimeFactor = AnimationSpeed;
            }
        }
    }
}
