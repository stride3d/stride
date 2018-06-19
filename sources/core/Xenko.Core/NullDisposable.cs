// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Xenko.Core
{
    /// <summary>
    /// This class is an implementation of the <see cref="IDisposable"/> interface that does nothing when disposed.
    /// </summary>
    public class NullDisposable : IDisposable
    {
        /// <summary>
        /// A static instance of the <see cref="NullDisposable"/> class.
        /// </summary>
        public static readonly NullDisposable Instance = new NullDisposable();

        /// <summary>
        /// Implementation of the <see cref="IDisposable.Dispose"/> method. This method does nothing.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
