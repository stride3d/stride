using System;
using System.Collections.Generic;
using System.Text;

namespace Stride.Networking
{
    class Messages
    {
        public enum MessageCodes
        {
            HitByRaycast,
            CollidedWithSender,
            LevelLoad,
            close,
        };
    }
}
