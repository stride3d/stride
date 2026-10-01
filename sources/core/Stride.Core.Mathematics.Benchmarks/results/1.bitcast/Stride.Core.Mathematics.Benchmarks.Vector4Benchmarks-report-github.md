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
| IsNormalized       |  0.4012 ns |      46 B |
| Length             |  0.2879 ns |      16 B |
| LengthSquared      |  0.1667 ns |      12 B |
| Normalize          |  0.5231 ns |      42 B |
| Pow                | 29.1852 ns |     220 B |
| Moveto             |  1.0228 ns |     101 B |
| Add                |  0.4805 ns |      18 B |
| Subtract           |  0.6852 ns |      18 B |
| Multiply           |  0.0000 ns |      23 B |
| Modulate           |  0.1643 ns |      18 B |
| Divide             |  0.1773 ns |      23 B |
| Demodulate         |  0.2276 ns |      18 B |
| Negate             |  0.1168 ns |      21 B |
| Barycentric        |  0.2474 ns |      67 B |
| Clamp              |  0.6278 ns |     105 B |
| Distance           |  0.3537 ns |      21 B |
| DistanceSquared    |  0.2057 ns |      17 B |
| Dot                |  0.1893 ns |      13 B |
| Lerp               |  0.0000 ns |      50 B |
| SmoothStep         |  0.2480 ns |     108 B |
| Hermite            |  0.8898 ns |     185 B |
| CatmullRom         |  0.9028 ns |     210 B |
| Max                |  0.4013 ns |      59 B |
| Min                |  0.2586 ns |      59 B |
| Orthogonalize      | 14.4356 ns |     424 B |
| Orthonormalize     | 16.2372 ns |     434 B |
| Transform          |  1.1986 ns |     219 B |
| Operator_Add       |  0.5220 ns |      18 B |
| Operator_Subtract  |  0.2346 ns |      18 B |
| Operator_Negate    |  0.2068 ns |      21 B |
| Operator_Multiply  |  0.1996 ns |      18 B |
| Operator_Divide    |  0.2288 ns |      18 B |
| Operator_Equals    |  0.0728 ns |      43 B |
| Operator_NotEquals |  0.0678 ns |      43 B |
| EqualsStrict       |  0.0548 ns |      25 B |
