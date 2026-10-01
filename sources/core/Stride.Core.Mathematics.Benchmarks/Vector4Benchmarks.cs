using BenchmarkDotNet.Attributes;

namespace Stride.Core.Mathematics.Benchmarks;

// dotnet run -c Release -f net10.0 --filter "*Vector4Benchmarks*"

[DisassemblyDiagnoser, HideColumns("Job", "Error", "StdDev", "Median", "RatioSD")]
public class Vector4Benchmarks
{
    private Vector4 v1;
    private Vector4 v2;
    private Vector4 v3;
    private Vector4 v4;
    private float value1;
    private float value2;
    private readonly Vector4[] buffer = new Vector4[4];

    [GlobalSetup]
    public void Setup()
    {
        Random rng = new(42);
        Vector4[] values = [.. Enumerable.Range(0, 4).Select(_ =>
        {
            var x = rng.NextSingle();
            var y = rng.NextSingle();
            var z = rng.NextSingle();
            var w = rng.NextSingle();
            return new Vector4(x, y, z, w);
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
    public Vector4 Normalize()
    {
        return Vector4.Normalize(v1);
    }

    [Benchmark]
    public Vector4 Pow()
    {
        var result = v1;
        result.Pow(value1);
        return result;
    }

    [Benchmark]
    public Vector4 Moveto()
    {
        return Vector4.Moveto(v1, v2, value1);
    }

    [Benchmark]
    public Vector4 Add()
    {
        return Vector4.Add(v1, v2);
    }

    [Benchmark]
    public Vector4 Subtract()
    {
        return Vector4.Subtract(v1, v2);
    }

    [Benchmark]
    public Vector4 Multiply()
    {
        return Vector4.Multiply(v1, value1);
    }

    [Benchmark]
    public Vector4 Modulate()
    {
        return Vector4.Modulate(v1, v2);
    }

    [Benchmark]
    public Vector4 Divide()
    {
        return Vector4.Divide(v1, value1);
    }

    [Benchmark]
    public Vector4 Demodulate()
    {
        return Vector4.Demodulate(v1, v2);
    }

    [Benchmark]
    public Vector4 Negate()
    {
        return Vector4.Negate(v1);
    }

    [Benchmark]
    public Vector4 Barycentric()
    {
        return Vector4.Barycentric(v1, v2, v3, value1, value2);
    }

    [Benchmark]
    public Vector4 Clamp()
    {
        return Vector4.Clamp(v1, v2, v3);
    }

    [Benchmark]
    public float Distance()
    {
        return Vector4.Distance(v1, v2);
    }

    [Benchmark]
    public float DistanceSquared()
    {
        return Vector4.DistanceSquared(v1, v2);
    }

    [Benchmark]
    public float Dot()
    {
        return Vector4.Dot(v1, v2);
    }

    [Benchmark]
    public Vector4 Lerp()
    {
        return Vector4.Lerp(v1, v2, value1);
    }

    [Benchmark]
    public Vector4 SmoothStep()
    {
        return Vector4.SmoothStep(v1, v2, value1);
    }

    [Benchmark]
    public Vector4 Hermite()
    {
        return Vector4.Hermite(v1, v2, v3, v4, value1);
    }

    [Benchmark]
    public Vector4 CatmullRom()
    {
        return Vector4.CatmullRom(v1, v2, v3, v4, value1);
    }

    [Benchmark]
    public Vector4 Max()
    {
        return Vector4.Max(v1, v2);
    }

    [Benchmark]
    public Vector4 Min()
    {
        return Vector4.Min(v1, v2);
    }

    [Benchmark]
    public void Orthogonalize()
    {
        Vector4.Orthogonalize(buffer, v1, v2, v3, v4);
    }

    [Benchmark]
    public void Orthonormalize()
    {
        Vector4.Orthonormalize(buffer, v1, v2, v3, v4);
    }

    [Benchmark]
    public Vector4 Transform()
    {
        return Vector4.Transform(v1, Quaternion.One);
    }

    [Benchmark]
    public Vector4 Operator_Add()
    {
        return v1 + v2;
    }

    [Benchmark]
    public Vector4 Operator_Subtract()
    {
        return v1 - v2;
    }

    [Benchmark]
    public Vector4 Operator_Negate()
    {
        return -v1;
    }

    [Benchmark]
    public Vector4 Operator_Multiply()
    {
        return v1 * v2;
    }

    [Benchmark]
    public Vector4 Operator_Divide()
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
