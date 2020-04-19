// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Stride.Core.Mathematics;

namespace Stride.Graphics
{
    /// <summary>
    /// Defines the window dimensions of a render-target surface onto which a 3D volume projects.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Viewport : IEquatable<Viewport>
    {
        /// <summary>
        /// Empty value for an undefined viewport.
        /// </summary>
        public static readonly Viewport Empty;

        /// <summary>
        /// Gets or sets the pixel coordinate of the upper-left corner of the viewport on the render-target surface.
        /// </summary>
        public float X;

        /// <summary>Gets or sets the pixel coordinate of the upper-left corner of the viewport on the render-target surface.</summary>
        public float Y;

        /// <summary>Gets or sets the width dimension of the viewport on the render-target surface, in pixels.</summary>
        public float Width;

        /// <summary>Gets or sets the height dimension of the viewport on the render-target surface, in pixels.</summary>
        public float Height;

        /// <summary>Gets or sets the minimum depth of the clip volume.</summary>
        public float MinDepth;

        /// <summary>Gets or sets the maximum depth of the clip volume.</summary>
        public float MaxDepth;

        /// <summary>
        /// Initializes a new instance of the <see cref="Viewport"/> struct.
        /// </summary>
        /// <param name="x">The x coordinate of the upper-left corner of the viewport in pixels.</param>
        /// <param name="y">The y coordinate of the upper-left corner of the viewport in pixels.</param>
        /// <param name="width">The width of the viewport in pixels.</param>
        /// <param name="height">The height of the viewport in pixels.</param>
        public Viewport(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            MinDepth = 0;
            MaxDepth = 1;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Viewport"/> struct.
        /// </summary>
        /// <param name="x">The x coordinate of the upper-left corner of the viewport in pixels.</param>
        /// <param name="y">The y coordinate of the upper-left corner of the viewport in pixels.</param>
        /// <param name="width">The width of the viewport in pixels.</param>
        /// <param name="height">The height of the viewport in pixels.</param>
        public Viewport(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            MinDepth = 0;
            MaxDepth = 1;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Viewport"/> struct.
        /// </summary>
        /// <param name="bounds">A bounding box that defines the location and size of the viewport in a render target.</param>
        public Viewport(Rectangle bounds)
        {
            X = bounds.X;
            Y = bounds.Y;
            Width = bounds.Width;
            Height = bounds.Height; 
            MinDepth = 0;
            MaxDepth = 1;            
        }

        /// <summary>Gets the size of this resource.</summary>
        public Rectangle Bounds
        {
            get { return new Rectangle((int)X, (int)Y, (int)Width, (int)Height); }
            set
            {
                X = value.X;
                Y = value.Y;
                Width = value.Width;
                Height = value.Height;
            }
        }

        public bool Equals(Viewport other)
        {
            return other.X.Equals(X) && other.Y.Equals(Y) && other.Width.Equals(Width) && other.Height.Equals(Height) && other.MinDepth.Equals(MinDepth) && other.MaxDepth.Equals(MaxDepth);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != typeof(Viewport)) return false;
            return Equals((Viewport)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = X.GetHashCode();
                result = (result * 397) ^ Y.GetHashCode();
                result = (result * 397) ^ Width.GetHashCode();
                result = (result * 397) ^ Height.GetHashCode();
                result = (result * 397) ^ MinDepth.GetHashCode();
                result = (result * 397) ^ MaxDepth.GetHashCode();
                return result;
            }
        }

        public static bool operator ==(Viewport left, Viewport right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Viewport left, Viewport right)
        {
            return !left.Equals(right);
        }

        /// <summary>Retrieves a string representation of this object.</summary>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "{{X:{0} Y:{1} Width:{2} Height:{3} MinDepth:{4} MaxDepth:{5}}}", new object[] { X, Y, Width, Height, MinDepth, MaxDepth });
        }

        private static bool WithinEpsilon(float a, float b)
        {
            float num = a - b;
            return ((num >= -1.401298E-45f) && (num <= float.Epsilon));
        }

        /// <summary>Projects a 3D vector from object space into screen space.</summary>
        /// <param name="source">The vector to project.</param>
        /// <param name="projection">The projection matrix.</param>
        /// <param name="view">The view matrix.</param>
        /// <param name="world">The world matrix.</param>
        public Vector3 Project(Vector3 source, Matrix projection, Matrix view, Matrix world)
        {
            Matrix matrix = Matrix.Multiply(Matrix.Multiply(world, view), projection);
            Vector4 vector = Vector3.Transform(source, matrix);
            float a = (((source.X * matrix.M14) + (source.Y * matrix.M24)) + (source.Z * matrix.M34)) + matrix.M44;
            if (!WithinEpsilon(a, 1f))
            {
                vector = (vector / a);
            }
            vector.X = (((vector.X + 1f) * 0.5f) * Width) + X;
            vector.Y = (((-vector.Y + 1f) * 0.5f) * Height) + Y;
            vector.Z = (vector.Z * (MaxDepth - MinDepth)) + MinDepth;
            return new Vector3(vector.X, vector.Y, vector.Z);
        }

        /// <summary>Converts a screen space point into a corresponding point in world space.</summary>
        /// <param name="source">The vector to project.</param>
        /// <param name="projection">The projection matrix.</param>
        /// <param name="view">The view matrix.</param>
        /// <param name="world">The world matrix.</param>
        public Vector3 Unproject(Vector3 source, Matrix projection, Matrix view, Matrix world)
        {
            Matrix matrix = Matrix.Multiply(Matrix.Multiply(world, view), projection);
            return Unproject(source, ref matrix);
        }

        /// <summary>Converts a screen space point into a corresponding point in world space.</summary>
        /// <param name="source">The vector to project.</param>
        /// <param name="worldViewProjection">The World-View-Projection matrix.</param>
        public Vector3 Unproject(Vector3 source, ref Matrix worldViewProjection)
        {
            Matrix matrix = Matrix.Invert(worldViewProjection);

            source.X = (((source.X - X) / Width) * 2f) - 1f;
            source.Y = -((((source.Y - Y) / Height) * 2f) - 1f);
            source.Z = (source.Z - MinDepth) / (MaxDepth - MinDepth);
            Vector4 vector = Vector3.Transform(source, matrix);
            float a = (((source.X * matrix.M14) + (source.Y * matrix.M24)) + (source.Z * matrix.M34)) + matrix.M44;
            if (!WithinEpsilon(a, 1f))
            {
                vector = vector / a;
            }
            return new Vector3(vector.X, vector.Y, vector.Z);
        }

        /// <summary>Gets the aspect ratio used by the viewport</summary>
        public float AspectRatio
        {
            get
            {
                if (Width != 0 && Height != 0)
                {
                    return Width / Height;
                }
                return 0f;
            }
        }

        /// <summary>
        /// Gets the size of the viewport (Width, Height).
        /// </summary>
        public Vector2 Size => new Vector2(Width, Height);
    }
}
