// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Games
{
    public class GameUnhandledExceptionEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GameUnhandledExceptionEventArgs"/> class.
        /// </summary>
        /// <param name="exceptionObject">The exception object.</param>
        /// <param name="isTerminating">if set to <c>true</c> [is terminating].</param>
        public GameUnhandledExceptionEventArgs(object exceptionObject, bool isTerminating)
        {
            ExceptionObject = exceptionObject;
            IsTerminating = isTerminating;
        }

        /// <summary>
        /// Gets the unhandled exception object.
        /// </summary>
        /// <value>
        /// The unhandled exception object.
        /// </value>
        public object ExceptionObject { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the CLR is terminating.
        /// </summary>
        /// <value>
        ///   <c>true</c> if CLR is terminating; otherwise, <c>false</c>.
        /// </value>
        public bool IsTerminating { get; private set; }
    }
}
