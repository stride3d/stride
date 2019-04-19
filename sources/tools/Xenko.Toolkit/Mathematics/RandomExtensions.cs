using Xenko.Core.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Xenko.Toolkit.Mathematics
{
    /// <summary>
    /// Extensions for <see cref="Random"/>.
    /// </summary>
    public static class RandomExtensions
    {
        /// <summary>
        /// Generates a random <see cref="float"/>.
        /// </summary>
        /// <param name="random">An instance of <see cref="Random"/>.</param>
        /// <returns>A random <see cref="float"/>.</returns>
        /// <exception cref="ArgumentNullException">If the random argument is null.</exception>
        public static float NextSingle(this Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            return (float)random.NextDouble();
        }

        /// <summary>
        /// Generates a random point in 2D space within the specified region.
        /// </summary>
        /// <param name="random">An instance of <see cref="Random"/>.</param>
        /// <param name="region">A 2D region in which point is generated.</param>
        /// <returns>A random point in 2D space within the specified region.</returns>
        /// <exception cref="ArgumentNullException">If the random argument is null.</exception>
        public static Vector2 NextPoint(this Random random, RectangleF region)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            return Vector2.Lerp(region.TopLeft, region.BottomRight, random.NextSingle());
            
        }

        /// <summary>
        /// Generates a random point in 3D space within the specified region.
        /// </summary>
        /// <param name="random">An instance of <see cref="Random"/>.</param>
        /// <param name="region">A 3D region in which point is generated.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">If the random argument is null.</exception>
        public static Vector3 NextPoint(this Random random, BoundingBox region)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            return Vector3.Lerp(region.Minimum, region.Maximum, random.NextSingle());

        }

        /// <summary>
        /// Generates a random normalized 2D direction vector.
        /// </summary>
        /// <param name="random">An instance of <see cref="Random"/>.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">If the random argument is null.</exception>
        public static Vector2 NextDirection2D(this Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            return Vector2.Normalize(new Vector2(random.NextSingle(), random.NextSingle()));
        }

        /// <summary>
        /// Generates a random normalized 3D direction vector.
        /// </summary>
        /// <param name="random">An instance of <see cref="Random"/>.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">If the random argument is null.</exception>
        public static Vector3 NextDirection3D(this Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            return Vector3.Normalize(new Vector3(random.NextSingle(), random.NextSingle(), random.NextSingle()));
        }

        /// <summary>
        /// Generates a random point in a circle of a given radius.
        /// </summary>
        /// <param name="random">An instance of <see cref="Random"/>.</param>
        /// <param name="radius">Radius of circle. Default 1.0f.</param>
        /// <returns>A random point in a circle of a given radius.</returns>
        /// <exception cref="ArgumentNullException">If the random argument is null.</exception>
        public static Vector2 PointInACircle(this Random random, float radius = 1.0f)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var randomRadius = random.NextSingle() * radius;

            return new Vector2
            {
                X = (float)Math.Cos(random.NextDouble()) * randomRadius,
                Y = (float)Math.Sin(random.NextDouble()) * randomRadius,
            };
        }


        /// <summary>
        /// Generates a random color.
        /// </summary>
        /// <param name="random">An instance of <see cref="Random"/>.</param>
        /// <returns>A random color. Aplha is set to 255. </returns>
        /// <exception cref="ArgumentNullException">If the random argument is null.</exception>
        public static Color NextColor(this Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            return new Color(NextDirection3D(random));
        }       
    }
}
