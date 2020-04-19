// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Internal
{
    /// <summary>
    /// Set of methods to help in refactoring that can be easily tracked by finding usages.
    /// </summary>
    internal static class Refactor
    {
        /// <summary>
        /// Throw a not implemented exception. Useful to avoid warnings about non-reachable code.
        /// </summary>
        /// <param name="message">Reason for the exception.</param>
        public static void ThrowNotImplementedException(string message = null)
        {
            // Although we could use one code path, when the message argument is null we use 
            // the overload without argument to build automatically a standard message.
            if (message != null)
            {
                throw new NotImplementedException(message);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        
        /// <summary>
        /// Create a new not implemented exception. Useful when we need to tell the C# compiler code
        /// won't be reached after.
        /// </summary>
        /// <param name="message">Reason for the exception.</param>
        public static Exception NewNotImplementedException(string message = null)
        {
            // Although we could use one code path, when the message argument is null we use 
            // the overload without argument to build automatically a standard message.
            if (message != null)
            {
                return new NotImplementedException(message);
            }
            else
            {
                return new NotImplementedException();
            }
        }    
    }
}
