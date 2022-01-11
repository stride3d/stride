#region License
/*
    MIT License
    Copyright(c) 2017-2018 Mattias Edlund
    Copyright(c) 2021 Stefan Boronczyk
*/
#endregion

using System;
using Stride.Rendering.MeshDecimator.Algorithms;
using Stride.Core.Mathematics;
using Stride.Rendering.Rendering.MeshDecimator.Math;
using System.Collections.Generic;
using Stride.Rendering.Rendering.MeshDecimator.Types;

namespace Stride.Rendering.MeshDecimator
{
/// <summary>
    /// A mesh.
    /// </summary>
    public sealed class MeshDecimatorData
    {
        #region Consts
        /// <summary>
        /// The count of supported UV channels.
        /// </summary>
        public const int UVChannelCount = 4;
        #endregion

        #region Fields
        private Double3[] vertices = null;
        private int[][] indices = null;
        private Vector3[] normals = null;
        private Vector4[] tangents = null;
        private Vector2[][] uvs2D = null;
        private Vector3[][] uvs3D = null;
        private Vector4[][] uvs4D = null;
        private Vector4[] colors = null;
        private BoneWeight[] boneWeights = null;

        private static readonly int[] emptyIndices = new int[0];
        #endregion

        #region Properties
        /// <summary>
        /// Gets the count of vertices of this mesh.
        /// </summary>
        public int VertexCount
        {
            get { return vertices.Length; }
        }

        /// <summary>
        /// Gets or sets the count of submeshes in this mesh.
        /// </summary>
        public int SubMeshCount
        {
            get { return indices.Length; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value");

                int[][] newIndices = new int[value][];
                Array.Copy(indices, 0, newIndices, 0, MathHelper.Min(indices.Length, newIndices.Length));
                indices = newIndices;
            }
        }

        /// <summary>
        /// Gets the total count of triangles in this mesh.
        /// </summary>
        public int TriangleCount
        {
            get
            {
                int triangleCount = 0;
                for (int i = 0; i < indices.Length; i++)
                {
                    if (indices[i] != null)
                    {
                        triangleCount += indices[i].Length / 3;
                    }
                }
                return triangleCount;
            }
        }

        /// <summary>
        /// Gets or sets the vertices for this mesh. Note that this resets all other vertex attributes.
        /// </summary>
        public Double3[] Vertices
        {
            get { return vertices; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                vertices = value;
                ClearVertexAttributes();
            }
        }

        /// <summary>
        /// Gets or sets the combined indices for this mesh. Once set, the sub-mesh count gets set to 1.
        /// </summary>
        public int[] Indices
        {
            get
            {
                if (indices.Length == 1)
                {
                    return indices[0] ?? emptyIndices;
                }
                else
                {
                    List<int> indexList = new List<int>(TriangleCount * 3);
                    for (int i = 0; i < indices.Length; i++)
                    {
                        if (indices[i] != null)
                        {
                            indexList.AddRange(indices[i]);
                        }
                    }
                    return indexList.ToArray();
                }
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                else if ((value.Length % 3) != 0)
                    throw new ArgumentException("The index count must be multiple by 3.", "value");

                SubMeshCount = 1;
                SetIndices(0, value);
            }
        }

        /// <summary>
        /// Gets or sets the normals for this mesh.
        /// </summary>
        public Vector3[] Normals
        {
            get { return normals; }
            set
            {
                if (value != null && value.Length != vertices.Length)
                    throw new ArgumentException(string.Format("The vertex normals must be as many as the vertices. Assigned: {0}  Require: {1}", value.Length, vertices.Length));

                normals = value;
            }
        }

        /// <summary>
        /// Gets or sets the tangents for this mesh.
        /// </summary>
        public Vector4[] Tangents
        {
            get { return tangents; }
            set
            {
                if (value != null && value.Length != vertices.Length)
                    throw new ArgumentException(string.Format("The vertex tangents must be as many as the vertices. Assigned: {0}  Require: {1}", value.Length, vertices.Length));

                tangents = value;
            }
        }

        /// <summary>
        /// Gets or sets the first UV set for this mesh.
        /// </summary>
        public Vector2[] UV1
        {
            get { return GetUVs2D(0); }
            set { SetUVs(0, value); }
        }

        /// <summary>
        /// Gets or sets the second UV set for this mesh.
        /// </summary>
        public Vector2[] UV2
        {
            get { return GetUVs2D(1); }
            set { SetUVs(1, value); }
        }

        /// <summary>
        /// Gets or sets the third UV set for this mesh.
        /// </summary>
        public Vector2[] UV3
        {
            get { return GetUVs2D(2); }
            set { SetUVs(2, value); }
        }

        /// <summary>
        /// Gets or sets the fourth UV set for this mesh.
        /// </summary>
        public Vector2[] UV4
        {
            get { return GetUVs2D(3); }
            set { SetUVs(3, value); }
        }

        /// <summary>
        /// Gets or sets the vertex colors for this mesh.
        /// </summary>
        public Vector4[] Colors
        {
            get { return colors; }
            set
            {
                if (value != null && value.Length != vertices.Length)
                    throw new ArgumentException(string.Format("The vertex colors must be as many as the vertices. Assigned: {0}  Require: {1}", value.Length, vertices.Length));

                colors = value;
            }
        }

        /// <summary>
        /// Gets or sets the vertex bone weights for this mesh.
        /// </summary>
        public BoneWeight[] BoneWeights
        {
            get { return boneWeights; }
            set
            {
                if (value != null && value.Length != vertices.Length)
                    throw new ArgumentException(string.Format("The vertex bone weights must be as many as the vertices. Assigned: {0}  Require: {1}", value.Length, vertices.Length));

                boneWeights = value;
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new mesh.
        /// </summary>
        /// <param name="vertices">The mesh vertices.</param>
        /// <param name="indices">The mesh indices.</param>
        public MeshDecimatorData(Double3[] vertices, int[] indices)
        {
            if (vertices == null)
                throw new ArgumentNullException("vertices");
            else if (indices == null)
                throw new ArgumentNullException("indices");
            else if ((indices.Length % 3) != 0)
                throw new ArgumentException("The index count must be multiple by 3.", "indices");

            this.vertices = vertices;
            this.indices = new int[1][];
            this.indices[0] = indices;
        }

        /// <summary>
        /// Creates a new mesh.
        /// </summary>
        /// <param name="vertices">The mesh vertices.</param>
        /// <param name="indices">The mesh indices.</param>
        public MeshDecimatorData(Double3[] vertices, int[][] indices)
        {
            if (vertices == null)
                throw new ArgumentNullException("vertices");
            else if (indices == null)
                throw new ArgumentNullException("indices");

            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i] != null && (indices[i].Length % 3) != 0)
                    throw new ArgumentException(string.Format("The index count must be multiple by 3 at sub-mesh index {0}.", i), "indices");
            }

            this.vertices = vertices;
            this.indices = indices;
        }
        #endregion

        #region Private Methods
        private void ClearVertexAttributes()
        {
            normals = null;
            tangents = null;
            uvs2D = null;
            uvs3D = null;
            uvs4D = null;
            colors = null;
            boneWeights = null;
        }
        #endregion

        #region Public Methods
        #region Recalculate Normals
        /// <summary>
        /// Recalculates the normals for this mesh smoothly.
        /// </summary>
        public void RecalculateNormals()
        {
            int vertexCount = vertices.Length;
            Vector3[] normals = new Vector3[vertexCount];

            int subMeshCount = this.indices.Length;
            for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
            {
                int[] indices = this.indices[subMeshIndex];
                if (indices == null)
                    continue;

                int indexCount = indices.Length;
                for (int i = 0; i < indexCount; i += 3)
                {
                    int i0 = indices[i];
                    int i1 = indices[i + 1];
                    int i2 = indices[i + 2];

                    var v0 = (Vector3)vertices[i0];
                    var v1 = (Vector3)vertices[i1];
                    var v2 = (Vector3)vertices[i2];

                    var nx = v1 - v0;
                    var ny = v2 - v0;
                    Vector3 normal;
                    Vector3.Cross(ref nx, ref ny, out normal);
                    normal.Normalize();

                    normals[i0] += normal;
                    normals[i1] += normal;
                    normals[i2] += normal;
                }
            }

            for (int i = 0; i < vertexCount; i++)
            {
                normals[i].Normalize();
            }

            this.normals = normals;
        }
        #endregion

        #region Recalculate Tangents
        /// <summary>
        /// Recalculates the tangents for this mesh.
        /// </summary>
        public void RecalculateTangents()
        {
            // Make sure we have the normals first
            if (normals == null)
                return;

            // Also make sure that we have the first UV set
            bool uvIs2D = (uvs2D != null && uvs2D[0] != null);
            bool uvIs3D = (uvs3D != null && uvs3D[0] != null);
            bool uvIs4D = (uvs4D != null && uvs4D[0] != null);
            if (!uvIs2D && !uvIs3D && !uvIs4D)
                return;

            int vertexCount = vertices.Length;
            
            var tangents = new Vector4[vertexCount];
            var tan1 = new Vector3[vertexCount];
            var tan2 = new Vector3[vertexCount];

            Vector2[] uv2D = (uvIs2D ? uvs2D[0] : null);
            Vector3[] uv3D = (uvIs3D ? uvs3D[0] : null);
            Vector4[] uv4D = (uvIs4D ? uvs4D[0] : null);

            int subMeshCount = this.indices.Length;
            for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
            {
                int[] indices = this.indices[subMeshIndex];
                if (indices == null)
                    continue;

                int indexCount = indices.Length;
                for (int i = 0; i < indexCount; i += 3)
                {
                    int i0 = indices[i];
                    int i1 = indices[i + 1];
                    int i2 = indices[i + 2];

                    var v0 = vertices[i0];
                    var v1 = vertices[i1];
                    var v2 = vertices[i2];

                    float s1, s2, t1, t2;
                    if (uvIs2D)
                    {
                        var w0 = uv2D[i0];
                        var w1 = uv2D[i1];
                        var w2 = uv2D[i2];
                        s1 = w1.X - w0.X;
                        s2 = w2.X - w0.X;
                        t1 = w1.Y - w0.Y;
                        t2 = w2.Y - w0.Y;
                    }
                    else if (uvIs3D)
                    {
                        var w0 = uv3D[i0];
                        var w1 = uv3D[i1];
                        var w2 = uv3D[i2];
                        s1 = w1.X - w0.X;
                        s2 = w2.X - w0.X;
                        t1 = w1.Y - w0.Y;
                        t2 = w2.Y - w0.Y;
                    }
                    else
                    {
                        var w0 = uv4D[i0];
                        var w1 = uv4D[i1];
                        var w2 = uv4D[i2];
                        s1 = w1.X - w0.X;
                        s2 = w2.X - w0.X;
                        t1 = w1.Y - w0.Y;
                        t2 = w2.Y - w0.Y;
                    }
                    

                    float x1 = (float)(v1.X - v0.X);
                    float x2 = (float)(v2.X - v0.X);
                    float y1 = (float)(v1.Y - v0.Y);
                    float y2 = (float)(v2.Y - v0.Y);
                    float z1 = (float)(v1.Z - v0.Z);
                    float z2 = (float)(v2.Z - v0.Z);
                    float r = 1f / (s1 * t2 - s2 * t1);

                    var sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                    var tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                    tan1[i0] += sdir;
                    tan1[i1] += sdir;
                    tan1[i2] += sdir;
                    tan2[i0] += tdir;
                    tan2[i1] += tdir;
                    tan2[i2] += tdir;
                }
            }

            for (int i = 0; i < vertexCount; i++)
            {
                var n = normals[i];
                var t = tan1[i];

                float dotCross;
                Vector3.Dot(ref n, ref t, out dotCross);
                var tmp = (t - n * dotCross);
                tmp.Normalize();

                Vector3 c;
                Vector3.Cross(ref n, ref t, out c);

                float dot;
                Vector3.Dot(ref c, ref tan2[i], out dot);

                float w = (dot < 0f ? -1f : 1f);
                tangents[i] = new Vector4(tmp.X, tmp.Y, tmp.Z, w);
            }

            this.tangents = tangents;
        }
        #endregion

        #region Triangles
        /// <summary>
        /// Returns the count of triangles for a specific sub-mesh in this mesh.
        /// </summary>
        /// <param name="subMeshIndex">The sub-mesh index.</param>
        /// <returns>The triangle count.</returns>
        public int GetTriangleCount(int subMeshIndex)
        {
            if (subMeshIndex < 0 || subMeshIndex >= indices.Length)
                throw new IndexOutOfRangeException();

            return indices[subMeshIndex].Length / 3;
        }

        /// <summary>
        /// Returns the triangle indices of a specific sub-mesh in this mesh.
        /// </summary>
        /// <param name="subMeshIndex">The sub-mesh index.</param>
        /// <returns>The triangle indices.</returns>
        public int[] GetIndices(int subMeshIndex)
        {
            if (subMeshIndex < 0 || subMeshIndex >= indices.Length)
                throw new IndexOutOfRangeException();

            return indices[subMeshIndex] ?? emptyIndices;
        }

        /// <summary>
        /// Returns the triangle indices for all sub-meshes in this mesh.
        /// </summary>
        /// <returns>The sub-mesh triangle indices.</returns>
        public int[][] GetSubMeshIndices()
        {
            var subMeshIndices = new int[indices.Length][];
            for (int subMeshIndex = 0; subMeshIndex < indices.Length; subMeshIndex++)
            {
                subMeshIndices[subMeshIndex] = indices[subMeshIndex] ?? emptyIndices;
            }
            return subMeshIndices;
        }

        /// <summary>
        /// Sets the triangle indices of a specific sub-mesh in this mesh.
        /// </summary>
        /// <param name="subMeshIndex">The sub-mesh index.</param>
        /// <param name="indices">The triangle indices.</param>
        public void SetIndices(int subMeshIndex, int[] indices)
        {
            if (subMeshIndex < 0 || subMeshIndex >= this.indices.Length)
                throw new IndexOutOfRangeException();
            else if (indices == null)
                throw new ArgumentNullException("indices");
            else if ((indices.Length % 3) != 0)
                throw new ArgumentException("The index count must be multiple by 3.", "indices");

            this.indices[subMeshIndex] = indices;
        }
        #endregion

        #region UV Sets
        #region Getting
        /// <summary>
        /// Returns the UV dimension for a specific channel.
        /// </summary>
        /// <param name="channel"></param>
        /// <returns>The UV dimension count.</returns>
        public int GetUVDimension(int channel)
        {
            if (channel < 0 || channel >= UVChannelCount)
                throw new ArgumentOutOfRangeException("channel");

            if (uvs2D != null && uvs2D[channel] != null)
            {
                return 2;
            }
            else if (uvs3D != null && uvs3D[channel] != null)
            {
                return 3;
            }
            else if (uvs4D != null && uvs4D[channel] != null)
            {
                return 4;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Returns the UVs (2D) from a specific channel.
        /// </summary>
        /// <param name="channel">The channel index.</param>
        /// <returns>The UVs.</returns>
        public Vector2[] GetUVs2D(int channel)
        {
            if (channel < 0 || channel >= UVChannelCount)
                throw new ArgumentOutOfRangeException("channel");

            if (uvs2D != null && uvs2D[channel] != null)
            {
                return uvs2D[channel];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the UVs (3D) from a specific channel.
        /// </summary>
        /// <param name="channel">The channel index.</param>
        /// <returns>The UVs.</returns>
        public Vector3[] GetUVs3D(int channel)
        {
            if (channel < 0 || channel >= UVChannelCount)
                throw new ArgumentOutOfRangeException("channel");

            if (uvs3D != null && uvs3D[channel] != null)
            {
                return uvs3D[channel];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the UVs (4D) from a specific channel.
        /// </summary>
        /// <param name="channel">The channel index.</param>
        /// <returns>The UVs.</returns>
        public Vector4[] GetUVs4D(int channel)
        {
            if (channel < 0 || channel >= UVChannelCount)
                throw new ArgumentOutOfRangeException("channel");

            if (uvs4D != null && uvs4D[channel] != null)
            {
                return uvs4D[channel];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the UVs (2D) from a specific channel.
        /// </summary>
        /// <param name="channel">The channel index.</param>
        /// <param name="uvs">The UVs.</param>
        public void GetUVs(int channel, List<Vector2> uvs)
        {
            if (channel < 0 || channel >= UVChannelCount)
                throw new ArgumentOutOfRangeException("channel");
            else if (uvs == null)
                throw new ArgumentNullException("uvs");

            uvs.Clear();
            if (uvs2D != null && uvs2D[channel] != null)
            {
                var uvData = uvs2D[channel];
                if (uvData != null)
                {
                    uvs.AddRange(uvData);
                }
            }
        }

        /// <summary>
        /// Returns the UVs (3D) from a specific channel.
        /// </summary>
        /// <param name="channel">The channel index.</param>
        /// <param name="uvs">The UVs.</param>
        public void GetUVs(int channel, List<Vector3> uvs)
        {
            if (channel < 0 || channel >= UVChannelCount)
                throw new ArgumentOutOfRangeException("channel");
            else if (uvs == null)
                throw new ArgumentNullException("uvs");

            uvs.Clear();
            if (uvs3D != null && uvs3D[channel] != null)
            {
                var uvData = uvs3D[channel];
                if (uvData != null)
                {
                    uvs.AddRange(uvData);
                }
            }
        }

        /// <summary>
        /// Returns the UVs (4D) from a specific channel.
        /// </summary>
        /// <param name="channel">The channel index.</param>
        /// <param name="uvs">The UVs.</param>
        public void GetUVs(int channel, List<Vector4> uvs)
        {
            if (channel < 0 || channel >= UVChannelCount)
                throw new ArgumentOutOfRangeException("channel");
            else if (uvs == null)
                throw new ArgumentNullException("uvs");

            uvs.Clear();
            if (uvs4D != null && uvs4D[channel] != null)
            {
                var uvData = uvs4D[channel];
                if (uvData != null)
                {
                    uvs.AddRange(uvData);
                }
            }
        }
        #endregion

        #region Setting
        /// <summary>
        /// Sets the UVs (2D) for a specific channel.
        /// </summary>
        /// <param name="channel">The channel index.</param>
        /// <param name="uvs">The UVs.</param>
        public void SetUVs(int channel, Vector2[] uvs)
        {
            if (channel < 0 || channel >= UVChannelCount)
                throw new ArgumentOutOfRangeException("channel");

            if (uvs != null && uvs.Length > 0)
            {
                if (uvs.Length != vertices.Length)
                    throw new ArgumentException(string.Format("The vertex UVs must be as many as the vertices. Assigned: {0}  Require: {1}", uvs.Length, vertices.Length));

                if (uvs2D == null)
                    uvs2D = new Vector2[UVChannelCount][];

                int uvCount = uvs.Length;
                var uvSet = new Vector2[uvCount];
                uvs2D[channel] = uvSet;
                uvs.CopyTo(uvSet, 0);
            }
            else
            {
                if (uvs2D != null)
                {
                    uvs2D[channel] = null;
                }
            }

            if (uvs3D != null)
            {
                uvs3D[channel] = null;
            }
            if (uvs4D != null)
            {
                uvs4D[channel] = null;
            }
        }

        /// <summary>
        /// Sets the UVs (3D) for a specific channel.
        /// </summary>
        /// <param name="channel">The channel index.</param>
        /// <param name="uvs">The UVs.</param>
        public void SetUVs(int channel, Vector3[] uvs)
        {
            if (channel < 0 || channel >= UVChannelCount)
                throw new ArgumentOutOfRangeException("channel");

            if (uvs != null && uvs.Length > 0)
            {
                int uvCount = uvs.Length;
                if (uvCount != vertices.Length)
                    throw new ArgumentException(string.Format("The vertex UVs must be as many as the vertices. Assigned: {0}  Require: {1}", uvCount, vertices.Length), "uvs");

                if (uvs3D == null)
                    uvs3D = new Vector3[UVChannelCount][];

                var uvSet = new Vector3[uvCount];
                uvs3D[channel] = uvSet;
                uvs.CopyTo(uvSet, 0);
            }
            else
            {
                if (uvs3D != null)
                {
                    uvs3D[channel] = null;
                }
            }

            if (uvs2D != null)
            {
                uvs2D[channel] = null;
            }
            if (uvs4D != null)
            {
                uvs4D[channel] = null;
            }
        }

        /// <summary>
        /// Sets the UVs (4D) for a specific channel.
        /// </summary>
        /// <param name="channel">The channel index.</param>
        /// <param name="uvs">The UVs.</param>
        public void SetUVs(int channel, Vector4[] uvs)
        {
            if (channel < 0 || channel >= UVChannelCount)
                throw new ArgumentOutOfRangeException("channel");

            if (uvs != null && uvs.Length > 0)
            {
                int uvCount = uvs.Length;
                if (uvCount != vertices.Length)
                    throw new ArgumentException(string.Format("The vertex UVs must be as many as the vertices. Assigned: {0}  Require: {1}", uvCount, vertices.Length), "uvs");

                if (uvs4D == null)
                    uvs4D = new Vector4[UVChannelCount][];

                var uvSet = new Vector4[uvCount];
                uvs4D[channel] = uvSet;
                uvs.CopyTo(uvSet, 0);
            }
            else
            {
                if (uvs4D != null)
                {
                    uvs4D[channel] = null;
                }
            }

            if (uvs2D != null)
            {
                uvs2D[channel] = null;
            }
            if (uvs3D != null)
            {
                uvs3D[channel] = null;
            }
        }

        /// <summary>
        /// Sets the UVs (2D) for a specific channel.
        /// </summary>
        /// <param name="channel">The channel index.</param>
        /// <param name="uvs">The UVs.</param>
        public void SetUVs(int channel, List<Vector2> uvs)
        {
            if (channel < 0 || channel >= UVChannelCount)
                throw new ArgumentOutOfRangeException("channel");

            if (uvs != null && uvs.Count > 0)
            {
                int uvCount = uvs.Count;
                if (uvCount != vertices.Length)
                    throw new ArgumentException(string.Format("The vertex UVs must be as many as the vertices. Assigned: {0}  Require: {1}", uvCount, vertices.Length), "uvs");

                if (uvs2D == null)
                    uvs2D = new Vector2[UVChannelCount][];

                var uvSet = new Vector2[uvCount];
                uvs2D[channel] = uvSet;
                uvs.CopyTo(uvSet, 0);
            }
            else
            {
                if (uvs2D != null)
                {
                    uvs2D[channel] = null;
                }
            }

            if (uvs3D != null)
            {
                uvs3D[channel] = null;
            }
            if (uvs4D != null)
            {
                uvs4D[channel] = null;
            }
        }

        /// <summary>
        /// Sets the UVs (3D) for a specific channel.
        /// </summary>
        /// <param name="channel">The channel index.</param>
        /// <param name="uvs">The UVs.</param>
        public void SetUVs(int channel, List<Vector3> uvs)
        {
            if (channel < 0 || channel >= UVChannelCount)
                throw new ArgumentOutOfRangeException("channel");

            if (uvs != null && uvs.Count > 0)
            {
                int uvCount = uvs.Count;
                if (uvCount != vertices.Length)
                    throw new ArgumentException(string.Format("The vertex UVs must be as many as the vertices. Assigned: {0}  Require: {1}", uvCount, vertices.Length), "uvs");

                if (uvs3D == null)
                    uvs3D = new Vector3[UVChannelCount][];

                var uvSet = new Vector3[uvCount];
                uvs3D[channel] = uvSet;
                uvs.CopyTo(uvSet, 0);
            }
            else
            {
                if (uvs3D != null)
                {
                    uvs3D[channel] = null;
                }
            }

            if (uvs2D != null)
            {
                uvs2D[channel] = null;
            }
            if (uvs4D != null)
            {
                uvs4D[channel] = null;
            }
        }

        /// <summary>
        /// Sets the UVs (4D) for a specific channel.
        /// </summary>
        /// <param name="channel">The channel index.</param>
        /// <param name="uvs">The UVs.</param>
        public void SetUVs(int channel, List<Vector4> uvs)
        {
            if (channel < 0 || channel >= UVChannelCount)
                throw new ArgumentOutOfRangeException("channel");

            if (uvs != null && uvs.Count > 0)
            {
                int uvCount = uvs.Count;
                if (uvCount != vertices.Length)
                    throw new ArgumentException(string.Format("The vertex UVs must be as many as the vertices. Assigned: {0}  Require: {1}", uvCount, vertices.Length), "uvs");

                if (uvs4D == null)
                    uvs4D = new Vector4[UVChannelCount][];

                var uvSet = new Vector4[uvCount];
                uvs4D[channel] = uvSet;
                uvs.CopyTo(uvSet, 0);
            }
            else
            {
                if (uvs4D != null)
                {
                    uvs4D[channel] = null;
                }
            }

            if (uvs2D != null)
            {
                uvs2D[channel] = null;
            }
            if (uvs3D != null)
            {
                uvs3D[channel] = null;
            }
        }
        #endregion
        #endregion

        #region To String
        /// <summary>
        /// Returns the text-representation of this mesh.
        /// </summary>
        /// <returns>The text-representation.</returns>
        public override string ToString()
        {
            return string.Format("Vertices: {0}", vertices.Length);
        }
        #endregion
        #endregion
    }
}   
