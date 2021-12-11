#region License
/*
    MIT License
    Copyright(c) 2017-2018 Mattias Edlund
    Copyright(c) 2021 Stefan Boronczyk
*/
#endregion


using System;
using Stride.Rendering.Rendering.MeshDecimator.Math;

namespace Stride.Rendering.MeshDecimator.Algorithms
{
    /// <summary>
    /// A decimation algorithm.
    /// </summary>
    public abstract class DecimationAlgorithm
    {
        #region Delegates
        /// <summary>
        /// A callback for decimation status reports.
        /// </summary>
        /// <param name="iteration">The current iteration, starting at zero.</param>
        /// <param name="originalTris">The original count of triangles.</param>
        /// <param name="currentTris">The current count of triangles.</param>
        /// <param name="targetTris">The target count of triangles.</param>
        public delegate void StatusReportCallback(int iteration, int originalTris, int currentTris, int targetTris);
        #endregion

        #region Fields
        private bool preserveBorders = false;
        private int maxVertexCount = 0;
        private bool verbose = false;

        private StatusReportCallback statusReportInvoker = null;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets if borders should be kept.
        /// Default value: false
        /// </summary>
        [Obsolete("Use the 'DecimationAlgorithm.PreserveBorders' property instead.", false)]
        public bool KeepBorders
        {
            get { return preserveBorders; }
            set { preserveBorders = value; }
        }

        /// <summary>
        /// Gets or sets if borders should be preserved.
        /// Default value: false
        /// </summary>
        public bool PreserveBorders
        {
            get { return preserveBorders; }
            set { preserveBorders = value; }
        }

        /// <summary>
        /// Gets or sets if linked vertices should be kept.
        /// Default value: false
        /// </summary>
        [Obsolete("This feature has been removed, for more details why please read the readme.", true)]
        public bool KeepLinkedVertices
        {
            get { return false; }
            set { }
        }

        /// <summary>
        /// Gets or sets the maximum vertex count. Set to zero for no limitation.
        /// Default value: 0 (no limitation)
        /// </summary>
        public int MaxVertexCount
        {
            get { return maxVertexCount; }
            set { maxVertexCount = MathHelper.Max(value, 0); }
        }

        /// <summary>
        /// Gets or sets if verbose information should be printed in the console.
        /// Default value: false
        /// </summary>
        public bool Verbose
        {
            get { return verbose; }
            set { verbose = value; }
        }
        #endregion

        #region Events
        /// <summary>
        /// An event for status reports for this algorithm.
        /// </summary>
        public event StatusReportCallback StatusReport
        {
            add { statusReportInvoker += value; }
            remove { statusReportInvoker -= value; }
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Reports the current status of the decimation.
        /// </summary>
        /// <param name="iteration">The current iteration, starting at zero.</param>
        /// <param name="originalTris">The original count of triangles.</param>
        /// <param name="currentTris">The current count of triangles.</param>
        /// <param name="targetTris">The target count of triangles.</param>
        protected void ReportStatus(int iteration, int originalTris, int currentTris, int targetTris)
        {
            var statusReportInvoker = this.statusReportInvoker;
            if (statusReportInvoker != null)
            {
                statusReportInvoker.Invoke(iteration, originalTris, currentTris, targetTris);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initializes the algorithm with the original mesh.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        public abstract void Initialize(MeshDecimatorData mesh);

        /// <summary>
        /// Decimates the mesh.
        /// </summary>
        /// <param name="targetTrisCount">The target triangle count.</param>
        public abstract void DecimateMesh(int targetTrisCount);

        /// <summary>
        /// Decimates the mesh without losing any quality.
        /// </summary>
        public abstract void DecimateMeshLossless();

        /// <summary>
        /// Returns the resulting mesh.
        /// </summary>
        /// <returns>The resulting mesh.</returns>
        public abstract MeshDecimatorData ToMesh();
        #endregion
    }
}
