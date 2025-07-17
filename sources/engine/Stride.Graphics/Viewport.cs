// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Stride.Core.Mathematics;

namespace Stride.Graphics
{
    /// <summary>
    ///   Defines the viewport dimensions of a render-target surface onto which a 3D volume projects.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Viewport : IEquatable<Viewport>
    {
        /// <summary>
        ///   Empty value for an undefined viewport.
        /// </summary>
        public static readonly Viewport Empty;

        /// <summary>
        ///   X coordinate of the upper-left corner of the viewport on the render-target surface, in pixels.
        /// </summary>
        public float X;

        /// <summary>
        ///   Y coordinate of the upper-left corner of the viewport on the render-target surface, in pixels.
        /// </summary>
        public float Y;

        /// <summary>
        ///   Width dimension of the viewport on the render-target surface, in pixels.
        /// </summary>
        public float Width;

        /// <summary>
        ///   Height dimension of the viewport on the render-target surface, in pixels.
        /// </summary>
        public float Height;

        /// <summary>
        ///   Minimum depth of the clip volume.
        /// </summary>
        public float MinDepth;

        /// <summary>
        ///   Maximum depth of the clip volume.
        /// </summary>
        public float MaxDepth;


        /// <summary>
        ///   Initializes a new instance of the <see cref="Viewport"/> struct.
        /// </summary>
        /// <param name="x">The X coordinate of the upper-left corner of the viewport in pixels.</param>
        /// <param name="y">The Y coordinate of the upper-left corner of the viewport in pixels.</param>
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
        ///   Initializes a new instance of the <see cref="Viewport"/> struct.
        /// </summary>
        /// <param name="x">The X coordinate of the upper-left corner of the viewport in pixels.</param>
        /// <param name="y">The Y coordinate of the upper-left corner of the viewport in pixels.</param>
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
        ///   Initializes a new instance of the <see cref="Viewport"/> struct.
        /// </summary>
        /// <param name="bounds">A rectangle that defines the location and size of the viewport in a render target.</param>
        public Viewport(Rectangle bounds)
        {
            X = bounds.X;
            Y = bounds.Y;
            Width = bounds.Width;
            Height = bounds.Height;
            MinDepth = 0;
            MaxDepth = 1;
        }


        /// <summary>
        ///   Gets a rectangle with the location and size of the viewport.
        /// </summary>
        public Rectangle Bounds
        {
            readonly get => new((int) X, (int) Y, (int) Width, (int) Height);
            set
            {
                X = value.X;
                Y = value.Y;
                Width = value.Width;
                Height = value.Height;
            }
        }

        /// <summary>
        ///   Gets the aspect ratio of the viewport, i.e. <c>Width / Height</c>.
        /// </summary>
        public readonly float AspectRatio => Width != 0 && Height != 0 ? Width / Height : 0;

        /// <summary>
        ///   Gets the size of the viewport.
        /// </summary>
        public readonly Vector2 Size => new(Width, Height);

        /// <inheritdoc/>
        public readonly bool Equals(Viewport other)
        {
            return other.X.Equals(X)
                && other.Y.Equals(Y)
                && other.Width.Equals(Width)
                && other.Height.Equals(Height)
                && other.MinDepth.Equals(MinDepth)
                && other.MaxDepth.Equals(MaxDepth);
        }

        /// <inheritdoc/>
        public override readonly bool Equals(object obj)
        {
            if (obj is null)
                return false;

            return obj is Viewport viewport && Equals(viewport);
        }

        /// <inheritdoc/>
        public override readonly int GetHashCode()
        {
            return HashCode.Combine(X, Y, Width, Height, MinDepth, MaxDepth);
        }

        public static bool operator ==(Viewport left, Viewport right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Viewport left, Viewport right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        ///   Returns a string representation of this viewport.
        /// </summary>
        public override readonly string ToString()
        {
            return FormattableString.CurrentCulture($"{{X:{X} Y:{Y} Width:{Width} Height:{Height} MinDepth:{MinDepth} MaxDepth:{MaxDepth}}}");
        }

        private static bool WithinEpsilon(float a, float b)
        {
            float difference = a - b;
            return (difference >= -1.401298E-45f) && (difference <= float.Epsilon);
        }

        /// <summary>
        ///   Projects a 3D vector from object space into screen space.
        /// </summary>
        /// <param name="source">The vector to project.</param>
        /// <param name="projection">The projection matrix.</param>
        /// <param name="view">The view matrix.</param>
        /// <param name="world">The world matrix.</param>
        public readonly Vector3 Project(Vector3 source, Matrix projection, Matrix view, Matrix world)
        {
            Matrix worldViewProj = Matrix.Multiply(Matrix.Multiply(world, view), projection);
            Vector4 vector = Vector3.Transform(source, worldViewProj);

            float w = (source.X * worldViewProj.M14) + (source.Y * worldViewProj.M24) + (source.Z * worldViewProj.M34) + worldViewProj.M44;
            if (!WithinEpsilon(w, 1))
            {
                vector /= w;
            }
            vector.X = ((vector.X + 1) * 0.5f * Width) + X;
            vector.Y = ((-vector.Y + 1) * 0.5f * Height) + Y;
            vector.Z = (vector.Z * (MaxDepth - MinDepth)) + MinDepth;
            return new Vector3(vector.X, vector.Y, vector.Z);
        }

        /// <summary>
        ///   Converts a screen space point into a corresponding point in world space.
        /// </summary>
        /// <param name="source">The vector to unproject.</param>
        /// <param name="projection">The projection matrix.</param>
        /// <param name="view">The view matrix.</param>
        /// <param name="world">The world matrix.</param>
        public Vector3 Unproject(Vector3 source, Matrix projection, Matrix view, Matrix world)
        {
            Matrix worldViewProj = Matrix.Multiply(Matrix.Multiply(world, view), projection);
            return Unproject(source, in worldViewProj);
        }

        /// <summary>
        ///   Converts a screen space point into a corresponding point in world space.
        /// </summary>
        /// <param name="source">The vector to unproject.</param>
        /// <param name="worldViewProjection">The World-View-Projection matrix.</param>
        public readonly Vector3 Unproject(Vector3 source, ref readonly Matrix worldViewProjection)
        {
            Matrix invWorldViewProj = Matrix.Invert(worldViewProjection);

            source.X = ((source.X - X) / Width * 2) - 1;
            source.Y = -(((source.Y - Y) / Height * 2) - 1);
            source.Z = (source.Z - MinDepth) / (MaxDepth - MinDepth);
            Vector4 vector = Vector3.Transform(source, invWorldViewProj);

            float w = (source.X * invWorldViewProj.M14) + (source.Y * invWorldViewProj.M24) + (source.Z * invWorldViewProj.M34) + invWorldViewProj.M44;
            if (!WithinEpsilon(w, 1))
            {
                vector /= w;
            }
            return new Vector3(vector.X, vector.Y, vector.Z);
        }
    }
}
