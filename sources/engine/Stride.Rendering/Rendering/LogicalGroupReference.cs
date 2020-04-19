// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Rendering
{
    /// <summary>
    /// Handle used to query logical group information.
    /// </summary>
    public struct LogicalGroupReference
    {
        public static readonly LogicalGroupReference Invalid = new LogicalGroupReference(-1);

        internal int Index;

        internal LogicalGroupReference(int index)
        {
            Index = index;
        }
    }
}
