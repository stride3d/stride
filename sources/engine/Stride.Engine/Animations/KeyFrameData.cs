// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using System.Runtime.InteropServices;
using Stride.Core;

namespace Stride.Animations
{
    /// <summary>
    /// A single key frame value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DataContract]
    [StructLayout(LayoutKind.Sequential)]
    public struct KeyFrameData<T>
    {
        public KeyFrameData(CompressedTimeSpan time, T value)
        {
            Time = time;
            Value = value;
        }

        public CompressedTimeSpan Time;
        public T Value;

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "Time: {0} Value:{1}", Time.Ticks, Value);
        }
    }
}
