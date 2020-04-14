// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Diagnostics
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public enum ComponentEventType
    {
        /// <summary>
        /// ComponentBase constructor event.
        /// </summary>
        Instantiate = 0,

        /// <summary>
        /// ComponentBase.Destroy() event.
        /// </summary>
        Destroy = 1,

        /// <summary>
        /// IReferencable.AddReference() event.
        /// </summary>
        AddReference = 2,

        /// <summary>
        /// IReferenceable.Release() event.
        /// </summary>
        Release = 3,
    }
}
