// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_UI_OPENTK
using System;

namespace Stride.Games
{
    internal class OpenTKMessageLoop : IMessageLoop
    {
        private OpenTK.GameWindow control;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsMessageLoop"/> class.
        /// </summary>
        public OpenTKMessageLoop(OpenTK.GameWindow control)
        {
            this.control = control;
        }

        /// <summary>
        /// Calls this method on each frame.
        /// </summary>
        /// <returns><c>true</c> if if the control is still active, <c>false</c> otherwise.</returns>
        /// <exception cref="System.InvalidOperationException">An error occurred </exception>
        public bool NextFrame()
        {
            control.ProcessEvents();
            return control.IsExiting;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            control = null;
        }
   }
}
#endif
