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
| IsNormalized       |   0.4606 ns |      65 B |
| Length             |   0.3198 ns |      35 B |
| LengthSquared      |   0.7340 ns |      31 B |
| Normalize          |  19.7256 ns |     121 B |
| MoveTo             |  25.6558 ns |     252 B |
| Add                |   0.1820 ns |      48 B |
| Subtract           |   0.2678 ns |      54 B |
| Multiply           |   0.0901 ns |      34 B |
| Modulate           |   0.2512 ns |      48 B |
| Divide             |   0.1671 ns |      34 B |
| Demodulate         |   0.2203 ns |      48 B |
| Negate             |   0.0676 ns |      32 B |
| Barycentric        |  18.0674 ns |     313 B |
| Clamp              |   1.0723 ns |     139 B |
| Distance           |   0.6423 ns |      59 B |
| DistanceSquared    |   0.5187 ns |      55 B |
| Dot                |   0.7921 ns |      57 B |
| Lerp               |   0.2452 ns |      76 B |
| SmoothStep         |   0.5589 ns |     134 B |
| Hermite            |   4.1808 ns |     331 B |
| CatmullRom         |  12.7934 ns |     956 B |
| Max                |   0.2585 ns |      85 B |
| Min                |   0.3677 ns |      85 B |
| Reflect            |   0.7683 ns |      79 B |
| Orthogonalize      |  53.5050 ns |     608 B |
| Orthonormalize     | 201.6561 ns |     894 B |
| Transform          |   2.4416 ns |     239 B |
| Operator_Add       |   0.2781 ns |      48 B |
| Operator_Subtract  |   0.2678 ns |      48 B |
| Operator_Negate    |   0.3274 ns |      32 B |
| Operator_Multiply  |   0.3584 ns |      48 B |
| Operator_Divide    |   0.2571 ns |      48 B |
| Operator_Equals    |  12.0232 ns |     146 B |
| Operator_NotEquals |  11.7499 ns |     146 B |
| EqualsStrict       |   0.3057 ns |      76 B |
