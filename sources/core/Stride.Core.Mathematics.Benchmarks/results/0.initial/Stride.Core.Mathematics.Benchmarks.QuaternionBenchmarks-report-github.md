```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
Intel Core Ultra 7 165H 1.40GHz, 1 CPU, 22 logical and 16 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=5  LaunchCount=1  
WarmupCount=5  

```
| Method               | Mean       | Code Size |
|--------------------- |-----------:|----------:|
| IsIdentity           |  0.4625 ns |     148 B |
| IsNormalized         |  0.2492 ns |      86 B |
| Angle                |  3.0487 ns |      88 B |
| Axis                 |  1.0264 ns |     155 B |
| YawPitchRoll         | 15.0707 ns |     503 B |
| Conjugate            |  0.2902 ns |      70 B |
| Invert               |  0.5052 ns |     133 B |
| Length               |  0.1738 ns |      56 B |
| LengthSquared        |  0.0505 ns |      52 B |
| Normalize            |  1.0629 ns |     113 B |
| Add                  |  0.7579 ns |     109 B |
| Subtract             |  0.4060 ns |     109 B |
| Multiply             |  1.4663 ns |     268 B |
| Negate               |  0.2866 ns |      75 B |
| Barycentric          | 67.0597 ns |   1,621 B |
| Dot                  |  0.1861 ns |      59 B |
| AngleBetween         |  3.5700 ns |     104 B |
| Exponential          |  5.4311 ns |     186 B |
| Lerp                 |  2.6002 ns |     381 B |
| Logarithm            | 10.3535 ns |     175 B |
| RotationX            |  4.9221 ns |      86 B |
| RotationY            |  5.0792 ns |      86 B |
| RotationZ            |  5.1188 ns |      86 B |
| RotationYawPitchRoll | 17.4256 ns |     280 B |
| Slerp                | 20.9019 ns |     623 B |
| RotateTowards        | 23.5041 ns |     711 B |
| Squad                | 70.8532 ns |   1,632 B |
| Operator_Add         |  0.4283 ns |      69 B |
| Operator_Subtract    |  0.4355 ns |      69 B |
| Operator_Negate      |  0.2916 ns |      78 B |
| Operator_Multiply    |  1.4264 ns |     268 B |
| Operator_Equals      |  0.2536 ns |     140 B |
| Operator_NotEquals   |  0.1641 ns |     150 B |
