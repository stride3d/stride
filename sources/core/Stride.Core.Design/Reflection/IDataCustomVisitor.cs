// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Core.Reflection
{
    /// <summary>
    /// A custom visitor used by <see cref="DataVisitorBase"/>.
    /// </summary>
    [AssemblyScan]
    public interface IDataCustomVisitor
    {
        /// <summary>
        /// Determines whether this instance can visit the specified object.
        /// </summary>
        /// <param name="type"></param>
        /// <returns><c>true</c> if this instance can visit the specified object; otherwise, <c>false</c>.</returns>
        bool CanVisit(Type type);

        /// <summary>
        /// Visits the specified object.
        /// </summary>
        /// <param name="context">The context.</param>
        void Visit(ref VisitorContext context);
    }
}
