// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Assets.Editor.ViewModel
{
    /// <summary>
    /// The result of a check that a drag and drop operation is possible.
    /// </summary>
    public enum DropAcceptance
    {
        /// <summary>
        /// The operation is not possible.
        /// </summary>
        Rejected,
        /// <summary>
        /// The operation is possible, but it changes nothing.
        /// </summary>
        /// <remarks>
        /// The user interface shows this result as neutral, because the user made no error. The caller must not do the
        /// operation.
        /// </remarks>
        NoOp,
        /// <summary>
        /// The operation is possible.
        /// </summary>
        Accepted,
    }
}
