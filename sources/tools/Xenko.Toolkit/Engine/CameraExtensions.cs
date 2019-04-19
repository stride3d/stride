using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Rendering.Compositing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xenko.Toolkit.Mathematics;

namespace Xenko.Toolkit.Engine
{
    /// <summary>
    /// Extensions for <see cref="CameraComponent"/>.
    /// </summary>
    public static class CameraExtensions
    {
        ///// <summary>
        ///// Gets the scenes composition main camera or null if there is no GraphicsCompositor or Cameras.
        ///// </summary>
        ///// <param name="sceneSystem"></param>
        ///// <param name="mainCameraSlotName"></param>
        ///// <returns>The selected main CameraComponent</returns>
        ///// <remarks>
        ///// If there is more than 1 SceneCameraSlot and there is one with matching name it returns it.
        ///// If no matching SceneCameraSlot is found the first CameraComponent is Returned.
        ///// </remarks>
        ///// <exception cref="ArgumentNullException">If the cameraComponent argument is null.</exception>
        //public static CameraComponent GetMainCamera(this SceneSystem sceneSystem, string mainCameraSlotName = "Main")
        //{
        //    if (sceneSystem == null)
        //    {
        //        throw new ArgumentNullException(nameof(sceneSystem));
        //    }

        //    var cameras = sceneSystem.GraphicsCompositor?.Cameras;

        //    if (cameras == null)
        //    {
        //        return null;
        //    }

        //    SceneCameraSlot cameraSlot = null;

        //    if (cameras.Count == 1)
        //    {
        //        cameraSlot = cameras[0];
        //    }
        //    else
        //    {
        //        cameraSlot = cameras.FirstOrDefault(s => s.Name == mainCameraSlotName);
        //    }

        //    if (cameraSlot == null && cameras.Count > 0)
        //    {
        //        cameraSlot = cameras[0];
        //    }

        //    return cameraSlot?.Camera;
        //}

        /// <summary>
        /// Converts the world position to clip space coordinates relative to camera.
        /// </summary>
        /// <param name="cameraComponent"></param>
        /// <param name="position"></param>
        /// <returns>
        /// The position in clip space.
        /// </returns>
        /// <exception cref="ArgumentNullException">If the cameraComponent argument is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
        /// If the <see cref="CameraComponent"/> or it's containing <see cref="Entity"/> <see cref="TransformComponent"/>has been modified since the last frame you may need to call the <see cref="CameraComponent.Update()"/> method first.
        /// </remarks>
        public static Vector3 WorldToClip(this CameraComponent cameraComponent, Vector3 position)
        {
            cameraComponent.WorldToClip(ref position, out var result);
            return result;

        }

        /// <summary>
        /// Converts the world position to clip space coordinates relative to camera.
        /// </summary>
        /// <param name="cameraComponent"></param>
        /// <param name="position"></param>
        /// <param name="result">The position in clip space.</param>
        /// <exception cref="ArgumentNullException">If the cameraComponent argument is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
        /// If the <see cref="CameraComponent"/> or it's containing <see cref="Entity"/> <see cref="TransformComponent"/>has been modified since the last frame you may need to call the <see cref="CameraComponent.Update()"/> method first.
        /// </remarks>
        public static void WorldToClip(this CameraComponent cameraComponent, ref Vector3 position, out Vector3 result)
        {
            if (cameraComponent == null)
            {
                throw new ArgumentNullException(nameof(cameraComponent));
            }

            Vector3.TransformCoordinate(ref position, ref cameraComponent.ViewProjectionMatrix, out result);

        }

        /// <summary>
        /// Converts the world position to screen space coordinates relative to camera.
        /// </summary>
        /// <param name="cameraComponent"></param>
        /// <param name="position"></param>
        /// <returns>
        /// The screen position in normalized X, Y coordinates. Top-left is (0,0), bottom-right is (1,1). Z is in world units from near camera plane.
        /// </returns>
        /// <exception cref="ArgumentNullException">If the cameraComponent argument is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
        /// If the <see cref="CameraComponent"/> or it's containing <see cref="Entity"/> <see cref="TransformComponent"/>has been modified since the last frame you may need to call the <see cref="CameraComponent.Update()"/> method first.
        /// </remarks>
        public static Vector3 WorldToScreenPoint(this CameraComponent cameraComponent, Vector3 position)
        {
            cameraComponent.WorldToScreenPoint(ref position, out var result);
            return result;
        }

        /// <summary>
        /// Converts the world position to screen space coordinates relative to camera.
        /// </summary>
        /// <param name="cameraComponent"></param>
        /// <param name="position"></param>
        /// <param name="result">The screen position in normalized X, Y coordinates. Top-left is (0,0), bottom-right is (1,1). Z is in world units from near camera plane.</param>
        /// <exception cref="ArgumentNullException">If the cameraComponent argument is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
        /// If the <see cref="CameraComponent"/> or it's containing <see cref="Entity"/> <see cref="TransformComponent"/>has been modified since the last frame you may need to call the <see cref="CameraComponent.Update()"/> method first.
        /// </remarks>
        public static void WorldToScreenPoint(this CameraComponent cameraComponent, ref Vector3 position, out Vector3 result)
        {
            cameraComponent.WorldToClip(ref position, out var clipSpace);

            Vector3.TransformCoordinate(ref position, ref cameraComponent.ViewMatrix, out var viewSpace);

            result = new Vector3
            {
                X = (clipSpace.X + 1f) / 2f,
                Y = 1f - (clipSpace.Y + 1f) / 2f,
                Z = viewSpace.Z + cameraComponent.NearClipPlane,
            };
        }

        /// <summary>
        /// Converts the screen position to a <see cref="RaySegment"/> in world coordinates.
        /// </summary>
        /// <param name="cameraComponent"></param>
        /// <param name="position"></param>
        /// <returns><see cref="RaySegment"/>, starting at near plain and ending at the far plain.</returns>
        /// <exception cref="ArgumentNullException">If the cameraComponent argument is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
        /// If the <see cref="CameraComponent"/> or it's containing <see cref="Entity"/> <see cref="TransformComponent"/>has been modified since the last frame you may need to call the <see cref="CameraComponent.Update()"/> method first.
        /// </remarks>
        public static RaySegment ScreenToWorldRaySegment(this CameraComponent cameraComponent, Vector2 position)
        {
            cameraComponent.ScreenToWorldRaySegment(ref position, out var result);

            return result;
        }

        /// <summary>
        /// Converts the screen position to a <see cref="RaySegment"/> in world coordinates.
        /// </summary>
        /// <param name="cameraComponent"></param>
        /// <param name="position"></param>
        /// <param name="result"><see cref="RaySegment"/>, starting at near plain and ending at the far plain.</param>
        /// <exception cref="ArgumentNullException">If the cameraComponent argument is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
        /// If the <see cref="CameraComponent"/> or it's containing <see cref="Entity"/> <see cref="TransformComponent"/>has been modified since the last frame you may need to call the <see cref="CameraComponent.Update()"/> method first.
        /// </remarks>
        public static void ScreenToWorldRaySegment(this CameraComponent cameraComponent, ref Vector2 position, out RaySegment result)
        {
            if (cameraComponent == null)
            {
                throw new ArgumentNullException(nameof(cameraComponent));
            }

            Matrix.Invert(ref cameraComponent.ViewProjectionMatrix, out var inverseViewProjection);

            ScreenToClipSpace(ref position, out var clipSpace);

            Vector3.TransformCoordinate(ref clipSpace, ref inverseViewProjection, out var near);

            clipSpace.Z = 1f;
            Vector3.TransformCoordinate(ref clipSpace, ref inverseViewProjection, out var far);

            result = new RaySegment(near, far);
        }

        private static void ScreenToClipSpace(ref Vector2 position, out Vector3 clipSpace)
        {
            clipSpace = new Vector3
            {
                X = position.X * 2f - 1f,
                Y = 1f - position.Y * 2f,
                Z = 0f
            };
        }

        private static Vector3 ScreenToClipSpace(Vector2 position)
        {
            ScreenToClipSpace(ref position, out var result);
            return result;
        }


        /// <summary>
        /// Converts the screen position to a point in world coordinates.
        /// </summary>
        /// <param name="cameraComponent"></param>
        /// <param name="position">The screen position in normalized X, Y coordinates. Top-left is (0,0), bottom-right is (1,1). Z is in world units from near camera plane.</param>
        /// <returns>Position in world coordinates.</returns>
        /// <exception cref="ArgumentNullException">If the cameraComponent argument is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
        /// If the <see cref="CameraComponent"/> or it's containing <see cref="Entity"/> <see cref="TransformComponent"/>has been modified since the last frame you may need to call the <see cref="CameraComponent.Update()"/> method first.
        /// </remarks>
        public static Vector3 ScreenToWorldPoint(this CameraComponent cameraComponent, Vector3 position)
        {
            cameraComponent.ScreenToWorldPoint(ref position, out var result);

            return result;
        }

        /// <summary>
        /// Converts the screen position to a point in world coordinates.
        /// </summary>
        /// <param name="cameraComponent"></param>
        /// <param name="position">The screen position in normalized X, Y coordinates. Top-left is (0,0), bottom-right is (1,1). Z is in world units from near camera plane.</param>
        /// <param name="result">Position in world coordinates.</param>
        /// <exception cref="ArgumentNullException">If the cameraComponent argument is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method does not update the <see cref="CameraComponent.ViewMatrix"/> or <see cref="CameraComponent.ProjectionMatrix"/> before performing the transformation.
        /// If the <see cref="CameraComponent"/> or it's containing <see cref="Entity"/> <see cref="TransformComponent"/>has been modified since the last frame you may need to call the <see cref="CameraComponent.Update()"/> method first.
        /// </remarks>
        public static void ScreenToWorldPoint(this CameraComponent cameraComponent, ref Vector3 position, out Vector3 result)
        {
            if (cameraComponent == null)
            {
                throw new ArgumentNullException(nameof(cameraComponent));
            }
            var position2D = position.XY();
            //Matrix.Invert(ref cameraComponent.ProjectionMatrix, out var inverseProjection);
            //Matrix.Invert(ref cameraComponent.ViewMatrix, out var inverseView);

            //ScreenToClipSpace(ref position2D, out var clipSpace);
            //Vector3.TransformCoordinate(ref clipSpace, ref inverseProjection, out var near);

            //near.Z = -position.Z;

            //Vector3.TransformCoordinate(ref near, ref inverseView, out result);

            cameraComponent.ScreenToWorldRaySegment(ref position2D, out var ray);

            var direction = ray.End - ray.Start;
            direction.Normalize();

            Vector3.TransformNormal(ref direction, ref cameraComponent.ViewMatrix, out var viewSpaceDir);

            float rayDistance = (position.Z / viewSpaceDir.Z);

            result = ray.Start + (direction * rayDistance);

        }
    }
}
