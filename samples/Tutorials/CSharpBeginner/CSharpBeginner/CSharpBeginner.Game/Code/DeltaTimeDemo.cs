using Stride.Core.Mathematics;
using Stride.Engine;

namespace CSharpBeginner.Code
{
    /// <summary>
    /// DeltaTime is used to calculate frame independent values. 
    /// DeltaTime can also be used for creating Timers.
    /// </summary>
    public class DeltaTimeDemo : SyncScript
    {
        private float _rotationSpeed = 0.6f;

        // In this variable we keep track of the total time the game runs
        private float _totalTime = 0;

        // We use these variable for creating a simple countdown timer
        private float _countdownStartTime = 5.0f;
        private float _countdownTime = 0;

        public override void Start()
        {
            // We start the countdown timer at the initial countdown time of 5 seconds
            _countdownTime = _countdownStartTime;
        }

        public override void Update()
        {
            /// We can access Delta time through the static 'Game' object.
            var deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;

            // We update the total time
            _totalTime += deltaTime;

            // Since we have a countdown timer, we subtract the delta time from the count down time
            _countdownTime -= deltaTime;

            // If the repeatTimer, reaches 0, we reset the countDownTime back to the count down start time
            if (_countdownTime < 0)
            {
                _countdownTime = _countdownStartTime;
                _rotationSpeed *= -1;
            }

            Entity.Transform.Rotation *= Quaternion.RotationY(deltaTime * _rotationSpeed);
             
            // We display the total time and the countdown time on screen
            DebugText.Print("Total time: " + _totalTime, new Int2(480, 540));
            DebugText.Print("Countdown time: " + _countdownTime, new Int2(480, 560));
        }
    }
}
