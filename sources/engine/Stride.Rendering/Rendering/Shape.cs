using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Rendering
{
    [DataContract]
    // Represet one instance of blend shape and control points
    public class Shape
    {
        public string Name { get; set; }

        public string ChannelName { get; set; }

        public string Shapeindex { get; set; }



        //Index Of control points in shape
        public int[] Indices { get; set; }


        public Core.Mathematics.Vector4[] Position { get; set; }

        public Shape()
        {

        }
    }

}
