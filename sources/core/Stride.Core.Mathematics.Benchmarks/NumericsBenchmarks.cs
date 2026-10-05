using BenchmarkDotNet.Attributes;

namespace Stride.Core.Mathematics.Benchmarks;

using System.Numerics;

// dotnet run -c Release -f net10.0 --filter "*NumericsBenchmarks*"

[DisassemblyDiagnoser, HideColumns("Job", "Error", "StdDev", "Median", "RatioSD")]
public class NumericsBenchmarks
{
    private Vector2 v2_1;
    private Vector2 v2_2;

    private Vector3 v3_1;
    private Vector3 v3_2;

    private Vector4 v4_1;
    private Vector4 v4_2;
    private float value1;

    [GlobalSetup]
    public void Setup()
    {
        Random rng = new(42);
        Vector4[] values = [.. Enumerable.Range(0, 2).Select(_ =>
        {
            var x = rng.NextSingle();
            var y = rng.NextSingle();
            var z = rng.NextSingle();
            var w = rng.NextSingle();
            return Vector4.Create(x, y, z, w);
        })];

        v2_1 = values[0].AsVector2();
        v2_2 = values[1].AsVector2();

        v3_1 = values[0].AsVector3();
        v3_2 = values[1].AsVector3();

        v4_1 = values[0];
        v4_2 = values[1];

        value1 = rng.NextSingle();
    }

#region Vector2
    [Benchmark]
    public float Vector2_Length()
    {
        return v2_1.Length();
    }

    [Benchmark]
    public float Vector2_LengthSquared()
    {
        return v2_1.LengthSquared();
    }

    [Benchmark]
    public Vector2 Vector2_Normalize()
    {
        return Vector2.Normalize(v2_1);
    }

    [Benchmark]
    public Vector2 Vector2_Add()
    {
        return Vector2.Add(v2_1, v2_2);
    }

    [Benchmark]
    public Vector2 Vector2_Subtract()
    {
        return Vector2.Subtract(v2_1, v2_2);
    }

    [Benchmark]
    public Vector2 Vector2_Multiply()
    {
        return Vector2.Multiply(v2_1, value1);
    }

    [Benchmark]
    public Vector2 Vector2_Divide()
    {
        return Vector2.Divide(v2_1, value1);
    }

    [Benchmark]
    public Vector2 Vector2_Negate()
    {
        return Vector2.Negate(v2_1);
    }

    [Benchmark]
    public float Vector2_Distance()
    {
        return Vector2.Distance(v2_1, v2_2);
    }

    [Benchmark]
    public float Vector2_DistanceSquared()
    {
        return Vector2.DistanceSquared(v2_1, v2_2);
    }

    [Benchmark]
    public float Vector2_Dot()
    {
        return Vector2.Dot(v2_1, v2_2);
    }

    [Benchmark]
    public Vector2 Vector2_Lerp()
    {
        return Vector2.Lerp(v2_1, v2_2, value1);
    }

    [Benchmark]
    public Vector2 Vector2_Max()
    {
        return Vector2.Max(v2_1, v2_2);
    }

    [Benchmark]
    public Vector2 Vector2_Min()
    {
        return Vector2.Min(v2_1, v2_2);
    }

    [Benchmark]
    public Vector2 Vector2_Transform()
    {
        return Vector2.Transform(v2_1, Quaternion.Identity);
    }
#endregion

#region Vector3
    [Benchmark]
    public float Vector3_Length()
    {
        return v3_1.Length();
    }

    [Benchmark]
    public float Vector3_LengthSquared()
    {
        return v3_1.LengthSquared();
    }

    [Benchmark]
    public Vector3 Vector3_Normalize()
    {
        return Vector3.Normalize(v3_1);
    }

    [Benchmark]
    public Vector3 Vector3_Add()
    {
        return Vector3.Add(v3_1, v3_2);
    }

    [Benchmark]
    public Vector3 Vector3_Subtract()
    {
        return Vector3.Subtract(v3_1, v3_2);
    }

    [Benchmark]
    public Vector3 Vector3_Multiply()
    {
        return Vector3.Multiply(v3_1, value1);
    }

    [Benchmark]
    public Vector3 Vector3_Divide()
    {
        return Vector3.Divide(v3_1, value1);
    }

    [Benchmark]
    public Vector3 Vector3_Negate()
    {
        return Vector3.Negate(v3_1);
    }

    [Benchmark]
    public float Vector3_Distance()
    {
        return Vector3.Distance(v3_1, v3_2);
    }

    [Benchmark]
    public float Vector3_DistanceSquared()
    {
        return Vector3.DistanceSquared(v3_1, v3_2);
    }

    [Benchmark]
    public float Vector3_Dot()
    {
        return Vector3.Dot(v3_1, v3_2);
    }

    [Benchmark]
    public Vector3 Vector3_Lerp()
    {
        return Vector3.Lerp(v3_1, v3_2, value1);
    }

    [Benchmark]
    public Vector3 Vector3_Max()
    {
        return Vector3.Max(v3_1, v3_2);
    }

    [Benchmark]
    public Vector3 Vector3_Min()
    {
        return Vector3.Min(v3_1, v3_2);
    }

    [Benchmark]
    public Vector3 Vector3_Transform()
    {
        return Vector3.Transform(v3_1, Quaternion.Identity);
    }
#endregion

#region Vector4
    [Benchmark]
    public float Vector4_Length()
    {
        return v4_1.Length();
    }

    [Benchmark]
    public float Vector4_LengthSquared()
    {
        return v4_1.LengthSquared();
    }

    [Benchmark]
    public Vector4 Vector4_Normalize()
    {
        return Vector4.Normalize(v4_1);
    }

    [Benchmark]
    public Vector4 Vector4_Add()
    {
        return Vector4.Add(v4_1, v4_2);
    }

    [Benchmark]
    public Vector4 Vector4_Subtract()
    {
        return Vector4.Subtract(v4_1, v4_2);
    }

    [Benchmark]
    public Vector4 Vector4_Multiply()
    {
        return Vector4.Multiply(v4_1, value1);
    }

    [Benchmark]
    public Vector4 Vector4_Divide()
    {
        return Vector4.Divide(v4_1, value1);
    }

    [Benchmark]
    public Vector4 Vector4_Negate()
    {
        return Vector4.Negate(v4_1);
    }

    [Benchmark]
    public float Vector4_Distance()
    {
        return Vector4.Distance(v4_1, v4_2);
    }

    [Benchmark]
    public float Vector4_DistanceSquared()
    {
        return Vector4.DistanceSquared(v4_1, v4_2);
    }

    [Benchmark]
    public float Vector4_Dot()
    {
        return Vector4.Dot(v4_1, v4_2);
    }

    [Benchmark]
    public Vector4 Vector4_Lerp()
    {
        return Vector4.Lerp(v4_1, v4_2, value1);
    }

    [Benchmark]
    public Vector4 Vector4_Max()
    {
        return Vector4.Max(v4_1, v4_2);
    }

    [Benchmark]
    public Vector4 Vector4_Min()
    {
        return Vector4.Min(v4_1, v4_2);
    }

    [Benchmark]
    public Vector4 Vector4_Transform()
    {
        return Vector4.Transform(v4_1, Quaternion.Identity);
    }
#endregion
}