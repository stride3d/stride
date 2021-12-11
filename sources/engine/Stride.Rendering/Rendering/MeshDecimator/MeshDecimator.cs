#region License
/*
    MIT License
    Copyright(c) 2017-2018 Mattias Edlund
    Copyright(c) 2021 Stefan Boronczyk
*/
#endregion

using System;
using Stride.Core.Diagnostics;
using Stride.Rendering.MeshDecimator.Algorithms;

namespace Stride.Rendering.MeshDecimator
{
    #region Algorithm
    /// <summary>
    /// The decimation algorithms.
    /// </summary>
    public enum Algorithm
    {
        /// <summary>
        /// The default algorithm.
        /// </summary>
        Default,
        /// <summary>
        /// The fast quadric mesh simplification algorithm.
        /// </summary>
        FastQuadricMesh
    }
    #endregion

    /// <summary>
    /// The mesh decimation API.
    /// </summary>
    public static class MeshDecimator
    {

        #region Public Methods
        #region Create Algorithm
        /// <summary>
        /// Creates a specific decimation algorithm.
        /// </summary>
        /// <param name="algorithm">The desired algorithm.</param>
        /// <returns>The decimation algorithm.</returns>
        public static DecimationAlgorithm CreateAlgorithm(Algorithm algorithm)
        {
            DecimationAlgorithm alg = null;

            switch (algorithm)
            {
                case Algorithm.Default:
                case Algorithm.FastQuadricMesh:
                    alg = new FastQuadricMeshSimplification();
                    break;
                default:
                    throw new ArgumentException("The specified algorithm is not supported.", "algorithm");
            }

            return alg;
        }
        #endregion

        #region Decimate Mesh
        /// <summary>
        /// Decimates a mesh.
        /// </summary>
        /// <param name="mesh">The mesh to decimate.</param>
        /// <param name="targetTriangleCount">The target triangle count.</param>
        /// <returns>The decimated mesh.</returns>
        public static MeshDecimatorData DecimateMesh(MeshDecimatorData mesh, int targetTriangleCount)
        {
            return DecimateMesh(Algorithm.Default, mesh, targetTriangleCount);
        }

        /// <summary>
        /// Decimates a mesh.
        /// </summary>
        /// <param name="algorithm">The desired algorithm.</param>
        /// <param name="mesh">The mesh to decimate.</param>
        /// <param name="targetTriangleCount">The target triangle count.</param>
        /// <returns>The decimated mesh.</returns>
        public static MeshDecimatorData DecimateMesh(Algorithm algorithm, MeshDecimatorData mesh, int targetTriangleCount)
        {
            if (mesh == null)
                throw new ArgumentNullException("mesh");

            var decimationAlgorithm = CreateAlgorithm(algorithm);
            return DecimateMesh(decimationAlgorithm, mesh, targetTriangleCount);
        }

        /// <summary>
        /// Decimates a mesh.
        /// </summary>
        /// <param name="algorithm">The decimation algorithm.</param>
        /// <param name="mesh">The mesh to decimate.</param>
        /// <param name="targetTriangleCount">The target triangle count.</param>
        /// <returns>The decimated mesh.</returns>
        public static MeshDecimatorData DecimateMesh(DecimationAlgorithm algorithm, MeshDecimatorData mesh, int targetTriangleCount)
        {
            if (algorithm == null)
                throw new ArgumentNullException("algorithm");
            else if (mesh == null)
                throw new ArgumentNullException("mesh");

            int currentTriangleCount = mesh.TriangleCount;
            if (targetTriangleCount > currentTriangleCount)
                targetTriangleCount = currentTriangleCount;
            else if (targetTriangleCount < 0)
                targetTriangleCount = 0;

            algorithm.Initialize(mesh);
            algorithm.DecimateMesh(targetTriangleCount);
            return algorithm.ToMesh();
        }
        #endregion

        #region Decimate Mesh Lossless
        /// <summary>
        /// Decimates a mesh without losing any quality.
        /// </summary>
        /// <param name="mesh">The mesh to decimate.</param>
        /// <returns>The decimated mesh.</returns>
        public static MeshDecimatorData DecimateMeshLossless(MeshDecimatorData mesh)
        {
            return DecimateMeshLossless(Algorithm.Default, mesh);
        }

        /// <summary>
        /// Decimates a mesh without losing any quality.
        /// </summary>
        /// <param name="algorithm">The desired algorithm.</param>
        /// <param name="mesh">The mesh to decimate.</param>
        /// <returns>The decimated mesh.</returns>
        public static MeshDecimatorData DecimateMeshLossless(Algorithm algorithm, MeshDecimatorData mesh)
        {
            if (mesh == null)
                throw new ArgumentNullException("mesh");

            var decimationAlgorithm = CreateAlgorithm(algorithm);
            return DecimateMeshLossless(decimationAlgorithm, mesh);
        }

        /// <summary>
        /// Decimates a mesh without losing any quality.
        /// </summary>
        /// <param name="algorithm">The decimation algorithm.</param>
        /// <param name="mesh">The mesh to decimate.</param>
        /// <returns>The decimated mesh.</returns>
        public static MeshDecimatorData DecimateMeshLossless(DecimationAlgorithm algorithm, MeshDecimatorData mesh)
        {
            if (algorithm == null)
                throw new ArgumentNullException("algorithm");
            else if (mesh == null)
                throw new ArgumentNullException("mesh");

            int currentTriangleCount = mesh.TriangleCount;
            algorithm.Initialize(mesh);
            algorithm.DecimateMeshLossless();
            return algorithm.ToMesh();
        }
        #endregion
        #endregion
    }
}
