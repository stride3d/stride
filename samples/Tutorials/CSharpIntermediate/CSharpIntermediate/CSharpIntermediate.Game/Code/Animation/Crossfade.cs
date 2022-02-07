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
    public class Crossfade : SyncScript
    {

        public float AnimationSpeed = 1.0f;
        private AnimationComponent animation;
        private PlayingAnimation playingAnimation;


        public override void Start()
        {
            animation = Entity.Get<AnimationComponent>();

            playingAnimation = animation.Play("Punch");
            playingAnimation.TimeFactor = AnimationSpeed;
        }

        public override void Update()
        {
            DebugText.Print("I to start playing Punch", new Int2(500, 80));
            if (Input.IsKeyPressed(Keys.I))
            {
                playingAnimation = animation.Play("Punch");
                playingAnimation.TimeFactor = AnimationSpeed;
            }

            DebugText.Print("C to crossfade to Run", new Int2(500, 100));
            if (Input.IsKeyPressed(Keys.C))
            {
                playingAnimation = animation.Crossfade("Run", TimeSpan.FromSeconds(1.0));
                playingAnimation.TimeFactor = AnimationSpeed;
            }
        }
    }
}
