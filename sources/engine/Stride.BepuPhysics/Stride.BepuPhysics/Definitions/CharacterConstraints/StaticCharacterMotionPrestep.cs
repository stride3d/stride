using BepuUtilities;
using System.Numerics;

namespace Stride.BepuPhysics.Definitions.CharacterConstraints;
//Note that all the solver-side data is in terms of 'Wide' data types- the solver never works on just one constraint at a time. Instead,
//it executes them in bundles of width equal to the runtime/hardware exposed SIMD unit width. This lets the solver scale with wider compute units.
//(This is important for machines that can perform 8 or more operations per instruction- there's no good way to map a single constraint instance's 
//computation onto such a wide instruction, so if the solver tried to do such a thing, it would leave a huge amount of performance on the table.)

//"Prestep" data can be thought of as the input to the solver. It describes everything the solver needs to know about.
/// <summary>
/// AOSOA formatted bundle of prestep data for multiple static-supported character motion constraints.
/// </summary>
public struct StaticCharacterMotionPrestep
{
    //Note that the prestep data layout is important. The solver tends to be severely memory bandwidth bound, so using a minimal representation is valuable.
    //That's why the Basis is stored as a quaternion and not a full Matrix- the cost of the arithmetic operations to expand it back into the original matrix form is far less
    //than the cost of loading all the extra lanes of data when scaled up to many cores.
    public QuaternionWide SurfaceBasis;
    public Vector<float> MaximumHorizontalForce;
    public Vector<float> MaximumVerticalForce;
    public Vector<float> Depth;
    public Vector2Wide TargetVelocity;
    public Vector3Wide OffsetFromCharacter;
}
