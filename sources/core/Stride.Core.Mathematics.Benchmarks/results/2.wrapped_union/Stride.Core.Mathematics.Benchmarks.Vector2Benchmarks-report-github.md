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
| IsNormalized       |  0.4348 ns |      55 B |
| Length             |  0.3295 ns |      25 B |
| LengthSquared      |  0.2427 ns |      21 B |
| Normalize          |  0.5662 ns |      50 B |
| MoveTo             |  0.8929 ns |      85 B |
| Add                |  0.0914 ns |      20 B |
| Subtract           |  0.0919 ns |      20 B |
| Multiply           |  0.1023 ns |      21 B |
| Modulate           |  0.0855 ns |      20 B |
| Divide             |  0.1158 ns |      21 B |
| Demodulate         |  0.0887 ns |      20 B |
| Negate             |  0.0905 ns |      19 B |
| Barycentric        |  0.2876 ns |      65 B |
| Clamp              |  0.6711 ns |     103 B |
| Distance           |  0.3067 ns |      31 B |
| DistanceSquared    |  0.2360 ns |      27 B |
| Dot                |  0.2025 ns |      29 B |
| Lerp               |  0.2198 ns |      48 B |
| SmoothStep         |  0.4483 ns |     106 B |
| Hermite            |  0.9617 ns |     183 B |
| CatmullRom         |  1.2093 ns |     254 B |
| Max                |  0.2684 ns |      57 B |
| Min                |  0.2476 ns |      57 B |
| Reflect            |  0.6040 ns |      51 B |
| Orthogonalize      | 15.7578 ns |     440 B |
| Orthonormalize     | 82.6859 ns |     443 B |
| Transform          |  1.5315 ns |     227 B |
| Operator_Add       |  0.1084 ns |      20 B |
| Operator_Subtract  |  0.0627 ns |      20 B |
| Operator_Negate    |  0.0885 ns |      19 B |
| Operator_Multiply  |  0.0821 ns |      20 B |
| Operator_Divide    |  0.0926 ns |      20 B |
| Operator_Equals    |  0.1985 ns |      61 B |
| Operator_NotEquals |  0.0599 ns |      61 B |
| EqualsStrict       |  0.1512 ns |      45 B |
