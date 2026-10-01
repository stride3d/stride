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
| IsNormalized       |  0.4278 ns |      86 B |
| Length             |  0.1740 ns |      56 B |
| LengthSquared      |  0.3333 ns |      52 B |
| Normalize          |  1.2103 ns |     113 B |
| Pow                | 36.0931 ns |     198 B |
| Moveto             |  1.5866 ns |     293 B |
| Add                |  0.4533 ns |     109 B |
| Subtract           |  0.2604 ns |      69 B |
| Multiply           |  0.3405 ns |      64 B |
| Modulate           |  0.4796 ns |     109 B |
| Divide             |  1.0639 ns |      64 B |
| Demodulate         |  1.3541 ns |     109 B |
| Negate             |  0.2583 ns |      75 B |
| Barycentric        |  1.6903 ns |     305 B |
| Clamp              |  1.1191 ns |     322 B |
| Distance           |  0.4610 ns |     119 B |
| DistanceSquared    |  0.4370 ns |     115 B |
| Dot                |  0.3764 ns |      99 B |
| Lerp               |  0.7915 ns |     162 B |
| SmoothStep         |  1.1001 ns |     267 B |
| Hermite            |  3.5255 ns |     495 B |
| CatmullRom         |  5.3210 ns |     788 B |
| Max                |  0.4491 ns |     141 B |
| Min                |  0.5195 ns |     141 B |
| Orthogonalize      | 16.2703 ns |     752 B |
| Orthonormalize     | 24.1401 ns |     665 B |
| Transform          |  0.6119 ns |     157 B |
| Operator_Add       |  0.4156 ns |     109 B |
| Operator_Subtract  |  0.4524 ns |     109 B |
| Operator_Negate    |  0.2951 ns |      75 B |
| Operator_Multiply  |  0.4468 ns |     109 B |
| Operator_Divide    |  1.3414 ns |     109 B |
| Operator_Equals    |  0.4358 ns |     183 B |
| Operator_NotEquals |  0.4383 ns |     208 B |
| EqualsStrict       |  0.1779 ns |      71 B |
