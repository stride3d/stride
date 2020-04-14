// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Stride.Core;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Native;

namespace Stride.Rendering.LightProbes
{
    /// <summary>
    /// Bowyer-Watson tetrahedralization algorithm. More details at http://en.wikipedia.org/wiki/Bowyer%E2%80%93Watson_algorithm.
    /// </summary>
    public class BowyerWatsonTetrahedralization
    {
        // Add 100 meters for extrapolation
        // TODO: Make this customizable
        public const float ExtrapolationDistance = 100.0f;

#pragma warning disable SA1300 // Element should begin with upper-case letter
        [DllImport(NativeInvoke.Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void exactinit();

        [DllImport(NativeInvoke.Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern float orient3d(ref Vector3 pa, ref Vector3 pb, ref Vector3 pc, ref Vector3 pd);

        [DllImport(NativeInvoke.Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern float insphere(ref Vector3 pa, ref Vector3 pb, ref Vector3 pc, ref Vector3 pd, ref Vector3 pe);
#pragma warning restore SA1300 // Element should begin with upper-case letter

        private readonly List<int> badTetrahedra = new List<int>();
        private readonly FastList<HoleFace> holeFaces = new FastList<HoleFace>();
        private readonly List<HoleEdge> edges = new List<HoleEdge>();
        private readonly List<int> freeTetrahedra = new List<int>();

        private Vector3[] vertices;

        private FastList<Tetrahedron> tetrahedralization;

        public struct Result
        {
            public Vector3[] Vertices;
            /// <summary>
            /// Any vertex in <see cref="Vertices"/> after this index are added automatically for boundaries.
            /// </summary>
            public int UserVertexCount;
            public FastList<Tetrahedron> Tetrahedra;
            public FastList<Face> Faces;
        }

        [DataSerializer(typeof(Face.Serializer))]
        public struct Face
        {
            public unsafe fixed int Vertices[3];
            public Vector3 Normal;
            public int FrontTetrahedron;
            public int BackTetrahedron;
            public sbyte FrontFace;
            public sbyte BackFace;

            public class Serializer : DataSerializer<Face>
            {
                public override unsafe void Serialize(ref Face face, ArchiveMode mode, SerializationStream stream)
                {
                    fixed (Face* facePtr = &face)
                        stream.Serialize((IntPtr)facePtr, sizeof(Face));
                }
            }

            public override unsafe string ToString()
            {
                fixed (int* vertices = Vertices)
                    return $"Vertices: {vertices[0]} {vertices[1]} {vertices[2]}; Normal: {Normal}; Front: tetra {FrontTetrahedron} face {FrontFace}; Back: tetra {BackTetrahedron} face {BackFace}";
            }
        }

        /// <summary>
        /// Represents a tetrahedron output as created by the <see cref="BowyerWatsonTetrahedralization"/> algorithm.
        /// </summary>
        [DataSerializer(typeof(Tetrahedron.Serializer))]
        public struct Tetrahedron
        {
            /// <summary>
            /// The vertices (as indices).
            /// </summary>
            public unsafe fixed int Vertices[4];

            /// <summary>
            /// The tetrahedron neighbours (as indices). They match opposite face of <see cref="Vertices"/> with same index.
            /// </summary>
            public unsafe fixed int Neighbours[4];

            /// <summary>
            /// The tetrahedron faces (as indices). If using bitwise complement (negative), it means the normal is reversed.
            /// </summary>
            public unsafe fixed int Faces[4];

            public override unsafe string ToString()
            {
                fixed (int* vertices = Vertices)
                fixed (int* neighbours = Neighbours)
                    return $"Vertices: {vertices[0]} {vertices[1]} {vertices[2]} {vertices[3]}; Neighbours: {neighbours[0]} {neighbours[1]} {neighbours[2]} {neighbours[3]}";
            }

            public class Serializer : DataSerializer<Tetrahedron>
            {
                public override unsafe void Serialize(ref Tetrahedron tetrahedron, ArchiveMode mode, SerializationStream stream)
                {
                    fixed (Tetrahedron* tetrahedronPtr = &tetrahedron)
                        stream.Serialize((IntPtr)tetrahedronPtr, sizeof(Tetrahedron));
                }
            }
        }

        static BowyerWatsonTetrahedralization()
        {
            // TODO: Add native to Stride.Engine?
#if STRIDE_PLATFORM_WINDOWS
            NativeLibrary.PreloadLibrary(NativeInvoke.Library + ".dll", typeof(BowyerWatsonTetrahedralization));
#endif
            exactinit();
        }

        public Result Compute(IReadOnlyList<Vector3> vertices)
        {
            // Stores 4 additional vertices for the enclosing "super-tetrahedron"
            // TODO: Another approach would be to receive a IList/Array directly and have a method GetVertexAtIndex(i) with special care for i in [vertices.Length; vertices.Length + 4[ range.
            this.vertices = vertices.Concat(new Vector3[4]).ToArray();

            tetrahedralization = new FastList<Tetrahedron>();
            freeTetrahedra.Clear();

            // Create super-tetrahedra that encompass everything
            AddSuperTetrahedron();

            // Add all the points one at a time to the tetrahedralization
            for (int index = 0; index < vertices.Count; index++)
            {
                AddVertex(index);
                CheckConnectivity();
            }

            // Add extra points
            GenerateExtrapolationProbes();

            // Done inserting points, now removes super-tetrahedron
            RemoveSuperTetrahedron(vertices.Count, vertices.Count + 4);

            // Remove unallocated tetrahedra from our list
            CleanupUnusedTetrahedra();

            // Generate faces
            var faces = GenerateFaces();

            return new Result
            {
                Vertices = this.vertices,
                UserVertexCount = vertices.Count,
                Tetrahedra = tetrahedralization,
                Faces = faces,
            };
        }

        private unsafe void GenerateExtrapolationProbes()
        {
            var outerVertexNormals = new Vector3[vertices.Length];

            var extraVertices = new List<Vector3>();

            // Sum normals on outer vertices
            fixed (Tetrahedron* tetrahedra = tetrahedralization.Items)
            {
                for (int index = 0; index < tetrahedralization.Count; index++)
                {
                    var currentTetrahedron = &tetrahedra[index];

                    var superTetrahedronVertexCount = 0;
                    var superTetrahedronVertexIndex = -1;
                    for (int i = 0; i < 4; ++i)
                    {
                        if (currentTetrahedron->Vertices[i] >= vertices.Length - 4)
                        {
                            superTetrahedronVertexCount++;
                            superTetrahedronVertexIndex = i;
                        }
                    }

                    // One vertex at infinity, 3 inside (face)
                    if (superTetrahedronVertexCount == 1)
                    {
                        var vertex0 = currentTetrahedron->Vertices[(superTetrahedronVertexIndex + 1) % 4];
                        var vertex1 = currentTetrahedron->Vertices[(superTetrahedronVertexIndex + 2) % 4];
                        var vertex2 = currentTetrahedron->Vertices[(superTetrahedronVertexIndex + 3) % 4];
                        var superTetrahedronVertex = currentTetrahedron->Vertices[superTetrahedronVertexIndex];

                        // Compute normal
                        Vector3 edge1, edge2, faceNormal;
                        Vector3.Subtract(ref vertices[vertex1], ref vertices[vertex0], out edge1);
                        Vector3.Subtract(ref vertices[vertex2], ref vertices[vertex0], out edge2);
                        Vector3.Cross(ref edge1, ref edge2, out faceNormal);
                        faceNormal.Normalize();

                        // Reverse it if not facing proper direction (extraVertex should be on the positive side)
                        var plane = new Plane(vertices[vertex0], faceNormal);
                        if (CollisionHelper.DistancePlanePoint(ref plane, ref vertices[superTetrahedronVertex]) < 0.0f)
                            faceNormal = -faceNormal;

                        // Sum normal to 3 vertices
                        outerVertexNormals[vertex0] += faceNormal;
                        outerVertexNormals[vertex1] += faceNormal;
                        outerVertexNormals[vertex2] += faceNormal;
                    }
                }
            }

            // Normalize and generate extrapolated probe
            for (int i = 0; i < outerVertexNormals.Length; ++i)
            {
                outerVertexNormals[i].Normalize();
                if (outerVertexNormals[i] != Vector3.Zero)
                {
                    extraVertices.Add(vertices[i] + outerVertexNormals[i] * ExtrapolationDistance);
                }
            }

            vertices = vertices.Concat(extraVertices).ToArray();

            // Add all the new points one at a time to the tetrahedralization
            for (int index = this.vertices.Length - extraVertices.Count; index < this.vertices.Length; index++)
            {
                AddVertex(index);
                CheckConnectivity();
            }
        }

        /// <summary>
        /// Generate faces info and normal.
        /// </summary>
        private unsafe FastList<Face> GenerateFaces()
        {
            var faces = new FastList<Face>();
            var currentFace = new Face();

            fixed (Tetrahedron* tetrahedra = tetrahedralization.Items)
            {
                for (int index = 0; index < tetrahedralization.Count; index++)
                {
                    var currentTetrahedron = &tetrahedra[index];

                    // Process each face
                    for (int i = 0; i < 4; ++i)
                    {
                        var neighbourTetrahedronIndex = currentTetrahedron->Neighbours[i];

                        // If no neighbour, it means there is no face
                        if (neighbourTetrahedronIndex == -1)
                            continue;

                        // Check if face is already created in neighbour tetrahedron
                        // If index is lower, it means it already exists (processed before)
                        if (neighbourTetrahedronIndex < index)
                        {
                            // Find which face are we in the neighbour tetrahedron
                            var neighbourTetrahedron = &tetrahedra[neighbourTetrahedronIndex];
                            for (int j = 0; j < 4; ++j)
                            {
                                if (neighbourTetrahedron->Neighbours[j] == index)
                                {
                                    // We store the bitwise complement since normal is opposite
                                    var oppositeFaceIndex = neighbourTetrahedron->Faces[j];
                                    currentTetrahedron->Faces[i] = ~oppositeFaceIndex;
                                    faces.Items[oppositeFaceIndex].BackTetrahedron = index;
                                    faces.Items[oppositeFaceIndex].BackFace = (sbyte)i;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // New face, let's create it
                            currentTetrahedron->Faces[i] = faces.Count;

                            // Create face
                            currentFace.FrontTetrahedron = index;
                            currentFace.FrontFace = (sbyte)i;
                            currentFace.BackTetrahedron = -1;
                            currentFace.BackFace = -1;
                            currentFace.Vertices[0] = currentTetrahedron->Vertices[(i + 1) % 4];                // 1 2 3 0
                            currentFace.Vertices[1] = currentTetrahedron->Vertices[3 - (i / 2) * 2];            // 3 3 1 1
                            currentFace.Vertices[2] = currentTetrahedron->Vertices[(((i + 3) / 2) * 2) % 4];    // 2 0 0 2

                            // Compute normal
                            Vector3 edge1, edge2, faceNormal;
                            Vector3.Subtract(ref vertices[currentFace.Vertices[1]], ref vertices[currentFace.Vertices[0]], out edge1);
                            Vector3.Subtract(ref vertices[currentFace.Vertices[2]], ref vertices[currentFace.Vertices[0]], out edge2);
                            Vector3.Cross(ref edge1, ref edge2, out faceNormal);
                            faceNormal.Normalize();

                            currentFace.Normal = faceNormal;

                            faces.Add(currentFace);
                        }
                    }
                }
            }
            return faces;
        }

        /// <summary>
        /// Cleanups the unused tetrahedra.
        /// </summary>
        private unsafe void CleanupUnusedTetrahedra()
        {
            fixed (Tetrahedron* tetrahedra = tetrahedralization.Items)
            {
                for (int index = 0; index < tetrahedralization.Count; index++)
                {
                    if (!IsTetrahedronAllocated(index))
                    {
                        // This is an unused tetrahedra, let's remove it
                        var currentTetrahedron = &tetrahedra[index];

                        // Swap-remove with latest tetrahedra (prevents RemoveAt shifting, only one tetrahedra is moving and needs its neighbour references updated)
                        int lastIndex = tetrahedralization.Count - 1;
                        if (index < lastIndex)
                        {
                            if (IsTetrahedronAllocated(lastIndex))
                            {
                                *currentTetrahedron = tetrahedra[lastIndex];

                                // We moved an allocated tretrahedra, we need to update neighbour indices pointing to this tetrahedra
                                // (neighbours pointing to lastIndex should now point to index)
                                for (int i = 0; i < 4; ++i)
                                {
                                    var neighbourTetrahedronIndex = currentTetrahedron->Neighbours[i];
                                    if (neighbourTetrahedronIndex == -1)
                                        continue;

                                    var neighbourTetrahedron = &tetrahedra[neighbourTetrahedronIndex];
                                    for (int j = 0; j < 4; ++j)
                                    {
                                        if (neighbourTetrahedron->Neighbours[j] == lastIndex)
                                            neighbourTetrahedron->Neighbours[j] = index;
                                    }
                                }
                            }
                            else
                            {
                                // Current tetrahedra is still not allocated. Go one step backward, so that next loop will also remove it.
                                index--;
                            }
                        }

                        tetrahedralization.RemoveAt(lastIndex);
                    }
                }

                // We can clear the free list as well
                freeTetrahedra.Clear();
            }
        }

        /// <summary>
        /// Removes the super tetrahedra, and any tetrahedra sharing its vertices.
        /// </summary>
        private unsafe void RemoveSuperTetrahedron(int startVertex, int endVertex)
        {
            fixed (Tetrahedron* tetrahedra = tetrahedralization.Items)
            {
                for (int index = 0; index < tetrahedralization.Count; index++)
                {
                    if (!IsTetrahedronAllocated(index))
                        continue;

                    var tetrahedron = &tetrahedra[index];

                    // Remove tetrahedra which have any point common with super tetrahedra (last 4 vertices)
                    for (int i = 0; i < 4; ++i)
                    {
                        if (tetrahedron->Vertices[i] >= startVertex && tetrahedron->Vertices[i] < endVertex)
                        {
                            FreeTetrahedron(index);
                            break;
                        }
                    }
                }

                // Remove invalid neighbour (pointing to nodes that were just deleted)
                for (int index = 0; index < tetrahedralization.Count; index++)
                {
                    if (!IsTetrahedronAllocated(index))
                        continue;

                    var tetrahedron = &tetrahedra[index];

                    // Remove tetrahedra which have any point common with super tetrahedra (last 4 vertices)
                    for (int i = 0; i < 4; ++i)
                    {
                        var neighbour = tetrahedron->Neighbours[i];
                        if (neighbour != -1 && !IsTetrahedronAllocated(neighbour))
                            tetrahedron->Neighbours[i] = -1;
                    }
                }
            }
        }

        /// <summary>
        /// Adds the super tetrahedron, which encompass every vertices.
        /// </summary>
        private unsafe void AddSuperTetrahedron()
        {
            // Compute the bounding box of the primitive
            var boundingBox = BoundingBox.Empty;
            for (int i = 0; i < vertices.Length; i++)
                BoundingBox.Merge(ref boundingBox, ref vertices[i], out boundingBox);

            // Add space for extrapolation
            boundingBox.Minimum -= ExtrapolationDistance;
            boundingBox.Maximum += ExtrapolationDistance;

            // Let's encompass the bounding box (Min:A, Max:B), in this manner (2D example, but same applies in 3D):
            // *
            // |\
            // | \
            // *--B
            // |  |\
            // A--*-*
            {
                // TODO: This might not be enough and we might want to pretend those points are at infinity (see incomplete implementation later)
                // http://stackoverflow.com/questions/30741459/bowyer-watson-algorithm-how-to-fill-holes-left-by-removing-triangles-with-sup#answer-36992359

                // Store additional vertices in extra space
                // Note: we build 3 extra vertices on a plane with normal (1,1,1) to makes computation easier
                // As an example, (assuming 2D), if B=(1,1) or (0.5,1.5), we can encompass it with (2,0) and (0,2))
                var combinedSize = boundingBox.Extent.X + boundingBox.Extent.Y + boundingBox.Extent.Z;
                vertices[vertices.Length - 4] = boundingBox.Minimum - boundingBox.Extent * 1000.0f;
                vertices[vertices.Length - 3] = boundingBox.Minimum + Vector3.UnitZ * combinedSize * 1000.0f;
                vertices[vertices.Length - 2] = boundingBox.Minimum + Vector3.UnitY * combinedSize * 1000.0f;
                vertices[vertices.Length - 1] = boundingBox.Minimum + Vector3.UnitX * combinedSize * 1000.0f;

                // Create super tetrahedron
                var superTetrahedron = new Tetrahedron();
                for (int i = 0; i < 4; ++i)
                {
                    superTetrahedron.Vertices[i] = vertices.Length - 4 + i;
                    superTetrahedron.Neighbours[i] = -1; // No neighbour
                }

                if (!IsTetrahedronPositiveOrder(vertices, ref superTetrahedron))
                    throw new InvalidOperationException("Tetrahedron not in positive order");

                tetrahedralization.Add(superTetrahedron);
            }
        }

        private unsafe void AddVertex(int vertexIndex)
        {
            // Clears reused structures (avoid reallocation every vertex)
            badTetrahedra.Clear();
            holeFaces.Clear();
            edges.Clear();

            var vertex = vertices[vertexIndex];

            fixed (Tetrahedron* tetrahedra = tetrahedralization.Items)
            {
                // First, find all the triangles that are no longer valid due to the insertion
                // TODO: Currently O(N^2); "By using the connectivity of the triangulation to efficiently locate triangles to remove, the algorithm can take O(N log N)"
                for (int index = 0; index < tetrahedralization.Count; index++)
                {
                    if (IsTetrahedronAllocated(index) && IsPointInCircumsphere(ref vertex, vertices, ref tetrahedra[index]))
                        badTetrahedra.Add(index);
                }

                // Find the boundary of the polygonal hole
                foreach (var tetrahedronIndex in badTetrahedra)
                {
                    var tetrahedron = &tetrahedra[tetrahedronIndex];
                    for (int i = 0; i < 4; ++i)
                    {
                        // If edge is not shared by any other bad tetrahedra, it means it's a boundary of our polygonal hole
                        var neighbourTetrahedronIndex = tetrahedron->Neighbours[i];
                        if (badTetrahedra.BinarySearch(neighbourTetrahedronIndex) < 0)
                        {
                            var neighbourTetrahedronSelfIndex = -1;
                            if (neighbourTetrahedronIndex != -1)
                            {
                                // Find the neighbour index of current tetrahedra in neighbourTetrahedra
                                var neighbourTetrahedron = &tetrahedra[neighbourTetrahedronIndex];
                                for (int j = 0; j < 4; ++j)
                                {
                                    if (neighbourTetrahedron->Neighbours[j] == tetrahedronIndex)
                                    {
                                        neighbourTetrahedronSelfIndex = j;
                                        break;
                                    }
                                }
                                if (neighbourTetrahedronSelfIndex == -1)
                                    throw new InvalidOperationException("Inconsistency: two tetrahedra don't agree on their neighbour information (they should both reference each other)");
                            }

                            // Store edges information (to easily reconstruct neighbour after)
                            var vertex0 = tetrahedron->Vertices[(i + 1) % 4];
                            var vertex1 = tetrahedron->Vertices[(i + 2) % 4];
                            var vertex2 = tetrahedron->Vertices[(i + 3) % 4];

                            // If new vertex is at an odd position, it means that newly constructed tetrahedron would have a negative order, let's swap 2 vertices
                            //if (!IsTetrahedraPositiveOrder(ref vertex, ref vertices[vertex0], ref vertices[vertex1], ref vertices[vertex2]))
                            if (i % 2 == 1)
                            {
                                var vertexTemp = vertex1;
                                vertex1 = vertex2;
                                vertex2 = vertexTemp;
                            }

                            if (!IsTetrahedronPositiveOrder(ref vertex, ref vertices[vertex0], ref vertices[vertex1], ref vertices[vertex2]))
                                throw new InvalidOperationException();

                            holeFaces.Add(new HoleFace(vertex0, vertex1, vertex2, neighbourTetrahedronIndex, neighbourTetrahedronSelfIndex));
                        }
                    }
                }

                // Remove bad tetrahedra (by marking them invalid)
                foreach (var tetrahedronIndex in badTetrahedra)
                {
                    FreeTetrahedron(tetrahedronIndex);
                }
            }

            // Allocate tetrahedron, and build edge list
            fixed (HoleFace* facesPointer = holeFaces.Items)
            {
                for (int index = 0; index < holeFaces.Count; index++)
                {
                    var face = &facesPointer[index];

                    var tetrahedronIndex = AllocateTetrahedron();
                    face->Tetrahedron = tetrahedronIndex;

                    if (!IsTetrahedronPositiveOrder(ref vertex, ref vertices[face->Vertex0], ref vertices[face->Vertex1], ref vertices[face->Vertex2]))
                        throw new InvalidOperationException();

                    // Note: we use opposite direction for half-edge 
                    edges.Add(new HoleEdge(face->Vertex0, face->Vertex1, tetrahedronIndex));
                    edges.Add(new HoleEdge(face->Vertex1, face->Vertex2, tetrahedronIndex));
                    edges.Add(new HoleEdge(face->Vertex2, face->Vertex0, tetrahedronIndex));
                }
            }

            // Sort hole edges to be able to binary search them when reconstructing neighbour information
            edges.Sort();

            // Re-triangulate the polygonal hole
            foreach (var face in holeFaces)
            {
                // Create a tetrahedron joining this edge and the new point
                // This should be outside of "fixed" statement since triangulation list might grow
                var tetrahedronIndex = face.Tetrahedron;

                fixed (Tetrahedron* tetrahedra = tetrahedralization.Items)
                {
                    var newTetrahedron = &tetrahedra[tetrahedronIndex];

                    if (face.Neighbour != -1)
                    {
                        // Update neighbour reference to self
                        var neighbourTetrahedron = &tetrahedra[face.Neighbour];
                        neighbourTetrahedron->Neighbours[face.NeighbourFaceIndex] = tetrahedronIndex;
                    }

                    newTetrahedron->Vertices[0] = vertexIndex;

                    var tetrahedronAdjacentIndex0 = edges.BinarySearch(new HoleEdge(face.Vertex2, face.Vertex1));
                    var tetrahedronAdjacentIndex1 = edges.BinarySearch(new HoleEdge(face.Vertex0, face.Vertex2));
                    var tetrahedronAdjacentIndex2 = edges.BinarySearch(new HoleEdge(face.Vertex1, face.Vertex0));

                    //if (tetrahedraAdjacentIndex0 < 0 || tetrahedraAdjacentIndex1 < 0 || tetrahedraAdjacentIndex2 < 0)
                    //    throw new InvalidOperationException("Could not find adjacent tetrahedra.");

                    // Add the shared face, in opposite order (so that tetrahedron is still positive order)
                    newTetrahedron->Vertices[1] = face.Vertex0;
                    newTetrahedron->Vertices[2] = face.Vertex1;
                    newTetrahedron->Vertices[3] = face.Vertex2;

                    // Neighbour at boundary is always opposite of newly added vertex
                    newTetrahedron->Neighbours[0] = face.Neighbour;

                    newTetrahedron->Neighbours[1] = tetrahedronAdjacentIndex0 >= 0 ? edges[tetrahedronAdjacentIndex0].Neighboor : -1;
                    newTetrahedron->Neighbours[2] = tetrahedronAdjacentIndex1 >= 0 ? edges[tetrahedronAdjacentIndex1].Neighboor : -1;
                    newTetrahedron->Neighbours[3] = tetrahedronAdjacentIndex2 >= 0 ? edges[tetrahedronAdjacentIndex2].Neighboor : -1;

                    if (!IsTetrahedronPositiveOrder(vertices, ref *newTetrahedron))
                        throw new InvalidOperationException("Tetrahedron not in positive order");
                }
            }
        }

        private unsafe void CheckConnectivity()
        {
            // Check connectivity
            fixed (Tetrahedron* tetrahedra = tetrahedralization.Items)
            {
                for (int index = 0; index < tetrahedralization.Count; index++)
                {
                    if (!IsTetrahedronAllocated(index))
                        continue;

                    var tetrahedron = &tetrahedra[index];

                    for (int i = 0; i < 4; ++i)
                    {
                        var neighbourTetrahedronIndex = tetrahedron->Neighbours[i];
                        var neighbourTetrahedronSelfIndex = -1;
                        if (neighbourTetrahedronIndex != -1)
                        {
                            // Find the neighbour index of current tetrahedron in neighbourTetrahedron
                            var neighbourTetrahedron = &tetrahedra[neighbourTetrahedronIndex];
                            for (int j = 0; j < 4; ++j)
                            {
                                if (neighbourTetrahedron->Neighbours[j] == index)
                                {
                                    neighbourTetrahedronSelfIndex = j;
                                    break;
                                }
                            }
                            if (neighbourTetrahedronSelfIndex == -1)
                                throw new InvalidOperationException("Inconsistency: two tetrahedra don't agree on their neighbour information (they should both reference each other)");
                        }
                    }
                }
            }
        }

        private int AllocateTetrahedron()
        {
            // Check free list
            if (freeTetrahedra.Count > 0)
            {
                var lastTetrahedron = freeTetrahedra[freeTetrahedra.Count - 1];
                freeTetrahedra.RemoveAt(freeTetrahedra.Count - 1);
                return lastTetrahedron;
            }

            // Otherwise, allocate new one
            tetrahedralization.Add(new Tetrahedron());

            return tetrahedralization.Count - 1;
        }

        private unsafe bool IsTetrahedronAllocated(int index)
        {
            fixed (Tetrahedron* tetrahedron = &tetrahedralization.Items[index])
            {
                return tetrahedron->Vertices[0] != -1;
            }
        }

        private unsafe void FreeTetrahedron(int index)
        {
            // Mark it as "unused"
            fixed (Tetrahedron* tetrahedron = &tetrahedralization.Items[index])
            {
                tetrahedron->Vertices[0] = -1;

                // Add it to free list
                freeTetrahedra.Add(index);
            }
        }

        /// <summary>
        /// Determines whether the given point is in the tetraedra.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <param name="tetrahedron">The tetrahedron.</param>
        /// <returns></returns>
        private unsafe bool IsPointInCircumsphere(ref Vector3 p, Vector3[] points, ref Tetrahedron tetrahedron)
        {
            fixed (Tetrahedron* tetrahedronPointer = &tetrahedron)
            {
                var ap = points[tetrahedronPointer->Vertices[0]] - p;
                var bp = points[tetrahedronPointer->Vertices[1]] - p;
                var cp = points[tetrahedronPointer->Vertices[2]] - p;
                var dp = points[tetrahedronPointer->Vertices[3]] - p;

                // TODO: Pretend super tetrahedron is at infinity
                // This is quite complex to handle and is probably not necessary given we take a "big enough" tetrahedron
                // http://stackoverflow.com/questions/30741459/bowyer-watson-algorithm-how-to-fill-holes-left-by-removing-triangles-with-sup#answer-36992359
                /*int infinityCount = 0;

                // It will store:
                // non-infinite elements indices
                // infinityCount elements indices
                int* infinityIndices = stackalloc int[4];
                bool* supertetrahedronUsed = stackalloc bool[4];
                for (int i = 0; i < 4; ++i)
                    supertetrahedronUsed[i] = false;

                for (int i = 0; i < 4; ++i)
                {
                    var vertex = tetrahedronPointer->Vertices[i];
                    if (vertex >= vertices.Length - 4)
                    {
                        infinityIndices[3 - infinityCount++] = vertex;
                        supertetrahedronUsed[vertex + 4 - vertices.Length] = true;
                    }
                    else
                    {
                        infinityIndices[i - infinityCount] = vertex;
                    }
                }

                // Trivial: trying to add first vertex in super tetrahedron
                if (infinityCount == 4)
                    return true;

                if (infinityCount == 3)
                {
                    // 3 from super tetrahedra out of 4
                    // Find which is not used
                    var unused = -1;
                    for (int i = 0; i < 4; ++i)
                    {
                        if (!supertetrahedronUsed[i])
                        {
                            unused = i;
                        }
                    }

                    Vector3 normal;
                    switch (unused)
                    {
                        case 0:
                            normal = new Vector3(1.0f, 1.0f, 1.0f);
                            normal.Normalize();
                            break;
                        case 1:
                            normal = -Vector3.UnitZ;
                            break;
                        case 2:
                            normal = -Vector3.UnitY;
                            break;
                        case 3:
                            normal = -Vector3.UnitX;
                            break;
                        default:
                            throw new InvalidOperationException("Invalid state when computing tetrahedron");
                    }

                    var plane = new Plane(points[infinityIndices[0]], normal);
                    var result = insphere(ref points[tetrahedronPointer->Vertices[0]], ref points[tetrahedronPointer->Vertices[1]], ref points[tetrahedronPointer->Vertices[2]], ref points[tetrahedronPointer->Vertices[3]], ref p);
                    return CollisionHelper.DistancePlanePoint(ref plane, ref p) > 0.0f;
                }
                else if (infinityCount == 2)
                {
                    // Build a plane that contains both points not at infinity, and that is also parallel to the line formed by the two points at "infinity"
                    var infinitePerpendicularLine = points[infinityIndices[3]] - points[infinityIndices[2]];
                    var plane = new Plane(points[infinityIndices[0]], points[infinityIndices[1]], points[infinityIndices[0]] + infinitePerpendicularLine);
                    var result = insphere(ref points[tetrahedronPointer->Vertices[0]], ref points[tetrahedronPointer->Vertices[1]], ref points[tetrahedronPointer->Vertices[2]], ref points[tetrahedronPointer->Vertices[3]], ref p);
                    return CollisionHelper.DistancePlanePoint(ref plane, ref p) > 0.0f;
                }
                else if (infinityCount == 1)
                {
                    // 1 from super tetrahedra out of 4
                    // Find which one is used
                    var used = infinityIndices[3] + 4 - vertices.Length;
                    // TODO
                }*/

                // Use Adaptive Precision Floating-Point Arithmetic
                return insphere(ref points[tetrahedronPointer->Vertices[0]], ref points[tetrahedronPointer->Vertices[1]], ref points[tetrahedronPointer->Vertices[2]], ref points[tetrahedronPointer->Vertices[3]], ref p) > 0.0f;
#if FALSE
                // Equivalent without Adaptive Precision Floating-Point Arithmetic
                var matrix = new Matrix(
                    ap.X, ap.Y, ap.Z, ap.LengthSquared(),
                    bp.X, bp.Y, bp.Z, bp.LengthSquared(),
                    cp.X, cp.Y, cp.Z, cp.LengthSquared(),
                    dp.X, dp.Y, dp.Z, dp.LengthSquared());

                return matrix.Determinant() > 0.0f;
#endif
            }
        }

        private unsafe bool IsTetrahedronPositiveOrder(Vector3[] points, ref Tetrahedron tetrahedron)
        {
            fixed (Tetrahedron* tetrahedronPointer = &tetrahedron)
            {
                return IsTetrahedronPositiveOrder(
                    ref points[tetrahedronPointer->Vertices[0]],
                    ref points[tetrahedronPointer->Vertices[1]],
                    ref points[tetrahedronPointer->Vertices[2]],
                    ref points[tetrahedronPointer->Vertices[3]]);
            }
        }

        private bool IsTetrahedronPositiveOrder(ref Vector3 a, ref Vector3 b, ref Vector3 c, ref Vector3 d)
        {
            return orient3d(ref a, ref b, ref c, ref d) > 0.0f;
#if FALSE
            var matrix = new Matrix(
                a.X, a.Y, a.Z, 1.0f,
                b.X, b.Y, b.Z, 1.0f,
                c.X, c.Y, c.Z, 1.0f,
                d.X, d.Y, d.Z, 1.0f);

            return matrix.Determinant() > 0.0f;
#endif
        }

        /// <summary>
        /// Internal structure used when adding vertex.
        /// </summary>
        private struct HoleFace
        {
            public readonly int Vertex0;
            public readonly int Vertex1;
            public readonly int Vertex2;
            public int Tetrahedron;
            public readonly int Neighbour;
            public readonly int NeighbourFaceIndex;

            public HoleFace(int vertex0, int vertex1, int vertex2, int neighbour, int neighbourFaceIndex)
            {
                Vertex0 = vertex0;
                Vertex1 = vertex1;
                Vertex2 = vertex2;
                Neighbour = neighbour;
                NeighbourFaceIndex = neighbourFaceIndex;
                Tetrahedron = -1;
            }

            public override string ToString()
            {
                return string.Format("{0}: Vertices: {1} {2} {3}; Neighbour: {4}({5})", Tetrahedron, Vertex0, Vertex1, Vertex2, Neighbour, NeighbourFaceIndex);
            }
        }

        /// <summary>
        /// Internal structure used when adding vertex.
        /// </summary>
        private struct HoleEdge : IComparable<HoleEdge>
        {
            public readonly int Vertex0;
            public readonly int Vertex1;

            public readonly int Neighboor;

            public HoleEdge(int vertex0, int vertex1)
            {
                Vertex0 = vertex0;
                Vertex1 = vertex1;
                Neighboor = 0;
            }

            public HoleEdge(int vertex0, int vertex1, int neighboor)
            {
                Vertex0 = vertex0;
                Vertex1 = vertex1;
                Neighboor = neighboor;
            }

            public int CompareTo(HoleEdge other)
            {
                var compare0 = Vertex0.CompareTo(other.Vertex0);
                if (compare0 != 0)
                    return compare0;

                return Vertex1.CompareTo(other.Vertex1);
            }

            public override string ToString()
            {
                return string.Format("{0} => {1}: {2}", Vertex0, Vertex1, Neighboor);
            }
        }
    }
}
