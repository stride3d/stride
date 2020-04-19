// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Assets.Editor.Components.Status
{
    /// <summary>
    /// This enum describes the priority of a job.
    /// </summary>
    public enum JobPriority
    {
        /// <summary>
        /// A background task of the application
        /// </summary>
        Background,
        /// <summary>
        /// An editor of the application that is working
        /// </summary>
        Editor,
        /// <summary>
        /// A major compilation process in the application
        /// </summary>
        Compile,
    }
}
