// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xenko.Core.Collections;

namespace Xenko.Rendering
{
    /// <summary>
    /// Holds associated data used during rendering.
    /// </summary>
    public partial struct RenderDataHolder
    {
        // storage for properties (struct of arrays)
        private FastListStruct<DataArray> dataArrays;
        private Dictionary<object, int> dataArraysByDefinition;
        private Func<DataType, int> computeDataArrayExpectedSize;

        public void Initialize(Func<DataType, int> computeDataArrayExpectedSize)
        {
            this.computeDataArrayExpectedSize = computeDataArrayExpectedSize;
            dataArraysByDefinition = new Dictionary<object, int>();
            dataArrays = new FastListStruct<DataArray>(8);
        }

        public void Clear()
        {
            dataArraysByDefinition.Clear();
            dataArrays.Clear();
        }

        public void SwapRemoveItem(DataType dataType, int source, int dest)
        {
            for (int i = 0; i < dataArrays.Count; ++i)
            {
                var dataArray = dataArrays[i];
                if (dataArray.Info.Type == dataType)
                    dataArray.Info.SwapRemoveItem(dataArray.Array, source, dest);
            }
        }

        public void PrepareDataArrays()
        {
            for (int i = 0; i < dataArrays.Count; ++i)
            {
                var dataArrayInfo = dataArrays[i].Info;
                var expectedSize = computeDataArrayExpectedSize(dataArrayInfo.Type);

                dataArrayInfo.EnsureSize(ref dataArrays.Items[i].Array, expectedSize);
            }
        }

        private struct DataArray
        {
            public Array Array;
            public readonly DataArrayInfo Info;

            public DataArray(DataArrayInfo info) : this()
            {
                Info = info;
            }
        }

        private abstract class DataArrayInfo
        {
            public DataType Type { get; }

            public int Multiplier { get; protected set; }

            public int ElementCount { get; protected set; }

            protected DataArrayInfo(DataType type, int multiplier)
            {
                Type = type;
                Multiplier = multiplier;
            }

            /// <summary>
            /// Ensure this array has the given size.
            /// </summary>
            /// <param name="array"></param>
            /// <param name="size"></param>
            public abstract void EnsureSize(ref Array array, int size);

            /// <summary>
            /// Move items around. Source will be reset to default values. No growing of array is expected.
            /// </summary>
            /// <param name="array"></param>
            /// <param name="removedElement"></param>
            /// <param name="lastElement"></param>
            public abstract void SwapRemoveItem(Array array, int removedElement, int lastElement);

            /// <summary>
            /// Change number of elements per entry.
            /// </summary>
            /// <param name="array"></param>
            /// <param name="newMultiplier"></param>
            public abstract void ChangeMutiplier(ref Array array, int multiplier);
        }

        private class DataArrayInfo<T> : DataArrayInfo
        {
            public DataArrayInfo(DataType type, int multiplier = 1) : base(type, multiplier)
            {
            }

            public override void EnsureSize(ref Array array, int size)
            {
                ElementCount = size;

                var totalSize = size * Multiplier;

                // TODO: we should probably shrink down if not used anymore during many frames)
                // TODO should we clear values if references?
                // Array has proper size already
                if (totalSize == 0 || (array != null && totalSize <= array.Length))
                    return;

                var arrayT = (T[])array;
                Array.Resize(ref arrayT, totalSize);
                array = arrayT;
            }

            public override void SwapRemoveItem(Array array, int removedElement, int lastElement)
            {
                // Items were not added yet?
                if (removedElement >= ElementCount || lastElement >= ElementCount)
                    return;

                var arrayT = (T[])array;

                lastElement *= Multiplier;
                removedElement *= Multiplier;

                if (removedElement < lastElement)
                {
                    for (int i = 0; i < Multiplier; ++i)
                        arrayT[removedElement + i] = arrayT[lastElement + i];
                }

                for (int i = 0; i < Multiplier; ++i)
                    arrayT[lastElement + i] = default(T);
            }

            public override void ChangeMutiplier(ref Array array, int multiplier)
            {
                if (multiplier == Multiplier)
                    return;

                var oldMultiplier = Multiplier;
                Multiplier = multiplier;

                // Zero size, we can keep old array as is
                // TODO should we clear values if references?
                if (multiplier == 0 || ElementCount == 0)
                    return;

                var newArray = new T[ElementCount * multiplier];
                var arrayT = (T[])array;

                var valuesToCopyPerElement = Math.Min(multiplier, oldMultiplier);

                for (int i = 0, arrayIndex = 0, newArrayIndex = 0; i < ElementCount; ++i, arrayIndex += oldMultiplier - valuesToCopyPerElement, newArrayIndex += multiplier - valuesToCopyPerElement)
                {
                    for (int j = 0; j < valuesToCopyPerElement; ++j)
                    {
                        newArray[newArrayIndex++] = arrayT[arrayIndex++];
                    }
                }

                array = newArray;
            }
        }
    }
}
