// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Updater
{
    /// <summary>
    /// Defines how to get and set an array value for the <see cref="UpdateEngine"/>.
    /// </summary>
    public class UpdatableArrayAccessor<T> : UpdatableField<T>
    {
        private static readonly int ArrayFirstElementOffset = ComputeArrayFirstElementOffset();

        public UpdatableArrayAccessor(int index) : base(0)
        {
            Offset = ArrayFirstElementOffset + index * Size;
        }

        /// <inheritdoc/>
        public override EnterChecker CreateEnterChecker()
        {
            // Compute index
            var index = (Offset - ArrayFirstElementOffset) / Size;

            // Expect an array at least index + 1 items
            return new ListEnterChecker<T>(index + 1);
        }

        private static unsafe int ComputeArrayFirstElementOffset()
        {
            var testArray = new int[1];
            fixed (int* testArrayStart = testArray)
            {
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
                var testArrayObjectStart = *(nint*)&testArray;
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
                return (int)((byte*)testArrayStart - (byte*)testArrayObjectStart);
            }
        }
    }
}
