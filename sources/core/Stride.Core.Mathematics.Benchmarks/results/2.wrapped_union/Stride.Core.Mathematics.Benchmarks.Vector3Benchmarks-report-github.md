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
| IsNormalized       |  0.4097 ns |      62 B |
| Length             |  0.3062 ns |      32 B |
| LengthSquared      |  0.2924 ns |      28 B |
| Normalize          |  0.8844 ns |      66 B |
| MoveTo             |  1.3204 ns |     244 B |
| Add                |  0.3085 ns |      43 B |
| Subtract           |  0.1015 ns |      43 B |
| Multiply           |  0.0000 ns |      37 B |
| Modulate           |  0.4055 ns |      43 B |
| Divide             |  0.5319 ns |      37 B |
| Demodulate         |  0.0962 ns |      43 B |
| Negate             |  0.1265 ns |      35 B |
| Barycentric        |  0.5764 ns |      95 B |
| Clamp              |  0.9510 ns |     133 B |
| Distance           |  0.4917 ns |      45 B |
| DistanceSquared    |  0.3718 ns |      41 B |
| Dot                |  0.3559 ns |      43 B |
| Lerp               |  0.3677 ns |      71 B |
| SmoothStep         |  0.4528 ns |     129 B |
| Hermite            |  1.2186 ns |     220 B |
| CatmullRom         |  1.5376 ns |     291 B |
| Max                |  0.2590 ns |      80 B |
| Min                |  0.3039 ns |      80 B |
| Reflect            |  0.7617 ns |      74 B |
| Orthogonalize      | 28.3158 ns |     629 B |
| Orthonormalize     | 44.5802 ns |     649 B |
| Transform          |  2.1629 ns |     243 B |
| Operator_Add       |  0.0664 ns |      43 B |
| Operator_Subtract  |  0.1257 ns |      43 B |
| Operator_Negate    |  0.0733 ns |      35 B |
| Operator_Multiply  |  0.3746 ns |      43 B |
| Operator_Divide    |  0.0000 ns |      43 B |
| Operator_Equals    |  0.2160 ns |      75 B |
| Operator_NotEquals |  0.0814 ns |      75 B |
| EqualsStrict       |  0.0490 ns |      59 B |
