using BenchmarkDotNet.Attributes;

namespace Stride.Core.Mathematics.Benchmarks;

// dotnet run -c Release -f net10.0 --filter "*QuaternionBenchmarks*"

[DisassemblyDiagnoser, HideColumns("Job", "Error", "StdDev", "Median", "RatioSD")]
public class QuaternionBenchmarks
{
    private Quaternion q1;
    private Quaternion q2;
    private Quaternion q3;
    private Quaternion q4;
    private float value1;
    private float value2;
    private float value3;

    [GlobalSetup]
    public void Setup()
    {
        Random rng = new(42);
        Quaternion[] values = [.. Enumerable.Range(0, 4).Select(_ =>
        {
            var x = rng.NextSingle();
            var y = rng.NextSingle();
            var z = rng.NextSingle();
            var w = rng.NextSingle();
            return new Quaternion(x, y, z, w);
        })];
        q1 = values[0];
        q2 = values[1];
        q3 = values[2];
        q4 = values[3];
        value1 = rng.NextSingle();
        value2 = rng.NextSingle();
        value3 = rng.NextSingle();
    }

    [Benchmark]
    public bool IsIdentity()
    {
        return q1.IsIdentity;
    }

    [Benchmark]
    public bool IsNormalized()
    {
        return q1.IsNormalized;
    }

    [Benchmark]
    public float Angle()
    {
        return q1.Angle;
    }

    [Benchmark]
    public Vector3 Axis()
    {
        return q1.Axis;
    }

    [Benchmark]
    public Vector3 YawPitchRoll()
    {
        return q1.YawPitchRoll;
    }

    [Benchmark]
    public Quaternion Conjugate()
    {
        return Quaternion.Conjugate(q1);
    }

    [Benchmark]
    public Quaternion Invert()
    {
        return Quaternion.Invert(q1);
    }

    [Benchmark]
    public float Length()
    {
        return q1.Length();
    }

    [Benchmark]
    public float LengthSquared()
    {
        return q1.LengthSquared();
    }

    [Benchmark]
    public Quaternion Normalize()
    {
        return Quaternion.Normalize(q1);
    }

    [Benchmark]
    public Quaternion Add()
    {
        return Quaternion.Add(q1, q2);
    }

    [Benchmark]
    public Quaternion Subtract()
    {
        return Quaternion.Subtract(q1, q2);
    }

    [Benchmark]
    public Quaternion Multiply()
    {
        return Quaternion.Multiply(q1, q2);
    }

    [Benchmark]
    public Quaternion Negate()
    {
        return Quaternion.Negate(q1);
    }

    [Benchmark]
    public Quaternion Barycentric()
    {
        return Quaternion.Barycentric(q1, q2, q3, value1, value2);
    }

    [Benchmark]
    public float Dot()
    {
        return Quaternion.Dot(q1, q2);
    }

    [Benchmark]
    public float AngleBetween()
    {
        return Quaternion.AngleBetween(q1, q2);
    }

    [Benchmark]
    public Quaternion Exponential()
    {
        return Quaternion.Exponential(q1);
    }

    [Benchmark]
    public Quaternion Lerp()
    {
        return Quaternion.Lerp(q1, q2, value1);
    }

    [Benchmark]
    public Quaternion Logarithm()
    {
        return Quaternion.Logarithm(q1);
    }

    [Benchmark]
    public Quaternion RotationX()
    {
        return Quaternion.RotationX(value1);
    }

    [Benchmark]
    public Quaternion RotationY()
    {
        return Quaternion.RotationX(value2);
    }

    [Benchmark]
    public Quaternion RotationZ()
    {
        return Quaternion.RotationX(value3);
    }

    [Benchmark]
    public Quaternion RotationYawPitchRoll()
    {
        return Quaternion.RotationYawPitchRoll(value1, value2, value3);
    }

    [Benchmark]
    public Quaternion Slerp()
    {
        return Quaternion.Slerp(q1, q2, value1);
    }

    [Benchmark]
    public Quaternion RotateTowards()
    {
        return Quaternion.RotateTowards(q1, q2, value1);
    }

    [Benchmark]
    public Quaternion Squad()
    {
        return Quaternion.Squad(q1, q2, q3, q4, value1);
    }

    [Benchmark]
    public Quaternion Operator_Add()
    {
        return q1 + q2;
    }

    [Benchmark]
    public Quaternion Operator_Subtract()
    {
        return q1 - q2;
    }

    [Benchmark]
    public Quaternion Operator_Negate()
    {
        return -q1;
    }

    [Benchmark]
    public Quaternion Operator_Multiply()
    {
        return q1 * q2;
    }

    [Benchmark]
    public bool Operator_Equals()
    {
        return q1 == q2;
    }

    [Benchmark]
    public bool Operator_NotEquals()
    {
        return q1 != q2;
    }
}
