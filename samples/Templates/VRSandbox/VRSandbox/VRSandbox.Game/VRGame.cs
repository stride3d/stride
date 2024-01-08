using System;
using Stride.Engine;
using Stride.Graphics;

namespace VRSandbox
{
    public class VRGame : Game
    {
        protected override void BeginRun()
        {
            base.BeginRun();
            // Ensure framerate is not capped if the window is in an unexpected state while the user is in VR
            MinimizedMinimumUpdateRate.MinimumElapsedTime = TimeSpan.Zero;
            WindowMinimumUpdateRate.MinimumElapsedTime = TimeSpan.Zero;
            DrawWhileMinimized = true;
            // Present interval only affects the window and is based on the monitor's refresh rate, not the HMD
            GraphicsDevice.Presenter.PresentInterval = PresentInterval.Immediate;
        }
    }
}
