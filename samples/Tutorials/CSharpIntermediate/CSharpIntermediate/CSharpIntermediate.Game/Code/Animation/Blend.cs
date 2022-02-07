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
    public class Blend : SyncScript
    {

        public float AnimationSpeed = 1.0f;
        public float blend = 1.0f;
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
            DebugText.Print("I to start playing Punch", new Int2(300, 80));
            if (Input.IsKeyPressed(Keys.I))
            {
                playingAnimation = animation.Play("Punch");
                playingAnimation.TimeFactor = AnimationSpeed;
            }

            DebugText.Print("B to blend", new Int2(300, 100));
            if (Input.IsKeyPressed(Keys.B))
            {
                playingAnimation = animation.Blend("Run", blend, TimeSpan.FromSeconds(1.0));
                playingAnimation.TimeFactor = AnimationSpeed;
            }
        }
    }
}
