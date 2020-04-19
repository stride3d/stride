// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Updater
{
    /// <summary>
    /// Defines a member to update for <see cref="UpdateEngine.Compile"/>.
    /// </summary>
    public struct UpdateMemberInfo
    {
        public string Name;
        public int DataOffset;

        /// <summary>
        /// Create a member update entry for <see cref="UpdateEngine.Compile"/>.
        /// </summary>
        /// <param name="name">The member path to update.</param>
        /// <param name="dataOffset">The offset of this member source as it will be given to <see cref="UpdateEngine.Run"/> (either byte offset if blittable, otherwise object index).</param>
        public UpdateMemberInfo(string name, int dataOffset)
        {
            Name = name;
            DataOffset = dataOffset;
        }
    }
}
