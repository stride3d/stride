## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.IsNormalized()
       add       rcx,18
       vmovss    xmm0,dword ptr [rcx]
       vmulss    xmm0,xmm0,xmm0
       vmovss    xmm1,dword ptr [rcx+4]
       vmulss    xmm1,xmm1,xmm1
       vaddss    xmm0,xmm0,xmm1
       vmovss    xmm1,dword ptr [rcx+8]
       vmulss    xmm1,xmm1,xmm1
       vaddss    xmm0,xmm0,xmm1
       vsubss    xmm0,xmm0,dword ptr [7FFC5B153FC0]
       vandps    xmm0,xmm0,[7FFC5B153FD0]
       vmovss    xmm1,dword ptr [7FFC5B153FE0]
       vucomiss  xmm1,xmm0
       seta      al
       movzx     eax,al
       ret
; Total bytes of code 73
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Length()
       add       rcx,18
       vmovss    xmm0,dword ptr [rcx]
       vmulss    xmm0,xmm0,xmm0
       vmovss    xmm1,dword ptr [rcx+4]
       vmulss    xmm1,xmm1,xmm1
       vaddss    xmm0,xmm0,xmm1
       vmovss    xmm1,dword ptr [rcx+8]
       vmulss    xmm1,xmm1,xmm1
       vaddss    xmm0,xmm0,xmm1
       vsqrtss   xmm0,xmm0,xmm0
       ret
; Total bytes of code 43
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.LengthSquared()
       add       rcx,18
       vmovss    xmm0,dword ptr [rcx]
       vmulss    xmm0,xmm0,xmm0
       vmovss    xmm1,dword ptr [rcx+4]
       vmulss    xmm1,xmm1,xmm1
       vaddss    xmm0,xmm0,xmm1
       vmovss    xmm1,dword ptr [rcx+8]
       vmulss    xmm1,xmm1,xmm1
       vaddss    xmm0,xmm0,xmm1
       ret
; Total bytes of code 39
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Normalize()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmulss    xmm3,xmm0,xmm0
       vmulss    xmm4,xmm1,xmm1
       vaddss    xmm3,xmm3,xmm4
       vmulss    xmm4,xmm2,xmm2
       vaddss    xmm3,xmm3,xmm4
       vsqrtss   xmm3,xmm3,xmm3
       vucomiss  xmm3,dword ptr [7FFC5B134290]
       jbe       short M00_L00
       vmovss    xmm4,dword ptr [7FFC5B134294]
       vdivss    xmm3,xmm4,xmm3
       vmulss    xmm0,xmm0,xmm3
       vmulss    xmm1,xmm1,xmm3
       vmulss    xmm2,xmm2,xmm3
M00_L00:
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       mov       rax,rdx
       ret
; Total bytes of code 91
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.MoveTo()
       push      rbx
       sub       rsp,60
       vmovaps   [rsp+50],xmm6
       vmovaps   [rsp+40],xmm7
       vmovaps   [rsp+30],xmm8
       mov       rbx,rdx
       lea       rax,[rcx+18]
       lea       rdx,[rcx+24]
       vmovss    xmm3,dword ptr [rcx+10]
       vmovss    xmm1,dword ptr [rdx]
       vsubss    xmm2,xmm1,dword ptr [rax]
       vmovss    xmm0,dword ptr [rdx+4]
       vsubss    xmm4,xmm0,dword ptr [rax+4]
       vmovss    xmm5,dword ptr [rdx+8]
       vsubss    xmm6,xmm5,dword ptr [rax+8]
       vmulss    xmm7,xmm2,xmm2
       vmulss    xmm8,xmm4,xmm4
       vaddss    xmm7,xmm7,xmm8
       vmulss    xmm8,xmm6,xmm6
       vaddss    xmm7,xmm7,xmm8
       vsqrtss   xmm7,xmm7,xmm7
       vucomiss  xmm3,xmm7
       jb        short M00_L01
M00_L00:
       vmovss    dword ptr [rbx],xmm1
       vmovss    dword ptr [rbx+4],xmm0
       vmovss    dword ptr [rbx+8],xmm5
       mov       rax,rbx
       vmovaps   xmm6,[rsp+50]
       vmovaps   xmm7,[rsp+40]
       vmovaps   xmm8,[rsp+30]
       add       rsp,60
       pop       rbx
       ret
M00_L01:
       vxorps    xmm8,xmm8,xmm8
       vucomiss  xmm7,xmm8
       jp        short M00_L02
       je        short M00_L00
M00_L02:
       xor       ecx,ecx
       mov       [rsp+20],rcx
       mov       [rsp+28],ecx
       vdivss    xmm1,xmm2,xmm7
       vmulss    xmm1,xmm1,xmm3
       vaddss    xmm1,xmm1,dword ptr [rax]
       vdivss    xmm2,xmm4,xmm7
       vmulss    xmm2,xmm2,xmm3
       vaddss    xmm2,xmm2,dword ptr [rax+4]
       vdivss    xmm0,xmm6,xmm7
       vmulss    xmm3,xmm0,xmm3
       vaddss    xmm3,xmm3,dword ptr [rax+8]
       lea       rcx,[rsp+20]
       call      qword ptr [7FFC5B4ECF48]; Stride.Core.Mathematics.Vector3..ctor(Single, Single, Single)
       vmovss    xmm1,dword ptr [rsp+20]
       vmovss    xmm0,dword ptr [rsp+24]
       vmovss    xmm5,dword ptr [rsp+28]
       jmp       near ptr M00_L00
; Total bytes of code 237
```
```assembly
; Stride.Core.Mathematics.Vector3..ctor(Single, Single, Single)
       vmovss    dword ptr [rcx],xmm1
       vmovss    dword ptr [rcx+4],xmm2
       vmovss    dword ptr [rcx+8],xmm3
       ret
; Total bytes of code 15
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Add()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vaddss    xmm0,xmm0,xmm3
       vaddss    xmm1,xmm1,xmm4
       vaddss    xmm2,xmm2,xmm5
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       mov       rax,rdx
       ret
; Total bytes of code 60
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Subtract()
       lea       rax,[rcx+18]
       add       rcx,24
       vmovss    xmm0,dword ptr [rax]
       vsubss    xmm0,xmm0,dword ptr [rcx]
       vmovss    xmm1,dword ptr [rax+4]
       vsubss    xmm1,xmm1,dword ptr [rcx+4]
       vmovss    xmm2,dword ptr [rax+8]
       vsubss    xmm2,xmm2,dword ptr [rcx+8]
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       mov       rax,rdx
       ret
; Total bytes of code 54
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Multiply()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+10]
       vmulss    xmm0,xmm0,xmm3
       vmulss    xmm1,xmm1,xmm3
       vmulss    xmm2,xmm2,xmm3
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       mov       rax,rdx
       ret
; Total bytes of code 50
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Modulate()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmulss    xmm0,xmm0,xmm3
       vmulss    xmm1,xmm1,xmm4
       vmulss    xmm2,xmm2,xmm5
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       mov       rax,rdx
       ret
; Total bytes of code 60
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Divide()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+10]
       vdivss    xmm0,xmm0,xmm3
       vdivss    xmm1,xmm1,xmm3
       vdivss    xmm2,xmm2,xmm3
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       mov       rax,rdx
       ret
; Total bytes of code 50
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Demodulate()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vdivss    xmm0,xmm0,xmm3
       vdivss    xmm1,xmm1,xmm4
       vdivss    xmm2,xmm2,xmm5
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       mov       rax,rdx
       ret
; Total bytes of code 60
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Negate()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vxorps    xmm0,xmm0,[7FFC5B134180]
       vxorps    xmm1,xmm1,[7FFC5B134180]
       vxorps    xmm2,xmm2,[7FFC5B134180]
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       mov       rax,rdx
       ret
; Total bytes of code 57
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Barycentric()
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
       vmovss    xmm8,dword ptr [rcx+38]
       vmovss    xmm9,dword ptr [rcx+10]
       vmovss    xmm10,dword ptr [rcx+14]
       vsubss    xmm3,xmm3,xmm0
       vmulss    xmm3,xmm3,xmm9
       vaddss    xmm3,xmm0,xmm3
       vsubss    xmm0,xmm6,xmm0
       vmulss    xmm0,xmm0,xmm10
       vaddss    xmm0,xmm3,xmm0
       vsubss    xmm3,xmm4,xmm1
       vmulss    xmm3,xmm3,xmm9
       vaddss    xmm3,xmm1,xmm3
       vsubss    xmm1,xmm7,xmm1
       vmulss    xmm1,xmm1,xmm10
       vaddss    xmm1,xmm3,xmm1
       vsubss    xmm3,xmm5,xmm2
       vmulss    xmm3,xmm3,xmm9
       vaddss    xmm3,xmm2,xmm3
       vsubss    xmm2,xmm8,xmm2
       vmulss    xmm2,xmm2,xmm10
       vaddss    xmm2,xmm3,xmm2
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       mov       rax,rdx
       vmovaps   xmm6,[rsp+40]
       vmovaps   xmm7,[rsp+30]
       vmovaps   xmm8,[rsp+20]
       vmovaps   xmm9,[rsp+10]
       vmovaps   xmm10,[rsp]
       add       rsp,58
       ret
; Total bytes of code 217
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Clamp()
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
       vmovss    xmm8,dword ptr [rcx+38]
       vucomiss  xmm0,xmm6
       ja        short M00_L06
M00_L00:
       vucomiss  xmm3,xmm0
       ja        short M00_L07
M00_L01:
       vucomiss  xmm1,xmm7
       ja        short M00_L08
M00_L02:
       vucomiss  xmm4,xmm1
       jbe       short M00_L09
M00_L03:
       vucomiss  xmm2,xmm8
       ja        short M00_L10
M00_L04:
       vucomiss  xmm5,xmm2
       jbe       short M00_L11
M00_L05:
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm4
       vmovss    dword ptr [rdx+8],xmm5
       mov       rax,rdx
       vmovaps   xmm6,[rsp+20]
       vmovaps   xmm7,[rsp+10]
       vmovaps   xmm8,[rsp]
       add       rsp,38
       ret
M00_L06:
       vmovaps   xmm0,xmm6
       jmp       short M00_L00
M00_L07:
       vmovaps   xmm0,xmm3
       jmp       short M00_L01
M00_L08:
       vmovaps   xmm1,xmm7
       jmp       short M00_L02
M00_L09:
       vmovaps   xmm4,xmm1
       jmp       short M00_L03
M00_L10:
       vmovaps   xmm2,xmm8
       jmp       short M00_L04
M00_L11:
       vmovaps   xmm5,xmm2
       jmp       short M00_L05
; Total bytes of code 179
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Distance()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vsubss    xmm1,xmm1,xmm4
       vsubss    xmm2,xmm2,xmm5
       vsubss    xmm0,xmm0,xmm3
       vmulss    xmm0,xmm0,xmm0
       vmulss    xmm1,xmm1,xmm1
       vaddss    xmm0,xmm0,xmm1
       vmulss    xmm1,xmm2,xmm2
       vaddss    xmm0,xmm0,xmm1
       vsqrtss   xmm0,xmm0,xmm0
       ret
; Total bytes of code 67
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.DistanceSquared()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vsubss    xmm1,xmm1,xmm4
       vsubss    xmm2,xmm2,xmm5
       vsubss    xmm0,xmm0,xmm3
       vmulss    xmm0,xmm0,xmm0
       vmulss    xmm1,xmm1,xmm1
       vaddss    xmm0,xmm0,xmm1
       vmulss    xmm1,xmm2,xmm2
       vaddss    xmm0,xmm0,xmm1
       ret
; Total bytes of code 63
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Dot()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmulss    xmm0,xmm0,xmm3
       vmulss    xmm1,xmm1,xmm4
       vaddss    xmm0,xmm0,xmm1
       vmulss    xmm1,xmm2,xmm5
       vaddss    xmm0,xmm0,xmm1
       ret
; Total bytes of code 51
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Lerp()
       sub       rsp,18
       vmovaps   [rsp],xmm6
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    xmm6,dword ptr [rcx+10]
       vsubss    xmm3,xmm3,xmm0
       vmulss    xmm3,xmm3,xmm6
       vaddss    xmm0,xmm3,xmm0
       vsubss    xmm3,xmm4,xmm1
       vmulss    xmm3,xmm3,xmm6
       vaddss    xmm1,xmm3,xmm1
       vsubss    xmm3,xmm5,xmm2
       vmulss    xmm3,xmm3,xmm6
       vaddss    xmm2,xmm3,xmm2
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       mov       rax,rdx
       vmovaps   xmm6,[rsp]
       add       rsp,18
       ret
; Total bytes of code 107
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.SmoothStep()
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
       vmovss    xmm6,dword ptr [rcx+10]
       vucomiss  xmm6,dword ptr [7FFC5B1443E0]
       ja        short M00_L02
       vxorps    xmm7,xmm7,xmm7
       vucomiss  xmm7,xmm6
       ja        short M00_L01
M00_L00:
       vmulss    xmm7,xmm6,xmm6
       vaddss    xmm6,xmm6,xmm6
       vmovss    xmm8,dword ptr [7FFC5B1443E4]
       vsubss    xmm6,xmm8,xmm6
       vmulss    xmm6,xmm7,xmm6
       vsubss    xmm3,xmm3,xmm0
       vmulss    xmm3,xmm3,xmm6
       vaddss    xmm0,xmm3,xmm0
       vsubss    xmm3,xmm4,xmm1
       vmulss    xmm3,xmm3,xmm6
       vaddss    xmm1,xmm3,xmm1
       vsubss    xmm3,xmm5,xmm2
       vmulss    xmm3,xmm3,xmm6
       vaddss    xmm2,xmm3,xmm2
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       mov       rax,rdx
       vmovaps   xmm6,[rsp+20]
       vmovaps   xmm7,[rsp+10]
       vmovaps   xmm8,[rsp]
       add       rsp,38
       ret
M00_L01:
       vxorps    xmm6,xmm6,xmm6
       jmp       short M00_L00
M00_L02:
       vmovss    xmm6,dword ptr [7FFC5B1443E0]
       jmp       short M00_L00
; Total bytes of code 191
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Hermite()
       sub       rsp,0B8
       vmovaps   [rsp+0A0],xmm6
       vmovaps   [rsp+90],xmm7
       vmovaps   [rsp+80],xmm8
       vmovaps   [rsp+70],xmm9
       vmovaps   [rsp+60],xmm10
       vmovaps   [rsp+50],xmm11
       vmovaps   [rsp+40],xmm12
       vmovaps   [rsp+30],xmm13
       vmovaps   [rsp+20],xmm14
       vmovaps   [rsp+10],xmm15
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    dword ptr [rsp+0C],xmm5
       vmovss    xmm6,dword ptr [rcx+30]
       vmovss    xmm7,dword ptr [rcx+34]
       vmovss    xmm8,dword ptr [rcx+38]
       vmovss    xmm9,dword ptr [rcx+3C]
       vmovss    xmm10,dword ptr [rcx+40]
       vmovss    xmm11,dword ptr [rcx+44]
       vmovss    dword ptr [rsp+8],xmm11
       vmovss    xmm12,dword ptr [rcx+10]
       vmulss    xmm13,xmm12,xmm12
       vmulss    xmm14,xmm12,xmm13
       vaddss    xmm15,xmm14,xmm14
       vmulss    xmm11,xmm13,dword ptr [7FFC5B144670]
       vsubss    xmm15,xmm15,xmm11
       vaddss    xmm15,xmm15,dword ptr [7FFC5B144674]
       vmulss    xmm5,xmm14,dword ptr [7FFC5B144678]
       vaddss    xmm5,xmm5,xmm11
       vaddss    xmm11,xmm13,xmm13
       vsubss    xmm11,xmm14,xmm11
       vaddss    xmm11,xmm11,xmm12
       vsubss    xmm12,xmm14,xmm13
       vmulss    xmm0,xmm0,xmm15
       vmulss    xmm6,xmm6,xmm5
       vaddss    xmm0,xmm0,xmm6
       vmulss    xmm3,xmm3,xmm11
       vaddss    xmm0,xmm0,xmm3
       vmulss    xmm3,xmm9,xmm12
       vaddss    xmm0,xmm0,xmm3
       vmulss    xmm1,xmm1,xmm15
       vmulss    xmm3,xmm7,xmm5
       vaddss    xmm1,xmm1,xmm3
       vmulss    xmm3,xmm4,xmm11
       vaddss    xmm1,xmm1,xmm3
       vmulss    xmm3,xmm10,xmm12
       vaddss    xmm1,xmm1,xmm3
       vmulss    xmm2,xmm2,xmm15
       vmulss    xmm3,xmm8,xmm5
       vaddss    xmm2,xmm2,xmm3
       vmulss    xmm3,xmm11,dword ptr [rsp+0C]
       vaddss    xmm2,xmm2,xmm3
       vmulss    xmm3,xmm12,dword ptr [rsp+8]
       vaddss    xmm2,xmm2,xmm3
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       mov       rax,rdx
       vmovaps   xmm6,[rsp+0A0]
       vmovaps   xmm7,[rsp+90]
       vmovaps   xmm8,[rsp+80]
       vmovaps   xmm9,[rsp+70]
       vmovaps   xmm10,[rsp+60]
       vmovaps   xmm11,[rsp+50]
       vmovaps   xmm12,[rsp+40]
       vmovaps   xmm13,[rsp+30]
       vmovaps   xmm14,[rsp+20]
       vmovaps   xmm15,[rsp+10]
       add       rsp,0B8
       ret
; Total bytes of code 411
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.CatmullRom()
       sub       rsp,0B8
       vmovaps   [rsp+0A0],xmm6
       vmovaps   [rsp+90],xmm7
       vmovaps   [rsp+80],xmm8
       vmovaps   [rsp+70],xmm9
       vmovaps   [rsp+60],xmm10
       vmovaps   [rsp+50],xmm11
       vmovaps   [rsp+40],xmm12
       vmovaps   [rsp+30],xmm13
       vmovaps   [rsp+20],xmm14
       vmovaps   [rsp+10],xmm15
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmovss    xmm6,dword ptr [rcx+30]
       vmovss    xmm7,dword ptr [rcx+34]
       vmovss    xmm8,dword ptr [rcx+38]
       vmovss    dword ptr [rsp+0C],xmm8
       vmovss    xmm9,dword ptr [rcx+3C]
       vmovss    dword ptr [rsp+8],xmm9
       vmovss    xmm10,dword ptr [rcx+40]
       vmovss    dword ptr [rsp+4],xmm10
       vmovss    xmm11,dword ptr [rcx+44]
       vmovss    dword ptr [rsp],xmm11
       vmovss    xmm12,dword ptr [rcx+10]
       vmulss    xmm13,xmm12,xmm12
       vmulss    xmm14,xmm12,xmm13
       vaddss    xmm15,xmm3,xmm3
       vsubss    xmm11,xmm6,xmm0
       vmulss    xmm11,xmm11,xmm12
       vaddss    xmm11,xmm15,xmm11
       vaddss    xmm15,xmm0,xmm0
       vmovss    xmm10,dword ptr [7FFC5B1549D8]
       vmulss    xmm9,xmm3,xmm10
       vsubss    xmm9,xmm15,xmm9
       vmovss    xmm15,dword ptr [7FFC5B1549DC]
       vmulss    xmm8,xmm6,xmm15
       vaddss    xmm8,xmm9,xmm8
       vmovss    xmm9,dword ptr [rsp+8]
       vsubss    xmm8,xmm8,xmm9
       vmulss    xmm8,xmm8,xmm13
       vaddss    xmm8,xmm11,xmm8
       vmovss    xmm11,dword ptr [7FFC5B1549E0]
       vmulss    xmm3,xmm3,xmm11
       vsubss    xmm0,xmm3,xmm0
       vmulss    xmm3,xmm6,xmm11
       vsubss    xmm0,xmm0,xmm3
       vaddss    xmm0,xmm0,xmm9
       vmulss    xmm0,xmm0,xmm14
       vaddss    xmm0,xmm8,xmm0
       vmovss    xmm3,dword ptr [7FFC5B1549E4]
       vmulss    xmm0,xmm0,xmm3
       vaddss    xmm6,xmm4,xmm4
       vsubss    xmm8,xmm7,xmm1
       vmulss    xmm8,xmm8,xmm12
       vaddss    xmm6,xmm6,xmm8
       vaddss    xmm8,xmm1,xmm1
       vmulss    xmm9,xmm4,xmm10
       vsubss    xmm8,xmm8,xmm9
       vmulss    xmm9,xmm7,xmm15
       vaddss    xmm8,xmm8,xmm9
       vmovss    xmm9,dword ptr [rsp+4]
       vsubss    xmm8,xmm8,xmm9
       vmulss    xmm8,xmm8,xmm13
       vaddss    xmm6,xmm6,xmm8
       vmulss    xmm4,xmm4,xmm11
       vsubss    xmm1,xmm4,xmm1
       vmulss    xmm4,xmm7,xmm11
       vsubss    xmm1,xmm1,xmm4
       vaddss    xmm1,xmm1,xmm9
       vmulss    xmm1,xmm1,xmm14
       vaddss    xmm1,xmm6,xmm1
       vmulss    xmm1,xmm1,xmm3
       vaddss    xmm4,xmm5,xmm5
       vmovss    xmm8,dword ptr [rsp+0C]
       vsubss    xmm6,xmm8,xmm2
       vmulss    xmm6,xmm6,xmm12
       vaddss    xmm4,xmm4,xmm6
       vaddss    xmm6,xmm2,xmm2
       vmulss    xmm7,xmm5,xmm10
       vsubss    xmm6,xmm6,xmm7
       vmulss    xmm7,xmm8,xmm15
       vaddss    xmm6,xmm6,xmm7
       vmovss    xmm7,dword ptr [rsp]
       vsubss    xmm6,xmm6,xmm7
       vmulss    xmm6,xmm6,xmm13
       vaddss    xmm4,xmm4,xmm6
       vmulss    xmm5,xmm5,xmm11
       vsubss    xmm2,xmm5,xmm2
       vmulss    xmm5,xmm8,xmm11
       vsubss    xmm2,xmm2,xmm5
       vaddss    xmm2,xmm2,xmm7
       vmulss    xmm2,xmm2,xmm14
       vaddss    xmm2,xmm4,xmm2
       vmulss    xmm2,xmm2,xmm3
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       mov       rax,rdx
       vmovaps   xmm6,[rsp+0A0]
       vmovaps   xmm7,[rsp+90]
       vmovaps   xmm8,[rsp+80]
       vmovaps   xmm9,[rsp+70]
       vmovaps   xmm10,[rsp+60]
       vmovaps   xmm11,[rsp+50]
       vmovaps   xmm12,[rsp+40]
       vmovaps   xmm13,[rsp+30]
       vmovaps   xmm14,[rsp+20]
       vmovaps   xmm15,[rsp+10]
       add       rsp,0B8
       ret
; Total bytes of code 596
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Max()
       sub       rsp,18
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       xor       eax,eax
       mov       [rsp+8],rax
       mov       [rsp+10],eax
       vucomiss  xmm0,xmm3
       jbe       short M00_L03
M00_L00:
       vmovss    dword ptr [rsp+8],xmm0
       vucomiss  xmm1,xmm4
       ja        short M00_L04
M00_L01:
       vmovss    dword ptr [rsp+0C],xmm4
       vucomiss  xmm2,xmm5
       ja        short M00_L05
M00_L02:
       vmovss    dword ptr [rsp+10],xmm5
       mov       rax,[rsp+8]
       mov       [rdx],rax
       mov       eax,[rsp+10]
       mov       [rdx+8],eax
       mov       rax,rdx
       add       rsp,18
       ret
M00_L03:
       vmovaps   xmm0,xmm3
       jmp       short M00_L00
M00_L04:
       vmovaps   xmm4,xmm1
       jmp       short M00_L01
M00_L05:
       vmovaps   xmm5,xmm2
       jmp       short M00_L02
; Total bytes of code 122
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Min()
       sub       rsp,18
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       xor       eax,eax
       mov       [rsp+8],rax
       mov       [rsp+10],eax
       vucomiss  xmm3,xmm0
       ja        short M00_L03
M00_L00:
       vmovss    dword ptr [rsp+8],xmm3
       vucomiss  xmm4,xmm1
       jbe       short M00_L04
M00_L01:
       vmovss    dword ptr [rsp+0C],xmm1
       vucomiss  xmm5,xmm2
       jbe       short M00_L05
M00_L02:
       vmovss    dword ptr [rsp+10],xmm2
       mov       rax,[rsp+8]
       mov       [rdx],rax
       mov       eax,[rsp+10]
       mov       [rdx+8],eax
       mov       rax,rdx
       add       rsp,18
       ret
M00_L03:
       vmovaps   xmm3,xmm0
       jmp       short M00_L00
M00_L04:
       vmovaps   xmm1,xmm4
       jmp       short M00_L01
M00_L05:
       vmovaps   xmm2,xmm5
       jmp       short M00_L02
; Total bytes of code 122
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Reflect()
       sub       rsp,28
       vmovaps   [rsp+10],xmm6
       vmovaps   [rsp],xmm7
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vmulss    xmm6,xmm0,xmm3
       vmulss    xmm7,xmm1,xmm4
       vaddss    xmm6,xmm6,xmm7
       vmulss    xmm7,xmm2,xmm5
       vaddss    xmm6,xmm6,xmm7
       vaddss    xmm6,xmm6,xmm6
       vmulss    xmm3,xmm6,xmm3
       vsubss    xmm0,xmm0,xmm3
       vmulss    xmm3,xmm6,xmm4
       vsubss    xmm1,xmm1,xmm3
       vmulss    xmm3,xmm6,xmm5
       vsubss    xmm2,xmm2,xmm3
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       mov       rax,rdx
       vmovaps   xmm6,[rsp+10]
       vmovaps   xmm7,[rsp]
       add       rsp,28
       ret
; Total bytes of code 126
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Orthogonalize()
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rsi,[rbx+8]
       mov       rcx,offset MT_Stride.Core.Mathematics.Vector3[]
       mov       edx,4
       call      CORINFO_HELP_NEWARR_1_VC
       vmovss    xmm0,dword ptr [rbx+18]
       vmovss    xmm1,dword ptr [rbx+1C]
       vmovss    xmm2,dword ptr [rbx+20]
       lea       rcx,[rax+10]
       vmovss    dword ptr [rcx],xmm0
       vmovss    dword ptr [rcx+4],xmm1
       vmovss    dword ptr [rcx+8],xmm2
       vmovss    xmm0,dword ptr [rbx+24]
       vmovss    xmm1,dword ptr [rbx+28]
       vmovss    xmm2,dword ptr [rbx+2C]
       lea       rcx,[rax+1C]
       vmovss    dword ptr [rcx],xmm0
       vmovss    dword ptr [rcx+4],xmm1
       vmovss    dword ptr [rcx+8],xmm2
       vmovss    xmm0,dword ptr [rbx+30]
       vmovss    xmm1,dword ptr [rbx+34]
       vmovss    xmm2,dword ptr [rbx+38]
       lea       rcx,[rax+28]
       vmovss    dword ptr [rcx],xmm0
       vmovss    dword ptr [rcx+4],xmm1
       vmovss    dword ptr [rcx+8],xmm2
       vmovss    xmm0,dword ptr [rbx+3C]
       vmovss    xmm1,dword ptr [rbx+40]
       vmovss    xmm2,dword ptr [rbx+44]
       lea       rcx,[rax+34]
       vmovss    dword ptr [rcx],xmm0
       vmovss    dword ptr [rcx+4],xmm1
       vmovss    dword ptr [rcx+8],xmm2
       mov       rcx,rsi
       mov       rdx,rax
       add       rsp,28
       pop       rbx
       pop       rsi
       jmp       qword ptr [7FFC5B585110]; Stride.Core.Mathematics.Vector3.Orthogonalize(Stride.Core.Mathematics.Vector3[], Stride.Core.Mathematics.Vector3[])
; Total bytes of code 183
```
```assembly
; Stride.Core.Mathematics.Vector3.Orthogonalize(Stride.Core.Mathematics.Vector3[], Stride.Core.Mathematics.Vector3[])
       push      rsi
       push      rbx
       sub       rsp,98
       vmovaps   [rsp+80],xmm6
       vmovaps   [rsp+70],xmm7
       vmovaps   [rsp+60],xmm8
       vmovaps   [rsp+50],xmm9
       vmovaps   [rsp+40],xmm10
       vmovaps   [rsp+30],xmm11
       vmovaps   [rsp+20],xmm12
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
       vmovaps   xmm6,[rsp+80]
       vmovaps   xmm7,[rsp+70]
       vmovaps   xmm8,[rsp+60]
       vmovaps   xmm9,[rsp+50]
       vmovaps   xmm10,[rsp+40]
       vmovaps   xmm11,[rsp+30]
       vmovaps   xmm12,[rsp+20]
       add       rsp,98
       pop       rbx
       pop       rsi
       ret
M01_L01:
       cmp       r11d,eax
       jae       near ptr M01_L07
       lea       rbx,[r11+r11*2]
       lea       rbx,[rcx+rbx*4+10]
       vmovss    xmm3,dword ptr [rbx]
       vmovaps   xmm4,xmm3
       vmovss    xmm5,dword ptr [rbx+4]
       vmovaps   xmm6,xmm5
       vmovss    xmm7,dword ptr [rbx+8]
       vmovaps   xmm8,xmm7
       vmulss    xmm4,xmm4,xmm0
       vmulss    xmm6,xmm6,xmm1
       vaddss    xmm4,xmm4,xmm6
       vmulss    xmm6,xmm8,xmm2
       vaddss    xmm4,xmm4,xmm6
       vmovaps   xmm6,xmm3
       vmovaps   xmm8,xmm5
       vmovaps   xmm9,xmm7
       vmovaps   xmm10,xmm3
       vmovaps   xmm11,xmm5
       vmovaps   xmm12,xmm7
       vmulss    xmm6,xmm6,xmm10
       vmulss    xmm8,xmm8,xmm11
       vaddss    xmm6,xmm6,xmm8
       vmulss    xmm8,xmm9,xmm12
       vaddss    xmm6,xmm6,xmm8
       vdivss    xmm4,xmm4,xmm6
       vmulss    xmm3,xmm3,xmm4
       vmulss    xmm5,xmm5,xmm4
       vmulss    xmm4,xmm7,xmm4
       vsubss    xmm0,xmm0,xmm3
       vsubss    xmm1,xmm1,xmm5
       vsubss    xmm2,xmm2,xmm4
       inc       r11d
M01_L02:
       cmp       r11d,r10d
       jl        near ptr M01_L01
       cmp       r10d,eax
       jae       near ptr M01_L07
       lea       r9,[rcx+r9*4+10]
       vmovss    dword ptr [r9],xmm0
       vmovss    dword ptr [r9+4],xmm1
       vmovss    dword ptr [r9+8],xmm2
       inc       r10d
       cmp       r8d,r10d
       jle       near ptr M01_L00
M01_L03:
       lea       r9,[r10+r10*2]
       lea       r11,[rdx+r9*4+10]
       vmovss    xmm0,dword ptr [r11]
       vmovss    xmm1,dword ptr [r11+4]
       vmovss    xmm2,dword ptr [r11+8]
       xor       r11d,r11d
       jmp       short M01_L02
M01_L04:
       mov       rcx,offset MT_System.ArgumentNullException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       ecx,603
       mov       rdx,7FFC5B3B36E0
       call      qword ptr [7FFC5B2F7798]
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FFC5B4354B8]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
M01_L05:
       mov       rcx,offset MT_System.ArgumentNullException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       ecx,611
       mov       rdx,7FFC5B3B36E0
       call      qword ptr [7FFC5B2F7798]
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FFC5B4354B8]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
M01_L06:
       mov       rcx,offset MT_System.ArgumentOutOfRangeException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       mov       ecx,611
       mov       rdx,7FFC5B3B36E0
       call      qword ptr [7FFC5B2F7798]
       mov       rsi,rax
       mov       ecx,629
       mov       rdx,7FFC5B3B36E0
       call      qword ptr [7FFC5B2F7798]
       mov       r8,rax
       mov       rdx,rsi
       mov       rcx,rbx
       call      qword ptr [7FFC5B4EE490]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
M01_L07:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 595
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Orthonormalize()
       push      rsi
       push      rbx
       sub       rsp,28
       mov       rbx,rcx
       mov       rsi,[rbx+8]
       mov       rcx,offset MT_Stride.Core.Mathematics.Vector3[]
       mov       edx,4
       call      CORINFO_HELP_NEWARR_1_VC
       vmovss    xmm0,dword ptr [rbx+18]
       vmovss    xmm1,dword ptr [rbx+1C]
       vmovss    xmm2,dword ptr [rbx+20]
       lea       rcx,[rax+10]
       vmovss    dword ptr [rcx],xmm0
       vmovss    dword ptr [rcx+4],xmm1
       vmovss    dword ptr [rcx+8],xmm2
       vmovss    xmm0,dword ptr [rbx+24]
       vmovss    xmm1,dword ptr [rbx+28]
       vmovss    xmm2,dword ptr [rbx+2C]
       lea       rcx,[rax+1C]
       vmovss    dword ptr [rcx],xmm0
       vmovss    dword ptr [rcx+4],xmm1
       vmovss    dword ptr [rcx+8],xmm2
       vmovss    xmm0,dword ptr [rbx+30]
       vmovss    xmm1,dword ptr [rbx+34]
       vmovss    xmm2,dword ptr [rbx+38]
       lea       rcx,[rax+28]
       vmovss    dword ptr [rcx],xmm0
       vmovss    dword ptr [rcx+4],xmm1
       vmovss    dword ptr [rcx+8],xmm2
       vmovss    xmm0,dword ptr [rbx+3C]
       vmovss    xmm1,dword ptr [rbx+40]
       vmovss    xmm2,dword ptr [rbx+44]
       lea       rcx,[rax+34]
       vmovss    dword ptr [rcx],xmm0
       vmovss    dword ptr [rcx+4],xmm1
       vmovss    dword ptr [rcx+8],xmm2
       mov       rcx,rsi
       mov       rdx,rax
       add       rsp,28
       pop       rbx
       pop       rsi
       jmp       qword ptr [7FFC5B595020]; Stride.Core.Mathematics.Vector3.Orthonormalize(Stride.Core.Mathematics.Vector3[], Stride.Core.Mathematics.Vector3[])
; Total bytes of code 183
```
```assembly
; Stride.Core.Mathematics.Vector3.Orthonormalize(Stride.Core.Mathematics.Vector3[], Stride.Core.Mathematics.Vector3[])
       push      rsi
       push      rbx
       sub       rsp,58
       vmovaps   [rsp+40],xmm6
       vmovaps   [rsp+30],xmm7
       vmovaps   [rsp+20],xmm8
       test      rdx,rdx
       je        near ptr M01_L05
       test      rcx,rcx
       je        near ptr M01_L06
       mov       eax,[rcx+8]
       mov       r8d,[rdx+8]
       cmp       eax,r8d
       jl        near ptr M01_L07
       xor       r10d,r10d
       cmp       r8d,r10d
       jg        near ptr M01_L04
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
       cmp       r11d,eax
       jae       near ptr M01_L08
       lea       rbx,[r11+r11*2]
       lea       rbx,[rcx+rbx*4+10]
       vmovss    xmm3,dword ptr [rbx]
       vmovaps   xmm4,xmm3
       vmovss    xmm5,dword ptr [rbx+4]
       vmovaps   xmm6,xmm5
       vmovss    xmm7,dword ptr [rbx+8]
       vmovaps   xmm8,xmm7
       vmulss    xmm4,xmm4,xmm0
       vmulss    xmm6,xmm6,xmm1
       vaddss    xmm4,xmm4,xmm6
       vmulss    xmm6,xmm8,xmm2
       vaddss    xmm4,xmm4,xmm6
       vmulss    xmm3,xmm3,xmm4
       vmulss    xmm5,xmm5,xmm4
       vmulss    xmm4,xmm7,xmm4
       vsubss    xmm0,xmm0,xmm3
       vsubss    xmm1,xmm1,xmm5
       vsubss    xmm2,xmm2,xmm4
       inc       r11d
M01_L02:
       cmp       r11d,r10d
       jl        short M01_L01
       vmulss    xmm3,xmm0,xmm0
       vmulss    xmm4,xmm1,xmm1
       vaddss    xmm3,xmm3,xmm4
       vmulss    xmm4,xmm2,xmm2
       vaddss    xmm3,xmm3,xmm4
       vsqrtss   xmm3,xmm3,xmm3
       vucomiss  xmm3,dword ptr [7FFC5B164D00]
       jbe       short M01_L03
       vmovss    xmm4,dword ptr [7FFC5B164D04]
       vdivss    xmm3,xmm4,xmm3
       vmulss    xmm0,xmm0,xmm3
       vmulss    xmm1,xmm1,xmm3
       vmulss    xmm2,xmm2,xmm3
M01_L03:
       cmp       r10d,eax
       jae       near ptr M01_L08
       lea       r9,[rcx+r9*4+10]
       vmovss    dword ptr [r9],xmm0
       vmovss    dword ptr [r9+4],xmm1
       vmovss    dword ptr [r9+8],xmm2
       inc       r10d
       cmp       r8d,r10d
       jle       near ptr M01_L00
M01_L04:
       lea       r9,[r10+r10*2]
       lea       r11,[rdx+r9*4+10]
       vmovss    xmm0,dword ptr [r11]
       vmovss    xmm1,dword ptr [r11+4]
       vmovss    xmm2,dword ptr [r11+8]
       xor       r11d,r11d
       jmp       near ptr M01_L02
M01_L05:
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
M01_L06:
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
M01_L07:
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
M01_L08:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 540
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Transform()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmulss    xmm3,xmm0,dword ptr [7FFC5B164B28]
       vxorps    xmm4,xmm4,xmm4
       vmulss    xmm4,xmm1,xmm4
       vaddss    xmm3,xmm3,xmm4
       vmulss    xmm4,xmm2,dword ptr [7FFC5B164B2C]
       vaddss    xmm3,xmm3,xmm4
       vmulss    xmm4,xmm0,dword ptr [7FFC5B164B2C]
       vmulss    xmm5,xmm1,dword ptr [7FFC5B164B28]
       vaddss    xmm4,xmm4,xmm5
       vxorps    xmm5,xmm5,xmm5
       vmulss    xmm5,xmm2,xmm5
       vaddss    xmm4,xmm4,xmm5
       vxorps    xmm5,xmm5,xmm5
       vmulss    xmm0,xmm0,xmm5
       vmulss    xmm1,xmm1,dword ptr [7FFC5B164B2C]
       vaddss    xmm0,xmm0,xmm1
       vmulss    xmm1,xmm2,dword ptr [7FFC5B164B28]
       vaddss    xmm0,xmm0,xmm1
       vmovss    dword ptr [rdx],xmm3
       vmovss    dword ptr [rdx+4],xmm4
       vmovss    dword ptr [rdx+8],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 129
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Operator_Add()
       lea       rax,[rcx+18]
       add       rcx,24
       vmovss    xmm0,dword ptr [rax]
       vaddss    xmm0,xmm0,dword ptr [rcx]
       vmovss    xmm1,dword ptr [rax+4]
       vaddss    xmm1,xmm1,dword ptr [rcx+4]
       vmovss    xmm2,dword ptr [rax+8]
       vaddss    xmm2,xmm2,dword ptr [rcx+8]
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       mov       rax,rdx
       ret
; Total bytes of code 54
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Operator_Subtract()
       lea       rax,[rcx+18]
       add       rcx,24
       vmovss    xmm0,dword ptr [rax]
       vsubss    xmm0,xmm0,dword ptr [rcx]
       vmovss    xmm1,dword ptr [rax+4]
       vsubss    xmm1,xmm1,dword ptr [rcx+4]
       vmovss    xmm2,dword ptr [rax+8]
       vsubss    xmm2,xmm2,dword ptr [rcx+8]
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       mov       rax,rdx
       ret
; Total bytes of code 54
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Operator_Negate()
       add       rcx,18
       vmovss    xmm0,dword ptr [rcx]
       vxorps    xmm0,xmm0,[7FFC5B154180]
       vmovss    xmm1,dword ptr [rcx+4]
       vxorps    xmm1,xmm1,[7FFC5B154180]
       vmovss    xmm2,dword ptr [rcx+8]
       vxorps    xmm2,xmm2,[7FFC5B154180]
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       mov       rax,rdx
       ret
; Total bytes of code 60
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Operator_Multiply()
       lea       rax,[rcx+18]
       add       rcx,24
       vmovss    xmm0,dword ptr [rax]
       vmulss    xmm0,xmm0,dword ptr [rcx]
       vmovss    xmm1,dword ptr [rax+4]
       vmulss    xmm1,xmm1,dword ptr [rcx+4]
       vmovss    xmm2,dword ptr [rax+8]
       vmulss    xmm2,xmm2,dword ptr [rcx+8]
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       mov       rax,rdx
       ret
; Total bytes of code 54
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Operator_Divide()
       lea       rax,[rcx+18]
       add       rcx,24
       vmovss    xmm0,dword ptr [rax]
       vdivss    xmm0,xmm0,[rcx]
       vmovss    xmm1,dword ptr [rax+4]
       vdivss    xmm1,xmm1,[rcx+4]
       vmovss    xmm2,dword ptr [rax+8]
       vdivss    xmm2,xmm2,[rcx+8]
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       mov       rax,rdx
       ret
; Total bytes of code 54
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Operator_Equals()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vsubss    xmm0,xmm3,xmm0
       vandps    xmm0,xmm0,[7FFC5B164130]
       vmovss    xmm3,dword ptr [7FFC5B164140]
       vucomiss  xmm3,xmm0
       ja        short M00_L02
M00_L00:
       xor       eax,eax
M00_L01:
       ret
M00_L02:
       vsubss    xmm0,xmm4,xmm1
       vandps    xmm0,xmm0,[7FFC5B164130]
       vmovss    xmm1,dword ptr [7FFC5B164140]
       vucomiss  xmm1,xmm0
       jbe       short M00_L00
       vsubss    xmm0,xmm5,xmm2
       vandps    xmm0,xmm0,[7FFC5B164130]
       vmovss    xmm1,dword ptr [7FFC5B164140]
       vucomiss  xmm1,xmm0
       seta      al
       movzx     eax,al
       jmp       short M00_L01
; Total bytes of code 117
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.Operator_NotEquals()
       vmovss    xmm0,dword ptr [rcx+18]
       vmovss    xmm1,dword ptr [rcx+1C]
       vmovss    xmm2,dword ptr [rcx+20]
       vmovss    xmm3,dword ptr [rcx+24]
       vmovss    xmm4,dword ptr [rcx+28]
       vmovss    xmm5,dword ptr [rcx+2C]
       vsubss    xmm0,xmm3,xmm0
       vandps    xmm0,xmm0,[7FFC5B164140]
       vmovss    xmm3,dword ptr [7FFC5B164150]
       vucomiss  xmm3,xmm0
       ja        short M00_L01
M00_L00:
       mov       eax,1
       ret
M00_L01:
       vsubss    xmm0,xmm4,xmm1
       vandps    xmm0,xmm0,[7FFC5B164140]
       vmovss    xmm1,dword ptr [7FFC5B164150]
       vucomiss  xmm1,xmm0
       jbe       short M00_L00
       vsubss    xmm0,xmm5,xmm2
       vandps    xmm0,xmm0,[7FFC5B164140]
       vmovss    xmm1,dword ptr [7FFC5B164150]
       vucomiss  xmm1,xmm0
       seta      al
       movzx     eax,al
       test      eax,eax
       sete      al
       movzx     eax,al
       ret
; Total bytes of code 127
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector3Benchmarks.EqualsStrict()
       lea       rax,[rcx+18]
       vmovss    xmm0,dword ptr [rcx+24]
       vmovss    xmm1,dword ptr [rcx+28]
       vmovss    xmm2,dword ptr [rcx+2C]
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
       setnp     al
       jp        short M00_L03
       sete      al
M00_L03:
       movzx     eax,al
       jmp       short M00_L01
; Total bytes of code 57
```

