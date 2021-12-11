#region License
/*
    MIT License
    Copyright(c) Sven Forstmann in 2014
    Copyright(c) 2017-2018 Mattias Edlund
    Copyright(c) 2021 Stefan Boronczyk
*/
#endregion

using System;
using System.Collections.Generic;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Rendering.MeshDecimator.Collections;
using Stride.Rendering.Rendering.MeshDecimator.Math;
using Stride.Rendering.Rendering.MeshDecimator.Types;

namespace Stride.Rendering.MeshDecimator.Algorithms
{
    /// <summary>
    /// The fast quadric mesh simplification algorithm.
    /// </summary>
    public sealed class FastQuadricMeshSimplification : DecimationAlgorithm
    {
        #region Consts
        private const double DoubleEpsilon = 1.0E-3;
        #endregion

        #region Classes
        #region Triangle
        private struct Triangle
        {
            #region Fields
            public int v0;
            public int v1;
            public int v2;
            public int subMeshIndex;

            public int va0;
            public int va1;
            public int va2;

            public double err0;
            public double err1;
            public double err2;
            public double err3;

            public bool deleted;
            public bool dirty;
            public Double3 n;
            #endregion

            #region Properties
            public int this[int index]
            {
                get
                {
                    return (index == 0 ? v0 : (index == 1 ? v1 : v2));
                }
                set
                {
                    switch (index)
                    {
                        case 0:
                            v0 = value;
                            break;
                        case 1:
                            v1 = value;
                            break;
                        case 2:
                            v2 = value;
                            break;
                        default:
                            throw new IndexOutOfRangeException();
                    }
                }
            }
            #endregion

            #region Constructor
            public Triangle(int v0, int v1, int v2, int subMeshIndex)
            {
                this.v0 = v0;
                this.v1 = v1;
                this.v2 = v2;
                this.subMeshIndex = subMeshIndex;

                this.va0 = v0;
                this.va1 = v1;
                this.va2 = v2;

                err0 = err1 = err2 = err3 = 0;
                deleted = dirty = false;
                n = new Double3();
            }
            #endregion

            #region Public Methods
            public void GetAttributeIndices(int[] attributeIndices)
            {
                attributeIndices[0] = va0;
                attributeIndices[1] = va1;
                attributeIndices[2] = va2;
            }

            public void SetAttributeIndex(int index, int value)
            {
                switch (index)
                {
                    case 0:
                        va0 = value;
                        break;
                    case 1:
                        va1 = value;
                        break;
                    case 2:
                        va2 = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }
            }

            public void GetErrors(double[] err)
            {
                err[0] = err0;
                err[1] = err1;
                err[2] = err2;
            }
            #endregion
        }
        #endregion

        #region Vertex
        private struct Vertex
        {
            public Double3 p;
            public int tstart;
            public int tcount;
            public SymmetricMatrix q;
            public bool border;
            public bool seam;
            public bool foldover;

            public Vertex(Double3 p)
            {
                this.p = p;
                this.tstart = 0;
                this.tcount = 0;
                this.q = new SymmetricMatrix();
                this.border = true;
                this.seam = false;
                this.foldover = false;
            }
        }
        #endregion

        #region Ref
        private struct Ref
        {
            public int tid;
            public int tvertex;

            public void Set(int tid, int tvertex)
            {
                this.tid = tid;
                this.tvertex = tvertex;
            }
        }
        #endregion

        #region Border Vertex
        private struct BorderVertex
        {
            public int index;
            public int hash;

            public BorderVertex(int index, int hash)
            {
                this.index = index;
                this.hash = hash;
            }
        }
        #endregion

        #region Border Vertex Comparer
        private class BorderVertexComparer : IComparer<BorderVertex>
        {
            public static readonly BorderVertexComparer instance = new BorderVertexComparer();

            public int Compare(BorderVertex x, BorderVertex y)
            {
                return x.hash.CompareTo(y.hash);
            }
        }
        #endregion
        #endregion

        #region Fields
        private bool preserveSeams = false;
        private bool preserveFoldovers = false;
        private bool enableSmartLink = true;
        private int maxIterationCount = 100;
        private double agressiveness = 7.0;
        private double vertexLinkDistanceSqr = double.Epsilon;

        private int subMeshCount = 0;
        private ResizableArray<Triangle> triangles = null;
        private ResizableArray<Vertex> vertices = null;
        private ResizableArray<Ref> refs = null;

        private ResizableArray<Vector3> vertNormals = null;
        private ResizableArray<Vector4> vertTangents = null;
        private UVChannels<Vector2> vertUV2D = null;
        private UVChannels<Vector3> vertUV3D = null;
        private UVChannels<Vector4> vertUV4D = null;
        private ResizableArray<Vector4> vertColors = null;
        private ResizableArray<BoneWeight> vertBoneWeights = null;

        private int remainingVertices = 0;

        // Pre-allocated buffers
        private double[] errArr = new double[3];
        private int[] attributeIndexArr = new int[3];
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets if seams should be preserved.
        /// Default value: false
        /// </summary>
        public bool PreserveSeams
        {
            get { return preserveSeams; }
            set { preserveSeams = value; }
        }

        /// <summary>
        /// Gets or sets if foldovers should be preserved.
        /// Default value: false
        /// </summary>
        public bool PreserveFoldovers
        {
            get { return preserveFoldovers; }
            set { preserveFoldovers = value; }
        }

        /// <summary>
        /// Gets or sets if a feature for smarter vertex linking should be enabled, reducing artifacts in the
        /// decimated result at the cost of a slightly more expensive initialization by treating vertices at
        /// the same position as the same vertex while separating the attributes.
        /// Default value: true
        /// </summary>
        public bool EnableSmartLink
        {
            get { return enableSmartLink; }
            set { enableSmartLink = value; }
        }

        /// <summary>
        /// Gets or sets the maximum iteration count. Higher number is more expensive but can bring you closer to your target quality.
        /// Sometimes a lower maximum count might be desired in order to lower the performance cost.
        /// Default value: 100
        /// </summary>
        public int MaxIterationCount
        {
            get { return maxIterationCount; }
            set { maxIterationCount = value; }
        }

        /// <summary>
        /// Gets or sets the agressiveness of this algorithm. Higher number equals higher quality, but more expensive to run.
        /// Default value: 7.0
        /// </summary>
        public double Agressiveness
        {
            get { return agressiveness; }
            set { agressiveness = value; }
        }

        /// <summary>
        /// Gets or sets the maximum squared distance between two vertices in order to link them.
        /// Note that this value is only used if EnableSmartLink is true.
        /// Default value: double.Epsilon
        /// </summary>
        public double VertexLinkDistanceSqr
        {
            get { return vertexLinkDistanceSqr; }
            set { vertexLinkDistanceSqr = value; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new fast quadric mesh simplification algorithm.
        /// </summary>
        public FastQuadricMeshSimplification()
        {
            triangles = new ResizableArray<Triangle>(0);
            vertices = new ResizableArray<Vertex>(0);
            refs = new ResizableArray<Ref>(0);
        }
        #endregion

        #region Private Methods
        #region Initialize Vertex Attribute
        private ResizableArray<T> InitializeVertexAttribute<T>(T[] attributeValues, string attributeName)
        {
            if (attributeValues != null && attributeValues.Length == vertices.Length)
            {
                var newArray = new ResizableArray<T>(attributeValues.Length, attributeValues.Length);
                var newArrayData = newArray.Data;
                Array.Copy(attributeValues, 0, newArrayData, 0, attributeValues.Length);
                return newArray;
            }
            else if (attributeValues != null && attributeValues.Length > 0)
            {
                GlobalLogger.GetLogger("Asset").Error(
                    string.Format("Failed to set vertex attribute '{0}' with {1} length of array, when {2} was needed.", 
                        attributeName, 
                        attributeValues.Length, 
                        vertices.Length)
                );
            }
            return null;
        }
        #endregion

        #region Calculate Error
        private double VertexError(ref SymmetricMatrix q, double x, double y, double z)
        {
            return  q.m0*x*x + 2*q.m1*x*y + 2*q.m2*x*z + 2*q.m3*x + q.m4*y*y
                + 2*q.m5*y*z + 2*q.m6*y +     q.m7*z*z + 2*q.m8*z + q.m9;
        }

        private double CalculateError(ref Vertex vert0, ref Vertex vert1, out Double3 result, out int resultIndex)
        {
            // compute interpolated vertex
            SymmetricMatrix q = (vert0.q + vert1.q);
            bool border = (vert0.border & vert1.border);
            double error = 0.0;
            double det = q.Determinant1();
            if (det != 0.0 && !border)
            {
                // q_delta is invertible
                result = new Double3(
                    -1.0 / det * q.Determinant2(),  // vx = A41/det(q_delta)
                    1.0 / det * q.Determinant3(),   // vy = A42/det(q_delta)
                    -1.0 / det * q.Determinant4()); // vz = A43/det(q_delta)
                error = VertexError(ref q, result.X, result.Y, result.Z);
                resultIndex = 2;
            }
            else
            {
                // det = 0 -> try to find best result
                Double3 p1 = vert0.p;
                Double3 p2 = vert1.p;
                Double3 p3 = (p1 + p2) * 0.5f;
                double error1 = VertexError(ref q, p1.X, p1.Y, p1.Z);
                double error2 = VertexError(ref q, p2.X, p2.Y, p2.Z);
                double error3 = VertexError(ref q, p3.X, p3.Y, p3.Z);

                error = MathHelper.Min(error1, error2, error3);
                if (error == error3)
                {
                    result = p3;
                    resultIndex = 2;
                }
                else if (error == error2)
                {
                    result = p2;
                    resultIndex = 1;
                }
                else if (error == error1)
                {
                    result = p1;
                    resultIndex = 0;
                }
                else
                {
                    result = p3;
                    resultIndex = 2;
                }
            }
            return error;
        }
        #endregion

        #region Flipped
        /// <summary>
        /// Check if a triangle flips when this edge is removed
        /// </summary>
        private bool Flipped(ref Double3 p, int i0, int i1, ref Vertex v0, bool[] deleted)
        {
            int tcount = v0.tcount;
            var refs = this.refs.Data;
            var triangles = this.triangles.Data;
            var vertices = this.vertices.Data;
            for (int k = 0; k < tcount; k++)
            {
                Ref r = refs[v0.tstart + k];
                if (triangles[r.tid].deleted)
                    continue;

                int s = r.tvertex;
                int id1 = triangles[r.tid][(s + 1) % 3];
                int id2 = triangles[r.tid][(s + 2) % 3];
                if (id1 == i1 || id2 == i1)
                {
                    deleted[k] = true;
                    continue;
                }

                Double3 d1 = vertices[id1].p - p;
                d1.Normalize();
                Double3 d2 = vertices[id2].p - p;

                d2.Normalize();

                double dot;
                Double3.Dot(ref d1, ref d2, out dot);

                if (System.Math.Abs(dot) > 0.999)
                    return true;

                Double3 n;
                Double3.Cross(ref d1, ref d2, out n);
                n.Normalize();
                deleted[k] = false;

                Double3.Dot(ref n, ref triangles[r.tid].n, out dot);
                if (dot < 0.2)
                    return true;
            }

            return false;
        }
        #endregion

        #region Update Triangles
        /// <summary>
        /// Update triangle connections and edge error after a edge is collapsed.
        /// </summary>
        private void UpdateTriangles(int i0, int ia0, ref Vertex v, ResizableArray<bool> deleted, ref int deletedTriangles)
        {
            Double3 p;
            int pIndex;
            int tcount = v.tcount;
            var triangles = this.triangles.Data;
            var vertices = this.vertices.Data;
            for (int k = 0; k < tcount; k++)
            {
                Ref r = refs[v.tstart + k];
                int tid = r.tid;
                Triangle t = triangles[tid];
                if (t.deleted)
                    continue;

                if (deleted[k])
                {
                    triangles[tid].deleted = true;
                    ++deletedTriangles;
                    continue;
                }

                t[r.tvertex] = i0;
                if (ia0 != -1)
                {
                    t.SetAttributeIndex(r.tvertex, ia0);
                }

                t.dirty = true;
                t.err0 = CalculateError(ref vertices[t.v0], ref vertices[t.v1], out p, out pIndex);
                t.err1 = CalculateError(ref vertices[t.v1], ref vertices[t.v2], out p, out pIndex);
                t.err2 = CalculateError(ref vertices[t.v2], ref vertices[t.v0], out p, out pIndex);
                t.err3 = MathHelper.Min(t.err0, t.err1, t.err2);
                triangles[tid] = t;
                refs.Add(r);
            }
        }
        #endregion

        #region Move/Merge Vertex Attributes
        private void MoveVertexAttributes(int i0, int i1)
        {
            if (vertNormals != null)
            {
                vertNormals[i0] = vertNormals[i1];
            }
            if (vertTangents != null)
            {
                vertTangents[i0] = vertTangents[i1];
            }
            if (vertUV2D != null)
            {
                for (int i = 0; i < MeshDecimatorData.UVChannelCount; i++)
                {
                    var vertUV = vertUV2D[i];
                    if (vertUV != null)
                    {
                        vertUV[i0] = vertUV[i1];
                    }
                }
            }
            if (vertUV3D != null)
            {
                for (int i = 0; i < MeshDecimatorData.UVChannelCount; i++)
                {
                    var vertUV = vertUV3D[i];
                    if (vertUV != null)
                    {
                        vertUV[i0] = vertUV[i1];
                    }
                }
            }
            if (vertUV4D != null)
            {
                for (int i = 0; i < MeshDecimatorData.UVChannelCount; i++)
                {
                    var vertUV = vertUV4D[i];
                    if (vertUV != null)
                    {
                        vertUV[i0] = vertUV[i1];
                    }
                }
            }
            if (vertColors != null)
            {
                vertColors[i0] = vertColors[i1];
            }
            if (vertBoneWeights != null)
            {
                vertBoneWeights[i0] = vertBoneWeights[i1];
            }
        }

        private void MergeVertexAttributes(int i0, int i1)
        {
            if (vertNormals != null)
            {
                vertNormals[i0] = (vertNormals[i0] + vertNormals[i1]) * 0.5f;
            }
            if (vertTangents != null)
            {
                vertTangents[i0] = (vertTangents[i0] + vertTangents[i1]) * 0.5f;
            }
            if (vertUV2D != null)
            {
                for (int i = 0; i < MeshDecimatorData.UVChannelCount; i++)
                {
                    var vertUV = vertUV2D[i];
                    if (vertUV != null)
                    {
                        vertUV[i0] = (vertUV[i0] + vertUV[i1]) * 0.5f;
                    }
                }
            }
            if (vertUV3D != null)
            {
                for (int i = 0; i < MeshDecimatorData.UVChannelCount; i++)
                {
                    var vertUV = vertUV3D[i];
                    if (vertUV != null)
                    {
                        vertUV[i0] = (vertUV[i0] + vertUV[i1]) * 0.5f;
                    }
                }
            }
            if (vertUV4D != null)
            {
                for (int i = 0; i < MeshDecimatorData.UVChannelCount; i++)
                {
                    var vertUV = vertUV4D[i];
                    if (vertUV != null)
                    {
                        vertUV[i0] = (vertUV[i0] + vertUV[i1]) * 0.5f;
                    }
                }
            }
            if (vertColors != null)
            {
                vertColors[i0] = (vertColors[i0] + vertColors[i1]) * 0.5f;
            }

            // TODO: Do we have to blend bone weights at all or can we just keep them as it is in this scenario?
        }
        #endregion

        #region Are UVs The Same
        private bool AreUVsTheSame(int channel, int indexA, int indexB)
        {
            if (vertUV2D != null)
            {
                var vertUV = vertUV2D[channel];
                if (vertUV != null)
                {
                    var uvA = vertUV[indexA];
                    var uvB = vertUV[indexB];
                    return uvA == uvB;
                }
            }

            if (vertUV3D != null)
            {
                var vertUV = vertUV3D[channel];
                if (vertUV != null)
                {
                    var uvA = vertUV[indexA];
                    var uvB = vertUV[indexB];
                    return uvA == uvB;
                }
            }

            if (vertUV4D != null)
            {
                var vertUV = vertUV4D[channel];
                if (vertUV != null)
                {
                    var uvA = vertUV[indexA];
                    var uvB = vertUV[indexB];
                    return uvA == uvB;
                }
            }

            return false;
        }
        #endregion

        #region Remove Vertex Pass
        /// <summary>
        /// Remove vertices and mark deleted triangles
        /// </summary>
        private void RemoveVertexPass(int startTrisCount, int targetTrisCount, double threshold, ResizableArray<bool> deleted0, ResizableArray<bool> deleted1, ref int deletedTris)
        {
            var triangles = this.triangles.Data;
            int triangleCount = this.triangles.Length;
            var vertices = this.vertices.Data;

            bool preserveBorders = base.PreserveBorders;
            int maxVertexCount = base.MaxVertexCount;
            if (maxVertexCount <= 0)
                maxVertexCount = int.MaxValue;

            Double3 p;
            int pIndex;
            for (int tid = 0; tid < triangleCount; tid++)
            {
                if (triangles[tid].dirty || triangles[tid].deleted || triangles[tid].err3 > threshold)
                    continue;

                triangles[tid].GetErrors(errArr);
                triangles[tid].GetAttributeIndices(attributeIndexArr);
                for (int edgeIndex = 0; edgeIndex < 3; edgeIndex++)
                {
                    if (errArr[edgeIndex] > threshold)
                        continue;

                    int nextEdgeIndex = ((edgeIndex + 1) % 3);
                    int i0 = triangles[tid][edgeIndex];
                    int i1 = triangles[tid][nextEdgeIndex];

                    // Border check
                    if (vertices[i0].border != vertices[i1].border)
                        continue;
                    // Seam check
                    else if (vertices[i0].seam != vertices[i1].seam)
                        continue;
                    // Foldover check
                    else if (vertices[i0].foldover != vertices[i1].foldover)
                        continue;
                    // If borders should be preserved
                    else if (preserveBorders && vertices[i0].border)
                        continue;
                    // If seams should be preserved
                    else if (preserveSeams && vertices[i0].seam)
                        continue;
                    // If foldovers should be preserved
                    else if (preserveFoldovers && vertices[i0].foldover)
                        continue;

                    // Compute vertex to collapse to
                    CalculateError(ref vertices[i0], ref vertices[i1], out p, out pIndex);
                    deleted0.Resize(vertices[i0].tcount); // normals temporarily
                    deleted1.Resize(vertices[i1].tcount); // normals temporarily

                    // Don't remove if flipped
                    if (Flipped(ref p, i0, i1, ref vertices[i0], deleted0.Data))
                        continue;
                    if (Flipped(ref p, i1, i0, ref vertices[i1], deleted1.Data))
                        continue;

                    int ia0 = attributeIndexArr[edgeIndex];

                    // Not flipped, so remove edge
                    vertices[i0].p = p;
                    vertices[i0].q += vertices[i1].q;

                    if (pIndex == 1)
                    {
                        // Move vertex attributes from ia1 to ia0
                        int ia1 = attributeIndexArr[nextEdgeIndex];
                        MoveVertexAttributes(ia0, ia1);
                    }
                    else if (pIndex == 2)
                    {
                        // Merge vertex attributes ia0 and ia1 into ia0
                        int ia1 = attributeIndexArr[nextEdgeIndex];
                        MergeVertexAttributes(ia0, ia1);
                    }

                    if (vertices[i0].seam)
                    {
                        ia0 = -1;
                    }

                    int tstart = refs.Length;
                    UpdateTriangles(i0, ia0, ref vertices[i0], deleted0, ref deletedTris);
                    UpdateTriangles(i0, ia0, ref vertices[i1], deleted1, ref deletedTris);

                    int tcount = refs.Length - tstart;
                    if (tcount <= vertices[i0].tcount)
                    {
                        // save ram
                        if (tcount > 0)
                        {
                            var refsArr = refs.Data;
                            Array.Copy(refsArr, tstart, refsArr, vertices[i0].tstart, tcount);
                        }
                    }
                    else
                    {
                        // append
                        vertices[i0].tstart = tstart;
                    }

                    vertices[i0].tcount = tcount;
                    --remainingVertices;
                    break;
                }

                // Check if we are already done
                if ((startTrisCount - deletedTris) <= targetTrisCount && remainingVertices < maxVertexCount)
                    break;
            }
        }
        #endregion

        #region Update Mesh
        /// <summary>
        /// Compact triangles, compute edge error and build reference list.
        /// </summary>
        /// <param name="iteration">The iteration index.</param>
        private void UpdateMesh(int iteration)
        {
            var triangles = this.triangles.Data;
            var vertices = this.vertices.Data;

            int triangleCount = this.triangles.Length;
            int vertexCount = this.vertices.Length;
            if (iteration > 0) // compact triangles
            {
                int dst = 0;
                for (int i = 0; i < triangleCount; i++)
                {
                    if (!triangles[i].deleted)
                    {
                        if (dst != i)
                        {
                            triangles[dst] = triangles[i];
                        }
                        dst++;
                    }
                }
                this.triangles.Resize(dst);
                triangles = this.triangles.Data;
                triangleCount = dst;
            }

            UpdateReferences();

            // Identify boundary : vertices[].border=0,1
            if (iteration == 0)
            {
                var refs = this.refs.Data;

                var vcount = new List<int>(8);
                var vids = new List<int>(8);
                int vsize = 0;
                for (int i = 0; i < vertexCount; i++)
                {
                    vertices[i].border = false;
                    vertices[i].seam = false;
                    vertices[i].foldover = false;
                }

                int ofs;
                int id;
                int borderVertexCount = 0;
                double borderMinX = double.MaxValue;
                double borderMaxX = double.MinValue;
                for (int i = 0; i < vertexCount; i++)
                {
                    int tstart = vertices[i].tstart;
                    int tcount = vertices[i].tcount;
                    vcount.Clear();
                    vids.Clear();
                    vsize = 0;

                    for (int j = 0; j < tcount; j++)
                    {
                        int tid = refs[tstart + j].tid;
                        for (int k = 0; k < 3; k++)
                        {
                            ofs = 0;
                            id = triangles[tid][k];
                            while (ofs < vsize)
                            {
                                if (vids[ofs] == id)
                                    break;

                                ++ofs;
                            }

                            if (ofs == vsize)
                            {
                                vcount.Add(1);
                                vids.Add(id);
                                ++vsize;
                            }
                            else
                            {
                                ++vcount[ofs];
                            }
                        }
                    }

                    for (int j = 0; j < vsize; j++)
                    {
                        if (vcount[j] == 1)
                        {
                            id = vids[j];
                            vertices[id].border = true;
                            ++borderVertexCount;

                            if (enableSmartLink)
                            {
                                if (vertices[id].p.X < borderMinX)
                                {
                                    borderMinX = vertices[id].p.X;
                                }
                                if (vertices[id].p.X > borderMaxX)
                                {
                                    borderMaxX = vertices[id].p.X;
                                }
                            }
                        }
                    }
                }

                if (enableSmartLink)
                {
                    // First find all border vertices
                    var borderVertices = new BorderVertex[borderVertexCount];
                    int borderIndexCount = 0;
                    double borderAreaWidth = borderMaxX - borderMinX;
                    for (int i = 0; i < vertexCount; i++)
                    {
                        if (vertices[i].border)
                        {
                            int vertexHash = (int)(((((vertices[i].p.X - borderMinX) / borderAreaWidth) * 2.0) - 1.0) * int.MaxValue);
                            borderVertices[borderIndexCount] = new BorderVertex(i, vertexHash);
                            ++borderIndexCount;
                        }
                    }

                    // Sort the border vertices by hash
                    Array.Sort(borderVertices, 0, borderIndexCount, BorderVertexComparer.instance);

                    // Calculate the maximum hash distance based on the maximum vertex link distance
                    double vertexLinkDistance = System.Math.Sqrt(vertexLinkDistanceSqr);
                    int hashMaxDistance = System.Math.Max((int)((vertexLinkDistance / borderAreaWidth) * int.MaxValue), 1);

                    // Then find identical border vertices and bind them together as one
                    for (int i = 0; i < borderIndexCount; i++)
                    {
                        int myIndex = borderVertices[i].index;
                        if (myIndex == -1)
                            continue;

                        var myPoint = vertices[myIndex].p;
                        for (int j = i + 1; j < borderIndexCount; j++)
                        {
                            int otherIndex = borderVertices[j].index;
                            if (otherIndex == -1)
                                continue;
                            else if ((borderVertices[j].hash - borderVertices[i].hash) > hashMaxDistance) // There is no point to continue beyond this point
                                break;

                            var otherPoint = vertices[otherIndex].p;
                            var sqrX = ((myPoint.X - otherPoint.X) * (myPoint.X - otherPoint.X));
                            var sqrY = ((myPoint.Y - otherPoint.Y) * (myPoint.Y - otherPoint.Y));
                            var sqrZ = ((myPoint.Z - otherPoint.Z) * (myPoint.Z - otherPoint.Z));
                            var sqrMagnitude = sqrX + sqrY + sqrZ;

                            if (sqrMagnitude <= vertexLinkDistanceSqr)
                            {
                                borderVertices[j].index = -1; // NOTE: This makes sure that the "other" vertex is not processed again
                                vertices[myIndex].border = false;
                                vertices[otherIndex].border = false;

                                if (AreUVsTheSame(0, myIndex, otherIndex))
                                {
                                    vertices[myIndex].foldover = true;
                                    vertices[otherIndex].foldover = true;
                                }
                                else
                                {
                                    vertices[myIndex].seam = true;
                                    vertices[otherIndex].seam = true;
                                }

                                int otherTriangleCount = vertices[otherIndex].tcount;
                                int otherTriangleStart = vertices[otherIndex].tstart;
                                for (int k = 0; k < otherTriangleCount; k++)
                                {
                                    var r = refs[otherTriangleStart + k];
                                    triangles[r.tid][r.tvertex] = myIndex;
                                }
                            }
                        }
                    }

                    // Update the references again
                    UpdateReferences();
                }

                // Init Quadrics by Plane & Edge Errors
                //
                // required at the beginning ( iteration == 0 )
                // recomputing during the simplification is not required,
                // but mostly improves the result for closed meshes
                for (int i = 0; i < vertexCount; i++)
                {
                    vertices[i].q = new SymmetricMatrix();
                }

                int v0, v1, v2;
                Double3 n, p0, p1, p2, p10, p20, dummy;
                int dummy2;
                SymmetricMatrix sm;
                for (int i = 0; i < triangleCount; i++)
                {
                    v0 = triangles[i].v0;
                    v1 = triangles[i].v1;
                    v2 = triangles[i].v2;

                    p0 = vertices[v0].p;
                    p1 = vertices[v1].p;
                    p2 = vertices[v2].p;
                    p10 = p1 - p0;
                    p20 = p2 - p0;
                    Double3.Cross(ref p10, ref p20, out n);
                    n.Normalize();
                    triangles[i].n = n;

                    double dot;
                    Double3.Dot(ref n, ref p0, out dot);

                    sm = new SymmetricMatrix(n.X, n.Y, n.Z, -dot);
                    vertices[v0].q += sm;
                    vertices[v1].q += sm;
                    vertices[v2].q += sm;
                }

                for (int i = 0; i < triangleCount; i++)
                {
                    // Calc Edge Error
                    var triangle = triangles[i];
                    triangles[i].err0 = CalculateError(ref vertices[triangle.v0], ref vertices[triangle.v1], out dummy, out dummy2);
                    triangles[i].err1 = CalculateError(ref vertices[triangle.v1], ref vertices[triangle.v2], out dummy, out dummy2);
                    triangles[i].err2 = CalculateError(ref vertices[triangle.v2], ref vertices[triangle.v0], out dummy, out dummy2);
                    triangles[i].err3 = MathHelper.Min(triangles[i].err0, triangles[i].err1, triangles[i].err2);
                }
            }
        }
        #endregion

        #region Update References
        private void UpdateReferences()
        {
            int triangleCount = this.triangles.Length;
            int vertexCount = this.vertices.Length;
            var triangles = this.triangles.Data;
            var vertices = this.vertices.Data;

            // Init Reference ID list
            for (int i = 0; i < vertexCount; i++)
            {
                vertices[i].tstart = 0;
                vertices[i].tcount = 0;
            }

            for (int i = 0; i < triangleCount; i++)
            {
                ++vertices[triangles[i].v0].tcount;
                ++vertices[triangles[i].v1].tcount;
                ++vertices[triangles[i].v2].tcount;
            }

            int tstart = 0;
            remainingVertices = 0;
            for (int i = 0; i < vertexCount; i++)
            {
                vertices[i].tstart = tstart;
                if (vertices[i].tcount > 0)
                {
                    tstart += vertices[i].tcount;
                    vertices[i].tcount = 0;
                    ++remainingVertices;
                }
            }

            // Write References
            this.refs.Resize(tstart);
            var refs = this.refs.Data;
            for (int i = 0; i < triangleCount; i++)
            {
                int v0 = triangles[i].v0;
                int v1 = triangles[i].v1;
                int v2 = triangles[i].v2;
                int start0 = vertices[v0].tstart;
                int count0 = vertices[v0].tcount;
                int start1 = vertices[v1].tstart;
                int count1 = vertices[v1].tcount;
                int start2 = vertices[v2].tstart;
                int count2 = vertices[v2].tcount;

                refs[start0 + count0].Set(i, 0);
                refs[start1 + count1].Set(i, 1);
                refs[start2 + count2].Set(i, 2);

                ++vertices[v0].tcount;
                ++vertices[v1].tcount;
                ++vertices[v2].tcount;
            }
        }
        #endregion

        #region Compact Mesh
        /// <summary>
        /// Finally compact mesh before exiting.
        /// </summary>
        private void CompactMesh()
        {
            int dst = 0;
            var vertices = this.vertices.Data;
            int vertexCount = this.vertices.Length;
            for (int i = 0; i < vertexCount; i++)
            {
                vertices[i].tcount = 0;
            }

            var vertNormals = (this.vertNormals != null ? this.vertNormals.Data : null);
            var vertTangents = (this.vertTangents != null ? this.vertTangents.Data : null);
            var vertUV2D = (this.vertUV2D != null ? this.vertUV2D.Data : null);
            var vertUV3D = (this.vertUV3D != null ? this.vertUV3D.Data : null);
            var vertUV4D = (this.vertUV4D != null ? this.vertUV4D.Data : null);
            var vertColors = (this.vertColors != null ? this.vertColors.Data : null);
            var vertBoneWeights = (this.vertBoneWeights != null ? this.vertBoneWeights.Data : null);

            var triangles = this.triangles.Data;
            int triangleCount = this.triangles.Length;
            for (int i = 0; i < triangleCount; i++)
            {
                var triangle = triangles[i];
                if (!triangle.deleted)
                {
                    if (triangle.va0 != triangle.v0)
                    {
                        int iDest = triangle.va0;
                        int iSrc = triangle.v0;
                        vertices[iDest].p = vertices[iSrc].p;
                        if (vertBoneWeights != null)
                        {
                            vertBoneWeights[iDest] = vertBoneWeights[iSrc];
                        }
                        triangle.v0 = triangle.va0;
                    }
                    if (triangle.va1 != triangle.v1)
                    {
                        int iDest = triangle.va1;
                        int iSrc = triangle.v1;
                        vertices[iDest].p = vertices[iSrc].p;
                        if (vertBoneWeights != null)
                        {
                            vertBoneWeights[iDest] = vertBoneWeights[iSrc];
                        }
                        triangle.v1 = triangle.va1;
                    }
                    if (triangle.va2 != triangle.v2)
                    {
                        int iDest = triangle.va2;
                        int iSrc = triangle.v2;
                        vertices[iDest].p = vertices[iSrc].p;
                        if (vertBoneWeights != null)
                        {
                            vertBoneWeights[iDest] = vertBoneWeights[iSrc];
                        }
                        triangle.v2 = triangle.va2;
                    }

                    triangles[dst++] = triangle;

                    vertices[triangle.v0].tcount = 1;
                    vertices[triangle.v1].tcount = 1;
                    vertices[triangle.v2].tcount = 1;
                }
            }

            triangleCount = dst;
            this.triangles.Resize(triangleCount);
            triangles = this.triangles.Data;

            dst = 0;
            for (int i = 0; i < vertexCount; i++)
            {
                var vert = vertices[i];
                if (vert.tcount > 0)
                {
                    vert.tstart = dst;
                    vertices[i] = vert;

                    if (dst != i)
                    {
                        vertices[dst].p = vert.p;
                        if (vertNormals != null) vertNormals[dst] = vertNormals[i];
                        if (vertTangents != null) vertTangents[dst] = vertTangents[i];
                        if (vertUV2D != null)
                        {
                            for (int j = 0; j < MeshDecimatorData.UVChannelCount; j++)
                            {
                                var vertUV = vertUV2D[j];
                                if (vertUV != null)
                                {
                                    vertUV[dst] = vertUV[i];
                                }
                            }
                        }
                        if (vertUV3D != null)
                        {
                            for (int j = 0; j < MeshDecimatorData.UVChannelCount; j++)
                            {
                                var vertUV = vertUV3D[j];
                                if (vertUV != null)
                                {
                                    vertUV[dst] = vertUV[i];
                                }
                            }
                        }
                        if (vertUV4D != null)
                        {
                            for (int j = 0; j < MeshDecimatorData.UVChannelCount; j++)
                            {
                                var vertUV = vertUV4D[j];
                                if (vertUV != null)
                                {
                                    vertUV[dst] = vertUV[i];
                                }
                            }
                        }
                        if (vertColors != null) vertColors[dst] = vertColors[i];
                        if (vertBoneWeights != null) vertBoneWeights[dst] = vertBoneWeights[i];
                    }
                    ++dst;
                }
            }

            for (int i = 0; i < triangleCount; i++)
            {
                var triangle = triangles[i];
                triangle.v0 = vertices[triangle.v0].tstart;
                triangle.v1 = vertices[triangle.v1].tstart;
                triangle.v2 = vertices[triangle.v2].tstart;
                triangles[i] = triangle;
            }

            vertexCount = dst;
            this.vertices.Resize(vertexCount);
            if (vertNormals != null) this.vertNormals.Resize(vertexCount, true);
            if (vertTangents != null) this.vertTangents.Resize(vertexCount, true);
            if (vertUV2D != null) this.vertUV2D.Resize(vertexCount, true);
            if (vertUV3D != null) this.vertUV3D.Resize(vertexCount, true);
            if (vertUV4D != null) this.vertUV4D.Resize(vertexCount, true);
            if (vertColors != null) this.vertColors.Resize(vertexCount, true);
            if (vertBoneWeights != null) this.vertBoneWeights.Resize(vertexCount, true);
        }
        #endregion
        #endregion

        #region Public Methods
        #region Initialize
        /// <summary>
        /// Initializes the algorithm with the original mesh.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        public override void Initialize(MeshDecimatorData mesh)
        {
            if (mesh == null)
                throw new ArgumentNullException("mesh");

            int meshSubMeshCount = mesh.SubMeshCount;
            int meshTriangleCount = mesh.TriangleCount;
            var meshVertices = mesh.Vertices;
            var meshNormals = mesh.Normals;
            var meshTangents = mesh.Tangents;
            var meshColors = mesh.Colors;
            var meshBoneWeights = mesh.BoneWeights;
            subMeshCount = meshSubMeshCount;

            vertices.Resize(meshVertices.Length);
            var vertArr = vertices.Data;
            for (int i = 0; i < meshVertices.Length; i++)
            {
                vertArr[i] = new Vertex(meshVertices[i]);
            }

            triangles.Resize(meshTriangleCount);
            var trisArr = triangles.Data;
            int triangleIndex = 0;
            for (int subMeshIndex = 0; subMeshIndex < meshSubMeshCount; subMeshIndex++)
            {
                int[] subMeshIndices = mesh.GetIndices(subMeshIndex);
                int subMeshTriangleCount = subMeshIndices.Length / 3;
                for (int i = 0; i < subMeshTriangleCount; i++)
                {
                    int offset = i * 3;
                    int v0 = subMeshIndices[offset];
                    int v1 = subMeshIndices[offset + 1];
                    int v2 = subMeshIndices[offset + 2];
                    trisArr[triangleIndex++] = new Triangle(v0, v1, v2, subMeshIndex);
                }
            }

            vertNormals = InitializeVertexAttribute(meshNormals, "normals");
            vertTangents = InitializeVertexAttribute(meshTangents, "tangents");
            vertColors = InitializeVertexAttribute(meshColors, "colors");
            vertBoneWeights = InitializeVertexAttribute(meshBoneWeights, "boneWeights");

            for (int i = 0; i < MeshDecimatorData.UVChannelCount; i++)
            {
                int uvDim = mesh.GetUVDimension(i);
                string uvAttributeName = string.Format("uv{0}", i);
                if (uvDim == 2)
                {
                    if (vertUV2D == null)
                        vertUV2D = new UVChannels<Vector2>();

                    var uvs = mesh.GetUVs2D(i);
                    vertUV2D[i] = InitializeVertexAttribute(uvs, uvAttributeName);
                }
                else if (uvDim == 3)
                {
                    if (vertUV3D == null)
                        vertUV3D = new UVChannels<Vector3>();

                    var uvs = mesh.GetUVs3D(i);
                    vertUV3D[i] = InitializeVertexAttribute(uvs, uvAttributeName);
                }
                else if (uvDim == 4)
                {
                    if (vertUV4D == null)
                        vertUV4D = new UVChannels<Vector4>();

                    var uvs = mesh.GetUVs4D(i);
                    vertUV4D[i] = InitializeVertexAttribute(uvs, uvAttributeName);
                }
            }
        }
        #endregion

        #region Decimate Mesh
        /// <summary>
        /// Decimates the mesh.
        /// </summary>
        /// <param name="targetTrisCount">The target triangle count.</param>
        public override void DecimateMesh(int targetTrisCount)
        {
            if (targetTrisCount < 0)
                throw new ArgumentOutOfRangeException("targetTrisCount");

            int deletedTris = 0;
            ResizableArray<bool> deleted0 = new ResizableArray<bool>(20);
            ResizableArray<bool> deleted1 = new ResizableArray<bool>(20);
            var triangles = this.triangles.Data;
            int triangleCount = this.triangles.Length;
            int startTrisCount = triangleCount;
            var vertices = this.vertices.Data;

            int maxVertexCount = base.MaxVertexCount;
            if (maxVertexCount <= 0)
                maxVertexCount = int.MaxValue;

            for (int iteration = 0; iteration < maxIterationCount; iteration++)
            {
                ReportStatus(iteration, startTrisCount, (startTrisCount - deletedTris), targetTrisCount);
                if ((startTrisCount - deletedTris) <= targetTrisCount && remainingVertices < maxVertexCount)
                    break;

                // Update mesh once in a while
                if ((iteration % 5) == 0)
                {
                    UpdateMesh(iteration);
                    triangles = this.triangles.Data;
                    triangleCount = this.triangles.Length;
                    vertices = this.vertices.Data;
                }

                // Clear dirty flag
                for (int i = 0; i < triangleCount; i++)
                {
                    triangles[i].dirty = false;
                }

                // All triangles with edges below the threshold will be removed
                //
                // The following numbers works well for most models.
                // If it does not, try to adjust the 3 parameters
                double threshold = 0.000000001 * System.Math.Pow(iteration + 3, agressiveness);

                if ((iteration % 5) == 0)
                {
                    GlobalLogger.GetLogger("Asset").Verbose(
                        string.Format("iteration {0} - triangles {1} threshold {2}", 
                            iteration, 
                            (startTrisCount - deletedTris), 
                            threshold)
                    );
                }

                // Remove vertices & mark deleted triangles
                RemoveVertexPass(startTrisCount, targetTrisCount, threshold, deleted0, deleted1, ref deletedTris);
            }

            CompactMesh();
        }
        #endregion

        #region Decimate Mesh Lossless
        /// <summary>
        /// Decimates the mesh without losing any quality.
        /// </summary>
        public override void DecimateMeshLossless()
        {
            int deletedTris = 0;
            ResizableArray<bool> deleted0 = new ResizableArray<bool>(0);
            ResizableArray<bool> deleted1 = new ResizableArray<bool>(0);
            var triangles = this.triangles.Data;
            int triangleCount = this.triangles.Length;
            int startTrisCount = triangleCount;
            var vertices = this.vertices.Data;

            ReportStatus(0, startTrisCount, startTrisCount, -1);
            for (int iteration = 0; iteration < 9999; iteration++)
            {
                // Update mesh constantly
                UpdateMesh(iteration);
                triangles = this.triangles.Data;
                triangleCount = this.triangles.Length;
                vertices = this.vertices.Data;

                ReportStatus(iteration, startTrisCount, triangleCount, -1);

                // Clear dirty flag
                for (int i = 0; i < triangleCount; i++)
                {
                    triangles[i].dirty = false;
                }

                // All triangles with edges below the threshold will be removed
                //
                // The following numbers works well for most models.
                // If it does not, try to adjust the 3 parameters
                double threshold = DoubleEpsilon;
                
                GlobalLogger.GetLogger("Asset").Verbose(
                    string.Format("Lossless iteration {2}", iteration)
                );

                // Remove vertices & mark deleted triangles
                RemoveVertexPass(startTrisCount, 0, threshold, deleted0, deleted1, ref deletedTris);

                if (deletedTris <= 0)
                    break;

                deletedTris = 0;
            }

            CompactMesh();
        }
        #endregion

        #region To Mesh
        /// <summary>
        /// Returns the resulting mesh.
        /// </summary>
        /// <returns>The resulting mesh.</returns>
        public override MeshDecimatorData ToMesh()
        {
            int vertexCount = this.vertices.Length;
            int triangleCount = this.triangles.Length;
            var vertices = new Double3[vertexCount];
            var indices = new int[subMeshCount][];

            var vertArr = this.vertices.Data;
            for (int i = 0; i < vertexCount; i++)
            {
                vertices[i] = vertArr[i].p;
            }

            // First get the sub-mesh offsets
            var triArr = this.triangles.Data;
            int[] subMeshOffsets = new int[subMeshCount];
            int lastSubMeshOffset = -1;
            for (int i = 0; i < triangleCount; i++)
            {
                var triangle = triArr[i];
                if (triangle.subMeshIndex != lastSubMeshOffset)
                {
                    for (int j = lastSubMeshOffset + 1; j < triangle.subMeshIndex; j++)
                    {
                        subMeshOffsets[j] = i;
                    }
                    subMeshOffsets[triangle.subMeshIndex] = i;
                    lastSubMeshOffset = triangle.subMeshIndex;
                }
            }
            for (int i = lastSubMeshOffset + 1; i < subMeshCount; i++)
            {
                subMeshOffsets[i] = triangleCount;
            }

            // Then setup the sub-meshes
            for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
            {
                int startOffset = subMeshOffsets[subMeshIndex];
                if (startOffset < triangleCount)
                {
                    int endOffset = ((subMeshIndex + 1) < subMeshCount ? subMeshOffsets[subMeshIndex + 1] : triangleCount);
                    int subMeshTriangleCount = endOffset - startOffset;
                    if (subMeshTriangleCount < 0) subMeshTriangleCount = 0;
                    int[] subMeshIndices = new int[subMeshTriangleCount * 3];

                    for (int triangleIndex = startOffset; triangleIndex < endOffset; triangleIndex++)
                    {
                        var triangle = triArr[triangleIndex];
                        int offset = (triangleIndex - startOffset) * 3;
                        subMeshIndices[offset] = triangle.v0;
                        subMeshIndices[offset + 1] = triangle.v1;
                        subMeshIndices[offset + 2] = triangle.v2;
                    }

                    indices[subMeshIndex] = subMeshIndices;
                }
                else
                {
                    // This mesh doesn't have any triangles left
                    indices[subMeshIndex] = new int[0];
                }
            }

            MeshDecimatorData newMesh = new MeshDecimatorData(vertices, indices);

            if (vertNormals != null)
            {
                newMesh.Normals = vertNormals.Data;
            }
            if (vertTangents != null)
            {
                newMesh.Tangents = vertTangents.Data;
            }
            if (vertColors != null)
            {
                newMesh.Colors = vertColors.Data;
            }
            if (vertBoneWeights != null)
            {
                newMesh.BoneWeights = vertBoneWeights.Data;
            }

            if (vertUV2D != null)
            {
                for (int i = 0; i < MeshDecimatorData.UVChannelCount; i++)
                {
                    if (vertUV2D[i] != null)
                    {
                        var uvSet = vertUV2D[i].Data;
                        newMesh.SetUVs(i, uvSet);
                    }
                }
            }

            if (vertUV3D != null)
            {
                for (int i = 0; i < MeshDecimatorData.UVChannelCount; i++)
                {
                    if (vertUV3D[i] != null)
                    {
                        var uvSet = vertUV3D[i].Data;
                        newMesh.SetUVs(i, uvSet);
                    }
                }
            }

            if (vertUV4D != null)
            {
                for (int i = 0; i < MeshDecimatorData.UVChannelCount; i++)
                {
                    if (vertUV4D[i] != null)
                    {
                        var uvSet = vertUV4D[i].Data;
                        newMesh.SetUVs(i, uvSet);
                    }
                }
            }

            return newMesh;
        }
        #endregion
        #endregion
    }
}
