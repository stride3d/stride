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
    /// Extension methods for <see cref="TransformComponent"/>.
    /// </summary>
    public static class TransformExtensions
    {
        /// <summary>
        /// The default world up vector. The default is <see cref="Vector3.UnitY"/>.
        /// </summary>
        public static Vector3 WorldUp = Vector3.UnitY;

        /// <summary>
        /// Updates the <see cref="TransformComponent.Position"/>, <see cref="TransformComponent.Rotation"/> and <see cref="TransformComponent.Scale"/> members of the given <see cref="TransformComponent"/>.
        /// </summary>
        /// <param name="transform">The <see cref="TransformComponent"/> to update.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="transform"/> is <see langword="null"/>.</exception>
        public static void UpdateTRSFromLocal(this TransformComponent transform)
        {
            if (transform == null)
            {
                throw new ArgumentNullException(nameof(transform));
            }

            transform.LocalMatrix.Decompose(out transform.Scale, out transform.Rotation, out transform.Position);
        }

        /// <summary>
        /// Moves the given <see cref="TransformComponent"/> position by the specified <paramref name="translation"/> in the coordinate space defined by <paramref name="relativeTo"/>.
        /// </summary>
        /// <param name="transform">The <see cref="TransformComponent"/> to update.</param>
        /// <param name="translation">The translation vector to move by.</param>
        /// <param name="relativeTo">The coordinate space to perform the translation in.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="transform"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="transform"/> has <see cref="TransformComponent.UseTRS"/> set to <see langword="null"/>.</exception>
        /// <remarks>
        /// This method updates the <see cref="TransformComponent.LocalMatrix"/> and <see cref="TransformComponent.WorldMatrix"/> after transformation.
        /// </remarks>
        public static void Translate(this TransformComponent transform, ref Vector3 translation, Space relativeTo = Space.Self)
        {
            if (transform == null)
            {
                throw new ArgumentNullException(nameof(transform));
            }

            if (!transform.UseTRS)
            {
                throw new ArgumentException("Must use TRS", nameof(transform));
            }

            var localTranslation = translation;


            if (relativeTo == Space.Self)
            {
                Vector3.TransformNormal(ref translation, ref transform.WorldMatrix, out localTranslation);
            }

            if (transform.Parent != null)
            {
                Matrix.Invert(ref transform.Parent.WorldMatrix, out var inverseParent);
                Vector3.TransformNormal(ref localTranslation, ref inverseParent, out localTranslation);
            }

            transform.Position += localTranslation;

            transform.UpdateWorldMatrix();
        }

        /// <summary>
        /// Moves the given <see cref="TransformComponent"/> position by the specified <paramref name="translation"/> in the coordinate space defined by <paramref name="relativeTo"/>.
        /// </summary>
        /// <param name="transform">The <see cref="TransformComponent"/> to update.</param>
        /// <param name="translation">The translation vector to move by.</param>
        /// <param name="relativeTo">The coordinate space to perform the translation in.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="transform"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="transform"/> has <see cref="TransformComponent.UseTRS"/> set to <see langword="null"/>.</exception>
        /// <remarks>
        /// This method updates the <see cref="TransformComponent.LocalMatrix"/> and <see cref="TransformComponent.WorldMatrix"/> after transformation.
        /// </remarks>
        public static void Translate(this TransformComponent transform, Vector3 translation, Space relativeTo = Space.Self)
        {
            transform.Translate(ref translation, relativeTo);
        }

        /// <summary>
        /// Moves the given <see cref="TransformComponent"/> position by the specified <paramref name="translation"/> relative to the local coordinate space of another <see cref="TransformComponent"/>.
        /// </summary>
        /// <param name="transform">The <see cref="TransformComponent"/> to update.</param>
        /// <param name="translation">The translation vector to move by.</param>
        /// <param name="relativeTo">The <see cref="TransformComponent"/> to perform the translation relative to.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="transform"/> or <paramref name="relativeTo"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="transform"/> has <see cref="TransformComponent.UseTRS"/> set to <see langword="null"/>.</exception>
        /// <remarks>
        /// This method updates the <see cref="TransformComponent.LocalMatrix"/> and <see cref="TransformComponent.WorldMatrix"/> after transformation.
        /// </remarks>
        public static void Translate(this TransformComponent transform, ref Vector3 translation, TransformComponent relativeTo)
        {
            if (transform == null)
            {
                throw new ArgumentNullException(nameof(transform));
            }

            if (!transform.UseTRS)
            {
                throw new ArgumentException("Must use TRS", nameof(transform));
            }

            if (relativeTo == null)
            {
                throw new ArgumentNullException(nameof(relativeTo));
            }

            relativeTo.TransformDirection(ref translation, out var localTranslation);
            transform.Translate(ref localTranslation, Space.World);

        }

        /// <summary>
        /// Moves the given <see cref="TransformComponent"/> position by the specified <paramref name="translation"/> relative to the local coordinate space of another <see cref="TransformComponent"/>.
        /// </summary>
        /// <param name="transform">The <see cref="TransformComponent"/> to update.</param>
        /// <param name="translation">The translation vector to move by.</param>
        /// <param name="relativeTo">The <see cref="TransformComponent"/> to perform the translation relative to.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="transform"/> or <paramref name="relativeTo"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="transform"/> has <see cref="TransformComponent.UseTRS"/> set to <see langword="null"/>.</exception>
        /// <remarks>
        /// This method updates the <see cref="TransformComponent.LocalMatrix"/> and <see cref="TransformComponent.WorldMatrix"/> after transformation.
        /// </remarks>
        public static void Translate(this TransformComponent transform, Vector3 translation, TransformComponent relativeTo)
        {
            transform.Translate(ref translation, relativeTo);
        }

        /// <summary>
        /// Rotates the given <see cref="TransformComponent"/> by the specified <paramref name="eulerAngles"/> in the coordinate space defined by <paramref name="relativeTo"/>.
        /// </summary>
        /// <param name="transform">The <see cref="TransformComponent"/> to update.</param>
        /// <param name="eulerAngles">The euler angles in radians to rotate by.</param>
        /// <param name="relativeTo">The coordinate space to perform the rotation in.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="transform"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="transform"/> has <see cref="TransformComponent.UseTRS"/> set to <see langword="null"/>.</exception>
        /// <remarks>
        /// This method updates the <see cref="TransformComponent.LocalMatrix"/> and <see cref="TransformComponent.WorldMatrix"/> after transformation.
        /// </remarks>
        public static void Rotate(this TransformComponent transform, ref Vector3 eulerAngles, Space relativeTo = Space.Self)
        {
            if (transform == null)
            {
                throw new ArgumentNullException(nameof(transform));
            }

            if (!transform.UseTRS)
            {
                throw new ArgumentException("Must use TRS", nameof(transform));
            }


            Quaternion rotation = Quaternion.Identity;

            if(relativeTo == Space.Self)
            {
                //What do I do here???
                if(eulerAngles.X != 0f)
                {
                    var right = transform.WorldMatrix.Right; right.Normalize();
                    Quaternion.RotationAxis(ref right, eulerAngles.X, out var axisRotation);
                    Quaternion.Multiply(ref rotation, ref axisRotation, out rotation);
                }

                if (eulerAngles.Y != 0f)
                {
                    var up = transform.WorldMatrix.Up; up.Normalize();
                    Quaternion.RotationAxis(ref up, eulerAngles.Y, out var axisRotation);
                    Quaternion.Multiply(ref rotation, ref axisRotation, out rotation);
                }

                if (eulerAngles.Z != 0f)
                {
                    var forward = transform.WorldMatrix.Forward; forward.Normalize();
                    Quaternion.RotationAxis(ref forward, eulerAngles.Z, out var axisRotation);
                    Quaternion.Multiply(ref rotation, ref axisRotation, out rotation);
                }
            }
            else
            {
                //Quaternion.RotationYawPitchRoll(eulerAngles.Y, eulerAngles.X, eulerAngles.Z, out rotation);
                MathUtilEx.ToQuaternion(ref eulerAngles, out rotation);
            }

            if(transform.Parent != null)
            {
                transform.Parent.WorldMatrix.Decompose(out var _, out Quaternion parentRotation, out var _);
                parentRotation.Conjugate();

                Quaternion.Multiply(ref rotation, ref parentRotation, out rotation);
            }

            Quaternion.Multiply(ref transform.Rotation, ref rotation, out transform.Rotation);

            transform.UpdateWorldMatrix();
        }

        /// <summary>
        /// Rotates the given <see cref="TransformComponent"/> by the specified <paramref name="eulerAngles"/> in the coordinate space defined by <paramref name="relativeTo"/>.
        /// </summary>
        /// <param name="transform">The <see cref="TransformComponent"/> to update.</param>
        /// <param name="eulerAngles">The euler angles in radians to rotate by.</param>
        /// <param name="relativeTo">The coordinate space to perform the rotation in.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="transform"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="transform"/> has <see cref="TransformComponent.UseTRS"/> set to <see langword="null"/>.</exception>
        /// <remarks>
        /// This method updates the <see cref="TransformComponent.LocalMatrix"/> and <see cref="TransformComponent.WorldMatrix"/> after transformation.
        /// </remarks>
        public static void Rotate(this TransformComponent transform, Vector3 eulerAngles, Space relativeTo = Space.Self)
        {
            transform.Rotate(ref eulerAngles, relativeTo);
        }

        //public static void RotateAround(this TransformComponent transform, ref Vector3 point, ref Vector3 axis, float angle)
        //{
        //    if (transform == null)
        //    {
        //        throw new ArgumentNullException(nameof(transform));
        //    }

        //    throw new NotImplementedException();

        //}

        //public static void RotateAround(this TransformComponent transform, Vector3 point, Vector3 axis, float angle)
        //{
        //    transform.RotateAround(ref point, ref axis, angle);
        //}

        /// <summary>
        /// Performs a direction transformation using the given <see cref="TransformComponent.WorldMatrix"/>.
        /// </summary>
        /// <param name="transform">The transform to get the world matrix from.</param>
        /// <param name="direction">The direction vector to transform.</param>
        /// <param name="result">The transformed direction.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="transform"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method does not update the <see cref="TransformComponent.WorldMatrix"/> before performing the transformation.
        /// If the <see cref="TransformComponent"/> has been modified since the last frame you may need to call the <see cref="TransformComponent.UpdateWorldMatrix"/> method first.
        /// </remarks>
        public static void TransformDirection(this TransformComponent transform, ref Vector3 direction, out Vector3 result)
        {
            if (transform == null)
            {
                throw new ArgumentNullException(nameof(transform));
            }            

            Vector3.TransformNormal(ref direction, ref transform.WorldMatrix, out result);
        }

        /// <summary>
        /// Performs a direction transformation using the given <see cref="TransformComponent.WorldMatrix"/>.
        /// </summary>
        /// <param name="transform">The transform to get the world matrix from.</param>
        /// <param name="direction">The direction vector to transform.</param>
        /// <returns>The transformed direction.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="transform"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method does not update the <see cref="TransformComponent.WorldMatrix"/> before performing the transformation.
        /// If the <see cref="TransformComponent"/> has been modified since the last frame you may need to call the <see cref="TransformComponent.UpdateWorldMatrix"/> method first.
        /// </remarks>
        public static Vector3 TransformDirection(this TransformComponent transform, Vector3 direction)
        {
            transform.TransformDirection(ref direction, out var result);
            return result;
        }

        /// <summary>
        /// Performs a direction transformation using the inverse of the given <see cref="TransformComponent.WorldMatrix"/>.
        /// </summary>
        /// <param name="transform">The transform to get the world matrix from.</param>
        /// <param name="direction">The direction vector to transform.</param>
        /// <param name="result">The transformed direction.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="transform"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method does not update the <see cref="TransformComponent.WorldMatrix"/> before performing the transformation.
        /// If the <see cref="TransformComponent"/> has been modified since the last frame you may need to call the <see cref="TransformComponent.UpdateWorldMatrix"/> method first.
        /// </remarks>
        public static void InverseTransformDirection(this TransformComponent transform, ref Vector3 direction, out Vector3 result)
        {
            if (transform == null)
            {
                throw new ArgumentNullException(nameof(transform));
            }

            Matrix.Invert(ref transform.WorldMatrix, out var inverseMatrix);

            Vector3.TransformNormal(ref direction, ref inverseMatrix, out result);
        }

        /// <summary>
        /// Performs a direction transformation using the inverse of the given <see cref="TransformComponent.WorldMatrix"/>.
        /// </summary>
        /// <param name="transform">The transform to get the world matrix from.</param>
        /// <param name="direction">The direction vector to transform.</param>
        /// <returns>The transformed direction.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="transform"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method does not update the <see cref="TransformComponent.WorldMatrix"/> before performing the transformation.
        /// If the <see cref="TransformComponent"/> has been modified since the last frame you may need to call the <see cref="TransformComponent.UpdateWorldMatrix"/> method first.
        /// </remarks>
        public static Vector3 InverseTransformDirection(this TransformComponent transform, Vector3 direction)
        {
            transform.InverseTransformDirection(ref direction, out var result);
            return result;
        }

        /// <summary>
        /// Performs a coordinate transformation using the given <see cref="TransformComponent.WorldMatrix"/>.
        /// </summary>
        /// <param name="transform">The transform to get the world matrix from.</param>
        /// <param name="position">The coordinate vector to transform.</param>
        /// <param name="result">The transformed coordinate.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="transform"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method does not update the <see cref="TransformComponent.WorldMatrix"/> before performing the transformation.
        /// If the <see cref="TransformComponent"/> has been modified since the last frame you may need to call the <see cref="TransformComponent.UpdateWorldMatrix"/> method first.
        /// </remarks>
        public static void TransformPosition(this TransformComponent transform, ref Vector3 position, out Vector3 result)
        {
            if (transform == null)
            {
                throw new ArgumentNullException(nameof(transform));
            }

            Vector3.TransformCoordinate(ref position, ref transform.WorldMatrix, out result);
        }

        /// <summary>
        /// Performs a coordinate transformation using the given <see cref="TransformComponent.WorldMatrix"/>.
        /// </summary>
        /// <param name="transform">The transform to get the world matrix from.</param>
        /// <param name="position">The coordinate vector to transform.</param>
        /// <returns>The transformed coordinate.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="transform"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method does not update the <see cref="TransformComponent.WorldMatrix"/> before performing the transformation.
        /// If the <see cref="TransformComponent"/> has been modified since the last frame you may need to call the <see cref="TransformComponent.UpdateWorldMatrix"/> method first.
        /// </remarks>
        public static Vector3 TransformPosition(this TransformComponent transform, Vector3 position)
        {
            transform.TransformPosition(ref position, out var result);
            return result;
        }

        /// <summary>
        /// Performs a coordinate transformation using the inverse of the given <see cref="TransformComponent.WorldMatrix"/>.
        /// </summary>
        /// <param name="transform">The transform to get the world matrix from.</param>
        /// <param name="position">The coordinate vector to transform.</param>
        /// <param name="result">The transformed coordinate.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="transform"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method does not update the <see cref="TransformComponent.WorldMatrix"/> before performing the transformation.
        /// If the <see cref="TransformComponent"/> has been modified since the last frame you may need to call the <see cref="TransformComponent.UpdateWorldMatrix"/> method first.
        /// </remarks>
        public static void InverseTransformPosition(this TransformComponent transform, ref Vector3 position, out Vector3 result)
        {
            if (transform == null)
            {
                throw new ArgumentNullException(nameof(transform));
            }

            Matrix.Invert(ref transform.WorldMatrix, out var inverseMatrix);

            Vector3.TransformCoordinate(ref position, ref inverseMatrix, out result);
        }

        /// <summary>
        /// Performs a coordinate transformation using the inverse of the given <see cref="TransformComponent.WorldMatrix"/>.
        /// </summary>
        /// <param name="transform">The transform to get the world matrix from.</param>
        /// <param name="position">The coordinate vector to transform.</param>
        /// <returns>The transformed coordinate.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="transform"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method does not update the <see cref="TransformComponent.WorldMatrix"/> before performing the transformation.
        /// If the <see cref="TransformComponent"/> has been modified since the last frame you may need to call the <see cref="TransformComponent.UpdateWorldMatrix"/> method first.
        /// </remarks>
        public static Vector3 InverseTransformPosition(this TransformComponent transform, Vector3 position)
        {
            transform.InverseTransformPosition(ref position, out var result);
            return result;
        }

        /// <summary>
        /// Performs a normal transformation using the given <see cref="TransformComponent.WorldMatrix"/>.
        /// </summary>
        /// <param name="transform">The transform to get the world matrix from.</param>
        /// <param name="vector">The normal vector to transform.</param>
        /// <param name="result">The transformed normal.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="transform"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method does not update the <see cref="TransformComponent.WorldMatrix"/> before performing the transformation.
        /// If the <see cref="TransformComponent"/> has been modified since the last frame you may need to call the <see cref="TransformComponent.UpdateWorldMatrix"/> method first.
        /// </remarks>
        public static void TransformVector(this TransformComponent transform, ref Vector3 vector, out Vector3 result)
        {
            if (transform == null)
            {
                throw new ArgumentNullException(nameof(transform));
            }

            Vector3.TransformNormal(ref vector, ref transform.WorldMatrix, out result);
        }

        /// <summary>
        /// Performs a normal transformation using the given <see cref="TransformComponent.WorldMatrix"/>.
        /// </summary>
        /// <param name="transform">The transform to get the world matrix from.</param>
        /// <param name="vector">The normal vector to transform.</param>
        /// <returns>The transformed normal.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="transform"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method does not update the <see cref="TransformComponent.WorldMatrix"/> before performing the transformation.
        /// If the <see cref="TransformComponent"/> has been modified since the last frame you may need to call the <see cref="TransformComponent.UpdateWorldMatrix"/> method first.
        /// </remarks>
        public static Vector3 TransformVector(this TransformComponent transform, Vector3 vector)
        {
            transform.TransformVector(ref vector, out var result);
            return result;
        }

        /// <summary>
        /// Performs a normal transformation using the inverse of the given <see cref="TransformComponent.WorldMatrix"/>.
        /// </summary>
        /// <param name="transform">The transform to get the world matrix from.</param>
        /// <param name="vector">The normal vector to transform.</param>
        /// <param name="result">The transformed normal.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="transform"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method does not update the <see cref="TransformComponent.WorldMatrix"/> before performing the transformation.
        /// If the <see cref="TransformComponent"/> has been modified since the last frame you may need to call the <see cref="TransformComponent.UpdateWorldMatrix"/> method first.
        /// </remarks>
        public static void InverseTransformVector(this TransformComponent transform, ref Vector3 vector, out Vector3 result)
        {
            if (transform == null)
            {
                throw new ArgumentNullException(nameof(transform));
            }

            Matrix.Invert(ref transform.WorldMatrix, out var inverseMatrix);

            Vector3.TransformNormal(ref vector, ref inverseMatrix, out result);
        }

        /// <summary>
        /// Performs a normal transformation using the inverse of the given <see cref="TransformComponent.WorldMatrix"/>.
        /// </summary>
        /// <param name="transform">The transform to get the world matrix from.</param>
        /// <param name="vector">The normal vector to transform.</param>
        /// <returns>The transformed normal.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="transform"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method does not update the <see cref="TransformComponent.WorldMatrix"/> before performing the transformation.
        /// If the <see cref="TransformComponent"/> has been modified since the last frame you may need to call the <see cref="TransformComponent.UpdateWorldMatrix"/> method first.
        /// </remarks>
        public static Vector3 InverseTransformVector(this TransformComponent transform, Vector3 vector)
        {
            transform.InverseTransformVector(ref vector, out var result);
            return result;
        }

        /// <summary>
        /// Sets the transforms rotation so it's forward vector points at the <paramref name="target"/>.
        /// </summary>
        /// <param name="transform">The <see cref="TransformComponent"/> to update.</param>
        /// <param name="target">The target to point towards</param>
        /// <param name="worldUp">A Vector specifying the upward direction.</param>
        /// <param name="smooth">Value between 0 and 1 indicating the weight of target orientation. The default is 1.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="transform"/> or <paramref name="target"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="transform"/> has <see cref="TransformComponent.UseTRS"/> set to <see langword="null"/>.</exception>
        /// <remarks>
        /// This method updates the <see cref="TransformComponent.LocalMatrix"/> and <see cref="TransformComponent.WorldMatrix"/> after transformation.
        /// </remarks>
        public static void LookAt(this TransformComponent transform, TransformComponent target, ref Vector3 worldUp, float smooth = 1.0f)
        {
            if (transform == null)
            {
                throw new ArgumentNullException(nameof(transform));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }            

            var targetPosition = target.WorldMatrix.TranslationVector;
            transform.LookAt(ref targetPosition, ref worldUp,smooth);
        }

        /// <summary>
        /// Sets the transforms rotation so it's forward vector points at the <paramref name="target"/>.
        /// </summary>
        /// <param name="transform">The <see cref="TransformComponent"/> to update.</param>
        /// <param name="target">The target to point towards</param>
        /// <param name="worldUp">A Vector specifying the upward direction.</param>
        /// <param name="smooth">Value between 0 and 1 indicating the weight of target orientation. The default is 1.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="transform"/> or <paramref name="target"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="transform"/> has <see cref="TransformComponent.UseTRS"/> set to <see langword="null"/>.</exception>
        /// <remarks>
        /// This method updates the <see cref="TransformComponent.LocalMatrix"/> and <see cref="TransformComponent.WorldMatrix"/> after transformation.
        /// </remarks>
        public static void LookAt(this TransformComponent transform, TransformComponent target, Vector3 worldUp, float smooth = 1.0f)
        {
            transform.LookAt(target, ref worldUp, smooth);
        }

        /// <summary>
        /// Sets the transforms rotation so it's forward vector points at the <paramref name="target"/>. 
        /// The world up vector use is defined by <see cref="WorldUp"/>.
        /// </summary>
        /// <param name="transform">The <see cref="TransformComponent"/> to update.</param>
        /// <param name="target">The target to point towards</param>
        /// <param name="smooth">Value between 0 and 1 indicating the weight of target orientation. The default is 1.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="transform"/> or <paramref name="target"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="transform"/> has <see cref="TransformComponent.UseTRS"/> set to <see langword="null"/>.</exception>
        /// <remarks>
        /// This method updates the <see cref="TransformComponent.LocalMatrix"/> and <see cref="TransformComponent.WorldMatrix"/> after transformation.
        /// </remarks>
        public static void LookAt(this TransformComponent transform, TransformComponent target, float smooth = 1.0f)
        {
            transform.LookAt(target, ref WorldUp, smooth);
        }

        /// <summary>
        /// Sets the transforms rotation so it's forward vector points at the <paramref name="target"/>.
        /// </summary>
        /// <param name="transform">The <see cref="TransformComponent"/> to update.</param>
        /// <param name="target">The target to point towards</param>
        /// <param name="worldUp">A Vector specifying the upward direction.</param>
        /// <param name="smooth">Value between 0 and 1 indicating the weight of target orientation. The default is 1.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="transform"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="transform"/> has <see cref="TransformComponent.UseTRS"/> set to <see langword="null"/>.</exception>
        /// <remarks>
        /// This method updates the <see cref="TransformComponent.LocalMatrix"/> and <see cref="TransformComponent.WorldMatrix"/> after transformation.
        /// </remarks>
        public static void LookAt(this TransformComponent transform, ref Vector3 target, ref Vector3 worldUp, float smooth = 1.0f)
        {
            if (transform == null)
            {
                throw new ArgumentNullException(nameof(transform));
            }

            if (!transform.UseTRS)
            {
                throw new ArgumentException("Must use TRS", nameof(transform));
            }

            var localTarget = target;
            var localUp = worldUp;

            if(transform.Parent != null)
            {
                Matrix.Invert(ref transform.Parent.WorldMatrix, out var inverseParent);
                Vector3.TransformCoordinate(ref target, ref inverseParent, out localTarget);
                Vector3.TransformNormal(ref worldUp, ref inverseParent, out localUp);
            }

            var localEye = transform.LocalMatrix.TranslationVector;

            MathUtilEx.LookRotation(ref localEye, ref localTarget, ref localUp, out var lookRotation);

            if(smooth == 1.0f)
            {
                transform.Rotation = lookRotation;
            }
            else
            {
                Quaternion.Slerp(ref transform.Rotation, ref lookRotation, smooth, out transform.Rotation);
            }


            transform.UpdateWorldMatrix();
        }

        /// <summary>
        /// Sets the transforms rotation so it's forward vector points at the <paramref name="target"/>.
        /// </summary>
        /// <param name="transform">The <see cref="TransformComponent"/> to update.</param>
        /// <param name="target">The target to point towards</param>
        /// <param name="worldUp">A Vector specifying the upward direction.</param>
        /// <param name="smooth">Value between 0 and 1 indicating the weight of target orientation. The default is 1.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="transform"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="transform"/> has <see cref="TransformComponent.UseTRS"/> set to <see langword="null"/>.</exception>
        /// <remarks>
        /// This method updates the <see cref="TransformComponent.LocalMatrix"/> and <see cref="TransformComponent.WorldMatrix"/> after transformation.
        /// </remarks>
        public static void LookAt(this TransformComponent transform, Vector3 target, Vector3 worldUp, float smooth = 1.0f)
        {
            transform.LookAt(ref target, ref worldUp, smooth);
        }

        /// <summary>
        /// Sets the transforms rotation so it's forward vector points at the <paramref name="target"/>. 
        /// The world up vector use is defined by <see cref="WorldUp"/>.
        /// </summary>
        /// <param name="transform">The <see cref="TransformComponent"/> to update.</param>
        /// <param name="target">The target to point towards</param>
        /// <param name="smooth">Value between 0 and 1 indicating the weight of target orientation. The default is 1.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="transform"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="transform"/> has <see cref="TransformComponent.UseTRS"/> set to <see langword="null"/>.</exception>
        /// <remarks>
        /// This method updates the <see cref="TransformComponent.LocalMatrix"/> and <see cref="TransformComponent.WorldMatrix"/> after transformation.
        /// </remarks>
        public static void LookAt(this TransformComponent transform, ref Vector3 target, float smooth = 1.0f)
        {
            transform.LookAt(ref target, ref WorldUp, smooth);
        }

        /// <summary>
        /// Sets the transforms rotation so it's forward vector points at the <paramref name="target"/>. 
        /// The world up vector use is defined by <see cref="WorldUp"/>.
        /// </summary>
        /// <param name="transform">The <see cref="TransformComponent"/> to update.</param>
        /// <param name="target">The target to point towards</param>
        /// <param name="smooth">Value between 0 and 1 indicating the weight of target orientation. The default is 1.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="transform"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="transform"/> has <see cref="TransformComponent.UseTRS"/> set to <see langword="null"/>.</exception>
        /// <remarks>
        /// This method updates the <see cref="TransformComponent.LocalMatrix"/> and <see cref="TransformComponent.WorldMatrix"/> after transformation.
        /// </remarks>
        public static void LookAt(this TransformComponent transform, Vector3 target, float smooth = 1.0f)
        {
            transform.LookAt(ref target, ref WorldUp, smooth);
        }
    }
}
