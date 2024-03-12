using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Printing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SharpFont.Bdf;
using Stride.Core;
using Stride.Core.Mathematics;
using Vector3 = Stride.Core.Mathematics.Vector3;

namespace Stride.Rendering
{
    /// <summary>
    /// Represet one instance of blend shape and control points
    /// </summary>
    [DataContract]   
    public class Shape:INotifyPropertyChanged
    {
        public string Name { get; set; }

        public string ChannelName { get; set; }

        public string Shapeindex { get; set; }


        private Core.Mathematics.Vector4[] position;

        public Core.Mathematics.Vector4[] Position
        {
            get { return position; }
            set
            {
                if (value != position)
                {
                    position = value;
                    OnPropertyChanged(nameof(Position));
                   
                }
            }
        }

        /// <summary>
        /// Cache variable optimize updates by storing list of vertex impacted by blendshape, significantly improves speed in case of facial animations where large no of blendshapes trageting groups of vertices, gets its value from PositionData
        /// </summary>
        public SortedSet<int> VertexImpactedByBlendShapes { get;private set; }

        internal void SetVextexImpacted(MeshDraw Draw)
        {
            VertexImpactedByBlendShapes = new SortedSet<int>();
            for(int i = 0;i<position.Length;i++) 
            {
                var impact = new Vector3(position[i].X - Draw.VerticesOriginal[i].X, position[i].Y - Draw.VerticesOriginal[i].Y, position[i].Z - Draw.VerticesOriginal[i].Z);
                if(impact!=Vector3.Zero)
                {
                    VertexImpactedByBlendShapes.Add(i);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected  void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));          
        }
        
    }

}
