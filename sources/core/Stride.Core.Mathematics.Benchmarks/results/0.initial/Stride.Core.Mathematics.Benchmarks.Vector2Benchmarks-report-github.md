```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
Intel Core Ultra 7 165H 1.40GHz, 1 CPU, 22 logical and 16 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=5  LaunchCount=1  
WarmupCount=5  

```
| Method             | Mean       | Code Size |
|------------------- |-----------:|----------:|
| IsNormalized       |  0.2167 ns |      60 B |
| Length             |  0.0102 ns |      30 B |
| LengthSquared      |  0.0526 ns |      26 B |
| Normalize          |  0.3437 ns |      72 B |
| MoveTo             |  1.2684 ns |     142 B |
| Add                |  0.3774 ns |      44 B |
| Subtract           |  1.3480 ns |      42 B |
| Multiply           |  1.6727 ns |      39 B |
| Modulate           |  0.4073 ns |      44 B |
| Divide             |  0.6051 ns |      39 B |
| Demodulate         |  0.6737 ns |      44 B |
| Negate             |  0.3217 ns |      42 B |
| Barycentric        |  0.9272 ns |     134 B |
| Clamp              |  0.7491 ns |      94 B |
| Distance           |  0.0000 ns |      45 B |
| DistanceSquared    |  0.1800 ns |      41 B |
| Dot                |  0.3147 ns |      33 B |
| Lerp               |  0.4822 ns |      65 B |
| SmoothStep         |  1.1973 ns |     143 B |
| Hermite            |  2.5926 ns |     302 B |
| CatmullRom         |  4.3628 ns |     427 B |
| Max                |  6.4317 ns |      71 B |
| Min                |  5.1438 ns |      71 B |
| Reflect            |  0.5227 ns |      68 B |
| Orthogonalize      | 22.0467 ns |     612 B |
| Orthonormalize     | 24.9212 ns |     703 B |
| Transform          |  0.0000 ns |      66 B |
| Operator_Add       |  0.3468 ns |      44 B |
| Operator_Subtract  |  0.2955 ns |      44 B |
| Operator_Negate    |  0.3795 ns |      42 B |
| Operator_Multiply  |  0.2558 ns |      44 B |
| Operator_Divide    |  0.5999 ns |      44 B |
| Operator_Equals    |  0.0743 ns |      81 B |
| Operator_NotEquals |  0.2858 ns |      91 B |
| EqualsStrict       |  0.0000 ns |      43 B |
