using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Stride.Animations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;

namespace CSharpIntermediate.Code
{
    public class AnimationDemo : SyncScript
    {

        public float AnimationSpeed = 1.0f;
        private AnimationComponent animation;
        private PlayingAnimation playingAnimation;


        public override void Start()
        {
            animation = Entity.Get<AnimationComponent>();

            // Set the default animation
            playingAnimation = animation.Play("Idle");
        }

        public override void Update()
        {
            DebugText.Print($"Q and E for speed {AnimationSpeed.ToString("0.0")}", new Int2(300, 180));
            if (Input.IsKeyPressed(Keys.E))
            {
                AnimationSpeed += 0.1f;
                playingAnimation.TimeFactor = AnimationSpeed;
            }
            if (Input.IsKeyPressed(Keys.Q))
            {
                AnimationSpeed -= 0.1f;
                playingAnimation.TimeFactor = AnimationSpeed;
            }

            DebugText.Print("I to start playing Idle", new Int2(300, 240));
            if (Input.IsKeyPressed(Keys.I))
            {
                playingAnimation = animation.Play("Idle");
                playingAnimation.TimeFactor = AnimationSpeed;
            }

            DebugText.Print("R to crossfade to Run", new Int2(300, 260));
            if (Input.IsKeyPressed(Keys.R))
            {
                playingAnimation = animation.Crossfade("Run", TimeSpan.FromSeconds(1.5));
                playingAnimation.TimeFactor = AnimationSpeed;
            }

            DebugText.Print("J to crossfade to Jump and play it once", new Int2(300, 280));
            if (Input.IsKeyPressed(Keys.J))
            {
                playingAnimation = animation.Crossfade("Jump", TimeSpan.FromSeconds(0.5));
                playingAnimation.RepeatMode = AnimationRepeatMode.PlayOnce;
                playingAnimation.TimeFactor = AnimationSpeed;
            }
        }
    }
}
