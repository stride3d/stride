```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
Intel Core Ultra 7 165H 1.40GHz, 1 CPU, 22 logical and 16 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=5  LaunchCount=1  
WarmupCount=5  

```
| Method             | Mean        | Code Size |
|------------------- |------------:|----------:|
| IsNormalized       |   0.8105 ns |      85 B |
| Length             |   0.6858 ns |      55 B |
| LengthSquared      |   0.6466 ns |      51 B |
| Normalize          |  20.0252 ns |     162 B |
| MoveTo             |  13.4774 ns |     518 B |
| Add                |  10.8029 ns |     121 B |
| Subtract           |   0.9318 ns |      95 B |
| Multiply           |   6.8439 ns |      84 B |
| Modulate           |  10.3472 ns |     121 B |
| Divide             |   8.0072 ns |      84 B |
| Demodulate         |  11.7439 ns |     121 B |
| Negate             |   0.3482 ns |      61 B |
| Barycentric        |  62.4182 ns |     982 B |
| Clamp              |   1.6416 ns |     195 B |
| Distance           |   0.9696 ns |      89 B |
| DistanceSquared    |   0.9505 ns |      85 B |
| Dot                |   0.8987 ns |      87 B |
| Lerp               |   0.8235 ns |     115 B |
| SmoothStep         |   1.1573 ns |     173 B |
| Hermite            |  56.4898 ns |     990 B |
| CatmullRom         | 158.8414 ns |   2,644 B |
| Max                |   0.8359 ns |     124 B |
| Min                |   0.7153 ns |     124 B |
| Reflect            |   1.3289 ns |     118 B |
| Orthogonalize      | 149.3584 ns |     870 B |
| Orthonormalize     | 202.4585 ns |   1,325 B |
| Transform          |   3.0871 ns |     261 B |
| Operator_Add       |   0.7211 ns |      95 B |
| Operator_Subtract  |   1.0859 ns |      95 B |
| Operator_Negate    |   0.3636 ns |      61 B |
| Operator_Multiply  |   0.6544 ns |      95 B |
| Operator_Divide    |   0.7738 ns |      95 B |
| Operator_Equals    |  22.2072 ns |     298 B |
| Operator_NotEquals |  20.7988 ns |     298 B |
| EqualsStrict       |   0.5073 ns |     106 B |
