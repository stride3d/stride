using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.BepuPhysics
{
    static internal class BepuOrderHelper
    {

        //Note : transform processor is at -200;

        public const int ORDER_OF_CONTAINER_P = -1000; //Handle the creation of bepu objects
        public const int ORDER_OF_CONSTRAINT_P = -900; //handle the creation of bepu constraints
        public const int ORDER_OF_GAME_SYSTEM = -800; //Handle the simulation Step(dt) + Transform update
        public const int ORDER_OF_DEBUG_P = -800; //Handle the Draw of debug shapes

    }
}
