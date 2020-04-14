// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#region Copyright and license
// Some parts of this file were inspired by AsyncEx (https://github.com/StephenCleary/AsyncEx)
/*
The MIT License (MIT)
https://opensource.org/licenses/MIT

Copyright (c) 2014 StephenCleary

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Stride.Core.Threading
{
    /// <summary>
    /// Allocates Ids for instances on demand. 0 is an invalid/unassigned Id. Ids may be non-unique in very long-running systems.
    /// This is similar to the Id system used by <see cref="System.Threading.Tasks.Task"/> and <see cref="System.Threading.Tasks.TaskScheduler"/>.
    /// </summary>
    /// <typeparam name="TTag">The type for which ids are generated.</typeparam>
    // ReSharper disable UnusedTypeParameter
    internal static class IdManager<TTag>
    // ReSharper restore UnusedTypeParameter
    {
        /// <summary>
        /// The last id generated for this type. This is 0 if no ids have been generated.
        /// </summary>
        // ReSharper disable StaticFieldInGenericType
        private static int lastId;
        // ReSharper restore StaticFieldInGenericType

        /// <summary>
        /// Returns the id, allocating it if necessary.
        /// </summary>
        /// <param name="id">A reference to the field containing the id.</param>
        public static int GetId(ref int id)
        {
            // If the Id has already been assigned, just use it.
            if (id != 0)
                return id;

            // Determine the new Id without modifying "id", since other threads may also be determining the new Id at the same time.
            int newId;

            // The Increment is in a while loop to ensure we get a non-zero Id:
            //  If we are incrementing -1, then we want to skip over 0.
            //  If there are tons of Id allocations going on, we want to skip over 0 no matter how many times we get it.
            do
            {
                newId = Interlocked.Increment(ref lastId);
            } while (newId == 0);

            // Update the Id unless another thread already updated it.
            Interlocked.CompareExchange(ref id, newId, 0);

            // Return the current Id, regardless of whether it's our new Id or a new Id from another thread.
            return id;
        }
    }
}
