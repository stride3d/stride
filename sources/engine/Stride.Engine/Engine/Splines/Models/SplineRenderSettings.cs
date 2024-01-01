//// Copyright (c) Stride contributors (https://Stride.com)
//// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Rendering;
using Stride.Core;
namespace Stride.Engine.Splines.Models
{
    [DataContract]
    public class SplineRenderSettings
    {
        private bool showSegments;
        private bool showBoundingBox;
        private bool showNodes;

        public delegate void SplineRendererSettingsUpdatedHandler();

        public event SplineRendererSettingsUpdatedHandler OnRendererSettingsUpdated;

        /// <summary>
        /// Display spline curve mesh
        /// </summary>
        [Display(10, "Show segments")]
        public bool ShowSegments
        {
            get
            {
                return showSegments;
            }
            set
            {
                showSegments = value;

                OnRendererSettingsUpdated?.Invoke();
            }
        }

        /// <summary>
        /// The material used by the spline mesh
        /// </summary>
        [Display(20, "Segments material")] 
        public Material SegmentsMaterial;

        /// <summary>
        /// Display Spline nodes
        /// </summary>
        [Display(23, "Show nodes")]
        public bool ShowNodes
        {
            get
            {
                return showNodes;
            }
            set
            {
                showNodes = value;

                OnRendererSettingsUpdated?.Invoke();
            }
        }

        /// <summary>
        /// The material used by the spline nodes mesh
        /// </summary>
        [Display(26, "Nodes material")] 
        public Material NodesMaterial;

        /// <summary>
        /// Display the bounding boxes of each node and the entire spline
        /// </summary>
        [Display(30, "Show bounding box")]
        public bool ShowBoundingBox
        {
            get { return showBoundingBox; }
            set
            {
                showBoundingBox = value;

                OnRendererSettingsUpdated?.Invoke();
            }
        }

        /// <summary>
        /// The material used by the spline boundingboxes
        /// </summary>
        [Display(40, "Boundingbox material")] 
        public Material BoundingBoxMaterial;
    }
}
