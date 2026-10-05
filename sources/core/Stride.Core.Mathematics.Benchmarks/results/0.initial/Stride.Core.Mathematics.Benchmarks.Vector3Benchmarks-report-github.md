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
| IsNormalized       |  0.3798 ns |      73 B |
| Length             |  0.0878 ns |      43 B |
| LengthSquared      |  0.0102 ns |      39 B |
| Normalize          |  0.5694 ns |      91 B |
| MoveTo             |  0.9214 ns |     252 B |
| Add                |  0.1516 ns |      60 B |
| Subtract           |  0.2688 ns |      54 B |
| Multiply           |  0.2880 ns |      50 B |
| Modulate           |  1.0268 ns |      60 B |
| Divide             |  0.9208 ns |      50 B |
| Demodulate         |  0.9346 ns |      60 B |
| Negate             |  0.1552 ns |      57 B |
| Barycentric        |  1.5082 ns |     217 B |
| Clamp              |  1.0320 ns |     179 B |
| Distance           |  0.2884 ns |      67 B |
| DistanceSquared    |  0.1454 ns |      63 B |
| Dot                |  0.0576 ns |      51 B |
| Lerp               |  0.6277 ns |     107 B |
| SmoothStep         |  1.1158 ns |     191 B |
| Hermite            |  3.3917 ns |     411 B |
| CatmullRom         |  5.2767 ns |     596 B |
| Max                |  6.0360 ns |     122 B |
| Min                |  6.0721 ns |     122 B |
| Reflect            |  0.8091 ns |     126 B |
| Orthogonalize      | 35.6617 ns |     778 B |
| Orthonormalize     | 33.7280 ns |     723 B |
| Transform          |  0.7585 ns |     129 B |
| Operator_Add       |  0.2054 ns |      54 B |
| Operator_Subtract  |  0.3753 ns |      54 B |
| Operator_Negate    |  0.1821 ns |      60 B |
| Operator_Multiply  |  0.3417 ns |      54 B |
| Operator_Divide    |  0.9909 ns |      54 B |
| Operator_Equals    |  0.3641 ns |     117 B |
| Operator_NotEquals |  0.2213 ns |     127 B |
| EqualsStrict       |  0.0284 ns |      57 B |
