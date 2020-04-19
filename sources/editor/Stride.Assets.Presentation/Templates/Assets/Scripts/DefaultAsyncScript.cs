using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Engine;

namespace ##Namespace##
{
    public class ##Scriptname## : AsyncScript
    {
        // Declared public member fields and properties will show in the game studio

        public override async Task Execute()
        {
            while(Game.IsRunning)
            {
                // Do stuff every new frame
                await Script.NextFrame();
            }
        }
    }
}
