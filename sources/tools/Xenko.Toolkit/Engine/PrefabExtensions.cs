using Xenko.Core.Mathematics;
using Xenko.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xenko.Toolkit.Mathematics;

namespace Xenko.Toolkit.Engine
{
    /// <summary>
    /// Extension methods for <see cref="Prefab"/>.
    /// </summary>
    public static class PrefabExtensions
    {
        private static void ValidateSinglePrefab(Prefab prefab)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            if (prefab.Entities.Count > 1)
            {
                throw new InvalidOperationException("Prefab contains for than 1 entity.");
            }
        }

        /// <summary>
        /// Instantiates a <see cref="Prefab"/> that contains a single <see cref="Entity"/>.
        /// </summary>
        /// <param name="prefab">The <see cref="Prefab"/> to instantiate.</param>
        /// <returns>The instatiated and translated <see cref="Entity"/>.</returns>
        /// <exception cref="ArgumentException">If <paramref name="prefab"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="prefab"/> does not have exactly 1 <see cref="Entity"/>.</exception>
        public static Entity InstantiateSingle(this Prefab prefab)
        {
            ValidateSinglePrefab(prefab);

            var instances = prefab.Instantiate();

            return instances[0];
        }
        
        /// <summary>
        /// Instantiates a <see cref="Prefab"/> that contains a single <see cref="Entity"/> and applies a transform.
        /// </summary>
        /// <param name="prefab">The <see cref="Prefab"/> to instantiate.</param>
        /// <param name="translation">The <see cref="Vector3"/> to translate the entity by.</param>
        /// <returns>The instatiated and translated <see cref="Entity"/>.</returns>
        /// <exception cref="ArgumentException">If <paramref name="prefab"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="prefab"/> does not have exactly 1 <see cref="Entity"/>.</exception>
        public static Entity InstantiateSingle(this Prefab prefab, ref Vector3 translation)
        {
            var one = Vector3.One;
            var rotation = Quaternion.Identity;
            return prefab.InstantiateSingle(ref translation, ref rotation, ref one);
        }

        /// <summary>
        /// Instantiates a <see cref="Prefab"/> that contains a single <see cref="Entity"/> and applies a transform.
        /// </summary>
        /// <param name="prefab">The <see cref="Prefab"/> to instantiate.</param>
        /// <param name="translation">The <see cref="Vector3"/> to translate the entity by.</param>
        /// <returns>The instatiated and translated <see cref="Entity"/>.</returns>
        /// <exception cref="ArgumentException">If <paramref name="prefab"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="prefab"/> does not have exactly 1 <see cref="Entity"/>.</exception>
        public static Entity InstantiateSingle(this Prefab prefab, Vector3 translation)
        {
            var one = Vector3.One;
            var rotation = Quaternion.Identity;
            return prefab.InstantiateSingle(ref translation, ref rotation, ref one);
        }

        /// <summary>
        /// Instantiates a <see cref="Prefab"/> that contains a single <see cref="Entity"/> and applies a transform.
        /// </summary>
        /// <param name="prefab">The <see cref="Prefab"/> to instantiate.</param>
        /// <param name="translation">The <see cref="Vector3"/> to translate the entity by.</param>
        /// <param name="rotationEulerAngles">The X, Y and Z rotations in radians to rotate the entity by.</param>
        /// <returns>The instatiated and translated <see cref="Entity"/>.</returns>
        /// <exception cref="ArgumentException">If <paramref name="prefab"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="prefab"/> does not have exactly 1 <see cref="Entity"/>.</exception>
        public static Entity InstantiateSingle(this Prefab prefab, ref Vector3 translation, ref Vector3 rotationEulerAngles)
        {
            var one = Vector3.One;
            MathUtilEx.ToQuaternion(ref rotationEulerAngles, out var rotation);
            return prefab.InstantiateSingle(ref translation, ref rotation, ref one);
        }

        /// <summary>
        /// Instantiates a <see cref="Prefab"/> that contains a single <see cref="Entity"/> and applies a transform.
        /// </summary>
        /// <param name="prefab">The <see cref="Prefab"/> to instantiate.</param>
        /// <param name="translation">The <see cref="Vector3"/> to translate the entity by.</param>
        /// <param name="rotationEulerAngles">The X, Y and Z rotations in radians to rotate the entity by.</param>
        /// <returns>The instatiated and translated <see cref="Entity"/>.</returns>
        /// <exception cref="ArgumentException">If <paramref name="prefab"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="prefab"/> does not have exactly 1 <see cref="Entity"/>.</exception>
        public static Entity InstantiateSingle(this Prefab prefab, Vector3 translation, Vector3 rotationEulerAngles)
        {
            var one = Vector3.One;
            MathUtilEx.ToQuaternion(ref rotationEulerAngles, out var rotation);
            return prefab.InstantiateSingle(ref translation, ref rotation, ref one);
        }

        /// <summary>
        /// Instantiates a <see cref="Prefab"/> that contains a single <see cref="Entity"/> and applies a transform.
        /// </summary>
        /// <param name="prefab">The <see cref="Prefab"/> to instantiate.</param>
        /// <param name="translation">The <see cref="Vector3"/> to translate the entity by.</param>
        /// <param name="rotation">The <see cref="Quaternion"/> to rotate the entity by.</param>
        /// <returns>The instatiated and translated <see cref="Entity"/>.</returns>
        /// <exception cref="ArgumentException">If <paramref name="prefab"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="prefab"/> does not have exactly 1 <see cref="Entity"/>.</exception>
        public static Entity InstantiateSingle(this Prefab prefab, ref Vector3 translation, ref Quaternion rotation)
        {
            var one = Vector3.One;
            return prefab.InstantiateSingle(ref translation, ref rotation, ref one);
        }

        /// <summary>
        /// Instantiates a <see cref="Prefab"/> that contains a single <see cref="Entity"/> and applies a transform.
        /// </summary>
        /// <param name="prefab">The <see cref="Prefab"/> to instantiate.</param>
        /// <param name="translation">The <see cref="Vector3"/> to translate the entity by.</param>
        /// <param name="rotation">The <see cref="Quaternion"/> to rotate the entity by.</param>
        /// <returns>The instatiated and translated <see cref="Entity"/>.</returns>
        /// <exception cref="ArgumentException">If <paramref name="prefab"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="prefab"/> does not have exactly 1 <see cref="Entity"/>.</exception>
        public static Entity InstantiateSingle(this Prefab prefab, Vector3 translation, Quaternion rotation)
        {
            var one = Vector3.One;
            return prefab.InstantiateSingle(ref translation, ref rotation, ref one);
        }

        /// <summary>
        /// Instantiates a <see cref="Prefab"/> that contains a single <see cref="Entity"/> and applies a transform.
        /// </summary>
        /// <param name="prefab">The <see cref="Prefab"/> to instantiate.</param>
        /// <param name="translation">The <see cref="Vector3"/> to translate the entity by.</param>
        /// <param name="rotationEulerAngles">The X, Y and Z rotations in radians to rotate the entity by.</param>
        /// <param name="scale">The <see cref="Vector3"/> to scale the entity by.</param>
        /// <returns>The instatiated and translated <see cref="Entity"/>.</returns>
        /// <exception cref="ArgumentException">If <paramref name="prefab"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="prefab"/> does not have exactly 1 <see cref="Entity"/>.</exception>
        public static Entity InstantiateSingle(this Prefab prefab, ref Vector3 translation, ref Vector3 rotationEulerAngles, ref Vector3 scale)
        {
            MathUtilEx.ToQuaternion(ref rotationEulerAngles, out var rotation);
            return prefab.InstantiateSingle(ref translation, ref rotation, ref scale);
        }

        /// <summary>
        /// Instantiates a <see cref="Prefab"/> that contains a single <see cref="Entity"/> and applies a transform.
        /// </summary>
        /// <param name="prefab">The <see cref="Prefab"/> to instantiate.</param>
        /// <param name="translation">The <see cref="Vector3"/> to translate the entity by.</param>
        /// <param name="rotationEulerAngles">The X, Y and Z rotations in radians to rotate the entity by.</param>
        /// <param name="scale">The <see cref="Vector3"/> to scale the entity by.</param>
        /// <returns>The instatiated and translated <see cref="Entity"/>.</returns>
        /// <exception cref="ArgumentException">If <paramref name="prefab"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="prefab"/> does not have exactly 1 <see cref="Entity"/>.</exception>
        public static Entity InstantiateSingle(this Prefab prefab, Vector3 translation, Vector3 rotationEulerAngles, Vector3 scale)
        {
            MathUtilEx.ToQuaternion(ref rotationEulerAngles, out var rotation);
            return prefab.InstantiateSingle(ref translation, ref rotation, ref scale);
        }

        /// <summary>
        /// Instantiates a <see cref="Prefab"/> that contains a single <see cref="Entity"/> and applies a transform.
        /// </summary>
        /// <param name="prefab">The <see cref="Prefab"/> to instantiate.</param>
        /// <param name="translation">The <see cref="Vector3"/> to translate the entity by.</param>
        /// <param name="rotation">The <see cref="Quaternion"/> to rotate the entity by.</param>
        /// <param name="scale">The <see cref="Vector3"/> to scale the entity by.</param>
        /// <returns>The instatiated and translated <see cref="Entity"/>.</returns>
        /// <exception cref="ArgumentException">If <paramref name="prefab"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="prefab"/> does not have exactly 1 <see cref="Entity"/>.</exception>
        public static Entity InstantiateSingle(this Prefab prefab, ref Vector3 translation, ref Quaternion rotation, ref Vector3 scale)
        {
            ValidateSinglePrefab(prefab);

            var instances = prefab.Instantiate();

            var instance = instances[0];
            Matrix.Transformation(ref scale, ref rotation, ref translation, out Matrix localMatrix);

            instance.Transform.UpdateLocalMatrix();
            var entityMatrix = instance.Transform.LocalMatrix * localMatrix;

            if (instance.Transform.UseTRS)
            {
                entityMatrix.Decompose(out instance.Transform.Scale, out instance.Transform.Rotation, out instance.Transform.Position);
            }
            else
            {
                instance.Transform.LocalMatrix = entityMatrix;
            }

            return instance;
        }

        /// <summary>
        /// Instantiates a <see cref="Prefab"/> that contains a single <see cref="Entity"/> and applies a transform.
        /// </summary>
        /// <param name="prefab">The <see cref="Prefab"/> to instantiate.</param>
        /// <param name="translation">The <see cref="Vector3"/> to translate the entity by.</param>
        /// <param name="rotation">The <see cref="Quaternion"/> to rotate the entity by.</param>
        /// <param name="scale">The <see cref="Vector3"/> to scale the entity by.</param>
        /// <returns>The instatiated and translated <see cref="Entity"/>.</returns>
        /// <exception cref="ArgumentException">If <paramref name="prefab"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">If <paramref name="prefab"/> does not have exactly 1 <see cref="Entity"/>.</exception>
        public static Entity InstantiateSingle(this Prefab prefab, Vector3 translation, Quaternion rotation, Vector3 scale)
        {
            return prefab.InstantiateSingle(ref translation, ref rotation, ref scale);
        }

        /// <summary>
        /// Instantiates a <see cref="Prefab"/> and a applies a transform to all the entites.
        /// </summary>
        /// <param name="prefab">The <see cref="Prefab"/> to instantiate.</param>
        /// <param name="translation">The <see cref="Vector3"/> to translate the entities by.</param>
        /// <returns>The instatiated and translated entities.</returns>
        /// <exception cref="ArgumentException">If <paramref name="prefab"/> is <see langword="null"/>.</exception>
        public static List<Entity> Instantiate(this Prefab prefab, ref Vector3 translation)
        {
            var one = Vector3.One;
            var rotation = Quaternion.Identity;
            return prefab.Instantiate(ref translation, ref rotation, ref one);
        }

        /// <summary>
        /// Instantiates a <see cref="Prefab"/> and a applies a transform to all the entites.
        /// </summary>
        /// <param name="prefab">The <see cref="Prefab"/> to instantiate.</param>
        /// <param name="translation">The <see cref="Vector3"/> to translate the entities by.</param>
        /// <returns>The instatiated and translated entities.</returns>
        /// <exception cref="ArgumentException">If <paramref name="prefab"/> is <see langword="null"/>.</exception>
        public static List<Entity> Instantiate(this Prefab prefab, Vector3 translation)
        {
            var one = Vector3.One;
            var rotation = Quaternion.Identity;
            return prefab.Instantiate(ref translation, ref rotation, ref one);
        }

        /// <summary>
        /// Instantiates a <see cref="Prefab"/> and a applies a transform to all the entites.
        /// </summary>
        /// <param name="prefab">The <see cref="Prefab"/> to instantiate.</param>
        /// <param name="translation">The <see cref="Vector3"/> to translate the entities by.</param>
        /// <param name="rotationEulerAngles">The X, Y and Z rotations in euler angles to rotate the entities by.</param>
        /// <returns>The instatiated and translated entities.</returns>
        /// <exception cref="ArgumentException">If <paramref name="prefab"/> is <see langword="null"/>.</exception>
        public static List<Entity> Instantiate(this Prefab prefab, ref Vector3 translation, ref Vector3 rotationEulerAngles)
        {
            var one = Vector3.One;
            MathUtilEx.ToQuaternion(ref rotationEulerAngles, out var rotation);
            return prefab.Instantiate(ref translation, ref rotation, ref one);
        }

        /// <summary>
        /// Instantiates a <see cref="Prefab"/> and a applies a transform to all the entites.
        /// </summary>
        /// <param name="prefab">The <see cref="Prefab"/> to instantiate.</param>
        /// <param name="translation">The <see cref="Vector3"/> to translate the entities by.</param>
        /// <param name="rotationEulerAngles">The X, Y and Z rotations in euler angles to rotate the entities by.</param>
        /// <returns>The instatiated and translated entities.</returns>
        /// <exception cref="ArgumentException">If <paramref name="prefab"/> is <see langword="null"/>.</exception>
        public static List<Entity> Instantiate(this Prefab prefab, Vector3 translation, Vector3 rotationEulerAngles)
        {
            var one = Vector3.One;
            MathUtilEx.ToQuaternion(ref rotationEulerAngles, out var rotation);
            return prefab.Instantiate(ref translation, ref rotation, ref one);
        }

        /// <summary>
        /// Instantiates a <see cref="Prefab"/> and a applies a transform to all the entites.
        /// </summary>
        /// <param name="prefab">The <see cref="Prefab"/> to instantiate.</param>
        /// <param name="translation">The <see cref="Vector3"/> to translate the entities by.</param>
        /// <param name="rotation">The <see cref="Quaternion"/> to rotate the entities by.</param>
        /// <returns>The instatiated and translated entities.</returns>
        /// <exception cref="ArgumentException">If <paramref name="prefab"/> is <see langword="null"/>.</exception>
        public static List<Entity> Instantiate(this Prefab prefab, ref Vector3 translation, ref Quaternion rotation)
        {
            var one = Vector3.One;
            return prefab.Instantiate(ref translation, ref rotation, ref one);
        }

        /// <summary>
        /// Instantiates a <see cref="Prefab"/> and a applies a transform to all the entites.
        /// </summary>
        /// <param name="prefab">The <see cref="Prefab"/> to instantiate.</param>
        /// <param name="translation">The <see cref="Vector3"/> to translate the entities by.</param>
        /// <param name="rotation">The <see cref="Quaternion"/> to rotate the entities by.</param>
        /// <returns>The instatiated and translated entities.</returns>
        /// <exception cref="ArgumentException">If <paramref name="prefab"/> is <see langword="null"/>.</exception>
        public static List<Entity> Instantiate(this Prefab prefab, Vector3 translation, Quaternion rotation)
        {
            var one = Vector3.One;
            return prefab.Instantiate(ref translation, ref rotation, ref one);
        }

        /// <summary>
        /// Instantiates a <see cref="Prefab"/> and a applies a transform to all the entites.
        /// </summary>
        /// <param name="prefab">The <see cref="Prefab"/> to instantiate.</param>
        /// <param name="translation">The <see cref="Vector3"/> to translate the entities by.</param>
        /// <param name="rotationEulerAngles">The X, Y and Z rotations in euler angles to rotate the entities by.</param>
        /// <param name="scale">The <see cref="Vector3"/> to scale the entities by.</param>
        /// <returns>The instatiated and translated entities.</returns>
        /// <exception cref="ArgumentException">If <paramref name="prefab"/> is <see langword="null"/>.</exception>
        public static List<Entity> Instantiate(this Prefab prefab, ref Vector3 translation, ref Vector3 rotationEulerAngles, ref Vector3 scale)
        {
            MathUtilEx.ToQuaternion(ref rotationEulerAngles, out var rotation);
            return prefab.Instantiate(ref translation, ref rotation, ref scale);
        }

        /// <summary>
        /// Instantiates a <see cref="Prefab"/> and a applies a transform to all the entites.
        /// </summary>
        /// <param name="prefab">The <see cref="Prefab"/> to instantiate.</param>
        /// <param name="translation">The <see cref="Vector3"/> to translate the entities by.</param>
        /// <param name="rotationEulerAngles">The X, Y and Z rotations in euler angles to rotate the entities by.</param>
        /// <param name="scale">The <see cref="Vector3"/> to scale the entities by.</param>
        /// <returns>The instatiated and translated entities.</returns>
        /// <exception cref="ArgumentException">If <paramref name="prefab"/> is <see langword="null"/>.</exception>
        public static List<Entity> Instantiate(this Prefab prefab, Vector3 translation, Vector3 rotationEulerAngles, Vector3 scale)
        {
            MathUtilEx.ToQuaternion(ref rotationEulerAngles, out var rotation);
            return prefab.Instantiate(ref translation, ref rotation, ref scale);
        }

        /// <summary>
        /// Instantiates a <see cref="Prefab"/> and a applies a transform to all the entites.
        /// </summary>
        /// <param name="prefab">The <see cref="Prefab"/> to instantiate.</param>
        /// <param name="translation">The <see cref="Vector3"/> to translate the entities by.</param>
        /// <param name="rotation">The <see cref="Quaternion"/> to rotate the entities by.</param>
        /// <param name="scale">The <see cref="Vector3"/> to scale the entities by.</param>
        /// <returns>The instatiated and translated entities.</returns>
        /// <exception cref="ArgumentException">If <paramref name="prefab"/> is <see langword="null"/>.</exception>
        public static List<Entity> Instantiate(this Prefab prefab, ref Vector3 translation, ref Quaternion rotation, ref Vector3 scale)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            Matrix localMatrix;
            Matrix.Transformation(ref scale, ref rotation, ref translation, out localMatrix);

            var instances = prefab.Instantiate();

            foreach (var instance in instances)
            {
                instance.Transform.UpdateLocalMatrix();
                var entityMatrix = instance.Transform.LocalMatrix * localMatrix;

                if (instance.Transform.UseTRS)
                {
                    entityMatrix.Decompose(out instance.Transform.Scale, out instance.Transform.Rotation, out instance.Transform.Position);
                }
                else
                {
                    instance.Transform.LocalMatrix = entityMatrix;
                }                
                
            }   

            return instances;
        }

        /// <summary>
        /// Instantiates a <see cref="Prefab"/> and a applies a transform to all the entites.
        /// </summary>
        /// <param name="prefab">The <see cref="Prefab"/> to instantiate.</param>
        /// <param name="translation">The <see cref="Vector3"/> to translate the entities by.</param>
        /// <param name="rotation">The <see cref="Quaternion"/> to rotate the entities by.</param>
        /// <param name="scale">The <see cref="Vector3"/> to scale the entities by.</param>
        /// <returns>The instatiated and translated entities.</returns>
        /// <exception cref="ArgumentException">If <paramref name="prefab"/> is <see langword="null"/>.</exception>
        public static List<Entity> Instantiate(this Prefab prefab, Vector3 translation, Quaternion rotation, Vector3 scale)
        {
            return prefab.Instantiate(ref translation, ref rotation, ref scale);
        }
    }
}
