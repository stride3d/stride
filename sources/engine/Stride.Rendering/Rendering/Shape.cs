using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Stride.Core;

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

        Vector4[] _positions;
        public Vector4[] Positions
        {
            get
            {
                return _positions;
            }
            set
            {
                if (value == null) { _positions = null; return; }
                
                Position = new Vec4[value.Length];
                for (int i = 0; i < value.Length; i++)
                {
                    Position[i] = value[i];
                }
            }
        }

        public Vec4[] Position { get; set; }

        public Shape()
        {

        }
    }


    [DataContract]
    public class Vec4
    {
        public float x { get; set; }


        public float y { get; set; }

        public float z { get; set; }

        public float w { get; set; }

        public Vec4()
        {

        }

        public static implicit operator Vector4(Vec4 v)
        {
            return new Vector4(v.x, v.y, v.z, v.w);
        }

        public static implicit operator Vec4(Vector4 v)
        {
            return new Vec4() { x = v.X, y = v.Y, z = v.Z, w = v.W };
        }

       
    }
}
