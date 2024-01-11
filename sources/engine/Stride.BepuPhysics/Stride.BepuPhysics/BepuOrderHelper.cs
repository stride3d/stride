using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stride.BepuPhysics
{
    static internal class BepuOrderHelper
    {

        //Note : transform processor's Draw() is at -200;

        public const int ORDER_OF_CONTAINER_P = -1000; //Handle the creation of bepu objects
        public const int ORDER_OF_CONSTRAINT_P = -900; //handle the creation of bepu constraints
        public const int ORDER_OF_GAME_SYSTEM = -810; //Handle the simulation Step(dt) + Transform update
        public const int ORDER_OF_DEBUG_P = 1000; // Transform processor's Draw operates at -200, as long as we're after it we're working optimally

    }
}
