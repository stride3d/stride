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
| IsIdentity           |  0.2833 ns |      46 B |
| IsNormalized         |  0.4301 ns |      46 B |
| Angle                |  3.1300 ns |      84 B |
| Axis                 |  1.3321 ns |     137 B |
| YawPitchRoll         | 15.6226 ns |     503 B |
| Conjugate            |  0.2503 ns |      21 B |
| Invert               |  0.7085 ns |      44 B |
| Length               |  0.2858 ns |      16 B |
| LengthSquared        |  0.1865 ns |      12 B |
| Normalize            |  0.6689 ns |      41 B |
| Add                  |  0.0674 ns |      18 B |
| Subtract             |  0.2715 ns |      18 B |
| Multiply             |  0.5649 ns |     122 B |
| Negate               |  0.1481 ns |      21 B |
| Barycentric          | 60.6823 ns |     839 B |
| Dot                  |  0.1922 ns |      13 B |
| AngleBetween         |  5.5051 ns |      58 B |
| Exponential          |  5.5646 ns |     144 B |
| Lerp                 |  1.6391 ns |      97 B |
| Logarithm            | 10.9630 ns |     150 B |
| RotationX            |  4.5838 ns |      77 B |
| RotationY            |  4.5712 ns |      77 B |
| RotationZ            |  4.6695 ns |      77 B |
| RotationYawPitchRoll |  6.9057 ns |     794 B |
| Slerp                | 16.5988 ns |     372 B |
| RotateTowards        | 23.9880 ns |     472 B |
| Squad                | 79.8819 ns |     817 B |
| Operator_Add         |  0.2566 ns |      18 B |
| Operator_Subtract    |  0.2337 ns |      18 B |
| Operator_Negate      |  0.2097 ns |      21 B |
| Operator_Multiply    |  0.5849 ns |     116 B |
| Operator_Equals      |  0.1917 ns |      43 B |
| Operator_NotEquals   |  0.2158 ns |      43 B |
