using BenchmarkDotNet.Attributes;

namespace Stride.Core.Mathematics.Benchmarks;

// dotnet run -c Release -f net10.0 --filter "*Vector3Benchmarks*"

[DisassemblyDiagnoser, HideColumns("Job", "Error", "StdDev", "Median", "RatioSD")]
public class Vector3Benchmarks
{
    private Vector3 v1;
    private Vector3 v2;
    private Vector3 v3;
    private Vector3 v4;
    private float value1;
    private float value2;
    private readonly Vector3[] buffer = new Vector3[4];

    [GlobalSetup]
    public void Setup()
    {
        Random rng = new(42);
        Vector3[] values = [.. Enumerable.Range(0, 4).Select(_ =>
        {
            var x = rng.NextSingle();
            var y = rng.NextSingle();
            var z = rng.NextSingle();
            return new Vector3(x, y, z);
        })];
        v1 = values[0];
        v2 = values[1];
        v3 = values[2];
        v4 = values[3];
        value1 = rng.NextSingle();
        value2 = rng.NextSingle();
    }

    [Benchmark]
    public bool IsNormalized()
    {
        return v1.IsNormalized;
    }

    [Benchmark]
    public float Length()
    {
        return v1.Length();
    }

    [Benchmark]
    public float LengthSquared()
    {
        return v1.LengthSquared();
    }

    [Benchmark]
    public Vector3 Normalize()
    {
        return Vector3.Normalize(v1);
    }

    [Benchmark]
    public Vector3 MoveTo()
    {
        return Vector3.MoveTo(v1, v2, value1);
    }

    [Benchmark]
    public Vector3 Add()
    {
        return Vector3.Add(v1, v2);
    }

    [Benchmark]
    public Vector3 Subtract()
    {
        return Vector3.Subtract(v1, v2);
    }

    [Benchmark]
    public Vector3 Multiply()
    {
        return Vector3.Multiply(v1, value1);
    }

    [Benchmark]
    public Vector3 Modulate()
    {
        return Vector3.Modulate(v1, v2);
    }

    [Benchmark]
    public Vector3 Divide()
    {
        return Vector3.Divide(v1, value1);
    }

    [Benchmark]
    public Vector3 Demodulate()
    {
        return Vector3.Demodulate(v1, v2);
    }

    [Benchmark]
    public Vector3 Negate()
    {
        return Vector3.Negate(v1);
    }

    [Benchmark]
    public Vector3 Barycentric()
    {
        return Vector3.Barycentric(v1, v2, v3, value1, value2);
    }

    [Benchmark]
    public Vector3 Clamp()
    {
        return Vector3.Clamp(v1, v2, v3);
    }

    [Benchmark]
    public float Distance()
    {
        return Vector3.Distance(v1, v2);
    }

    [Benchmark]
    public float DistanceSquared()
    {
        return Vector3.DistanceSquared(v1, v2);
    }

    [Benchmark]
    public float Dot()
    {
        return Vector3.Dot(v1, v2);
    }

    [Benchmark]
    public Vector3 Lerp()
    {
        return Vector3.Lerp(v1, v2, value1);
    }

    [Benchmark]
    public Vector3 SmoothStep()
    {
        return Vector3.SmoothStep(v1, v2, value1);
    }

    [Benchmark]
    public Vector3 Hermite()
    {
        return Vector3.Hermite(v1, v2, v3, v4, value1);
    }

    [Benchmark]
    public Vector3 CatmullRom()
    {
        return Vector3.CatmullRom(v1, v2, v3, v4, value1);
    }

    [Benchmark]
    public Vector3 Max()
    {
        return Vector3.Max(v1, v2);
    }

    [Benchmark]
    public Vector3 Min()
    {
        return Vector3.Min(v1, v2);
    }

    [Benchmark]
    public Vector3 Reflect()
    {
        return Vector3.Reflect(v1, v2);
    }

    [Benchmark]
    public void Orthogonalize()
    {
        Vector3.Orthogonalize(buffer, v1, v2, v3, v4);
    }

    [Benchmark]
    public void Orthonormalize()
    {
        Vector3.Orthonormalize(buffer, v1, v2, v3, v4);
    }

    [Benchmark]
    public Vector3 Transform()
    {
        return Vector3.Transform(v1, Quaternion.One);
    }

    [Benchmark]
    public Vector3 Operator_Add()
    {
        return v1 + v2;
    }

    [Benchmark]
    public Vector3 Operator_Subtract()
    {
        return v1 - v2;
    }

    [Benchmark]
    public Vector3 Operator_Negate()
    {
        return -v1;
    }

    [Benchmark]
    public Vector3 Operator_Multiply()
    {
        return v1 * v2;
    }

    [Benchmark]
    public Vector3 Operator_Divide()
    {
        return v1 / v2;
    }

    [Benchmark]
    public bool Operator_Equals()
    {
        return v1 == v2;
    }

    [Benchmark]
    public bool Operator_NotEquals()
    {
        return v1 != v2;
    }

    [Benchmark]
    public bool EqualsStrict()
    {
        return v1.EqualsStrict(v2);
    }
}
