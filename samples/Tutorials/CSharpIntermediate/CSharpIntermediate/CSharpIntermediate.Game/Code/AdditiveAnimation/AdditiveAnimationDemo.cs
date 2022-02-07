using System;
using Stride.Animations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace CSharpIntermediate.Code
{
    public class AdditiveAnimationDemo : SyncScript
    {

        public float AnimationSpeed = 1.0f;
        private AnimationComponent animation;
        private PlayingAnimation latestAnimation;

        public override void Start()
        {
            animation = Entity.Get<AnimationComponent>();

            // Set the default animation
            //latestAnimation = animation.Play("Run");
        }

        public override void Update()
        {
            StopOrResumeAnimations();

            AdjustAnimationSpeed();

            var isRunning = Input.IsKeyDown(Keys.W) || Input.IsKeyDown(Keys.Up);

            DebugText.Print("W or Up to run", new Int2(300, 60));

            //if (isRunning && latestAnimation.Name == "Idle")
            //{
            //    latestAnimation = animation.Crossfade("Run", TimeSpan.FromSeconds(0.5));
            //    latestAnimation.TimeFactor = AnimationSpeed;
            //}

            //if (!isRunning && latestAnimation.Name == "Run")
            //{
            //    latestAnimation = animation.Crossfade("Idle", TimeSpan.FromSeconds(0.5));
            //    latestAnimation.TimeFactor = AnimationSpeed;
            //}

            DebugText.Print("Left mouse to punch", new Int2(300, 80));
            if (Input.IsMouseButtonPressed(MouseButton.Left))
            {
                //if(latestAnimation.Name == "Idle")
                //{
                //    latestAnimation = animation.Crossfade("Punch", TimeSpan.FromSeconds(0.2));
                //}

                //if (latestAnimation.Name == "Run")
                {
                    //foreach (var anim in Animations)
                    //{
                    //    if (anim.Clip != null)
                    //        animComponent.Add(anim.Clip, anim.StartTime, anim.BlendOperation);
                    //}
                    var run = animation.NewPlayingAnimation("run");
                    run.Weight = 1;
                    run.BlendOperation = AnimationBlendOperation.LinearBlend;

                    var runpunch = animation.NewPlayingAnimation("runpunch");
                    runpunch.Weight = 1;
                    runpunch.BlendOperation = AnimationBlendOperation.Add;

                    animation.PlayingAnimations.Add(run);
                    animation.PlayingAnimations.Add(runpunch);

                    //latestAnimation = animation.Crossfade("Runpunch", TimeSpan.FromSeconds(0.2));
                    //latestAnimation.BlendOperation = AnimationBlendOperation.Add;
                }

                //latestAnimation.TimeFactor = AnimationSpeed;
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
