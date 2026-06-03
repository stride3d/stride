// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using Stride.Physics;

namespace Stride.Assets.Physics
{
    internal sealed class ConvexHullMesh : IDisposable
    {
        private IntPtr _internalCompound;

        public struct DecompositionDesc
        {
            public uint VertexCount;
            public uint TriangleCount;
            public float[] Vertexes;
            public uint[] Indices;

            public bool SimpleHull;

            public uint MaxConvexHulls;
            public uint Resolution;
            public uint MaxRecursionDepth;
            public double MinimumVolumePercentErrorAllowed;
            public bool ShrinkWrap;
            public VhacdFillMode FillMode;
            public uint MaxNumVerticesPerCH;
        }

        private static int _sTokens = 0;
        private readonly int _token;

        public ConvexHullMesh()
        {
            _token = Interlocked.Increment(ref _sTokens);
        }

        public void Generate(DecompositionDesc desc)
        {
            _internalCompound = vhacdGenerate(
                desc.Vertexes, desc.VertexCount,
                desc.Indices, desc.TriangleCount,
                desc.SimpleHull,
                desc.MaxConvexHulls,
                desc.Resolution,
                desc.MaxRecursionDepth,
                desc.MinimumVolumePercentErrorAllowed,
                desc.ShrinkWrap,
                (int)desc.FillMode,
                desc.MaxNumVerticesPerCH,
                _token);
        }

        public void Cancel() => vhacdCancel(_token);

        public uint Count => vhacdGetHullCount(_internalCompound);

        public void CopyPoints(uint index, out float[] points)
        {
            points = new float[vhacdGetHullPointCount(_internalCompound, index) * 3];
            vhacdCopyHullPoints(_internalCompound, index, points);
        }

        public void CopyIndices(uint index, out uint[] indices)
        {
            indices = new uint[vhacdGetHullIndexCount(_internalCompound, index) * 3];
            vhacdCopyHullIndices(_internalCompound, index, indices);
        }

        public void Dispose()
        {
            if (_internalCompound != IntPtr.Zero)
            {
                vhacdRelease(_internalCompound);
                _internalCompound = IntPtr.Zero;
            }
        }

        private const string DllName = "stride_vhacd";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private static extern IntPtr vhacdGenerate(
            [In] float[] points, uint pointCount,
            [In] uint[] indices, uint triangleCount,
            [MarshalAs(UnmanagedType.I1)] bool simpleHull,
            uint maxConvexHulls,
            uint resolution,
            uint maxRecursionDepth,
            double minimumVolumePercentErrorAllowed,
            [MarshalAs(UnmanagedType.I1)] bool shrinkWrap,
            int fillMode,
            uint maxNumVerticesPerCH,
            int cancelToken);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private static extern void vhacdRelease(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private static extern void vhacdCancel(int cancelToken);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private static extern uint vhacdGetHullCount(IntPtr handle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private static extern uint vhacdGetHullPointCount(IntPtr handle, uint hullIndex);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private static extern void vhacdCopyHullPoints(IntPtr handle, uint hullIndex, [Out] float[] outPoints);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private static extern uint vhacdGetHullIndexCount(IntPtr handle, uint hullIndex);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private static extern void vhacdCopyHullIndices(IntPtr handle, uint hullIndex, [Out] uint[] outIndices);
    }
}
