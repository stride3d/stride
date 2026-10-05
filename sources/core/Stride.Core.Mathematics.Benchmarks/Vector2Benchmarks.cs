using BenchmarkDotNet.Attributes;

namespace Stride.Core.Mathematics.Benchmarks;

// dotnet run -c Release -f net10.0 --filter "*Vector2Benchmarks*"

[DisassemblyDiagnoser, HideColumns("Job", "Error", "StdDev", "Median", "RatioSD")]
public class Vector2Benchmarks
{
    private Vector2 v1;
    private Vector2 v2;
    private Vector2 v3;
    private Vector2 v4;
    private float value1;
    private float value2;
    private readonly Vector2[] buffer = new Vector2[4];

    [GlobalSetup]
    public void Setup()
    {
        Random rng = new(42);
        Vector2[] values = [.. Enumerable.Range(0, 4).Select(_ =>
        {
            var x = rng.NextSingle();
            var y = rng.NextSingle();
            return new Vector2(x, y);
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
    public Vector2 Normalize()
    {
        return Vector2.Normalize(v1);
    }

    [Benchmark]
    public Vector2 MoveTo()
    {
        return Vector2.MoveTo(v1, v2, value1);
    }

    [Benchmark]
    public Vector2 Add()
    {
        return Vector2.Add(v1, v2);
    }

    [Benchmark]
    public Vector2 Subtract()
    {
        return Vector2.Subtract(v1, v2);
    }

    [Benchmark]
    public Vector2 Multiply()
    {
        return Vector2.Multiply(v1, value1);
    }

    [Benchmark]
    public Vector2 Modulate()
    {
        return Vector2.Modulate(v1, v2);
    }

    [Benchmark]
    public Vector2 Divide()
    {
        return Vector2.Divide(v1, value1);
    }

    [Benchmark]
    public Vector2 Demodulate()
    {
        return Vector2.Demodulate(v1, v2);
    }

    [Benchmark]
    public Vector2 Negate()
    {
        return Vector2.Negate(v1);
    }

    [Benchmark]
    public Vector2 Barycentric()
    {
        return Vector2.Barycentric(v1, v2, v3, value1, value2);
    }

    [Benchmark]
    public Vector2 Clamp()
    {
        return Vector2.Clamp(v1, v2, v3);
    }

    [Benchmark]
    public float Distance()
    {
        return Vector2.Distance(v1, v2);
    }

    [Benchmark]
    public float DistanceSquared()
    {
        return Vector2.DistanceSquared(v1, v2);
    }

    [Benchmark]
    public float Dot()
    {
        return Vector2.Dot(v1, v2);
    }

    [Benchmark]
    public Vector2 Lerp()
    {
        return Vector2.Lerp(v1, v2, value1);
    }

    [Benchmark]
    public Vector2 SmoothStep()
    {
        return Vector2.SmoothStep(v1, v2, value1);
    }

    [Benchmark]
    public Vector2 Hermite()
    {
        return Vector2.Hermite(v1, v2, v3, v4, value1);
    }

    [Benchmark]
    public Vector2 CatmullRom()
    {
        return Vector2.CatmullRom(v1, v2, v3, v4, value1);
    }

    [Benchmark]
    public Vector2 Max()
    {
        return Vector2.Max(v1, v2);
    }

    [Benchmark]
    public Vector2 Min()
    {
        return Vector2.Min(v1, v2);
    }

    [Benchmark]
    public Vector2 Reflect()
    {
        return Vector2.Reflect(v1, v2);
    }

    [Benchmark]
    public void Orthogonalize()
    {
        Vector2.Orthogonalize(buffer, v1, v2, v3, v4);
    }

    [Benchmark]
    public void Orthonormalize()
    {
        Vector2.Orthonormalize(buffer, v1, v2, v3, v4);
    }

    [Benchmark]
    public Vector2 Transform()
    {
        return Vector2.Transform(v1, Quaternion.One);
    }

    [Benchmark]
    public Vector2 Operator_Add()
    {
        return v1 + v2;
    }

    [Benchmark]
    public Vector2 Operator_Subtract()
    {
        return v1 - v2;
    }

    [Benchmark]
    public Vector2 Operator_Negate()
    {
        return -v1;
    }

    [Benchmark]
    public Vector2 Operator_Multiply()
    {
        return v1 * v2;
    }

    [Benchmark]
    public Vector2 Operator_Divide()
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
