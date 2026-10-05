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
| IsNormalized       |  0.5677 ns |      49 B |
| Length             |  0.4244 ns |      19 B |
| LengthSquared      |  0.1917 ns |      15 B |
| Normalize          |  0.7256 ns |      42 B |
| Pow                | 28.7762 ns |     223 B |
| Moveto             |  0.8582 ns |      83 B |
| Add                |  0.0536 ns |      18 B |
| Subtract           |  0.0000 ns |      18 B |
| Multiply           |  0.0751 ns |      23 B |
| Modulate           |  0.0706 ns |      18 B |
| Divide             |  0.0654 ns |      23 B |
| Demodulate         |  0.0606 ns |      18 B |
| Negate             |  0.0857 ns |      21 B |
| Barycentric        |  0.2823 ns |      67 B |
| Clamp              |  0.6205 ns |     105 B |
| Distance           |  0.3080 ns |      21 B |
| DistanceSquared    |  0.1594 ns |      17 B |
| Dot                |  0.1127 ns |      13 B |
| Lerp               |  0.2320 ns |      50 B |
| SmoothStep         |  0.5377 ns |     108 B |
| Hermite            |  0.9866 ns |     185 B |
| CatmullRom         |  1.0915 ns |     210 B |
| Max                |  0.2406 ns |      59 B |
| Min                |  0.2505 ns |      59 B |
| Orthogonalize      | 15.4464 ns |     424 B |
| Orthonormalize     | 21.2759 ns |     434 B |
| Transform          |  1.7793 ns |     219 B |
| Operator_Add       |  0.0822 ns |      18 B |
| Operator_Subtract  |  0.0822 ns |      18 B |
| Operator_Negate    |  0.0444 ns |      21 B |
| Operator_Multiply  |  0.0779 ns |      18 B |
| Operator_Divide    |  0.0896 ns |      18 B |
| Operator_Equals    |  0.1745 ns |      43 B |
| Operator_NotEquals |  0.2337 ns |      43 B |
| EqualsStrict       |  0.0105 ns |      25 B |
