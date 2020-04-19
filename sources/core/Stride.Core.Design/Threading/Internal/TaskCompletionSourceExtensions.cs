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
using System.Threading.Tasks;
using Stride.Core.Annotations;

namespace Stride.Core.Threading
{
    /// <summary>
    /// Provides extension methods for <see cref="TaskCompletionSource{TResult}"/>.
    /// </summary>
    internal static class TaskCompletionSourceExtensions
    {
        /// <summary>
        /// Attempts to complete a <see cref="TaskCompletionSource{TResult}"/> with the specified value, forcing all continuations onto a threadpool thread even if they specified <c>ExecuteSynchronously</c>.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the asynchronous operation.</typeparam>
        /// <param name="this">The task completion source. May not be <c>null</c>.</param>
        /// <param name="result">The result of the asynchronous operation.</param>
        public static void TrySetResultWithBackgroundContinuations<TResult>([NotNull] this TaskCompletionSource<TResult> @this, TResult result)
        {
            // Set the result on a threadpool thread, so any synchronous continuations will execute in the background.
            Task.Run(() => @this.TrySetResult(result));

            // Wait for the TCS task to complete; note that the continuations may not be complete.
            @this.Task.Wait();
        }

        /// <summary>
        /// Attempts to complete a <see cref="TaskCompletionSource{TResult}"/> as canceled, forcing all continuations onto a threadpool thread even if they specified <c>ExecuteSynchronously</c>.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the asynchronous operation.</typeparam>
        /// <param name="this">The task completion source. May not be <c>null</c>.</param>
        public static void TrySetCanceledWithBackgroundContinuations<TResult>([NotNull] this TaskCompletionSource<TResult> @this)
        {
            // Complete on a threadpool thread, so any synchronous continuations will execute in the background.
            Task.Run(() => @this.TrySetCanceled());

            // Wait for the TCS task to complete; note that the continuations may not be complete.
            try
            {
                @this.Task.Wait();
            }
            catch (AggregateException)
            {
            }
        }
    }
}
