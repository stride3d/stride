## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.IsNormalized()
       add       rcx,18
       vmovups   xmm0,[rcx]
       vdpps     xmm0,xmm0,xmm0,0FF
       vsubss    xmm0,xmm0,dword ptr [7FFC5B1640A0]
       vandps    xmm0,xmm0,[7FFC5B1640B0]
       vmovss    xmm1,dword ptr [7FFC5B1640C0]
       vucomiss  xmm1,xmm0
       seta      al
       movzx     eax,al
       ret
; Total bytes of code 49
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Length()
       add       rcx,18
       vmovups   xmm0,[rcx]
       vdpps     xmm0,xmm0,xmm0,0FF
       vsqrtss   xmm0,xmm0,xmm0
       ret
; Total bytes of code 19
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.LengthSquared()
       add       rcx,18
       vmovups   xmm0,[rcx]
       vdpps     xmm0,xmm0,xmm0,0FF
       ret
; Total bytes of code 15
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Normalize()
       vmovups   xmm0,[rcx+18]
       vdpps     xmm1,xmm0,xmm0,0FF
       vsqrtss   xmm1,xmm1,xmm1
       vucomiss  xmm1,dword ptr [7FFC5B164560]
       jbe       short M00_L00
       vbroadcastss xmm1,xmm1
       vdivps    xmm0,xmm0,xmm1
M00_L00:
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 42
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
       lea       rax,[rcx+18]
       vmovss    xmm6,dword ptr [rax]
       vmovss    xmm7,dword ptr [rax+4]
       vmovss    xmm8,dword ptr [rax+8]
       vmovss    xmm9,dword ptr [rax+0C]
       vmovss    xmm10,dword ptr [rcx+10]
       vmovaps   xmm0,xmm6
       vmovaps   xmm1,xmm10
       call      qword ptr [7FFC5B5751D0]; System.Single.Pow(Single, Single)
       vmovaps   xmm6,xmm0
       vmovaps   xmm0,xmm7
       vmovaps   xmm1,xmm10
       call      qword ptr [7FFC5B5751D0]; System.Single.Pow(Single, Single)
       vmovaps   xmm7,xmm0
       vmovaps   xmm0,xmm8
       vmovaps   xmm1,xmm10
       call      qword ptr [7FFC5B5751D0]; System.Single.Pow(Single, Single)
       vmovaps   xmm8,xmm0
       vmovaps   xmm0,xmm9
       vmovaps   xmm1,xmm10
       call      qword ptr [7FFC5B5751D0]; System.Single.Pow(Single, Single)
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
; Total bytes of code 205
```
```assembly
; System.Single.Pow(Single, Single)
       sub       rsp,28
       vzeroupper
       call      00007FFCBAD607B0
       nop
       add       rsp,28
       ret
; Total bytes of code 18
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Moveto()
       lea       rax,[rcx+18]
       lea       r8,[rcx+28]
       vmovss    xmm0,dword ptr [rcx+10]
       vmovups   xmm1,[r8]
       vmovups   xmm2,[rax]
       vsubps    xmm1,xmm1,xmm2
       vdpps     xmm3,xmm1,xmm1,0FF
       vsqrtss   xmm3,xmm3,xmm3
       vucomiss  xmm0,xmm3
       jae       short M00_L02
       vxorps    xmm4,xmm4,xmm4
       vucomiss  xmm3,xmm4
       jp        short M00_L00
       je        short M00_L02
M00_L00:
       vdivss    xmm0,xmm0,xmm3
       vbroadcastss xmm0,xmm0
       vfmadd213ps xmm1,xmm0,xmm2
M00_L01:
       vmovups   [rdx],xmm1
       mov       rax,rdx
       ret
M00_L02:
       vmovups   xmm1,[r8]
       jmp       short M00_L01
; Total bytes of code 83
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Add()
       vmovups   xmm0,[rcx+18]
       vaddps    xmm0,xmm0,[rcx+28]
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 18
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Subtract()
       vmovups   xmm0,[rcx+18]
       vsubps    xmm0,xmm0,[rcx+28]
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 18
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Multiply()
       vmovups   xmm0,[rcx+18]
       vbroadcastss xmm1,dword ptr [rcx+10]
       vmulps    xmm0,xmm1,xmm0
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 23
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Modulate()
       vmovups   xmm0,[rcx+18]
       vmulps    xmm0,xmm0,[rcx+28]
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 18
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Divide()
       vmovups   xmm0,[rcx+18]
       vbroadcastss xmm1,dword ptr [rcx+10]
       vdivps    xmm0,xmm0,xmm1
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 23
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Demodulate()
       vmovups   xmm0,[rcx+18]
       vdivps    xmm0,xmm0,[rcx+28]
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 18
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Negate()
       vmovups   xmm0,[rcx+18]
       vxorps    xmm0,xmm0,[7FFC5B144360]
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 21
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Barycentric()
       vmovups   xmm0,[rcx+18]
       vmovups   xmm1,[rcx+28]
       vmovups   xmm2,[rcx+38]
       vmovss    xmm3,dword ptr [rcx+10]
       vmovss    xmm4,dword ptr [rcx+14]
       vsubps    xmm1,xmm1,xmm0
       vbroadcastss xmm3,xmm3
       vmulps    xmm1,xmm1,xmm3
       vaddps    xmm1,xmm1,xmm0
       vsubps    xmm0,xmm2,xmm0
       vbroadcastss xmm2,xmm4
       vmulps    xmm0,xmm0,xmm2
       vaddps    xmm0,xmm0,xmm1
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 67
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Clamp()
       vmovups   xmm0,[rcx+18]
       vmovups   xmm1,[rcx+28]
       vmovups   xmm2,[rcx+38]
       vcmpeqps  xmm3,xmm0,xmm1
       vxorps    xmm4,xmm4,xmm4
       vpcmpgtd  xmm4,xmm4,xmm1
       vandps    xmm3,xmm4,xmm3
       vcmpneqps xmm4,xmm0,xmm0
       vorps     xmm3,xmm4,xmm3
       vcmpltps  xmm4,xmm1,xmm0
       vorps     xmm3,xmm4,xmm3
       vblendvps xmm0,xmm1,xmm0,xmm3
       vcmpeqps  xmm1,xmm0,xmm2
       vxorps    xmm3,xmm3,xmm3
       vpcmpgtd  xmm3,xmm3,xmm0
       vandps    xmm1,xmm3,xmm1
       vcmpneqps xmm3,xmm0,xmm0
       vorps     xmm1,xmm3,xmm1
       vcmpltps  xmm3,xmm0,xmm2
       vorps     xmm1,xmm3,xmm1
       vblendvps xmm0,xmm2,xmm0,xmm1
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 105
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Distance()
       vmovups   xmm0,[rcx+18]
       vsubps    xmm0,xmm0,[rcx+28]
       vdpps     xmm0,xmm0,xmm0,0FF
       vsqrtss   xmm0,xmm0,xmm0
       ret
; Total bytes of code 21
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.DistanceSquared()
       vmovups   xmm0,[rcx+18]
       vsubps    xmm0,xmm0,[rcx+28]
       vdpps     xmm0,xmm0,xmm0,0FF
       ret
; Total bytes of code 17
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Dot()
       vmovups   xmm0,[rcx+18]
       vdpps     xmm0,xmm0,[rcx+28],0FF
       ret
; Total bytes of code 13
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Lerp()
       vmovups   xmm0,[rcx+18]
       vmovups   xmm1,[rcx+28]
       vmovss    xmm2,dword ptr [rcx+10]
       vbroadcastss xmm2,xmm2
       vbroadcastss xmm3,dword ptr [7FFC5B144448]
       vsubps    xmm3,xmm3,xmm2
       vmulps    xmm1,xmm2,xmm1
       vfmadd213ps xmm0,xmm3,xmm1
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 50
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.SmoothStep()
       vmovups   xmm0,[rcx+18]
       vmovups   xmm1,[rcx+28]
       vmovss    xmm2,dword ptr [rcx+10]
       vxorps    xmm3,xmm3,xmm3
       vcmpneqps xmm4,xmm2,xmm2
       vorps     xmm3,xmm4,xmm3
       vxorps    xmm4,xmm4,xmm4
       vcmpgtps  xmm4,xmm2,xmm4
       vorps     xmm3,xmm4,xmm3
       vandps    xmm2,xmm3,xmm2
       vbroadcastss xmm3,dword ptr [7FFC5B1446D0]
       vminss    xmm2,xmm3,xmm2
       vmulss    xmm4,xmm2,xmm2
       vaddss    xmm2,xmm2,xmm2
       vmovss    xmm5,dword ptr [7FFC5B1446D4]
       vsubss    xmm2,xmm5,xmm2
       vmulss    xmm2,xmm4,xmm2
       vbroadcastss xmm2,xmm2
       vsubps    xmm3,xmm3,xmm2
       vmulps    xmm1,xmm2,xmm1
       vfmadd213ps xmm0,xmm3,xmm1
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 108
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Hermite()
       sub       rsp,38
       vmovaps   [rsp+20],xmm6
       vmovaps   [rsp+10],xmm7
       vmovaps   [rsp],xmm8
       vmovups   xmm0,[rcx+18]
       vmovups   xmm1,[rcx+28]
       vmovups   xmm2,[rcx+38]
       vmovups   xmm3,[rcx+48]
       vmovss    xmm4,dword ptr [rcx+10]
       vmulss    xmm5,xmm4,xmm4
       vmulss    xmm6,xmm5,xmm4
       vaddss    xmm7,xmm5,xmm5
       vsubss    xmm7,xmm6,xmm7
       vaddss    xmm4,xmm7,xmm4
       vmulss    xmm7,xmm6,dword ptr [7FFC5B134970]
       vmulss    xmm8,xmm5,dword ptr [7FFC5B134974]
       vaddss    xmm7,xmm7,xmm8
       vsubss    xmm5,xmm6,xmm5
       vaddss    xmm6,xmm6,xmm6
       vsubss    xmm6,xmm6,xmm8
       vaddss    xmm6,xmm6,dword ptr [7FFC5B134978]
       vbroadcastss xmm6,xmm6
       vmulps    xmm0,xmm6,xmm0
       vbroadcastss xmm4,xmm4
       vmulps    xmm1,xmm4,xmm1
       vaddps    xmm0,xmm1,xmm0
       vbroadcastss xmm1,xmm7
       vmulps    xmm1,xmm1,xmm2
       vaddps    xmm0,xmm1,xmm0
       vbroadcastss xmm1,xmm5
       vmulps    xmm1,xmm1,xmm3
       vaddps    xmm0,xmm1,xmm0
       vmovups   [rdx],xmm0
       mov       rax,rdx
       vmovaps   xmm6,[rsp+20]
       vmovaps   xmm7,[rsp+10]
       vmovaps   xmm8,[rsp]
       add       rsp,38
       ret
; Total bytes of code 185
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.CatmullRom()
       sub       rsp,38
       vmovaps   [rsp+20],xmm6
       vmovaps   [rsp+10],xmm7
       vmovaps   [rsp],xmm8
       vmovups   xmm0,[rcx+18]
       vmovups   xmm1,[rcx+28]
       vmovups   xmm2,[rcx+38]
       vmovups   xmm3,[rcx+48]
       vmovss    xmm4,dword ptr [rcx+10]
       vmulss    xmm5,xmm4,xmm4
       vmulss    xmm6,xmm5,xmm4
       vsubps    xmm7,xmm2,xmm0
       vbroadcastss xmm4,xmm4
       vmulps    xmm4,xmm4,xmm7
       vbroadcastss xmm7,dword ptr [7FFC5B164F90]
       vmulps    xmm8,xmm7,xmm1
       vaddps    xmm4,xmm4,xmm8
       vmulps    xmm7,xmm7,xmm0
       vmulps    xmm8,xmm1,[7FFC5B164FA0]
       vsubps    xmm7,xmm7,xmm8
       vmulps    xmm8,xmm2,[7FFC5B164FB0]
       vaddps    xmm7,xmm8,xmm7
       vsubps    xmm7,xmm7,xmm3
       vbroadcastss xmm5,xmm5
       vmulps    xmm5,xmm5,xmm7
       vaddps    xmm4,xmm5,xmm4
       vbroadcastss xmm5,dword ptr [7FFC5B164FC0]
       vmulps    xmm1,xmm5,xmm1
       vsubps    xmm0,xmm1,xmm0
       vmulps    xmm1,xmm5,xmm2
       vsubps    xmm0,xmm0,xmm1
       vaddps    xmm0,xmm0,xmm3
       vbroadcastss xmm1,xmm6
       vmulps    xmm0,xmm1,xmm0
       vaddps    xmm0,xmm0,xmm4
       vmulps    xmm0,xmm0,[7FFC5B164FD0]
       vmovups   [rdx],xmm0
       mov       rax,rdx
       vmovaps   xmm6,[rsp+20]
       vmovaps   xmm7,[rsp+10]
       vmovaps   xmm8,[rsp]
       add       rsp,38
       ret
; Total bytes of code 210
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Max()
       vmovups   xmm0,[rcx+18]
       vmovups   xmm1,[rcx+28]
       vcmpeqps  xmm2,xmm0,xmm1
       vxorps    xmm3,xmm3,xmm3
       vpcmpgtd  xmm3,xmm3,xmm1
       vandps    xmm2,xmm3,xmm2
       vcmpneqps xmm3,xmm0,xmm0
       vorps     xmm2,xmm3,xmm2
       vcmpltps  xmm3,xmm1,xmm0
       vorps     xmm2,xmm3,xmm2
       vblendvps xmm0,xmm1,xmm0,xmm2
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 59
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Min()
       vmovups   xmm0,[rcx+18]
       vmovups   xmm1,[rcx+28]
       vcmpeqps  xmm2,xmm0,xmm1
       vxorps    xmm3,xmm3,xmm3
       vpcmpgtd  xmm3,xmm3,xmm0
       vandps    xmm2,xmm3,xmm2
       vcmpneqps xmm3,xmm0,xmm0
       vorps     xmm2,xmm3,xmm2
       vcmpltps  xmm3,xmm0,xmm1
       vorps     xmm2,xmm3,xmm2
       vblendvps xmm0,xmm1,xmm0,xmm2
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 59
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Orthogonalize()
       push      rsi
       push      rbx
       sub       rsp,78
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+30],ymm4
       vmovdqu   ymmword ptr [rsp+50],ymm4
       mov       [rsp+70],rax
       mov       rax,[rcx+8]
       mov       rdx,offset MT_Stride.Core.Mathematics.Vector4[]
       mov       [rsp+28],rdx
       lea       rdx,[rsp+28]
       mov       dword ptr [rdx+8],4
       lea       rdx,[rsp+28]
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
       jmp       short M00_L02
       nop
M00_L00:
       cmp       r9d,ecx
       jae       near ptr M00_L07
       mov       r11d,r9d
       shl       r11,4
       vmovups   xmm1,[rax+r11+10]
       vdpps     xmm2,xmm1,xmm0,0FF
       vdpps     xmm3,xmm1,xmm1,0FF
       vdivss    xmm2,xmm2,xmm3
       vbroadcastss xmm2,xmm2
       vmulps    xmm1,xmm2,xmm1
       vsubps    xmm0,xmm0,xmm1
       inc       r9d
       cmp       r9d,r8d
       jl        short M00_L00
M00_L01:
       vmovups   [rax+r10+10],xmm0
       inc       r8d
       cmp       r8d,4
       jge       short M00_L04
M00_L02:
       mov       r10,r8
       shl       r10,4
       vmovups   xmm0,[rdx+r10+10]
       xor       r9d,r9d
       test      r8d,r8d
       jle       short M00_L01
       cmp       ecx,r8d
       jl        short M00_L00
M00_L03:
       mov       r11d,r9d
       shl       r11,4
       vmovups   xmm1,[rax+r11+10]
       vdpps     xmm2,xmm1,xmm0,0FF
       vdpps     xmm3,xmm1,xmm1,0FF
       vdivss    xmm2,xmm2,xmm3
       vbroadcastss xmm2,xmm2
       vmulps    xmm1,xmm2,xmm1
       vsubps    xmm0,xmm0,xmm1
       inc       r9d
       cmp       r9d,r8d
       jl        short M00_L03
       jmp       short M00_L01
M00_L04:
       add       rsp,78
       pop       rbx
       pop       rsi
       ret
M00_L05:
       mov       ecx,611
       mov       rdx,7FFC5B3A36E0
       call      qword ptr [7FFC5B2E7798]
       mov       rcx,rax
       call      qword ptr [7FFC5B5775B8]
       int       3
M00_L06:
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
       call      qword ptr [7FFC5B4EE538]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
M00_L07:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 424
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Orthonormalize()
       push      rsi
       push      rbx
       sub       rsp,78
       xor       eax,eax
       mov       [rsp+28],rax
       vxorps    xmm4,xmm4,xmm4
       vmovdqu   ymmword ptr [rsp+30],ymm4
       vmovdqu   ymmword ptr [rsp+50],ymm4
       mov       [rsp+70],rax
       mov       rax,[rcx+8]
       mov       rdx,offset MT_Stride.Core.Mathematics.Vector4[]
       mov       [rsp+28],rdx
       lea       rdx,[rsp+28]
       mov       dword ptr [rdx+8],4
       lea       rdx,[rsp+28]
       vmovups   xmm0,[rcx+18]
       vmovups   [rdx+10],xmm0
       vmovups   xmm0,[rcx+28]
       vmovups   [rdx+20],xmm0
       vmovups   xmm0,[rcx+38]
       vmovups   [rdx+30],xmm0
       vmovups   xmm0,[rcx+48]
       vmovups   [rdx+40],xmm0
       test      rax,rax
       je        near ptr M00_L06
       mov       ecx,[rax+8]
       cmp       ecx,4
       jl        near ptr M00_L07
       xor       r8d,r8d
       vmovss    xmm0,dword ptr [7FFC5B165178]
       jmp       short M00_L03
M00_L00:
       cmp       r9d,ecx
       jae       near ptr M00_L08
       mov       r11d,r9d
       shl       r11,4
       vmovups   xmm2,[rax+r11+10]
       vmovaps   xmm3,xmm2
       vdpps     xmm3,xmm3,xmm1,0FF
       vmulps    xmm2,xmm3,xmm2
       vsubps    xmm1,xmm1,xmm2
       inc       r9d
       cmp       r9d,r8d
       jl        short M00_L00
M00_L01:
       vdpps     xmm2,xmm1,xmm1,0FF
       vsqrtss   xmm2,xmm2,xmm2
       vucomiss  xmm2,xmm0
       jbe       short M00_L02
       vbroadcastss xmm2,xmm2
       vdivps    xmm1,xmm1,xmm2
M00_L02:
       vmovups   [rax+r10+10],xmm1
       inc       r8d
       cmp       r8d,4
       jge       short M00_L05
M00_L03:
       mov       r10,r8
       shl       r10,4
       vmovups   xmm1,[rdx+r10+10]
       xor       r9d,r9d
       test      r8d,r8d
       jle       short M00_L01
       cmp       ecx,r8d
       jl        short M00_L00
M00_L04:
       mov       r11d,r9d
       shl       r11,4
       vmovups   xmm2,[rax+r11+10]
       vmovaps   xmm3,xmm2
       vdpps     xmm3,xmm3,xmm1,0FF
       vmulps    xmm2,xmm3,xmm2
       vsubps    xmm1,xmm1,xmm2
       inc       r9d
       cmp       r9d,r8d
       jl        short M00_L04
       jmp       short M00_L01
M00_L05:
       add       rsp,78
       pop       rbx
       pop       rsi
       ret
M00_L06:
       mov       ecx,611
       mov       rdx,7FFC5B3C36E0
       call      qword ptr [7FFC5B307798]
       mov       rcx,rax
       call      qword ptr [7FFC5B597618]
       int       3
M00_L07:
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
       call      qword ptr [7FFC5B50E538]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
M00_L08:
       call      CORINFO_HELP_RNGCHKFAIL
       int       3
; Total bytes of code 434
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Transform()
       sub       rsp,28
       vmovaps   [rsp+10],xmm6
       vmovaps   [rsp],xmm7
       vmovups   xmm0,[rcx+18]
       vmovups   xmm1,[7FFC5B164EE0]
       vpermilps xmm2,xmm1,4E
       vmovshdup xmm3,xmm0
       vbroadcastss xmm3,xmm3
       vmulps    xmm2,xmm3,xmm2
       vmovups   xmm3,[7FFC5B164EF0]
       vpermilps xmm4,xmm1,1B
       vmovaps   xmm5,xmm0
       vbroadcastss xmm5,xmm5
       vmulps    xmm4,xmm5,xmm4
       vmovddup  xmm5,qword ptr [7FFC5B164F00]
       vshufps   xmm6,xmm0,xmm0,0FF
       vbroadcastss xmm6,xmm6
       vmulps    xmm6,xmm6,xmm1
       vfmadd213ps xmm4,xmm5,xmm6
       vfmadd213ps xmm2,xmm3,xmm4
       vpermilps xmm1,xmm1,0B1
       vunpckhps xmm0,xmm0,xmm0
       vbroadcastss xmm0,xmm0
       vmulps    xmm0,xmm0,xmm1
       vmovups   xmm1,[7FFC5B164F10]
       vfmadd231ps xmm2,xmm1,xmm0
       vpermilps xmm0,xmm2,4E
       vbroadcastss xmm4,dword ptr [7FFC5B164EF0]
       vmulps    xmm0,xmm0,xmm4
       vpermilps xmm6,xmm2,1B
       vmulps    xmm6,xmm6,xmm4
       vmulps    xmm7,xmm4,xmm2
       vfmadd213ps xmm6,xmm5,xmm7
       vfmadd213ps xmm0,xmm3,xmm6
       vpermilps xmm2,xmm2,0B1
       vmulps    xmm2,xmm2,xmm4
       vfmadd213ps xmm2,xmm1,xmm0
       vmovups   [rdx],xmm2
       mov       rax,rdx
       vmovaps   xmm6,[rsp+10]
       vmovaps   xmm7,[rsp]
       add       rsp,28
       ret
; Total bytes of code 219
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Operator_Add()
       vmovups   xmm0,[rcx+18]
       vaddps    xmm0,xmm0,[rcx+28]
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 18
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Operator_Subtract()
       vmovups   xmm0,[rcx+18]
       vsubps    xmm0,xmm0,[rcx+28]
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 18
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Operator_Negate()
       vmovups   xmm0,[rcx+18]
       vxorps    xmm0,xmm0,[7FFC5B1642B0]
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 21
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Operator_Multiply()
       vmovups   xmm0,[rcx+18]
       vmulps    xmm0,xmm0,[rcx+28]
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 18
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Operator_Divide()
       vmovups   xmm0,[rcx+18]
       vdivps    xmm0,xmm0,[rcx+28]
       vmovups   [rdx],xmm0
       mov       rax,rdx
       ret
; Total bytes of code 18
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Operator_Equals()
       vmovups   xmm0,[rcx+18]
       vsubps    xmm0,xmm0,[rcx+28]
       vandps    xmm0,xmm0,[7FFC5B164350]
       vcmpltps  xmm0,xmm0,[7FFC5B164360]
       vpcmpeqd  xmm1,xmm1,xmm1
       vptest    xmm0,xmm1
       setb      al
       movzx     eax,al
       ret
; Total bytes of code 43
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.Operator_NotEquals()
       vmovups   xmm0,[rcx+18]
       vsubps    xmm0,xmm0,[rcx+28]
       vandps    xmm0,xmm0,[7FFC5B164350]
       vcmpltps  xmm0,xmm0,[7FFC5B164360]
       vpcmpeqd  xmm1,xmm1,xmm1
       vptest    xmm0,xmm1
       setae     al
       movzx     eax,al
       ret
; Total bytes of code 43
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.Vector4Benchmarks.EqualsStrict()
       vmovups   xmm0,[rcx+18]
       vcmpeqps  xmm0,xmm0,[rcx+28]
       vmovmskps eax,xmm0
       cmp       eax,0F
       sete      al
       movzx     eax,al
       ret
; Total bytes of code 25
```

