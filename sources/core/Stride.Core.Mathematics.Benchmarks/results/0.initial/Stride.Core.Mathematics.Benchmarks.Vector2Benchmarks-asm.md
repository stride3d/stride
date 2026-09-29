## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.IsNormalized()
       add       rcx,18
       vmovss    xmm0,dword ptr [rcx]
       vmulss    xmm0,xmm0,xmm0
       vmovss    xmm1,dword ptr [rcx+4]
       vmulss    xmm1,xmm1,xmm1
       vaddss    xmm0,xmm0,xmm1
       vsubss    xmm0,xmm0,dword ptr [7FFC5B133CA0]
       vandps    xmm0,xmm0,[7FFC5B133CB0]
       vmovss    xmm1,dword ptr [7FFC5B133CC0]
       vucomiss  xmm1,xmm0
       seta      al
       movzx     eax,al
       ret
; Total bytes of code 60
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Length()
       add       rcx,18
       vmovss    xmm0,dword ptr [rcx]
       vmulss    xmm0,xmm0,xmm0
       vmovss    xmm1,dword ptr [rcx+4]
       vmulss    xmm1,xmm1,xmm1
       vaddss    xmm0,xmm0,xmm1
       vsqrtss   xmm0,xmm0,xmm0
       ret
; Total bytes of code 30
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.LengthSquared()
       add       rcx,18
       vmovss    xmm0,dword ptr [rcx]
       vmulss    xmm0,xmm0,xmm0
       vmovss    xmm1,dword ptr [rcx+4]
       vmulss    xmm1,xmm1,xmm1
       vaddss    xmm0,xmm0,xmm1
       ret
; Total bytes of code 26
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Normalize()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmulss    xmm2,xmm0,xmm0
       vmulss    xmm3,xmm1,xmm1
       vaddss    xmm2,xmm2,xmm3
       vsqrtss   xmm2,xmm2,xmm2
       vucomiss  xmm2,dword ptr [7FFC5B143EF8]
       jbe       short M00_L00
       vmovss    xmm3,dword ptr [7FFC5B143EFC]
       vdivss    xmm2,xmm3,xmm2
       vmulss    xmm0,xmm0,xmm2
       vmulss    xmm1,xmm1,xmm2
M00_L00:
       vmovd     eax,xmm0
       vmovd     ecx,xmm1
       shl       rcx,20
       or        rax,rcx
       ret
; Total bytes of code 72
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.MoveTo()
       sub       rsp,18
       vmovaps   [rsp],xmm6
       lea       rax,[rcx+18]
       lea       rdx,[rcx+20]
       vmovss    xmm0,dword ptr [rcx+10]
       vmovss    xmm1,dword ptr [rdx]
       vmovss    xmm2,dword ptr [rax]
       vsubss    xmm1,xmm1,xmm2
       vmovss    xmm3,dword ptr [rdx+4]
       vmovss    xmm4,dword ptr [rax+4]
       vsubss    xmm3,xmm3,xmm4
       vmulss    xmm5,xmm1,xmm1
       vmulss    xmm6,xmm3,xmm3
       vaddss    xmm5,xmm5,xmm6
       vsqrtss   xmm5,xmm5,xmm5
       vucomiss  xmm0,xmm5
       jae       short M00_L02
       vxorps    xmm6,xmm6,xmm6
       vucomiss  xmm5,xmm6
       jp        short M00_L00
       je        short M00_L02
M00_L00:
       vdivss    xmm1,xmm1,xmm5
       vmulss    xmm1,xmm1,xmm0
       vaddss    xmm1,xmm1,xmm2
       vdivss    xmm2,xmm3,xmm5
       vmulss    xmm0,xmm2,xmm0
       vaddss    xmm0,xmm0,xmm4
M00_L01:
       vmovd     eax,xmm1
       vmovd     ecx,xmm0
       shl       rcx,20
       or        rax,rcx
       vmovaps   xmm6,[rsp]
       add       rsp,18
       ret
M00_L02:
       vmovss    xmm1,dword ptr [rdx]
       vmovss    xmm0,dword ptr [rdx+4]
       jmp       short M00_L01
; Total bytes of code 142
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Add()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vaddss    xmm0,xmm0,xmm2
       vaddss    xmm1,xmm1,xmm3
       vmovd     eax,xmm0
       vmovd     ecx,xmm1
       shl       rcx,20
       or        rax,rcx
       ret
; Total bytes of code 44
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Subtract()
       lea       rax,[rcx+18]
       add       rcx,20
       vmovss    xmm0,dword ptr [rax]
       vsubss    xmm0,xmm0,dword ptr [rcx]
       vmovss    xmm1,dword ptr [rax+4]
       vsubss    xmm1,xmm1,dword ptr [rcx+4]
       vmovd     eax,xmm0
       vmovd     ecx,xmm1
       shl       rcx,20
       or        rax,rcx
       ret
; Total bytes of code 42
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Multiply()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+10]
       vmulss    xmm0,xmm0,xmm2
       vmulss    xmm1,xmm1,xmm2
       vmovd     eax,xmm0
       vmovd     ecx,xmm1
       shl       rcx,20
       or        rax,rcx
       ret
; Total bytes of code 39
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Modulate()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmulss    xmm0,xmm0,xmm2
       vmulss    xmm1,xmm1,xmm3
       vmovd     eax,xmm0
       vmovd     ecx,xmm1
       shl       rcx,20
       or        rax,rcx
       ret
; Total bytes of code 44
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Divide()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+10]
       vdivss    xmm0,xmm0,xmm2
       vdivss    xmm1,xmm1,xmm2
       vmovd     eax,xmm0
       vmovd     ecx,xmm1
       shl       rcx,20
       or        rax,rcx
       ret
; Total bytes of code 39
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Demodulate()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vdivss    xmm0,xmm0,xmm2
       vdivss    xmm1,xmm1,xmm3
       vmovd     eax,xmm0
       vmovd     ecx,xmm1
       shl       rcx,20
       or        rax,rcx
       ret
; Total bytes of code 44
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Negate()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vxorps    xmm0,xmm0,[7FFC5B163DE0]
       vxorps    xmm1,xmm1,[7FFC5B163DE0]
       vmovd     eax,xmm0
       vmovd     ecx,xmm1
       shl       rcx,20
       or        rax,rcx
       ret
; Total bytes of code 42
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Barycentric()
       sub       rsp,28
       vmovaps   [rsp+10],xmm6
       vmovaps   [rsp],xmm7
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    xmm6,dword ptr [rcx+10]
       vmovss    xmm7,dword ptr [rcx+14]
       vsubss    xmm2,xmm2,xmm0
       vmulss    xmm2,xmm2,xmm6
       vaddss    xmm2,xmm0,xmm2
       vsubss    xmm0,xmm4,xmm0
       vmulss    xmm0,xmm0,xmm7
       vaddss    xmm0,xmm2,xmm0
       vsubss    xmm2,xmm3,xmm1
       vmulss    xmm2,xmm2,xmm6
       vaddss    xmm2,xmm1,xmm2
       vsubss    xmm1,xmm5,xmm1
       vmulss    xmm1,xmm1,xmm7
       vaddss    xmm1,xmm2,xmm1
       vmovd     eax,xmm0
       vmovd     ecx,xmm1
       shl       rcx,20
       or        rax,rcx
       vmovaps   xmm6,[rsp+10]
       vmovaps   xmm7,[rsp]
       add       rsp,28
       ret
; Total bytes of code 134
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Clamp()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vucomiss  xmm0,xmm4
       jbe       short M00_L04
M00_L00:
       vucomiss  xmm2,xmm4
       ja        short M00_L05
M00_L01:
       vucomiss  xmm1,xmm5
       ja        short M00_L06
M00_L02:
       vucomiss  xmm3,xmm1
       jbe       short M00_L07
M00_L03:
       vmovd     eax,xmm4
       vmovd     ecx,xmm3
       shl       rcx,20
       or        rax,rcx
       ret
M00_L04:
       vmovaps   xmm4,xmm0
       jmp       short M00_L00
M00_L05:
       vmovaps   xmm4,xmm2
       jmp       short M00_L01
M00_L06:
       vmovaps   xmm1,xmm5
       jmp       short M00_L02
M00_L07:
       vmovaps   xmm3,xmm1
       jmp       short M00_L03
; Total bytes of code 94
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Distance()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vsubss    xmm1,xmm1,xmm3
       vsubss    xmm0,xmm0,xmm2
       vmulss    xmm0,xmm0,xmm0
       vmulss    xmm1,xmm1,xmm1
       vaddss    xmm0,xmm0,xmm1
       vsqrtss   xmm0,xmm0,xmm0
       ret
; Total bytes of code 45
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.DistanceSquared()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vsubss    xmm1,xmm1,xmm3
       vsubss    xmm0,xmm0,xmm2
       vmulss    xmm0,xmm0,xmm0
       vmulss    xmm1,xmm1,xmm1
       vaddss    xmm0,xmm0,xmm1
       ret
; Total bytes of code 41
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Dot()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmulss    xmm0,xmm0,xmm2
       vmulss    xmm1,xmm1,xmm3
       vaddss    xmm0,xmm0,xmm1
       ret
; Total bytes of code 33
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Lerp()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+10]
       vsubss    xmm2,xmm2,xmm0
       vmulss    xmm2,xmm2,xmm4
       vaddss    xmm0,xmm2,xmm0
       vsubss    xmm2,xmm3,xmm1
       vmulss    xmm2,xmm2,xmm4
       vaddss    xmm1,xmm2,xmm1
       vmovd     eax,xmm0
       vmovd     ecx,xmm1
       shl       rcx,20
       or        rax,rcx
       ret
; Total bytes of code 65
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.SmoothStep()
       sub       rsp,18
       vmovaps   [rsp],xmm6
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+10]
       vucomiss  xmm4,dword ptr [7FFC5B174000]
       ja        short M00_L02
       vxorps    xmm5,xmm5,xmm5
       vucomiss  xmm5,xmm4
       ja        short M00_L01
M00_L00:
       vmulss    xmm5,xmm4,xmm4
       vaddss    xmm4,xmm4,xmm4
       vmovss    xmm6,dword ptr [7FFC5B174004]
       vsubss    xmm4,xmm6,xmm4
       vmulss    xmm4,xmm5,xmm4
       vsubss    xmm2,xmm2,xmm0
       vmulss    xmm2,xmm2,xmm4
       vaddss    xmm0,xmm2,xmm0
       vsubss    xmm2,xmm3,xmm1
       vmulss    xmm2,xmm2,xmm4
       vaddss    xmm1,xmm2,xmm1
       vmovd     eax,xmm0
       vmovd     ecx,xmm1
       shl       rcx,20
       or        rax,rcx
       vmovaps   xmm6,[rsp]
       add       rsp,18
       ret
M00_L01:
       vxorps    xmm4,xmm4,xmm4
       jmp       short M00_L00
M00_L02:
       vmovss    xmm4,dword ptr [7FFC5B174000]
       jmp       short M00_L00
; Total bytes of code 143
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Hermite()
       sub       rsp,88
       vmovaps   [rsp+70],xmm6
       vmovaps   [rsp+60],xmm7
       vmovaps   [rsp+50],xmm8
       vmovaps   [rsp+40],xmm9
       vmovaps   [rsp+30],xmm10
       vmovaps   [rsp+20],xmm11
       vmovaps   [rsp+10],xmm12
       vmovaps   [rsp],xmm13
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    xmm6,dword ptr [rcx+30]
       vmovss    xmm7,dword ptr [rcx+34]
       vmovss    xmm8,dword ptr [rcx+10]
       vmulss    xmm9,xmm8,xmm8
       vmulss    xmm10,xmm8,xmm9
       vaddss    xmm11,xmm10,xmm10
       vmulss    xmm12,xmm9,dword ptr [7FFC5B1441B0]
       vsubss    xmm11,xmm11,xmm12
       vaddss    xmm11,xmm11,dword ptr [7FFC5B1441B4]
       vmulss    xmm13,xmm10,dword ptr [7FFC5B1441B8]
       vaddss    xmm12,xmm13,xmm12
       vaddss    xmm13,xmm9,xmm9
       vsubss    xmm13,xmm10,xmm13
       vaddss    xmm8,xmm13,xmm8
       vsubss    xmm9,xmm10,xmm9
       vmulss    xmm0,xmm0,xmm11
       vmulss    xmm4,xmm4,xmm12
       vaddss    xmm0,xmm0,xmm4
       vmulss    xmm2,xmm2,xmm8
       vaddss    xmm0,xmm0,xmm2
       vmulss    xmm2,xmm6,xmm9
       vaddss    xmm0,xmm0,xmm2
       vmulss    xmm1,xmm1,xmm11
       vmulss    xmm2,xmm5,xmm12
       vaddss    xmm1,xmm1,xmm2
       vmulss    xmm2,xmm3,xmm8
       vaddss    xmm1,xmm1,xmm2
       vmulss    xmm2,xmm7,xmm9
       vaddss    xmm1,xmm1,xmm2
       vmovd     eax,xmm0
       vmovd     ecx,xmm1
       shl       rcx,20
       or        rax,rcx
       vmovaps   xmm6,[rsp+70]
       vmovaps   xmm7,[rsp+60]
       vmovaps   xmm8,[rsp+50]
       vmovaps   xmm9,[rsp+40]
       vmovaps   xmm10,[rsp+30]
       vmovaps   xmm11,[rsp+20]
       vmovaps   xmm12,[rsp+10]
       vmovaps   xmm13,[rsp]
       add       rsp,88
       ret
; Total bytes of code 302
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.CatmullRom()
       sub       rsp,0A8
       vmovaps   [rsp+90],xmm6
       vmovaps   [rsp+80],xmm7
       vmovaps   [rsp+70],xmm8
       vmovaps   [rsp+60],xmm9
       vmovaps   [rsp+50],xmm10
       vmovaps   [rsp+40],xmm11
       vmovaps   [rsp+30],xmm12
       vmovaps   [rsp+20],xmm13
       vmovaps   [rsp+10],xmm14
       vmovaps   [rsp],xmm15
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    xmm6,dword ptr [rcx+30]
       vmovss    xmm7,dword ptr [rcx+34]
       vmovss    xmm8,dword ptr [rcx+10]
       vmulss    xmm9,xmm8,xmm8
       vmulss    xmm10,xmm8,xmm9
       vaddss    xmm11,xmm2,xmm2
       vsubss    xmm12,xmm4,xmm0
       vmulss    xmm12,xmm12,xmm8
       vaddss    xmm11,xmm11,xmm12
       vaddss    xmm12,xmm0,xmm0
       vmovss    xmm13,dword ptr [7FFC5B1443B0]
       vmulss    xmm14,xmm2,xmm13
       vsubss    xmm12,xmm12,xmm14
       vmovss    xmm14,dword ptr [7FFC5B1443B4]
       vmulss    xmm15,xmm4,xmm14
       vaddss    xmm12,xmm12,xmm15
       vsubss    xmm12,xmm12,xmm6
       vmulss    xmm12,xmm12,xmm9
       vaddss    xmm11,xmm11,xmm12
       vmovss    xmm12,dword ptr [7FFC5B1443B8]
       vmulss    xmm2,xmm2,xmm12
       vsubss    xmm0,xmm2,xmm0
       vmulss    xmm2,xmm4,xmm12
       vsubss    xmm0,xmm0,xmm2
       vaddss    xmm0,xmm0,xmm6
       vmulss    xmm0,xmm0,xmm10
       vaddss    xmm0,xmm11,xmm0
       vmovss    xmm2,dword ptr [7FFC5B1443BC]
       vmulss    xmm0,xmm0,xmm2
       vaddss    xmm4,xmm3,xmm3
       vsubss    xmm6,xmm5,xmm1
       vmulss    xmm6,xmm6,xmm8
       vaddss    xmm4,xmm4,xmm6
       vaddss    xmm6,xmm1,xmm1
       vmulss    xmm8,xmm3,xmm13
       vsubss    xmm6,xmm6,xmm8
       vmulss    xmm8,xmm5,xmm14
       vaddss    xmm6,xmm6,xmm8
       vsubss    xmm6,xmm6,xmm7
       vmulss    xmm6,xmm6,xmm9
       vaddss    xmm4,xmm4,xmm6
       vmulss    xmm3,xmm3,xmm12
       vsubss    xmm1,xmm3,xmm1
       vmulss    xmm3,xmm5,xmm12
       vsubss    xmm1,xmm1,xmm3
       vaddss    xmm1,xmm1,xmm7
       vmulss    xmm1,xmm1,xmm10
       vaddss    xmm1,xmm4,xmm1
       vmulss    xmm1,xmm1,xmm2
       vmovd     eax,xmm0
       vmovd     ecx,xmm1
       shl       rcx,20
       or        rax,rcx
       vmovaps   xmm6,[rsp+90]
       vmovaps   xmm7,[rsp+80]
       vmovaps   xmm8,[rsp+70]
       vmovaps   xmm9,[rsp+60]
       vmovaps   xmm10,[rsp+50]
       vmovaps   xmm11,[rsp+40]
       vmovaps   xmm12,[rsp+30]
       vmovaps   xmm13,[rsp+20]
       vmovaps   xmm14,[rsp+10]
       vmovaps   xmm15,[rsp]
       add       rsp,0A8
       ret
; Total bytes of code 427
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Max()
       push      rax
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       xor       eax,eax
       mov       [rsp],rax
       vucomiss  xmm0,xmm2
       jbe       short M00_L02
M00_L00:
       vmovss    dword ptr [rsp],xmm0
       vucomiss  xmm1,xmm3
       ja        short M00_L03
M00_L01:
       vmovss    dword ptr [rsp+4],xmm3
       mov       rax,[rsp]
       add       rsp,8
       ret
M00_L02:
       vmovaps   xmm0,xmm2
       jmp       short M00_L00
M00_L03:
       vmovaps   xmm3,xmm1
       jmp       short M00_L01
; Total bytes of code 71
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Min()
       push      rax
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       xor       eax,eax
       mov       [rsp],rax
       vucomiss  xmm2,xmm0
       ja        short M00_L02
M00_L00:
       vmovss    dword ptr [rsp],xmm2
       vucomiss  xmm3,xmm1
       jbe       short M00_L03
M00_L01:
       vmovss    dword ptr [rsp+4],xmm1
       mov       rax,[rsp]
       add       rsp,8
       ret
M00_L02:
       vmovaps   xmm2,xmm0
       jmp       short M00_L00
M00_L03:
       vmovaps   xmm1,xmm3
       jmp       short M00_L01
; Total bytes of code 71
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Reflect()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmulss    xmm4,xmm0,xmm2
       vmulss    xmm5,xmm1,xmm3
       vaddss    xmm4,xmm4,xmm5
       vaddss    xmm4,xmm4,xmm4
       vmulss    xmm2,xmm4,xmm2
       vsubss    xmm0,xmm0,xmm2
       vmulss    xmm2,xmm4,xmm3
       vsubss    xmm1,xmm1,xmm2
       vmovd     eax,xmm0
       vmovd     ecx,xmm1
       shl       rcx,20
       or        rax,rcx
       ret
; Total bytes of code 68
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Orthogonalize()
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rsi,[rbx+8]
       mov       rcx,offset MT_Stride.Core.Mathematics.Vector2[]
       mov       edx,4
       call      CORINFO_HELP_NEWARR_1_PTR
       vmovss    xmm0,dword ptr [rbx+18]
       vmovss    xmm1,dword ptr [rbx+1C]
       lea       rcx,[rax+10]
       vmovss    dword ptr [rcx],xmm0
       vmovss    dword ptr [rcx+4],xmm1
       vmovss    xmm0,dword ptr [rbx+20]
       vmovss    xmm1,dword ptr [rbx+24]
       lea       rcx,[rax+18]
       vmovss    dword ptr [rcx],xmm0
       vmovss    dword ptr [rcx+4],xmm1
       vmovss    xmm0,dword ptr [rbx+28]
       vmovss    xmm1,dword ptr [rbx+2C]
       lea       rcx,[rax+20]
       vmovss    dword ptr [rcx],xmm0
       vmovss    dword ptr [rcx+4],xmm1
       vmovss    xmm0,dword ptr [rbx+30]
       vmovss    xmm1,dword ptr [rbx+34]
       lea       rcx,[rax+28]
       vmovss    dword ptr [rcx],xmm0
       vmovss    dword ptr [rcx+4],xmm1
       mov       rcx,rsi
       mov       rdx,rax
       add       rsp,28
       pop       rbx
       pop       rsi
       jmp       qword ptr [7FFC5B575020]; Stride.Core.Mathematics.Vector2.Orthogonalize(Stride.Core.Mathematics.Vector2[], Stride.Core.Mathematics.Vector2[])
; Total bytes of code 143
```
```assembly
; Stride.Core.Mathematics.Vector2.Orthogonalize(Stride.Core.Mathematics.Vector2[], Stride.Core.Mathematics.Vector2[])
       push      rsi
       push      rbx
       sub       rsp,58
       vmovaps   [rsp+40],xmm6
       vmovaps   [rsp+30],xmm7
       vmovaps   [rsp+20],xmm8
       test      rdx,rdx
       je        near ptr M01_L04
       test      rcx,rcx
       je        near ptr M01_L05
       mov       eax,[rcx+8]
       mov       r8d,[rdx+8]
       cmp       eax,r8d
       jl        near ptr M01_L06
       xor       r10d,r10d
       cmp       r8d,r10d
       jg        near ptr M01_L03
M01_L00:
       vmovaps   xmm6,[rsp+40]
       vmovaps   xmm7,[rsp+30]
       vmovaps   xmm8,[rsp+20]
       add       rsp,58
       pop       rbx
       pop       rsi
       ret
       nop
M01_L01:
       cmp       r9d,eax
       jae       near ptr M01_L07
       lea       r11,[rcx+r9*8+10]
       vmovss    xmm2,dword ptr [r11]
       vmovaps   xmm3,xmm2
       vmovss    xmm4,dword ptr [r11+4]
       vmovaps   xmm5,xmm4
       vmulss    xmm3,xmm3,xmm0
       vmulss    xmm5,xmm5,xmm1
       vaddss    xmm3,xmm3,xmm5
       vmovaps   xmm5,xmm2
       vmovaps   xmm6,xmm4
       vmovaps   xmm7,xmm2
       vmovaps   xmm8,xmm4
       vmulss    xmm5,xmm5,xmm7
       vmulss    xmm6,xmm6,xmm8
       vaddss    xmm5,xmm5,xmm6
       vdivss    xmm3,xmm3,xmm5
       vmulss    xmm2,xmm2,xmm3
       vmulss    xmm3,xmm4,xmm3
       vsubss    xmm0,xmm0,xmm2
       vsubss    xmm1,xmm1,xmm3
       inc       r9d
M01_L02:
       cmp       r9d,r10d
       jl        short M01_L01
       cmp       r10d,eax
       jae       near ptr M01_L07
       lea       r9,[rcx+r10*8+10]
       vmovss    dword ptr [r9],xmm0
       vmovss    dword ptr [r9+4],xmm1
       inc       r10d
       cmp       r8d,r10d
       jle       near ptr M01_L00
M01_L03:
       lea       r9,[rdx+r10*8+10]
       vmovss    xmm0,dword ptr [r9]
       vmovss    xmm1,dword ptr [r9+4]
       xor       r9d,r9d
       jmp       short M01_L02
M01_L04:
       mov       rcx,offset MT_System.ArgumentNullException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       ecx,603
       mov       rdx,7FFC5B3A36E0
       call      qword ptr [7FFC5B2E7798]
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FFC5B4254B8]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
M01_L05:
       mov       rcx,offset MT_System.ArgumentNullException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       ecx,611
       mov       rdx,7FFC5B3A36E0
       call      qword ptr [7FFC5B2E7798]
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FFC5B4254B8]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
M01_L06:
       mov       rcx,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       ecx,611
       mov       rdx,7FFC5B3A36E0
       call      qword ptr [7FFC5B2E7798]
       mov       rsi,rax
       mov       ecx,629
       mov       rdx,7FFC5B3A36E0
       call      qword ptr [7FFC5B2E7798]
       mov       r8,rax
       mov       rdx,rsi
       mov       rcx,rbx
       call      qword ptr [7FFC5B4DE490]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
M01_L07:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 469
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Orthonormalize()
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rsi,[rbx+8]
       mov       rcx,offset MT_Stride.Core.Mathematics.Vector2[]
       mov       edx,4
       call      CORINFO_HELP_NEWARR_1_PTR
       vmovss    xmm0,dword ptr [rbx+18]
       vmovss    xmm1,dword ptr [rbx+1C]
       lea       rcx,[rax+10]
       vmovss    dword ptr [rcx],xmm0
       vmovss    dword ptr [rcx+4],xmm1
       vmovss    xmm0,dword ptr [rbx+20]
       vmovss    xmm1,dword ptr [rbx+24]
       lea       rcx,[rax+18]
       vmovss    dword ptr [rcx],xmm0
       vmovss    dword ptr [rcx+4],xmm1
       vmovss    xmm0,dword ptr [rbx+28]
       vmovss    xmm1,dword ptr [rbx+2C]
       lea       rcx,[rax+20]
       vmovss    dword ptr [rcx],xmm0
       vmovss    dword ptr [rcx+4],xmm1
       vmovss    xmm0,dword ptr [rbx+30]
       vmovss    xmm1,dword ptr [rbx+34]
       lea       rcx,[rax+28]
       vmovss    dword ptr [rcx],xmm0
       vmovss    dword ptr [rcx+4],xmm1
       mov       rcx,rsi
       mov       rdx,rax
       add       rsp,28
       pop       rbx
       pop       rsi
       jmp       qword ptr [7FFC5B595110]; Stride.Core.Mathematics.Vector2.Orthonormalize(Stride.Core.Mathematics.Vector2[], Stride.Core.Mathematics.Vector2[])
; Total bytes of code 143
```
```assembly
; Stride.Core.Mathematics.Vector2.Orthonormalize(Stride.Core.Mathematics.Vector2[], Stride.Core.Mathematics.Vector2[])
       push      rsi
       push      rbx
       sub       rsp,38
       vmovaps   [rsp+20],xmm6
       test      rdx,rdx
       je        near ptr M01_L07
       test      rcx,rcx
       je        near ptr M01_L08
       mov       eax,[rcx+8]
       mov       r8d,[rdx+8]
       cmp       eax,r8d
       jl        near ptr M01_L09
       xor       r10d,r10d
       cmp       r8d,r10d
       jg        near ptr M01_L04
M01_L00:
       vmovaps   xmm6,[rsp+20]
       add       rsp,38
       pop       rbx
       pop       rsi
       ret
M01_L01:
       cmp       r11d,eax
       jae       near ptr M01_L10
       mov       esi,r11d
       lea       rbx,[rcx+rsi*8+10]
       mov       rsi,rbx
       vmovss    xmm3,dword ptr [rsi]
       vmovss    xmm4,dword ptr [rsi+4]
       vmovaps   xmm5,xmm4
       vmulss    xmm2,xmm3,xmm0
       vmulss    xmm3,xmm5,xmm1
       vaddss    xmm3,xmm2,xmm3
       vmovss    xmm2,dword ptr [rbx]
       vmulss    xmm5,xmm2,xmm3
       vmulss    xmm6,xmm4,xmm3
       vsubss    xmm0,xmm0,xmm5
       vsubss    xmm1,xmm1,xmm6
       inc       r11d
       cmp       r11d,r10d
       jl        short M01_L01
       jmp       near ptr M01_L06
M01_L02:
       vmovss    xmm3,dword ptr [7FFC5B164990]
       vdivss    xmm2,xmm3,xmm2
       vmulss    xmm0,xmm0,xmm2
       vmulss    xmm1,xmm1,xmm2
M01_L03:
       cmp       r10d,eax
       jae       near ptr M01_L10
       lea       r9,[rcx+r9*8+10]
       vmovss    dword ptr [r9],xmm0
       vmovss    dword ptr [r9+4],xmm1
       inc       r10d
       cmp       r8d,r10d
       jle       near ptr M01_L00
M01_L04:
       mov       r9d,r10d
       lea       r11,[rdx+r9*8+10]
       vmovss    xmm0,dword ptr [r11]
       vmovss    xmm1,dword ptr [r11+4]
       xor       r11d,r11d
       test      r10d,r10d
       jle       short M01_L06
       cmp       eax,r10d
       jl        near ptr M01_L01
       lea       r11,[rcx+10]
       mov       ebx,r10d
       nop       dword ptr [rax]
M01_L05:
       mov       rsi,r11
       vmovss    xmm2,dword ptr [rsi]
       vmovaps   xmm3,xmm2
       vmovss    xmm4,dword ptr [rsi+4]
       vmovaps   xmm5,xmm4
       vmulss    xmm3,xmm3,xmm0
       vmulss    xmm5,xmm5,xmm1
       vaddss    xmm3,xmm3,xmm5
       vmulss    xmm5,xmm2,xmm3
       vmulss    xmm6,xmm4,xmm3
       vsubss    xmm0,xmm0,xmm5
       vsubss    xmm1,xmm1,xmm6
       add       r11,8
       dec       ebx
       jne       short M01_L05
M01_L06:
       vmulss    xmm2,xmm0,xmm0
       vmulss    xmm3,xmm1,xmm1
       vaddss    xmm2,xmm2,xmm3
       vsqrtss   xmm2,xmm2,xmm2
       vucomiss  xmm2,dword ptr [7FFC5B164994]
       jbe       near ptr M01_L03
       jmp       near ptr M01_L02
M01_L07:
       mov       rcx,offset MT_System.ArgumentNullException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       ecx,603
       mov       rdx,7FFC5B3C36E0
       call      qword ptr [7FFC5B307798]
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FFC5B4454B8]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
M01_L08:
       mov       rcx,offset MT_System.ArgumentNullException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       ecx,611
       mov       rdx,7FFC5B3C36E0
       call      qword ptr [7FFC5B307798]
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FFC5B4454B8]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
M01_L09:
       mov       rcx,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       ecx,611
       mov       rdx,7FFC5B3C36E0
       call      qword ptr [7FFC5B307798]
       mov       rsi,rax
       mov       ecx,629
       mov       rdx,7FFC5B3C36E0
       call      qword ptr [7FFC5B307798]
       mov       r8,rax
       mov       rdx,rsi
       mov       rcx,rbx
       call      qword ptr [7FFC5B4FE490]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
M01_L10:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 560
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Transform()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmulss    xmm2,xmm0,dword ptr [7FFC5B174438]
       vxorps    xmm3,xmm3,xmm3
       vmulss    xmm3,xmm1,xmm3
       vaddss    xmm2,xmm2,xmm3
       vmulss    xmm0,xmm0,dword ptr [7FFC5B17443C]
       vmulss    xmm1,xmm1,dword ptr [7FFC5B174438]
       vaddss    xmm0,xmm0,xmm1
       vmovd     eax,xmm2
       vmovd     ecx,xmm0
       shl       rcx,20
       or        rax,rcx
       ret
; Total bytes of code 66
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Operator_Add()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vaddss    xmm0,xmm0,xmm2
       vaddss    xmm1,xmm1,xmm3
       vmovd     eax,xmm0
       vmovd     ecx,xmm1
       shl       rcx,20
       or        rax,rcx
       ret
; Total bytes of code 44
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Operator_Subtract()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vsubss    xmm0,xmm0,xmm2
       vsubss    xmm1,xmm1,xmm3
       vmovd     eax,xmm0
       vmovd     ecx,xmm1
       shl       rcx,20
       or        rax,rcx
       ret
; Total bytes of code 44
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Operator_Negate()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vxorps    xmm0,xmm0,[7FFC5B153DE0]
       vxorps    xmm1,xmm1,[7FFC5B153DE0]
       vmovd     eax,xmm0
       vmovd     ecx,xmm1
       shl       rcx,20
       or        rax,rcx
       ret
; Total bytes of code 42
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Operator_Multiply()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmulss    xmm0,xmm0,xmm2
       vmulss    xmm1,xmm1,xmm3
       vmovd     eax,xmm0
       vmovd     ecx,xmm1
       shl       rcx,20
       or        rax,rcx
       ret
; Total bytes of code 44
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Operator_Divide()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vdivss    xmm0,xmm0,xmm2
       vdivss    xmm1,xmm1,xmm3
       vmovd     eax,xmm0
       vmovd     ecx,xmm1
       shl       rcx,20
       or        rax,rcx
       ret
; Total bytes of code 44
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Operator_Equals()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vsubss    xmm0,xmm2,xmm0
       vandps    xmm0,xmm0,[7FFC5B163D60]
       vmovss    xmm2,dword ptr [7FFC5B163D70]
       vucomiss  xmm2,xmm0
       ja        short M00_L01
       xor       eax,eax
M00_L00:
       ret
M00_L01:
       vsubss    xmm0,xmm3,xmm1
       vandps    xmm0,xmm0,[7FFC5B163D60]
       vmovss    xmm1,dword ptr [7FFC5B163D70]
       vucomiss  xmm1,xmm0
       seta      al
       movzx     eax,al
       jmp       short M00_L00
; Total bytes of code 81
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.Operator_NotEquals()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vsubss    xmm0,xmm2,xmm0
       vandps    xmm0,xmm0,[7FFC5B143D70]
       vmovss    xmm2,dword ptr [7FFC5B143D80]
       vucomiss  xmm2,xmm0
       ja        short M00_L00
       mov       eax,1
       ret
M00_L00:
       vsubss    xmm0,xmm3,xmm1
       vandps    xmm0,xmm0,[7FFC5B143D70]
       vmovss    xmm1,dword ptr [7FFC5B143D80]
       vucomiss  xmm1,xmm0
       seta      al
       movzx     eax,al
       test      eax,eax
       sete      al
       movzx     eax,al
       ret
; Total bytes of code 91
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector2Benchmarks.EqualsStrict()
       lea       rax,[rcx+18]
       vmovss    xmm0,dword ptr [rcx+20]
       vmovss    xmm1,dword ptr [rcx+24]
       vucomiss  xmm0,dword ptr [rax]
       jp        short M00_L00
       je        short M00_L02
M00_L00:
       xor       eax,eax
M00_L01:
       ret
M00_L02:
       vucomiss  xmm1,dword ptr [rax+4]
       setnp     al
       jp        short M00_L03
       sete      al
M00_L03:
       movzx     eax,al
       jmp       short M00_L01
; Total bytes of code 43
```

