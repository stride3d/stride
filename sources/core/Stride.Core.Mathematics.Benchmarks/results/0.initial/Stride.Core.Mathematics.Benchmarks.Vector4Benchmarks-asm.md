## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.IsNormalized()
       add       rcx,18
       vmovss    xmm0,dword ptr [rcx]
       vmulss    xmm0,xmm0,xmm0
       vmovss    xmm1,dword ptr [rcx+4]
       vmulss    xmm1,xmm1,xmm1
       vaddss    xmm0,xmm0,xmm1
       vmovss    xmm1,dword ptr [rcx+8]
       vmulss    xmm1,xmm1,xmm1
       vaddss    xmm0,xmm0,xmm1
       vmovss    xmm1,dword ptr [rcx+0C]
       vmulss    xmm1,xmm1,xmm1
       vaddss    xmm0,xmm0,xmm1
       vsubss    xmm0,xmm0,dword ptr [7FFC5B144080]
       vandps    xmm0,xmm0,[7FFC5B144090]
       vmovss    xmm1,dword ptr [7FFC5B1440A0]
       vucomiss  xmm1,xmm0
       seta      al
       movzx     eax,al
       ret
; Total bytes of code 86
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Length()
       add       rcx,18
       vmovss    xmm0,dword ptr [rcx]
       vmulss    xmm0,xmm0,xmm0
       vmovss    xmm1,dword ptr [rcx+4]
       vmulss    xmm1,xmm1,xmm1
       vaddss    xmm0,xmm0,xmm1
       vmovss    xmm1,dword ptr [rcx+8]
       vmulss    xmm1,xmm1,xmm1
       vaddss    xmm0,xmm0,xmm1
       vmovss    xmm1,dword ptr [rcx+0C]
       vmulss    xmm1,xmm1,xmm1
       vaddss    xmm0,xmm0,xmm1
       vsqrtss   xmm0,xmm0,xmm0
       ret
; Total bytes of code 56
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.LengthSquared()
       add       rcx,18
       vmovss    xmm0,dword ptr [rcx]
       vmulss    xmm0,xmm0,xmm0
       vmovss    xmm1,dword ptr [rcx+4]
       vmulss    xmm1,xmm1,xmm1
       vaddss    xmm0,xmm0,xmm1
       vmovss    xmm1,dword ptr [rcx+8]
       vmulss    xmm1,xmm1,xmm1
       vaddss    xmm0,xmm0,xmm1
       vmovss    xmm1,dword ptr [rcx+0C]
       vmulss    xmm1,xmm1,xmm1
       vaddss    xmm0,xmm0,xmm1
       ret
; Total bytes of code 52
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Normalize()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmulss    xmm4,xmm0,xmm0
       vmulss    xmm5,xmm1,xmm1
       vaddss    xmm4,xmm4,xmm5
       vmulss    xmm5,xmm2,xmm2
       vaddss    xmm4,xmm4,xmm5
       vmulss    xmm5,xmm3,xmm3
       vaddss    xmm4,xmm4,xmm5
       vsqrtss   xmm4,xmm4,xmm4
       vucomiss  xmm4,dword ptr [7FFC5B1543C8]
       jbe       short M00_L00
       vmovss    xmm5,dword ptr [7FFC5B1543CC]
       vdivss    xmm4,xmm5,xmm4
       vmulss    xmm0,xmm0,xmm4
       vmulss    xmm1,xmm1,xmm4
       vmulss    xmm2,xmm2,xmm4
       vmulss    xmm3,xmm3,xmm4
M00_L00:
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       vmovss    dword ptr [rdx+0C],xmm3
       mov       rax,rdx
       ret
; Total bytes of code 113
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Pow()
       push      rbx
       sub       rsp,70
       vzeroupper
       vmovaps   [rsp+60],xmm6
       vmovaps   [rsp+50],xmm7
       vmovaps   [rsp+40],xmm8
       vmovaps   [rsp+30],xmm9
       vmovaps   [rsp+20],xmm10
       mov       rbx,rdx
       vmovss    xmm6,dword ptr [rcx+18]
       vmovss    xmm7,dword ptr [rcx+1C]
       vmovss    xmm8,dword ptr [rcx+20]
       vmovss    xmm9,dword ptr [rcx+24]
       vmovss    xmm10,dword ptr [rcx+10]
       vmovaps   xmm0,xmm6
       vmovaps   xmm1,xmm10
       call      00007FFCBAD607B0
       vmovaps   xmm6,xmm0
       vmovaps   xmm0,xmm7
       vmovaps   xmm1,xmm10
       call      00007FFCBAD607B0
       vmovaps   xmm7,xmm0
       vmovaps   xmm0,xmm8
       vmovaps   xmm1,xmm10
       call      00007FFCBAD607B0
       vmovaps   xmm8,xmm0
       vmovaps   xmm0,xmm9
       vmovaps   xmm1,xmm10
       call      00007FFCBAD607B0
       vmovaps   xmm9,xmm0
       vmovss    dword ptr [rbx],xmm6
       vmovss    dword ptr [rbx+4],xmm7
       vmovss    dword ptr [rbx+8],xmm8
       vmovss    dword ptr [rbx+0C],xmm9
       mov       rax,rbx
       vmovaps   xmm6,[rsp+60]
       vmovaps   xmm7,[rsp+50]
       vmovaps   xmm8,[rsp+40]
       vmovaps   xmm9,[rsp+30]
       vmovaps   xmm10,[rsp+20]
       add       rsp,70
       pop       rbx
       ret
; Total bytes of code 198
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Moveto()
       sub       rsp,58
       vmovaps   [rsp+40],xmm6
       vmovaps   [rsp+30],xmm7
       vmovaps   [rsp+20],xmm8
       vmovaps   [rsp+10],xmm9
       vmovaps   [rsp],xmm10
       lea       rax,[rcx+18]
       lea       r8,[rcx+28]
       vmovss    xmm0,dword ptr [rcx+10]
       vmovss    xmm1,dword ptr [r8]
       vmovss    xmm2,dword ptr [rax]
       vsubss    xmm1,xmm1,xmm2
       vmovss    xmm3,dword ptr [r8+4]
       vmovss    xmm4,dword ptr [rax+4]
       vsubss    xmm3,xmm3,xmm4
       vmovss    xmm5,dword ptr [r8+8]
       vmovss    xmm6,dword ptr [rax+8]
       vsubss    xmm5,xmm5,xmm6
       vmovss    xmm7,dword ptr [r8+0C]
       vmovss    xmm8,dword ptr [rax+0C]
       vsubss    xmm7,xmm7,xmm8
       vmulss    xmm9,xmm1,xmm1
       vmulss    xmm10,xmm3,xmm3
       vaddss    xmm9,xmm9,xmm10
       vmulss    xmm10,xmm5,xmm5
       vaddss    xmm9,xmm9,xmm10
       vmulss    xmm10,xmm7,xmm7
       vaddss    xmm9,xmm9,xmm10
       vsqrtss   xmm9,xmm9,xmm9
       vucomiss  xmm0,xmm9
       jae       short M00_L02
       vxorps    xmm10,xmm10,xmm10
       vucomiss  xmm9,xmm10
       jp        short M00_L00
       je        short M00_L02
M00_L00:
       vmovss    xmm10,dword ptr [7FFC5B1646F8]
       vdivss    xmm9,xmm10,xmm9
       vmulss    xmm0,xmm9,xmm0
       vmulss    xmm1,xmm1,xmm0
       vaddss    xmm1,xmm1,xmm2
       vmulss    xmm2,xmm3,xmm0
       vaddss    xmm2,xmm2,xmm4
       vmulss    xmm3,xmm5,xmm0
       vaddss    xmm3,xmm3,xmm6
       vmulss    xmm0,xmm7,xmm0
       vaddss    xmm0,xmm0,xmm8
M00_L01:
       vmovss    dword ptr [rdx],xmm1
       vmovss    dword ptr [rdx+4],xmm2
       vmovss    dword ptr [rdx+8],xmm3
       vmovss    dword ptr [rdx+0C],xmm0
       mov       rax,rdx
       vmovaps   xmm6,[rsp+40]
       vmovaps   xmm7,[rsp+30]
       vmovaps   xmm8,[rsp+20]
       vmovaps   xmm9,[rsp+10]
       vmovaps   xmm10,[rsp]
       add       rsp,58
       ret
M00_L02:
       vmovss    xmm1,dword ptr [r8]
       vmovss    xmm2,dword ptr [r8+4]
       vmovss    xmm3,dword ptr [r8+8]
       vmovss    xmm0,dword ptr [r8+0C]
       jmp       short M00_L01
; Total bytes of code 293
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Add()
       sub       rsp,28
       vmovaps   [rsp+10],xmm6
       vmovaps   [rsp],xmm7
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    xmm6,dword ptr [rcx+30]
       vmovss    xmm7,dword ptr [rcx+34]
       vaddss    xmm0,xmm0,xmm4
       vaddss    xmm1,xmm1,xmm5
       vaddss    xmm2,xmm2,xmm6
       vaddss    xmm3,xmm3,xmm7
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       vmovss    dword ptr [rdx+0C],xmm3
       mov       rax,rdx
       vmovaps   xmm6,[rsp+10]
       vmovaps   xmm7,[rsp]
       add       rsp,28
       ret
; Total bytes of code 109
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Subtract()
       lea       rax,[rcx+18]
       add       rcx,28
       vmovss    xmm0,dword ptr [rax]
       vsubss    xmm0,xmm0,dword ptr [rcx]
       vmovss    xmm1,dword ptr [rax+4]
       vsubss    xmm1,xmm1,dword ptr [rcx+4]
       vmovss    xmm2,dword ptr [rax+8]
       vsubss    xmm2,xmm2,dword ptr [rcx+8]
       vmovss    xmm3,dword ptr [rax+0C]
       vsubss    xmm3,xmm3,dword ptr [rcx+0C]
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       vmovss    dword ptr [rdx+0C],xmm3
       mov       rax,rdx
       ret
; Total bytes of code 69
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Multiply()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+10]
       vmulss    xmm0,xmm0,xmm4
       vmulss    xmm1,xmm1,xmm4
       vmulss    xmm2,xmm2,xmm4
       vmulss    xmm3,xmm3,xmm4
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       vmovss    dword ptr [rdx+0C],xmm3
       mov       rax,rdx
       ret
; Total bytes of code 64
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Modulate()
       sub       rsp,28
       vmovaps   [rsp+10],xmm6
       vmovaps   [rsp],xmm7
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    xmm6,dword ptr [rcx+30]
       vmovss    xmm7,dword ptr [rcx+34]
       vmulss    xmm0,xmm0,xmm4
       vmulss    xmm1,xmm1,xmm5
       vmulss    xmm2,xmm2,xmm6
       vmulss    xmm3,xmm3,xmm7
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       vmovss    dword ptr [rdx+0C],xmm3
       mov       rax,rdx
       vmovaps   xmm6,[rsp+10]
       vmovaps   xmm7,[rsp]
       add       rsp,28
       ret
; Total bytes of code 109
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Divide()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+10]
       vdivss    xmm0,xmm0,xmm4
       vdivss    xmm1,xmm1,xmm4
       vdivss    xmm2,xmm2,xmm4
       vdivss    xmm3,xmm3,xmm4
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       vmovss    dword ptr [rdx+0C],xmm3
       mov       rax,rdx
       ret
; Total bytes of code 64
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Demodulate()
       sub       rsp,28
       vmovaps   [rsp+10],xmm6
       vmovaps   [rsp],xmm7
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    xmm6,dword ptr [rcx+30]
       vmovss    xmm7,dword ptr [rcx+34]
       vdivss    xmm0,xmm0,xmm4
       vdivss    xmm1,xmm1,xmm5
       vdivss    xmm2,xmm2,xmm6
       vdivss    xmm3,xmm3,xmm7
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       vmovss    dword ptr [rdx+0C],xmm3
       mov       rax,rdx
       vmovaps   xmm6,[rsp+10]
       vmovaps   xmm7,[rsp]
       add       rsp,28
       ret
; Total bytes of code 109
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Negate()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vxorps    xmm0,xmm0,[7FFC5B154260]
       vxorps    xmm1,xmm1,[7FFC5B154260]
       vxorps    xmm2,xmm2,[7FFC5B154260]
       vxorps    xmm3,xmm3,[7FFC5B154260]
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       vmovss    dword ptr [rdx+0C],xmm3
       mov       rax,rdx
       ret
; Total bytes of code 75
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Barycentric()
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
       vmovss    xmm8,dword ptr [rcx+38]
       vmovss    xmm9,dword ptr [rcx+3C]
       vmovss    xmm10,dword ptr [rcx+40]
       vmovss    xmm11,dword ptr [rcx+44]
       vmovss    xmm12,dword ptr [rcx+10]
       vmovss    xmm13,dword ptr [rcx+14]
       vsubss    xmm4,xmm4,xmm0
       vmulss    xmm4,xmm4,xmm12
       vaddss    xmm4,xmm0,xmm4
       vsubss    xmm0,xmm8,xmm0
       vmulss    xmm0,xmm0,xmm13
       vaddss    xmm0,xmm4,xmm0
       vsubss    xmm4,xmm5,xmm1
       vmulss    xmm4,xmm4,xmm12
       vaddss    xmm4,xmm1,xmm4
       vsubss    xmm1,xmm9,xmm1
       vmulss    xmm1,xmm1,xmm13
       vaddss    xmm1,xmm4,xmm1
       vsubss    xmm4,xmm6,xmm2
       vmulss    xmm4,xmm4,xmm12
       vaddss    xmm4,xmm2,xmm4
       vsubss    xmm2,xmm10,xmm2
       vmulss    xmm2,xmm2,xmm13
       vaddss    xmm2,xmm4,xmm2
       vsubss    xmm4,xmm7,xmm3
       vmulss    xmm4,xmm4,xmm12
       vaddss    xmm4,xmm3,xmm4
       vsubss    xmm3,xmm11,xmm3
       vmulss    xmm3,xmm3,xmm13
       vaddss    xmm3,xmm4,xmm3
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       vmovss    dword ptr [rdx+0C],xmm3
       mov       rax,rdx
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
; Total bytes of code 305
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Clamp()
       sub       rsp,68
       vmovaps   [rsp+50],xmm6
       vmovaps   [rsp+40],xmm7
       vmovaps   [rsp+30],xmm8
       vmovaps   [rsp+20],xmm9
       vmovaps   [rsp+10],xmm10
       vmovaps   [rsp],xmm11
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    xmm6,dword ptr [rcx+30]
       vmovss    xmm7,dword ptr [rcx+34]
       vmovss    xmm8,dword ptr [rcx+38]
       vmovss    xmm9,dword ptr [rcx+3C]
       vmovss    xmm10,dword ptr [rcx+40]
       vmovss    xmm11,dword ptr [rcx+44]
       vucomiss  xmm0,xmm8
       jbe       near ptr M00_L08
M00_L00:
       vucomiss  xmm4,xmm8
       ja        near ptr M00_L09
M00_L01:
       vucomiss  xmm1,xmm9
       ja        near ptr M00_L10
M00_L02:
       vucomiss  xmm5,xmm1
       jbe       near ptr M00_L11
M00_L03:
       vucomiss  xmm2,xmm10
       ja        near ptr M00_L12
M00_L04:
       vucomiss  xmm6,xmm2
       jbe       near ptr M00_L13
M00_L05:
       vucomiss  xmm3,xmm11
       jbe       near ptr M00_L14
M00_L06:
       vucomiss  xmm7,xmm11
       jbe       near ptr M00_L15
M00_L07:
       vmovss    dword ptr [rdx],xmm8
       vmovss    dword ptr [rdx+4],xmm5
       vmovss    dword ptr [rdx+8],xmm6
       vmovss    dword ptr [rdx+0C],xmm7
       mov       rax,rdx
       vmovaps   xmm6,[rsp+50]
       vmovaps   xmm7,[rsp+40]
       vmovaps   xmm8,[rsp+30]
       vmovaps   xmm9,[rsp+20]
       vmovaps   xmm10,[rsp+10]
       vmovaps   xmm11,[rsp]
       add       rsp,68
       ret
M00_L08:
       vmovaps   xmm8,xmm0
       jmp       near ptr M00_L00
M00_L09:
       vmovaps   xmm8,xmm4
       jmp       near ptr M00_L01
M00_L10:
       vmovaps   xmm1,xmm9
       jmp       near ptr M00_L02
M00_L11:
       vmovaps   xmm5,xmm1
       jmp       near ptr M00_L03
M00_L12:
       vmovaps   xmm2,xmm10
       jmp       near ptr M00_L04
M00_L13:
       vmovaps   xmm6,xmm2
       jmp       near ptr M00_L05
M00_L14:
       vmovaps   xmm11,xmm3
       jmp       near ptr M00_L06
M00_L15:
       vmovaps   xmm7,xmm11
       jmp       near ptr M00_L07
; Total bytes of code 322
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Distance()
       sub       rsp,28
       vmovaps   [rsp+10],xmm6
       vmovaps   [rsp],xmm7
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    xmm6,dword ptr [rcx+30]
       vmovss    xmm7,dword ptr [rcx+34]
       vsubss    xmm1,xmm1,xmm5
       vsubss    xmm2,xmm2,xmm6
       vsubss    xmm3,xmm3,xmm7
       vsubss    xmm0,xmm0,xmm4
       vmulss    xmm0,xmm0,xmm0
       vmulss    xmm1,xmm1,xmm1
       vaddss    xmm0,xmm0,xmm1
       vmulss    xmm1,xmm2,xmm2
       vaddss    xmm0,xmm0,xmm1
       vmulss    xmm1,xmm3,xmm3
       vaddss    xmm0,xmm0,xmm1
       vsqrtss   xmm0,xmm0,xmm0
       vmovaps   xmm6,[rsp+10]
       vmovaps   xmm7,[rsp]
       add       rsp,28
       ret
; Total bytes of code 119
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.DistanceSquared()
       sub       rsp,28
       vmovaps   [rsp+10],xmm6
       vmovaps   [rsp],xmm7
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    xmm6,dword ptr [rcx+30]
       vmovss    xmm7,dword ptr [rcx+34]
       vsubss    xmm1,xmm1,xmm5
       vsubss    xmm2,xmm2,xmm6
       vsubss    xmm3,xmm3,xmm7
       vsubss    xmm0,xmm0,xmm4
       vmulss    xmm0,xmm0,xmm0
       vmulss    xmm1,xmm1,xmm1
       vaddss    xmm0,xmm0,xmm1
       vmulss    xmm1,xmm2,xmm2
       vaddss    xmm0,xmm0,xmm1
       vmulss    xmm1,xmm3,xmm3
       vaddss    xmm0,xmm0,xmm1
       vmovaps   xmm6,[rsp+10]
       vmovaps   xmm7,[rsp]
       add       rsp,28
       ret
; Total bytes of code 115
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Dot()
       sub       rsp,28
       vmovaps   [rsp+10],xmm6
       vmovaps   [rsp],xmm7
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    xmm6,dword ptr [rcx+30]
       vmovss    xmm7,dword ptr [rcx+34]
       vmulss    xmm0,xmm0,xmm4
       vmulss    xmm1,xmm1,xmm5
       vaddss    xmm0,xmm0,xmm1
       vmulss    xmm1,xmm2,xmm6
       vaddss    xmm0,xmm0,xmm1
       vmulss    xmm1,xmm3,xmm7
       vaddss    xmm0,xmm0,xmm1
       vmovaps   xmm6,[rsp+10]
       vmovaps   xmm7,[rsp]
       add       rsp,28
       ret
; Total bytes of code 99
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Lerp()
       sub       rsp,38
       vmovaps   [rsp+20],xmm6
       vmovaps   [rsp+10],xmm7
       vmovaps   [rsp],xmm8
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    xmm6,dword ptr [rcx+30]
       vmovss    xmm7,dword ptr [rcx+34]
       vmovss    xmm8,dword ptr [rcx+10]
       vsubss    xmm4,xmm4,xmm0
       vmulss    xmm4,xmm4,xmm8
       vaddss    xmm0,xmm4,xmm0
       vsubss    xmm4,xmm5,xmm1
       vmulss    xmm4,xmm4,xmm8
       vaddss    xmm1,xmm4,xmm1
       vsubss    xmm4,xmm6,xmm2
       vmulss    xmm4,xmm4,xmm8
       vaddss    xmm2,xmm4,xmm2
       vsubss    xmm4,xmm7,xmm3
       vmulss    xmm4,xmm4,xmm8
       vaddss    xmm3,xmm4,xmm3
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       vmovss    dword ptr [rdx+0C],xmm3
       mov       rax,rdx
       vmovaps   xmm6,[rsp+20]
       vmovaps   xmm7,[rsp+10]
       vmovaps   xmm8,[rsp]
       add       rsp,38
       ret
; Total bytes of code 162
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.SmoothStep()
       sub       rsp,58
       vmovaps   [rsp+40],xmm6
       vmovaps   [rsp+30],xmm7
       vmovaps   [rsp+20],xmm8
       vmovaps   [rsp+10],xmm9
       vmovaps   [rsp],xmm10
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    xmm6,dword ptr [rcx+30]
       vmovss    xmm7,dword ptr [rcx+34]
       vmovss    xmm8,dword ptr [rcx+10]
       vucomiss  xmm8,dword ptr [7FFC5B164520]
       ja        near ptr M00_L02
       vxorps    xmm9,xmm9,xmm9
       vucomiss  xmm9,xmm8
       ja        near ptr M00_L01
M00_L00:
       vmulss    xmm9,xmm8,xmm8
       vaddss    xmm8,xmm8,xmm8
       vmovss    xmm10,dword ptr [7FFC5B164524]
       vsubss    xmm8,xmm10,xmm8
       vmulss    xmm8,xmm9,xmm8
       vsubss    xmm4,xmm4,xmm0
       vmulss    xmm4,xmm4,xmm8
       vaddss    xmm0,xmm4,xmm0
       vsubss    xmm4,xmm5,xmm1
       vmulss    xmm4,xmm4,xmm8
       vaddss    xmm1,xmm4,xmm1
       vsubss    xmm4,xmm6,xmm2
       vmulss    xmm4,xmm4,xmm8
       vaddss    xmm2,xmm4,xmm2
       vsubss    xmm4,xmm7,xmm3
       vmulss    xmm4,xmm4,xmm8
       vaddss    xmm3,xmm4,xmm3
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       vmovss    dword ptr [rdx+0C],xmm3
       mov       rax,rdx
       vmovaps   xmm6,[rsp+40]
       vmovaps   xmm7,[rsp+30]
       vmovaps   xmm8,[rsp+20]
       vmovaps   xmm9,[rsp+10]
       vmovaps   xmm10,[rsp]
       add       rsp,58
       ret
M00_L01:
       vxorps    xmm8,xmm8,xmm8
       jmp       near ptr M00_L00
M00_L02:
       vmovss    xmm8,dword ptr [7FFC5B164520]
       jmp       near ptr M00_L00
; Total bytes of code 267
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Hermite()
       sub       rsp,0C8
       vmovaps   [rsp+0B0],xmm6
       vmovaps   [rsp+0A0],xmm7
       vmovaps   [rsp+90],xmm8
       vmovaps   [rsp+80],xmm9
       vmovaps   [rsp+70],xmm10
       vmovaps   [rsp+60],xmm11
       vmovaps   [rsp+50],xmm12
       vmovaps   [rsp+40],xmm13
       vmovaps   [rsp+30],xmm14
       vmovaps   [rsp+20],xmm15
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    dword ptr [rsp+1C],xmm3
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    xmm6,dword ptr [rcx+30]
       vmovss    dword ptr [rsp+18],xmm6
       vmovss    xmm7,dword ptr [rcx+34]
       vmovss    dword ptr [rsp+14],xmm7
       vmovss    xmm8,dword ptr [rcx+38]
       vmovss    xmm9,dword ptr [rcx+3C]
       vmovss    xmm10,dword ptr [rcx+40]
       vmovss    xmm11,dword ptr [rcx+44]
       vmovss    dword ptr [rsp+10],xmm11
       vmovss    xmm12,dword ptr [rcx+48]
       vmovss    xmm13,dword ptr [rcx+4C]
       vmovss    xmm14,dword ptr [rcx+50]
       vmovss    dword ptr [rsp+0C],xmm14
       vmovss    xmm15,dword ptr [rcx+54]
       vmovss    dword ptr [rsp+8],xmm15
       vmovss    xmm15,dword ptr [rcx+10]
       vmulss    xmm7,xmm15,xmm15
       vmulss    xmm11,xmm15,xmm7
       vaddss    xmm3,xmm11,xmm11
       vmulss    xmm14,xmm7,dword ptr [7FFC5B164880]
       vsubss    xmm3,xmm3,xmm14
       vaddss    xmm3,xmm3,dword ptr [7FFC5B164884]
       vmulss    xmm6,xmm11,dword ptr [7FFC5B164888]
       vaddss    xmm6,xmm6,xmm14
       vaddss    xmm14,xmm7,xmm7
       vsubss    xmm14,xmm11,xmm14
       vaddss    xmm14,xmm14,xmm15
       vsubss    xmm7,xmm11,xmm7
       vmulss    xmm0,xmm0,xmm3
       vmulss    xmm8,xmm8,xmm6
       vaddss    xmm0,xmm0,xmm8
       vmulss    xmm4,xmm4,xmm14
       vaddss    xmm0,xmm0,xmm4
       vmulss    xmm4,xmm12,xmm7
       vaddss    xmm0,xmm0,xmm4
       vmulss    xmm1,xmm1,xmm3
       vmulss    xmm4,xmm9,xmm6
       vaddss    xmm1,xmm1,xmm4
       vmulss    xmm4,xmm5,xmm14
       vaddss    xmm1,xmm1,xmm4
       vmulss    xmm4,xmm13,xmm7
       vaddss    xmm1,xmm1,xmm4
       vmulss    xmm2,xmm2,xmm3
       vmulss    xmm4,xmm10,xmm6
       vaddss    xmm2,xmm2,xmm4
       vmulss    xmm4,xmm14,dword ptr [rsp+18]
       vaddss    xmm2,xmm2,xmm4
       vmulss    xmm4,xmm7,dword ptr [rsp+0C]
       vaddss    xmm2,xmm2,xmm4
       vmulss    xmm3,xmm3,dword ptr [rsp+1C]
       vmulss    xmm4,xmm6,dword ptr [rsp+10]
       vaddss    xmm3,xmm3,xmm4
       vmulss    xmm4,xmm14,dword ptr [rsp+14]
       vaddss    xmm3,xmm3,xmm4
       vmulss    xmm4,xmm7,dword ptr [rsp+8]
       vaddss    xmm3,xmm3,xmm4
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       vmovss    dword ptr [rdx+0C],xmm3
       mov       rax,rdx
       vmovaps   xmm6,[rsp+0B0]
       vmovaps   xmm7,[rsp+0A0]
       vmovaps   xmm8,[rsp+90]
       vmovaps   xmm9,[rsp+80]
       vmovaps   xmm10,[rsp+70]
       vmovaps   xmm11,[rsp+60]
       vmovaps   xmm12,[rsp+50]
       vmovaps   xmm13,[rsp+40]
       vmovaps   xmm14,[rsp+30]
       vmovaps   xmm15,[rsp+20]
       add       rsp,0C8
       ret
; Total bytes of code 495
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.CatmullRom()
       sub       rsp,0D8
       vmovaps   [rsp+0C0],xmm6
       vmovaps   [rsp+0B0],xmm7
       vmovaps   [rsp+0A0],xmm8
       vmovaps   [rsp+90],xmm9
       vmovaps   [rsp+80],xmm10
       vmovaps   [rsp+70],xmm11
       vmovaps   [rsp+60],xmm12
       vmovaps   [rsp+50],xmm13
       vmovaps   [rsp+40],xmm14
       vmovaps   [rsp+30],xmm15
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    xmm6,dword ptr [rcx+30]
       vmovss    xmm7,dword ptr [rcx+34]
       vmovss    xmm8,dword ptr [rcx+38]
       vmovss    dword ptr [rsp+2C],xmm8
       vmovss    xmm9,dword ptr [rcx+3C]
       vmovss    dword ptr [rsp+28],xmm9
       vmovss    xmm10,dword ptr [rcx+40]
       vmovss    dword ptr [rsp+24],xmm10
       vmovss    xmm11,dword ptr [rcx+44]
       vmovss    dword ptr [rsp+20],xmm11
       vmovss    xmm12,dword ptr [rcx+48]
       vmovss    dword ptr [rsp+1C],xmm12
       vmovss    xmm13,dword ptr [rcx+4C]
       vmovss    dword ptr [rsp+18],xmm13
       vmovss    xmm14,dword ptr [rcx+50]
       vmovss    dword ptr [rsp+14],xmm14
       vmovss    xmm15,dword ptr [rcx+54]
       vmovss    dword ptr [rsp+10],xmm15
       vmovss    xmm15,dword ptr [rcx+10]
       vmulss    xmm14,xmm15,xmm15
       vmulss    xmm13,xmm15,xmm14
       vaddss    xmm12,xmm4,xmm4
       vsubss    xmm11,xmm8,xmm0
       vmulss    xmm11,xmm11,xmm15
       vaddss    xmm11,xmm12,xmm11
       vaddss    xmm12,xmm0,xmm0
       vmovss    xmm10,dword ptr [7FFC5B154CC8]
       vmulss    xmm9,xmm4,xmm10
       vsubss    xmm9,xmm12,xmm9
       vmovss    xmm12,dword ptr [7FFC5B154CCC]
       vmulss    xmm8,xmm8,xmm12
       vaddss    xmm8,xmm9,xmm8
       vmovss    xmm9,dword ptr [rsp+1C]
       vsubss    xmm8,xmm8,xmm9
       vmulss    xmm8,xmm8,xmm14
       vaddss    xmm8,xmm11,xmm8
       vmovss    xmm11,dword ptr [7FFC5B154CD0]
       vmulss    xmm4,xmm4,xmm11
       vsubss    xmm0,xmm4,xmm0
       vmulss    xmm4,xmm11,dword ptr [rsp+2C]
       vsubss    xmm0,xmm0,xmm4
       vaddss    xmm0,xmm0,xmm9
       vmulss    xmm0,xmm0,xmm13
       vaddss    xmm0,xmm8,xmm0
       vmovss    xmm4,dword ptr [7FFC5B154CD4]
       vmulss    xmm0,xmm0,xmm4
       vmovss    dword ptr [rsp+0C],xmm0
       vaddss    xmm8,xmm5,xmm5
       vmovss    xmm9,dword ptr [rsp+28]
       vsubss    xmm0,xmm9,xmm1
       vmulss    xmm0,xmm0,xmm15
       vaddss    xmm0,xmm8,xmm0
       vaddss    xmm8,xmm1,xmm1
       vmulss    xmm9,xmm5,xmm10
       vsubss    xmm8,xmm8,xmm9
       vmulss    xmm9,xmm12,dword ptr [rsp+28]
       vaddss    xmm8,xmm8,xmm9
       vmovss    xmm9,dword ptr [rsp+18]
       vsubss    xmm8,xmm8,xmm9
       vmulss    xmm8,xmm8,xmm14
       vaddss    xmm0,xmm0,xmm8
       vmulss    xmm5,xmm5,xmm11
       vsubss    xmm1,xmm5,xmm1
       vmulss    xmm5,xmm11,dword ptr [rsp+28]
       vsubss    xmm1,xmm1,xmm5
       vaddss    xmm1,xmm1,xmm9
       vmulss    xmm1,xmm1,xmm13
       vaddss    xmm0,xmm0,xmm1
       vmulss    xmm0,xmm0,xmm4
       vaddss    xmm1,xmm6,xmm6
       vmovss    xmm5,dword ptr [rsp+24]
       vsubss    xmm8,xmm5,xmm2
       vmulss    xmm8,xmm8,xmm15
       vaddss    xmm1,xmm1,xmm8
       vaddss    xmm8,xmm2,xmm2
       vmulss    xmm9,xmm6,xmm10
       vsubss    xmm8,xmm8,xmm9
       vmulss    xmm9,xmm5,xmm12
       vaddss    xmm8,xmm8,xmm9
       vmovss    xmm9,dword ptr [rsp+14]
       vsubss    xmm8,xmm8,xmm9
       vmulss    xmm8,xmm8,xmm14
       vaddss    xmm1,xmm1,xmm8
       vmulss    xmm6,xmm6,xmm11
       vsubss    xmm2,xmm6,xmm2
       vmulss    xmm5,xmm5,xmm11
       vsubss    xmm2,xmm2,xmm5
       vaddss    xmm2,xmm2,xmm9
       vmulss    xmm2,xmm2,xmm13
       vaddss    xmm1,xmm1,xmm2
       vmulss    xmm1,xmm1,xmm4
       vaddss    xmm2,xmm7,xmm7
       vmovss    xmm5,dword ptr [rsp+20]
       vsubss    xmm6,xmm5,xmm3
       vmulss    xmm6,xmm6,xmm15
       vaddss    xmm2,xmm2,xmm6
       vaddss    xmm6,xmm3,xmm3
       vmulss    xmm8,xmm7,xmm10
       vsubss    xmm6,xmm6,xmm8
       vmulss    xmm8,xmm5,xmm12
       vaddss    xmm6,xmm6,xmm8
       vmovss    xmm15,dword ptr [rsp+10]
       vsubss    xmm6,xmm6,xmm15
       vmulss    xmm6,xmm6,xmm14
       vaddss    xmm2,xmm2,xmm6
       vmulss    xmm6,xmm7,xmm11
       vsubss    xmm3,xmm6,xmm3
       vmulss    xmm5,xmm5,xmm11
       vsubss    xmm3,xmm3,xmm5
       vaddss    xmm3,xmm3,xmm15
       vmulss    xmm3,xmm3,xmm13
       vaddss    xmm2,xmm2,xmm3
       vmulss    xmm2,xmm2,xmm4
       vmovss    xmm3,dword ptr [rsp+0C]
       vmovss    dword ptr [rdx],xmm3
       vmovss    dword ptr [rdx+4],xmm0
       vmovss    dword ptr [rdx+8],xmm1
       vmovss    dword ptr [rdx+0C],xmm2
       mov       rax,rdx
       vmovaps   xmm6,[rsp+0C0]
       vmovaps   xmm7,[rsp+0B0]
       vmovaps   xmm8,[rsp+0A0]
       vmovaps   xmm9,[rsp+90]
       vmovaps   xmm10,[rsp+80]
       vmovaps   xmm11,[rsp+70]
       vmovaps   xmm12,[rsp+60]
       vmovaps   xmm13,[rsp+50]
       vmovaps   xmm14,[rsp+40]
       vmovaps   xmm15,[rsp+30]
       add       rsp,0D8
       ret
; Total bytes of code 788
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Max()
       sub       rsp,28
       vmovaps   [rsp+10],xmm6
       vmovaps   [rsp],xmm7
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    xmm6,dword ptr [rcx+30]
       vmovss    xmm7,dword ptr [rcx+34]
       vucomiss  xmm0,xmm4
       jbe       short M00_L04
M00_L00:
       vucomiss  xmm1,xmm5
       ja        short M00_L05
M00_L01:
       vucomiss  xmm2,xmm6
       ja        short M00_L06
M00_L02:
       vucomiss  xmm3,xmm7
       jbe       short M00_L07
M00_L03:
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm5
       vmovss    dword ptr [rdx+8],xmm6
       vmovss    dword ptr [rdx+0C],xmm3
       mov       rax,rdx
       vmovaps   xmm6,[rsp+10]
       vmovaps   xmm7,[rsp]
       add       rsp,28
       ret
M00_L04:
       vmovaps   xmm0,xmm4
       jmp       short M00_L00
M00_L05:
       vmovaps   xmm5,xmm1
       jmp       short M00_L01
M00_L06:
       vmovaps   xmm6,xmm2
       jmp       short M00_L02
M00_L07:
       vmovaps   xmm3,xmm7
       jmp       short M00_L03
; Total bytes of code 141
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Min()
       sub       rsp,28
       vmovaps   [rsp+10],xmm6
       vmovaps   [rsp],xmm7
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    xmm6,dword ptr [rcx+30]
       vmovss    xmm7,dword ptr [rcx+34]
       vucomiss  xmm4,xmm0
       ja        short M00_L04
M00_L00:
       vucomiss  xmm5,xmm1
       jbe       short M00_L05
M00_L01:
       vucomiss  xmm6,xmm2
       jbe       short M00_L06
M00_L02:
       vucomiss  xmm7,xmm3
       ja        short M00_L07
M00_L03:
       vmovss    dword ptr [rdx],xmm4
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       vmovss    dword ptr [rdx+0C],xmm7
       mov       rax,rdx
       vmovaps   xmm6,[rsp+10]
       vmovaps   xmm7,[rsp]
       add       rsp,28
       ret
M00_L04:
       vmovaps   xmm4,xmm0
       jmp       short M00_L00
M00_L05:
       vmovaps   xmm1,xmm5
       jmp       short M00_L01
M00_L06:
       vmovaps   xmm2,xmm6
       jmp       short M00_L02
M00_L07:
       vmovaps   xmm7,xmm3
       jmp       short M00_L03
; Total bytes of code 141
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Orthogonalize()
       push      rsi
       push      rbx
       sub       rsp,128
       vmovaps   [rsp+110],xmm6
       vmovaps   [rsp+100],xmm7
       vmovaps   [rsp+0F0],xmm8
       vmovaps   [rsp+0E0],xmm9
       vmovaps   [rsp+0D0],xmm10
       vmovaps   [rsp+0C0],xmm11
       vmovaps   [rsp+0B0],xmm12
       vmovaps   [rsp+0A0],xmm13
       vmovaps   [rsp+90],xmm14
       vmovaps   [rsp+80],xmm15
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+30],ymm4
       vmovdqu   ymmword ptr [rsp+50],ymm4
       vmovdqa   xmmword ptr [rsp+70],xmm4
       mov       rax,[rcx+8]
       mov       rdx,offset MT_Stride.Core.Mathematics.Vector4[]
       mov       [rsp+30],rdx
       lea       rdx,[rsp+30]
       mov       dword ptr [rdx+8],4
       lea       rdx,[rsp+30]
       vmovups   xmm0,[rcx+18]
       vmovups   [rdx+10],xmm0
       vmovups   xmm0,[rcx+28]
       vmovups   [rdx+20],xmm0
       vmovups   xmm0,[rcx+38]
       vmovups   [rdx+30],xmm0
       vmovups   xmm0,[rcx+48]
       vmovups   [rdx+40],xmm0
       test      rax,rax
       je        near ptr M00_L04
       mov       ecx,[rax+8]
       cmp       ecx,4
       jl        near ptr M00_L05
       xor       r8d,r8d
       cmp       r8d,4
       jl        near ptr M00_L03
M00_L00:
       vmovaps   xmm6,[rsp+110]
       vmovaps   xmm7,[rsp+100]
       vmovaps   xmm8,[rsp+0F0]
       vmovaps   xmm9,[rsp+0E0]
       vmovaps   xmm10,[rsp+0D0]
       vmovaps   xmm11,[rsp+0C0]
       vmovaps   xmm12,[rsp+0B0]
       vmovaps   xmm13,[rsp+0A0]
       vmovaps   xmm14,[rsp+90]
       vmovaps   xmm15,[rsp+80]
       add       rsp,128
       pop       rbx
       pop       rsi
       ret
M00_L01:
       cmp       r9d,ecx
       jae       near ptr M00_L06
       mov       r11,r9
       shl       r11,4
       lea       r11,[rax+r11+10]
       vmovss    xmm4,dword ptr [r11]
       vmovaps   xmm5,xmm4
       vmovss    xmm6,dword ptr [r11+4]
       vmovaps   xmm7,xmm6
       vmovss    xmm8,dword ptr [r11+8]
       vmovaps   xmm9,xmm8
       vmovss    xmm10,dword ptr [r11+0C]
       vmovaps   xmm11,xmm10
       vmulss    xmm5,xmm5,xmm0
       vmulss    xmm7,xmm7,xmm1
       vaddss    xmm5,xmm5,xmm7
       vmulss    xmm7,xmm9,xmm2
       vaddss    xmm5,xmm5,xmm7
       vmulss    xmm7,xmm11,xmm3
       vaddss    xmm5,xmm5,xmm7
       vmovaps   xmm7,xmm4
       vmovaps   xmm9,xmm6
       vmovaps   xmm11,xmm8
       vmovss    dword ptr [rsp+2C],xmm10
       vmovaps   xmm13,xmm4
       vmovaps   xmm14,xmm6
       vmovaps   xmm15,xmm8
       vmovaps   xmm12,xmm10
       vmulss    xmm7,xmm7,xmm13
       vmulss    xmm9,xmm9,xmm14
       vaddss    xmm7,xmm7,xmm9
       vmulss    xmm9,xmm11,xmm15
       vaddss    xmm7,xmm7,xmm9
       vmovss    xmm9,dword ptr [rsp+2C]
       vmulss    xmm9,xmm9,xmm12
       vaddss    xmm7,xmm7,xmm9
       vdivss    xmm5,xmm5,xmm7
       vmulss    xmm4,xmm4,xmm5
       vmulss    xmm6,xmm6,xmm5
       vmulss    xmm7,xmm8,xmm5
       vmulss    xmm5,xmm10,xmm5
       vsubss    xmm0,xmm0,xmm4
       vsubss    xmm1,xmm1,xmm6
       vsubss    xmm2,xmm2,xmm7
       vsubss    xmm3,xmm3,xmm5
       inc       r9d
M00_L02:
       cmp       r9d,r8d
       jl        near ptr M00_L01
       lea       r10,[rax+r10+10]
       vmovss    dword ptr [r10],xmm0
       vmovss    dword ptr [r10+4],xmm1
       vmovss    dword ptr [r10+8],xmm2
       vmovss    dword ptr [r10+0C],xmm3
       inc       r8d
       cmp       r8d,4
       jge       near ptr M00_L00
M00_L03:
       mov       r10,r8
       shl       r10,4
       lea       r9,[rdx+r10+10]
       vmovss    xmm0,dword ptr [r9]
       vmovss    xmm1,dword ptr [r9+4]
       vmovss    xmm2,dword ptr [r9+8]
       vmovss    xmm3,dword ptr [r9+0C]
       xor       r9d,r9d
       jmp       short M00_L02
M00_L04:
       mov       ecx,611
       mov       rdx,7FFC5B3C36E0
       call      qword ptr [7FFC5B307798]
       mov       rcx,rax
       call      qword ptr [7FFC5B597480]
       int       3
M00_L05:
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
M00_L06:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 752
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Orthonormalize()
       push      rsi
       push      rbx
       sub       rsp,0D8
       vmovaps   [rsp+0C0],xmm6
       vmovaps   [rsp+0B0],xmm7
       vmovaps   [rsp+0A0],xmm8
       vmovaps   [rsp+90],xmm9
       vmovaps   [rsp+80],xmm10
       vmovaps   [rsp+70],xmm11
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+20],ymm4
       vmovdqu   ymmword ptr [rsp+40],ymm4
       vmovdqa   xmmword ptr [rsp+60],xmm4
       mov       rax,[rcx+8]
       mov       rdx,offset MT_Stride.Core.Mathematics.Vector4[]
       mov       [rsp+20],rdx
       lea       rdx,[rsp+20]
       mov       dword ptr [rdx+8],4
       lea       rdx,[rsp+20]
       vmovups   xmm0,[rcx+18]
       vmovups   [rdx+10],xmm0
       vmovups   xmm0,[rcx+28]
       vmovups   [rdx+20],xmm0
       vmovups   xmm0,[rcx+38]
       vmovups   [rdx+30],xmm0
       vmovups   xmm0,[rcx+48]
       vmovups   [rdx+40],xmm0
       test      rax,rax
       je        near ptr M00_L05
       mov       ecx,[rax+8]
       cmp       ecx,4
       jl        near ptr M00_L06
       xor       r8d,r8d
       cmp       r8d,4
       jl        near ptr M00_L04
M00_L00:
       vmovaps   xmm6,[rsp+0C0]
       vmovaps   xmm7,[rsp+0B0]
       vmovaps   xmm8,[rsp+0A0]
       vmovaps   xmm9,[rsp+90]
       vmovaps   xmm10,[rsp+80]
       vmovaps   xmm11,[rsp+70]
       add       rsp,0D8
       pop       rbx
       pop       rsi
       ret
M00_L01:
       cmp       r9d,ecx
       jae       near ptr M00_L07
       mov       r11,r9
       shl       r11,4
       lea       r11,[rax+r11+10]
       vmovss    xmm4,dword ptr [r11]
       vmovaps   xmm5,xmm4
       vmovss    xmm6,dword ptr [r11+4]
       vmovaps   xmm7,xmm6
       vmovss    xmm8,dword ptr [r11+8]
       vmovaps   xmm9,xmm8
       vmovss    xmm10,dword ptr [r11+0C]
       vmovaps   xmm11,xmm10
       vmulss    xmm5,xmm5,xmm0
       vmulss    xmm7,xmm7,xmm1
       vaddss    xmm5,xmm5,xmm7
       vmulss    xmm7,xmm9,xmm2
       vaddss    xmm5,xmm5,xmm7
       vmulss    xmm7,xmm11,xmm3
       vaddss    xmm5,xmm5,xmm7
       vmulss    xmm4,xmm4,xmm5
       vmulss    xmm6,xmm6,xmm5
       vmulss    xmm7,xmm8,xmm5
       vmulss    xmm5,xmm10,xmm5
       vsubss    xmm0,xmm0,xmm4
       vsubss    xmm1,xmm1,xmm6
       vsubss    xmm2,xmm2,xmm7
       vsubss    xmm3,xmm3,xmm5
       inc       r9d
M00_L02:
       cmp       r9d,r8d
       jl        near ptr M00_L01
       vmulss    xmm4,xmm0,xmm0
       vmulss    xmm5,xmm1,xmm1
       vaddss    xmm4,xmm4,xmm5
       vmulss    xmm5,xmm2,xmm2
       vaddss    xmm4,xmm4,xmm5
       vmulss    xmm5,xmm3,xmm3
       vaddss    xmm4,xmm4,xmm5
       vsqrtss   xmm4,xmm4,xmm4
       vucomiss  xmm4,dword ptr [7FFC5B165020]
       jbe       short M00_L03
       vmovss    xmm5,dword ptr [7FFC5B165024]
       vdivss    xmm4,xmm5,xmm4
       vmulss    xmm0,xmm0,xmm4
       vmulss    xmm1,xmm1,xmm4
       vmulss    xmm2,xmm2,xmm4
       vmulss    xmm3,xmm3,xmm4
M00_L03:
       lea       r10,[rax+r10+10]
       vmovss    dword ptr [r10],xmm0
       vmovss    dword ptr [r10+4],xmm1
       vmovss    dword ptr [r10+8],xmm2
       vmovss    dword ptr [r10+0C],xmm3
       inc       r8d
       cmp       r8d,4
       jge       near ptr M00_L00
M00_L04:
       mov       r10,r8
       shl       r10,4
       lea       r9,[rdx+r10+10]
       vmovss    xmm0,dword ptr [r9]
       vmovss    xmm1,dword ptr [r9+4]
       vmovss    xmm2,dword ptr [r9+8]
       vmovss    xmm3,dword ptr [r9+0C]
       xor       r9d,r9d
       jmp       near ptr M00_L02
M00_L05:
       mov       ecx,611
       mov       rdx,7FFC5B3C36E0
       call      qword ptr [7FFC5B307798]
       mov       rcx,rax
       call      qword ptr [7FFC5B5974B0]
       int       3
M00_L06:
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
M00_L07:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 665
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Transform()
       sub       rsp,18
       vmovaps   [rsp],xmm6
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmulss    xmm4,xmm0,dword ptr [7FFC5B134C30]
       vxorps    xmm5,xmm5,xmm5
       vmulss    xmm5,xmm1,xmm5
       vaddss    xmm4,xmm4,xmm5
       vmulss    xmm5,xmm2,dword ptr [7FFC5B134C34]
       vaddss    xmm4,xmm4,xmm5
       vmulss    xmm5,xmm0,dword ptr [7FFC5B134C34]
       vmulss    xmm6,xmm1,dword ptr [7FFC5B134C30]
       vaddss    xmm5,xmm5,xmm6
       vxorps    xmm6,xmm6,xmm6
       vmulss    xmm6,xmm2,xmm6
       vaddss    xmm5,xmm5,xmm6
       vxorps    xmm6,xmm6,xmm6
       vmulss    xmm0,xmm0,xmm6
       vmulss    xmm1,xmm1,dword ptr [7FFC5B134C34]
       vaddss    xmm0,xmm0,xmm1
       vmulss    xmm1,xmm2,dword ptr [7FFC5B134C30]
       vaddss    xmm0,xmm0,xmm1
       vmovss    dword ptr [rdx],xmm4
       vmovss    dword ptr [rdx+4],xmm5
       vmovss    dword ptr [rdx+8],xmm0
       vmovss    dword ptr [rdx+0C],xmm3
       mov       rax,rdx
       vmovaps   xmm6,[rsp]
       add       rsp,18
       ret
; Total bytes of code 157
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Operator_Add()
       sub       rsp,28
       vmovaps   [rsp+10],xmm6
       vmovaps   [rsp],xmm7
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    xmm6,dword ptr [rcx+30]
       vmovss    xmm7,dword ptr [rcx+34]
       vaddss    xmm0,xmm0,xmm4
       vaddss    xmm1,xmm1,xmm5
       vaddss    xmm2,xmm2,xmm6
       vaddss    xmm3,xmm3,xmm7
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       vmovss    dword ptr [rdx+0C],xmm3
       mov       rax,rdx
       vmovaps   xmm6,[rsp+10]
       vmovaps   xmm7,[rsp]
       add       rsp,28
       ret
; Total bytes of code 109
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Operator_Subtract()
       sub       rsp,28
       vmovaps   [rsp+10],xmm6
       vmovaps   [rsp],xmm7
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    xmm6,dword ptr [rcx+30]
       vmovss    xmm7,dword ptr [rcx+34]
       vsubss    xmm0,xmm0,xmm4
       vsubss    xmm1,xmm1,xmm5
       vsubss    xmm2,xmm2,xmm6
       vsubss    xmm3,xmm3,xmm7
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       vmovss    dword ptr [rdx+0C],xmm3
       mov       rax,rdx
       vmovaps   xmm6,[rsp+10]
       vmovaps   xmm7,[rsp]
       add       rsp,28
       ret
; Total bytes of code 109
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Operator_Negate()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vxorps    xmm0,xmm0,[7FFC5B134260]
       vxorps    xmm1,xmm1,[7FFC5B134260]
       vxorps    xmm2,xmm2,[7FFC5B134260]
       vxorps    xmm3,xmm3,[7FFC5B134260]
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       vmovss    dword ptr [rdx+0C],xmm3
       mov       rax,rdx
       ret
; Total bytes of code 75
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Operator_Multiply()
       sub       rsp,28
       vmovaps   [rsp+10],xmm6
       vmovaps   [rsp],xmm7
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    xmm6,dword ptr [rcx+30]
       vmovss    xmm7,dword ptr [rcx+34]
       vmulss    xmm0,xmm0,xmm4
       vmulss    xmm1,xmm1,xmm5
       vmulss    xmm2,xmm2,xmm6
       vmulss    xmm3,xmm3,xmm7
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       vmovss    dword ptr [rdx+0C],xmm3
       mov       rax,rdx
       vmovaps   xmm6,[rsp+10]
       vmovaps   xmm7,[rsp]
       add       rsp,28
       ret
; Total bytes of code 109
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Operator_Divide()
       sub       rsp,28
       vmovaps   [rsp+10],xmm6
       vmovaps   [rsp],xmm7
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    xmm6,dword ptr [rcx+30]
       vmovss    xmm7,dword ptr [rcx+34]
       vdivss    xmm0,xmm0,xmm4
       vdivss    xmm1,xmm1,xmm5
       vdivss    xmm2,xmm2,xmm6
       vdivss    xmm3,xmm3,xmm7
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       vmovss    dword ptr [rdx+0C],xmm3
       mov       rax,rdx
       vmovaps   xmm6,[rsp+10]
       vmovaps   xmm7,[rsp]
       add       rsp,28
       ret
; Total bytes of code 109
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Operator_Equals()
       sub       rsp,28
       vmovaps   [rsp+10],xmm6
       vmovaps   [rsp],xmm7
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    xmm6,dword ptr [rcx+30]
       vmovss    xmm7,dword ptr [rcx+34]
       vsubss    xmm0,xmm4,xmm0
       vandps    xmm0,xmm0,[7FFC5B164270]
       vmovss    xmm4,dword ptr [7FFC5B164280]
       vucomiss  xmm4,xmm0
       ja        short M00_L02
M00_L00:
       xor       eax,eax
M00_L01:
       vmovaps   xmm6,[rsp+10]
       vmovaps   xmm7,[rsp]
       add       rsp,28
       ret
M00_L02:
       vsubss    xmm0,xmm5,xmm1
       vandps    xmm0,xmm0,[7FFC5B164270]
       vmovss    xmm1,dword ptr [7FFC5B164280]
       vucomiss  xmm1,xmm0
       jbe       short M00_L00
       vsubss    xmm0,xmm6,xmm2
       vandps    xmm0,xmm0,[7FFC5B164270]
       vmovss    xmm1,dword ptr [7FFC5B164280]
       vucomiss  xmm1,xmm0
       jbe       short M00_L00
       vsubss    xmm0,xmm7,xmm3
       vandps    xmm0,xmm0,[7FFC5B164270]
       vmovss    xmm1,dword ptr [7FFC5B164280]
       vucomiss  xmm1,xmm0
       seta      al
       movzx     eax,al
       jmp       short M00_L01
; Total bytes of code 183
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Operator_NotEquals()
       sub       rsp,28
       vmovaps   [rsp+10],xmm6
       vmovaps   [rsp],xmm7
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    xmm6,dword ptr [rcx+30]
       vmovss    xmm7,dword ptr [rcx+34]
       vsubss    xmm0,xmm4,xmm0
       vandps    xmm0,xmm0,[7FFC5B174280]
       vmovss    xmm4,dword ptr [7FFC5B174290]
       vucomiss  xmm4,xmm0
       ja        short M00_L01
M00_L00:
       mov       eax,1
       vmovaps   xmm6,[rsp+10]
       vmovaps   xmm7,[rsp]
       add       rsp,28
       ret
M00_L01:
       vsubss    xmm0,xmm5,xmm1
       vandps    xmm0,xmm0,[7FFC5B174280]
       vmovss    xmm1,dword ptr [7FFC5B174290]
       vucomiss  xmm1,xmm0
       jbe       short M00_L00
       vsubss    xmm0,xmm6,xmm2
       vandps    xmm0,xmm0,[7FFC5B174280]
       vmovss    xmm1,dword ptr [7FFC5B174290]
       vucomiss  xmm1,xmm0
       jbe       short M00_L00
       vsubss    xmm0,xmm7,xmm3
       vandps    xmm0,xmm0,[7FFC5B174280]
       vmovss    xmm1,dword ptr [7FFC5B174290]
       vucomiss  xmm1,xmm0
       seta      al
       movzx     eax,al
       test      eax,eax
       sete      al
       movzx     eax,al
       vmovaps   xmm6,[rsp+10]
       vmovaps   xmm7,[rsp]
       add       rsp,28
       ret
; Total bytes of code 208
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.EqualsStrict()
       lea       rax,[rcx+18]
       vmovss    xmm0,dword ptr [rcx+28]
       vmovss    xmm1,dword ptr [rcx+2C]
       vmovss    xmm2,dword ptr [rcx+30]
       vmovss    xmm3,dword ptr [rcx+34]
       vucomiss  xmm0,dword ptr [rax]
       jp        short M00_L00
       je        short M00_L02
M00_L00:
       xor       eax,eax
M00_L01:
       ret
M00_L02:
       vucomiss  xmm1,dword ptr [rax+4]
       jp        short M00_L00
       jne       short M00_L00
       vucomiss  xmm2,dword ptr [rax+8]
       jp        short M00_L00
       jne       short M00_L00
       vucomiss  xmm3,dword ptr [rax+0C]
       setnp     al
       jp        short M00_L03
       sete      al
M00_L03:
       movzx     eax,al
       jmp       short M00_L01
; Total bytes of code 71
```

