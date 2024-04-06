using System.Runtime.InteropServices;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using NRigidPose = BepuPhysics.RigidPose;

namespace Stride.BepuPhysics.Definitions.Colliders;

[DataContract]
public sealed class ConvexHullCollider : ColliderBase
{
    private Vector3 _scale = new(1, 1, 1);
    private DecomposedHulls _hull = null!;

    public Vector3 Scale
    {
        get => _scale;
        set
        {
            _scale = value;
            Container?.TryUpdateContainer();
        }
    }

    [MemberRequired(ReportAs = MemberRequiredReportType.Error)]
    public required DecomposedHulls Hull
    {
        get
        { 
            return _hull;
        }
        set
        {
            _hull = value;
            Container?.TryUpdateContainer();
        }
    }

    internal override void AddToCompoundBuilder(ShapeCacheSystem shape, BufferPool pool, ref CompoundBuilder builder, NRigidPose localPose)
    {
        foreach (var mesh in Hull.Hulls)
        {
#warning find a way to cache all of this to reuse the same ConvexHull
            foreach (var hull in mesh) // Can't merge all of them into one since individual hulls are convex but aggregate of them may not be
            {
                var points = MemoryMarshal.Cast<Vector3, System.Numerics.Vector3>(hull.Points);

                if (_scale != Vector3.One) // Bepu doesn't support scaling on the collider itself, we have to create a temporary array and scale the points before passing it on
                {
                    var copy = points.ToArray();
                    var scaleAsNumerics = _scale.ToNumeric();
                    for (int i = 0; i < copy.Length; i++)
                    {
                        copy[i] *= scaleAsNumerics;
                    }

                    points = copy;
                }

                var convex = new ConvexHull(points, pool, out var center);
                localPose.Position += center;
                builder.Add(convex, localPose, Mass);
            }
        }
    }
}
