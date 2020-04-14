// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.InteropServices;

namespace Stride.Core.Diagnostics
{
    [StructLayout(LayoutKind.Explicit)]
    public struct ProfilingCustomValue
    {
        [FieldOffset(0)]
        public int IntValue;

        [FieldOffset(0)]
        public float FloatValue;

        [FieldOffset(0)]
        public long LongValue;

        [FieldOffset(0)]
        public double DoubleValue;

        [FieldOffset(8)]
        public Type ValueType;

        public static implicit operator ProfilingCustomValue(int value)
        {
            return new ProfilingCustomValue { IntValue = value, ValueType = typeof(int) };
        }

        public static implicit operator ProfilingCustomValue(float value)
        {
            return new ProfilingCustomValue { FloatValue = value, ValueType = typeof(float) };
        }

        public static implicit operator ProfilingCustomValue(long value)
        {
            return new ProfilingCustomValue { LongValue = value, ValueType = typeof(long) };
        }

        public static implicit operator ProfilingCustomValue(double value)
        {
            return new ProfilingCustomValue { DoubleValue = value, ValueType = typeof(double) };
        }
    }
}
