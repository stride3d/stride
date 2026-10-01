## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.IsIdentity()
       cmp       [rcx],cl
       add       rcx,14
       jmp       qword ptr [7FFC5B595128]; Stride.Core.Mathematics.Quaternion.get_IsIdentity()
; Total bytes of code 12
```
```assembly
; Stride.Core.Mathematics.Quaternion.get_IsIdentity()
       vxorps    xmm0,xmm0,xmm0
       vsubss    xmm0,xmm0,dword ptr [rcx]
       vandps    xmm0,xmm0,[7FFC5B164460]
       vmovss    xmm1,dword ptr [7FFC5B164470]
       vucomiss  xmm1,xmm0
       ja        short M01_L02
M01_L00:
       xor       eax,eax
M01_L01:
       ret
M01_L02:
       vxorps    xmm0,xmm0,xmm0
       vsubss    xmm0,xmm0,dword ptr [rcx+4]
       vandps    xmm0,xmm0,[7FFC5B164460]
       vmovss    xmm1,dword ptr [7FFC5B164470]
       vucomiss  xmm1,xmm0
       jbe       short M01_L00
       vxorps    xmm0,xmm0,xmm0
       vsubss    xmm0,xmm0,dword ptr [rcx+8]
       vandps    xmm0,xmm0,[7FFC5B164460]
       vmovss    xmm1,dword ptr [7FFC5B164470]
       vucomiss  xmm1,xmm0
       jbe       short M01_L00
       vmovss    xmm0,dword ptr [7FFC5B164474]
       vsubss    xmm0,xmm0,dword ptr [rcx+0C]
       vandps    xmm0,xmm0,[7FFC5B164460]
       vmovss    xmm1,dword ptr [7FFC5B164470]
       vucomiss  xmm1,xmm0
       seta      al
       movzx     eax,al
       jmp       short M01_L01
; Total bytes of code 136
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.IsNormalized()
       add       rcx,14
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
       vsubss    xmm0,xmm0,dword ptr [7FFC5B164080]
       vandps    xmm0,xmm0,[7FFC5B164090]
       vmovss    xmm1,dword ptr [7FFC5B1640A0]
       vucomiss  xmm1,xmm0
       seta      al
       movzx     eax,al
       ret
; Total bytes of code 86
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Angle()
       sub       rsp,28
       vzeroupper
       add       rcx,14
       vmovss    xmm0,dword ptr [rcx]
       vmulss    xmm0,xmm0,xmm0
       vmovss    xmm1,dword ptr [rcx+4]
       vmulss    xmm1,xmm1,xmm1
       vaddss    xmm0,xmm0,xmm1
       vmovss    xmm1,dword ptr [rcx+8]
       vmulss    xmm1,xmm1,xmm1
       vaddss    xmm0,xmm0,xmm1
       vmovss    xmm1,dword ptr [7FFC5B1440B8]
       vucomiss  xmm1,xmm0
       ja        short M00_L01
       vmovss    xmm0,dword ptr [rcx+0C]
       call      00007FFCBAD606C0
       vmulss    xmm0,xmm0,dword ptr [7FFC5B1440BC]
M00_L00:
       add       rsp,28
       ret
M00_L01:
       vxorps    xmm0,xmm0,xmm0
       jmp       short M00_L00
; Total bytes of code 88
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Axis()
       cmp       [rcx],cl
       add       rcx,14
       jmp       qword ptr [7FFC5B5A5128]; Stride.Core.Mathematics.Quaternion.get_Axis()
; Total bytes of code 12
```
```assembly
; Stride.Core.Mathematics.Quaternion.get_Axis()
       push      rbx
       sub       rsp,20
       mov       rbx,rdx
       vmovss    xmm0,dword ptr [rcx]
       vmulss    xmm1,xmm0,xmm0
       vmovss    xmm2,dword ptr [rcx+4]
       vmulss    xmm3,xmm2,xmm2
       vaddss    xmm1,xmm1,xmm3
       vmovss    xmm3,dword ptr [rcx+8]
       vmulss    xmm4,xmm3,xmm3
       vaddss    xmm1,xmm1,xmm4
       vmovss    xmm4,dword ptr [7FFC5B1743F0]
       vucomiss  xmm4,xmm1
       ja        short M01_L01
       vmovss    xmm4,dword ptr [7FFC5B1743F4]
       vdivss    xmm1,xmm4,xmm1
       vmulss    xmm0,xmm0,xmm1
       vmulss    xmm2,xmm2,xmm1
       vmulss    xmm1,xmm3,xmm1
       vmovss    dword ptr [rbx],xmm0
       vmovss    dword ptr [rbx+4],xmm2
       vmovss    dword ptr [rbx+8],xmm1
M01_L00:
       mov       rax,rbx
       add       rsp,20
       pop       rbx
       ret
M01_L01:
       mov       rcx,offset MT_Stride.Core.Mathematics.Vector3
       call      qword ptr [7FFC5B0F5740]; System.Runtime.CompilerServices.StaticsHelpers.GetNonGCStaticBase(System.Runtime.CompilerServices.MethodTable*)
       mov       rax,1B7CD1DD4F0
       mov       rcx,[rax]
       mov       [rbx],rcx
       mov       ecx,[rax+8]
       mov       [rbx+8],ecx
       jmp       short M01_L00
; Total bytes of code 143
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.YawPitchRoll()
       push      rbx
       sub       rsp,0A0
       vzeroupper
       vmovaps   [rsp+90],xmm6
       vmovaps   [rsp+80],xmm7
       vmovaps   [rsp+70],xmm8
       vmovaps   [rsp+60],xmm9
       vmovaps   [rsp+50],xmm10
       vmovaps   [rsp+40],xmm11
       vmovaps   [rsp+30],xmm12
       mov       rbx,rdx
       add       rcx,14
       vmovss    xmm0,dword ptr [rcx]
       vmulss    xmm1,xmm0,xmm0
       vmovss    xmm2,dword ptr [rcx+4]
       vmulss    xmm3,xmm2,xmm2
       vmovss    xmm4,dword ptr [rcx+8]
       vmulss    xmm5,xmm4,xmm4
       vmulss    xmm6,xmm0,xmm2
       vmovss    xmm7,dword ptr [rcx+0C]
       vmulss    xmm8,xmm4,xmm7
       vmulss    xmm9,xmm4,xmm0
       vmulss    xmm10,xmm2,xmm7
       vmulss    xmm2,xmm2,xmm4
       vmulss    xmm0,xmm0,xmm7
       vaddss    xmm4,xmm3,xmm5
       vmovss    xmm7,dword ptr [7FFC5B164A60]
       vmulss    xmm4,xmm4,xmm7
       vmovss    xmm11,dword ptr [7FFC5B164A64]
       vsubss    xmm4,xmm11,xmm4
       vaddss    xmm12,xmm6,xmm8
       vmulss    xmm12,xmm12,xmm7
       vsubss    xmm6,xmm6,xmm8
       vmulss    xmm6,xmm6,xmm7
       vaddss    xmm5,xmm5,xmm1
       vmulss    xmm5,xmm5,xmm7
       vsubss    xmm5,xmm11,xmm5
       vmovss    dword ptr [rsp+2C],xmm5
       vaddss    xmm8,xmm9,xmm10
       vmulss    xmm8,xmm8,xmm7
       vsubss    xmm0,xmm2,xmm0
       vmulss    xmm9,xmm0,xmm7
       vaddss    xmm0,xmm3,xmm1
       vmulss    xmm0,xmm0,xmm7
       vsubss    xmm1,xmm11,xmm0
       vmovss    dword ptr [rsp+28],xmm1
       vandps    xmm0,xmm9,[7FFC5B164A70]
       vsubss    xmm0,xmm0,xmm11
       vandps    xmm0,xmm0,[7FFC5B164A70]
       vmovss    xmm2,dword ptr [7FFC5B164A80]
       vucomiss  xmm2,xmm0
       ja        near ptr M00_L01
       vxorps    xmm0,xmm9,[7FFC5B164A90]
       call      00007FFCBAD606D0
       vmovss    dword ptr [rsp+20],xmm0
       vmovaps   xmm0,xmm8
       vmovss    xmm1,dword ptr [rsp+28]
       call      00007FFCBAD606E0
       vmovss    dword ptr [rsp+24],xmm0
       vmovaps   xmm0,xmm12
       vmovss    xmm1,dword ptr [rsp+2C]
       call      00007FFCBAD606E0
M00_L00:
       vmovss    xmm1,dword ptr [rsp+24]
       vmovss    dword ptr [rbx],xmm1
       vmovss    xmm1,dword ptr [rsp+20]
       vmovss    dword ptr [rbx+4],xmm1
       vmovss    dword ptr [rbx+8],xmm0
       mov       rax,rbx
       vmovaps   xmm6,[rsp+90]
       vmovaps   xmm7,[rsp+80]
       vmovaps   xmm8,[rsp+70]
       vmovaps   xmm9,[rsp+60]
       vmovaps   xmm10,[rsp+50]
       vmovaps   xmm11,[rsp+40]
       vmovaps   xmm12,[rsp+30]
       add       rsp,0A0
       pop       rbx
       ret
M00_L01:
       vxorps    xmm0,xmm0,xmm0
       vucomiss  xmm9,xmm0
       jb        short M00_L03
       vmovss    xmm0,dword ptr [7FFC5B164AA0]
       vmovss    dword ptr [rsp+20],xmm0
       vxorps    xmm0,xmm6,[7FFC5B164A90]
       vmovaps   xmm1,xmm4
       call      00007FFCBAD606E0
M00_L02:
       vxorps    xmm1,xmm1,xmm1
       vmovss    dword ptr [rsp+24],xmm0
       vmovaps   xmm0,xmm1
       jmp       near ptr M00_L00
M00_L03:
       vmovss    xmm0,dword ptr [7FFC5B164AA4]
       vmovss    dword ptr [rsp+20],xmm0
       vxorps    xmm0,xmm6,[7FFC5B164A90]
       vmovaps   xmm1,xmm4
       call      00007FFCBAD606E0
       vxorps    xmm0,xmm0,[7FFC5B164A90]
       jmp       short M00_L02
; Total bytes of code 503
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Conjugate()
       add       rcx,14
       vmovss    xmm0,dword ptr [rcx]
       vxorps    xmm0,xmm0,[7FFC5B144250]
       vmovss    xmm1,dword ptr [rcx+4]
       vxorps    xmm1,xmm1,[7FFC5B144250]
       vmovss    xmm2,dword ptr [rcx+8]
       vxorps    xmm2,xmm2,[7FFC5B144250]
       vmovss    xmm3,dword ptr [rcx+0C]
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       vmovss    dword ptr [rdx+0C],xmm3
       mov       rax,rdx
       ret
; Total bytes of code 70
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Invert()
       vmovss    xmm0,dword ptr [rcx+14]
       vmovss    xmm1,dword ptr [rcx+18]
       vmovss    xmm2,dword ptr [rcx+1C]
       vmovss    xmm3,dword ptr [rcx+20]
       vmulss    xmm4,xmm0,xmm0
       vmulss    xmm5,xmm1,xmm1
       vaddss    xmm4,xmm4,xmm5
       vmulss    xmm5,xmm2,xmm2
       vaddss    xmm4,xmm4,xmm5
       vmulss    xmm5,xmm3,xmm3
       vaddss    xmm4,xmm4,xmm5
       vucomiss  xmm4,dword ptr [7FFC5B1444E0]
       jbe       short M00_L00
       vmovss    xmm5,dword ptr [7FFC5B1444E4]
       vdivss    xmm4,xmm5,xmm4
       vxorps    xmm0,xmm0,[7FFC5B1444F0]
       vmulss    xmm0,xmm0,xmm4
       vxorps    xmm1,xmm1,[7FFC5B1444F0]
       vmulss    xmm1,xmm1,xmm4
       vxorps    xmm2,xmm2,[7FFC5B1444F0]
       vmulss    xmm2,xmm2,xmm4
       vmulss    xmm3,xmm3,xmm4
M00_L00:
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       vmovss    dword ptr [rdx+0C],xmm3
       mov       rax,rdx
       ret
; Total bytes of code 133
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Length()
       add       rcx,14
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
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.LengthSquared()
       add       rcx,14
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
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Normalize()
       vmovss    xmm0,dword ptr [rcx+14]
       vmovss    xmm1,dword ptr [rcx+18]
       vmovss    xmm2,dword ptr [rcx+1C]
       vmovss    xmm3,dword ptr [rcx+20]
       vmulss    xmm4,xmm0,xmm0
       vmulss    xmm5,xmm1,xmm1
       vaddss    xmm4,xmm4,xmm5
       vmulss    xmm5,xmm2,xmm2
       vaddss    xmm4,xmm4,xmm5
       vmulss    xmm5,xmm3,xmm3
       vaddss    xmm4,xmm4,xmm5
       vsqrtss   xmm4,xmm4,xmm4
       vucomiss  xmm4,dword ptr [7FFC5B1443C8]
       jbe       short M00_L00
       vmovss    xmm5,dword ptr [7FFC5B1443CC]
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
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Add()
       sub       rsp,28
       vmovaps   [rsp+10],xmm6
       vmovaps   [rsp],xmm7
       vmovss    xmm0,dword ptr [rcx+14]
       vmovss    xmm1,dword ptr [rcx+18]
       vmovss    xmm2,dword ptr [rcx+1C]
       vmovss    xmm3,dword ptr [rcx+20]
       vmovss    xmm4,dword ptr [rcx+24]
       vmovss    xmm5,dword ptr [rcx+28]
       vmovss    xmm6,dword ptr [rcx+2C]
       vmovss    xmm7,dword ptr [rcx+30]
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
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Subtract()
       sub       rsp,28
       vmovaps   [rsp+10],xmm6
       vmovaps   [rsp],xmm7
       vmovss    xmm0,dword ptr [rcx+14]
       vmovss    xmm1,dword ptr [rcx+18]
       vmovss    xmm2,dword ptr [rcx+1C]
       vmovss    xmm3,dword ptr [rcx+20]
       vmovss    xmm4,dword ptr [rcx+24]
       vmovss    xmm5,dword ptr [rcx+28]
       vmovss    xmm6,dword ptr [rcx+2C]
       vmovss    xmm7,dword ptr [rcx+30]
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
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Multiply()
       sub       rsp,68
       vmovaps   [rsp+50],xmm6
       vmovaps   [rsp+40],xmm7
       vmovaps   [rsp+30],xmm8
       vmovaps   [rsp+20],xmm9
       vmovaps   [rsp+10],xmm10
       vmovaps   [rsp],xmm11
       lea       rax,[rcx+14]
       add       rcx,24
       vmovss    xmm0,dword ptr [rax]
       vmovss    xmm1,dword ptr [rax+4]
       vmovss    xmm2,dword ptr [rax+8]
       vmovss    xmm3,dword ptr [rax+0C]
       vmovss    xmm4,dword ptr [rcx]
       vmovss    xmm5,dword ptr [rcx+4]
       vmovss    xmm6,dword ptr [rcx+8]
       vmovss    xmm7,dword ptr [rcx+0C]
       vmulss    xmm8,xmm4,xmm3
       vmulss    xmm9,xmm0,xmm7
       vaddss    xmm8,xmm8,xmm9
       vmulss    xmm9,xmm5,xmm2
       vaddss    xmm8,xmm8,xmm9
       vmulss    xmm9,xmm6,xmm1
       vsubss    xmm8,xmm8,xmm9
       vmulss    xmm9,xmm5,xmm3
       vmulss    xmm10,xmm1,xmm7
       vaddss    xmm9,xmm9,xmm10
       vmulss    xmm10,xmm6,xmm0
       vaddss    xmm9,xmm9,xmm10
       vmulss    xmm10,xmm4,xmm2
       vsubss    xmm9,xmm9,xmm10
       vmulss    xmm10,xmm6,xmm3
       vmulss    xmm11,xmm2,xmm7
       vaddss    xmm10,xmm10,xmm11
       vmulss    xmm11,xmm4,xmm1
       vaddss    xmm10,xmm10,xmm11
       vmulss    xmm11,xmm5,xmm0
       vsubss    xmm10,xmm10,xmm11
       vmulss    xmm0,xmm4,xmm0
       vmulss    xmm1,xmm5,xmm1
       vaddss    xmm0,xmm0,xmm1
       vmulss    xmm1,xmm6,xmm2
       vaddss    xmm0,xmm0,xmm1
       vmulss    xmm1,xmm7,xmm3
       vsubss    xmm0,xmm1,xmm0
       vmovss    dword ptr [rdx],xmm8
       vmovss    dword ptr [rdx+4],xmm9
       vmovss    dword ptr [rdx+8],xmm10
       vmovss    dword ptr [rdx+0C],xmm0
       mov       rax,rdx
       vmovaps   xmm6,[rsp+50]
       vmovaps   xmm7,[rsp+40]
       vmovaps   xmm8,[rsp+30]
       vmovaps   xmm9,[rsp+20]
       vmovaps   xmm10,[rsp+10]
       vmovaps   xmm11,[rsp]
       add       rsp,68
       ret
; Total bytes of code 268
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Negate()
       vmovss    xmm0,dword ptr [rcx+14]
       vmovss    xmm1,dword ptr [rcx+18]
       vmovss    xmm2,dword ptr [rcx+1C]
       vmovss    xmm3,dword ptr [rcx+20]
       vxorps    xmm0,xmm0,[7FFC5B154270]
       vxorps    xmm1,xmm1,[7FFC5B154270]
       vxorps    xmm2,xmm2,[7FFC5B154270]
       vxorps    xmm3,xmm3,[7FFC5B154270]
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
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Barycentric()
       push      rsi
       push      rbx
       sub       rsp,118
       vzeroupper
       vmovaps   [rsp+100],xmm6
       vmovaps   [rsp+0F0],xmm7
       vmovaps   [rsp+0E0],xmm8
       vmovaps   [rsp+0D0],xmm9
       vmovaps   [rsp+0C0],xmm10
       vmovaps   [rsp+0B0],xmm11
       vmovaps   [rsp+0A0],xmm12
       vmovaps   [rsp+90],xmm13
       vmovaps   [rsp+80],xmm14
       vmovaps   [rsp+70],xmm15
       mov       rbx,rdx
       vmovss    xmm6,dword ptr [rcx+14]
       vmovss    xmm7,dword ptr [rcx+18]
       vmovss    xmm8,dword ptr [rcx+1C]
       vmovss    xmm9,dword ptr [rcx+20]
       vmovss    xmm10,dword ptr [rcx+24]
       vmovss    xmm11,dword ptr [rcx+28]
       vmovss    xmm12,dword ptr [rcx+2C]
       vmovss    xmm13,dword ptr [rcx+30]
       vmovss    xmm14,dword ptr [rcx+34]
       vmovss    xmm15,dword ptr [rcx+38]
       vmovss    dword ptr [rsp+34],xmm15
       vmovss    xmm0,dword ptr [rcx+3C]
       vmovss    dword ptr [rsp+30],xmm0
       vmovss    xmm1,dword ptr [rcx+40]
       vmovss    dword ptr [rsp+2C],xmm1
       vmovss    xmm2,dword ptr [rcx+8]
       vmovss    xmm3,dword ptr [rcx+0C]
       vmovss    dword ptr [rsp+6C],xmm3
       vaddss    xmm2,xmm2,xmm3
       vmovss    dword ptr [rsp+24],xmm2
       vmovss    dword ptr [rsp+5C],xmm2
       vmulss    xmm5,xmm6,xmm10
       vmulss    xmm15,xmm7,xmm11
       vaddss    xmm5,xmm5,xmm15
       vmulss    xmm15,xmm8,xmm12
       vaddss    xmm5,xmm5,xmm15
       vmulss    xmm15,xmm9,xmm13
       vaddss    xmm5,xmm5,xmm15
       vmovss    dword ptr [rsp+68],xmm5
       vandps    xmm15,xmm5,[7FFC5B1351C0]
       vucomiss  xmm15,dword ptr [7FFC5B1351D0]
       ja        near ptr M00_L12
       vmovaps   xmm0,xmm15
       call      00007FFCBAD606C0
       vmovss    dword ptr [rsp+64],xmm0
       call      00007FFCBAD607C0
       vmovss    xmm15,dword ptr [7FFC5B1351D4]
       vdivss    xmm0,xmm15,xmm0
       vmovss    dword ptr [rsp+60],xmm0
       vsubss    xmm2,xmm15,dword ptr [rsp+5C]
       vmovss    dword ptr [rsp+28],xmm2
       vmovss    xmm3,dword ptr [rsp+64]
       vmulss    xmm0,xmm2,xmm3
       call      00007FFCBAD607C0
       vmulss    xmm0,xmm0,dword ptr [rsp+60]
       vmovss    dword ptr [rsp+58],xmm0
       vmovss    xmm3,dword ptr [rsp+5C]
       vmulss    xmm0,xmm3,dword ptr [rsp+64]
       call      00007FFCBAD607C0
       vmulss    xmm0,xmm0,dword ptr [rsp+60]
       vxorps    xmm1,xmm1,xmm1
       vmovss    xmm5,dword ptr [rsp+68]
       vucomiss  xmm1,xmm5
       ja        short M00_L01
       vxorps    xmm1,xmm1,xmm1
       vucomiss  xmm5,xmm1
       ja        short M00_L00
       vxorps    xmm1,xmm1,xmm1
       vucomiss  xmm5,xmm1
       jp        near ptr M00_L13
       jne       near ptr M00_L13
       xor       esi,esi
       jmp       short M00_L02
M00_L00:
       mov       esi,1
       jmp       short M00_L02
M00_L01:
       mov       esi,0FFFFFFFF
M00_L02:
       vxorps    xmm1,xmm1,xmm1
       vcvtsi2ss xmm1,xmm1,esi
       vmulss    xmm0,xmm0,xmm1
M00_L03:
       vmovss    xmm1,dword ptr [rsp+58]
       vmulss    xmm2,xmm1,xmm6
       vmulss    xmm3,xmm0,xmm10
       vaddss    xmm10,xmm2,xmm3
       vmulss    xmm2,xmm1,xmm7
       vmulss    xmm3,xmm0,xmm11
       vaddss    xmm11,xmm2,xmm3
       vmulss    xmm2,xmm1,xmm8
       vmulss    xmm3,xmm0,xmm12
       vaddss    xmm12,xmm2,xmm3
       vmulss    xmm1,xmm1,xmm9
       vmulss    xmm0,xmm0,xmm13
       vaddss    xmm13,xmm1,xmm0
       vmovss    xmm1,dword ptr [rsp+24]
       vmovss    dword ptr [rsp+48],xmm1
       vmulss    xmm0,xmm6,xmm14
       vmulss    xmm4,xmm7,dword ptr [rsp+34]
       vaddss    xmm0,xmm0,xmm4
       vmulss    xmm5,xmm8,dword ptr [rsp+30]
       vaddss    xmm0,xmm0,xmm5
       vmulss    xmm5,xmm9,dword ptr [rsp+2C]
       vaddss    xmm0,xmm0,xmm5
       vmovss    dword ptr [rsp+54],xmm0
       vandps    xmm5,xmm0,[7FFC5B1351C0]
       vucomiss  xmm5,dword ptr [7FFC5B1351D0]
       ja        near ptr M00_L14
       vmovaps   xmm0,xmm5
       call      00007FFCBAD606C0
       vmovss    dword ptr [rsp+50],xmm0
       call      00007FFCBAD607C0
       vdivss    xmm0,xmm15,xmm0
       vmovss    dword ptr [rsp+4C],xmm0
       vmovss    xmm1,dword ptr [rsp+50]
       vmulss    xmm0,xmm1,dword ptr [rsp+28]
       call      00007FFCBAD607C0
       vmulss    xmm0,xmm0,dword ptr [rsp+4C]
       vmovss    dword ptr [rsp+44],xmm0
       vmovss    xmm3,dword ptr [rsp+48]
       vmulss    xmm0,xmm3,dword ptr [rsp+50]
       call      00007FFCBAD607C0
       vmulss    xmm0,xmm0,dword ptr [rsp+4C]
       vxorps    xmm1,xmm1,xmm1
       vmovss    xmm2,dword ptr [rsp+54]
       vucomiss  xmm1,xmm2
       ja        short M00_L05
       vxorps    xmm1,xmm1,xmm1
       vucomiss  xmm2,xmm1
       ja        short M00_L04
       vxorps    xmm1,xmm1,xmm1
       vucomiss  xmm2,xmm1
       jp        near ptr M00_L15
       jne       near ptr M00_L15
       xor       esi,esi
       jmp       short M00_L06
M00_L04:
       mov       esi,1
       jmp       short M00_L06
M00_L05:
       mov       esi,0FFFFFFFF
M00_L06:
       vxorps    xmm1,xmm1,xmm1
       vcvtsi2ss xmm1,xmm1,esi
       vmulss    xmm0,xmm0,xmm1
M00_L07:
       vmovss    xmm5,dword ptr [rsp+44]
       vmulss    xmm1,xmm5,xmm6
       vmulss    xmm2,xmm0,xmm14
       vaddss    xmm6,xmm1,xmm2
       vmulss    xmm1,xmm5,xmm7
       vmulss    xmm2,xmm0,dword ptr [rsp+34]
       vaddss    xmm7,xmm1,xmm2
       vmulss    xmm1,xmm5,xmm8
       vmulss    xmm2,xmm0,dword ptr [rsp+30]
       vaddss    xmm8,xmm1,xmm2
       vmulss    xmm1,xmm5,xmm9
       vmulss    xmm0,xmm0,dword ptr [rsp+2C]
       vaddss    xmm9,xmm1,xmm0
       vmovss    xmm14,dword ptr [rsp+6C]
       vdivss    xmm14,xmm14,[rsp+24]
       vmulss    xmm0,xmm10,xmm6
       vmulss    xmm1,xmm11,xmm7
       vaddss    xmm0,xmm0,xmm1
       vmulss    xmm1,xmm12,xmm8
       vaddss    xmm0,xmm0,xmm1
       vmulss    xmm1,xmm13,xmm9
       vaddss    xmm0,xmm0,xmm1
       vmovss    dword ptr [rsp+40],xmm0
       vandps    xmm1,xmm0,[7FFC5B1351C0]
       vucomiss  xmm1,dword ptr [7FFC5B1351D0]
       ja        near ptr M00_L16
       vmovaps   xmm0,xmm1
       call      00007FFCBAD606C0
       vmovss    dword ptr [rsp+3C],xmm0
       call      00007FFCBAD607C0
       vdivss    xmm0,xmm15,xmm0
       vmovss    dword ptr [rsp+38],xmm0
       vsubss    xmm1,xmm15,xmm14
       vmulss    xmm0,xmm1,dword ptr [rsp+3C]
       call      00007FFCBAD607C0
       vmulss    xmm15,xmm0,dword ptr [rsp+38]
       vmulss    xmm0,xmm14,dword ptr [rsp+3C]
       call      00007FFCBAD607C0
       vmulss    xmm14,xmm0,dword ptr [rsp+38]
       vxorps    xmm0,xmm0,xmm0
       vmovss    xmm1,dword ptr [rsp+40]
       vucomiss  xmm0,xmm1
       ja        short M00_L09
       vxorps    xmm0,xmm0,xmm0
       vucomiss  xmm1,xmm0
       ja        short M00_L08
       vxorps    xmm0,xmm0,xmm0
       vucomiss  xmm1,xmm0
       jp        near ptr M00_L17
       jne       near ptr M00_L17
       xor       esi,esi
       jmp       short M00_L10
M00_L08:
       mov       esi,1
       jmp       short M00_L10
M00_L09:
       mov       esi,0FFFFFFFF
M00_L10:
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2ss xmm0,xmm0,esi
       vmulss    xmm14,xmm14,xmm0
M00_L11:
       vmulss    xmm0,xmm15,xmm10
       vmulss    xmm1,xmm14,xmm6
       vaddss    xmm0,xmm0,xmm1
       vmulss    xmm1,xmm15,xmm11
       vmulss    xmm2,xmm14,xmm7
       vaddss    xmm1,xmm1,xmm2
       vmulss    xmm2,xmm15,xmm12
       vmulss    xmm3,xmm14,xmm8
       vaddss    xmm2,xmm2,xmm3
       vmulss    xmm3,xmm15,xmm13
       vmulss    xmm4,xmm14,xmm9
       vaddss    xmm3,xmm3,xmm4
       vmovss    dword ptr [rbx],xmm0
       vmovss    dword ptr [rbx+4],xmm1
       vmovss    dword ptr [rbx+8],xmm2
       vmovss    dword ptr [rbx+0C],xmm3
       mov       rax,rbx
       vmovaps   xmm6,[rsp+100]
       vmovaps   xmm7,[rsp+0F0]
       vmovaps   xmm8,[rsp+0E0]
       vmovaps   xmm9,[rsp+0D0]
       vmovaps   xmm10,[rsp+0C0]
       vmovaps   xmm11,[rsp+0B0]
       vmovaps   xmm12,[rsp+0A0]
       vmovaps   xmm13,[rsp+90]
       vmovaps   xmm14,[rsp+80]
       vmovaps   xmm15,[rsp+70]
       add       rsp,118
       pop       rbx
       pop       rsi
       ret
M00_L12:
       vmovss    xmm5,dword ptr [rsp+68]
       vmovss    xmm15,dword ptr [7FFC5B1351D4]
       vsubss    xmm1,xmm15,dword ptr [rsp+5C]
       vmovss    dword ptr [rsp+28],xmm1
       vmovss    dword ptr [rsp+58],xmm1
       vmovaps   xmm0,xmm5
       call      qword ptr [7FFC5B5650B0]; System.Math.Sign(Single)
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2ss xmm0,xmm0,eax
       vmulss    xmm0,xmm0,dword ptr [rsp+5C]
       jmp       near ptr M00_L03
M00_L13:
       mov       rcx,offset MT_System.ArithmeticException
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       call      qword ptr [7FFC5B5674B0]
       mov       rdx,rax
       mov       rcx,rsi
       call      qword ptr [7FFC5B5674C8]
       mov       rcx,rsi
       call      CORINFO_HELP_THROW
       int       3
M00_L14:
       vmovss    xmm5,dword ptr [rsp+28]
       vmovss    dword ptr [rsp+44],xmm5
       vmovss    xmm0,dword ptr [rsp+54]
       call      qword ptr [7FFC5B5650B0]; System.Math.Sign(Single)
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2ss xmm0,xmm0,eax
       vmulss    xmm0,xmm0,dword ptr [rsp+48]
       jmp       near ptr M00_L07
M00_L15:
       mov       rcx,offset MT_System.ArithmeticException
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       call      qword ptr [7FFC5B5674B0]
       mov       rdx,rax
       mov       rcx,rsi
       call      qword ptr [7FFC5B5674C8]
       mov       rcx,rsi
       call      CORINFO_HELP_THROW
       int       3
M00_L16:
       vsubss    xmm15,xmm15,xmm14
       vmovss    xmm0,dword ptr [rsp+40]
       call      qword ptr [7FFC5B5650B0]; System.Math.Sign(Single)
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2ss xmm0,xmm0,eax
       vmulss    xmm14,xmm0,xmm14
       jmp       near ptr M00_L11
M00_L17:
       mov       rcx,offset MT_System.ArithmeticException
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       call      qword ptr [7FFC5B5674B0]
       mov       rdx,rax
       mov       rcx,rsi
       call      qword ptr [7FFC5B5674C8]
       mov       rcx,rsi
       call      CORINFO_HELP_THROW
       int       3
; Total bytes of code 1464
```
```assembly
; System.Math.Sign(Single)
       push      rbx
       sub       rsp,20
       vxorps    xmm1,xmm1,xmm1
       vucomiss  xmm1,xmm0
       ja        short M01_L01
       vxorps    xmm1,xmm1,xmm1
       vucomiss  xmm0,xmm1
       ja        short M01_L00
       vxorps    xmm1,xmm1,xmm1
       vucomiss  xmm0,xmm1
       jp        short M01_L02
       jne       short M01_L02
       mov       rcx,7FFC5B5B7080
       call      CORINFO_HELP_COUNTPROFILE32
       xor       eax,eax
       add       rsp,20
       pop       rbx
       ret
M01_L00:
       mov       rcx,7FFC5B5B707C
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,1
       add       rsp,20
       pop       rbx
       ret
M01_L01:
       mov       rcx,7FFC5B5B7078
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,0FFFFFFFF
       add       rsp,20
       pop       rbx
       ret
M01_L02:
       mov       rcx,offset MT_System.ArithmeticException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       call      qword ptr [7FFC5B5674B0]
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FFC5B5674C8]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
; Total bytes of code 157
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Dot()
       lea       rax,[rcx+14]
       add       rcx,24
       vmovss    xmm0,dword ptr [rax]
       vmulss    xmm0,xmm0,dword ptr [rcx]
       vmovss    xmm1,dword ptr [rax+4]
       vmulss    xmm1,xmm1,dword ptr [rcx+4]
       vaddss    xmm0,xmm0,xmm1
       vmovss    xmm1,dword ptr [rax+8]
       vmulss    xmm1,xmm1,dword ptr [rcx+8]
       vaddss    xmm0,xmm0,xmm1
       vmovss    xmm1,dword ptr [rax+0C]
       vmulss    xmm1,xmm1,dword ptr [rcx+0C]
       vaddss    xmm0,xmm0,xmm1
       ret
; Total bytes of code 59
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.AngleBetween()
       sub       rsp,28
       vzeroupper
       lea       rax,[rcx+14]
       add       rcx,24
       vmovss    xmm0,dword ptr [rax]
       vmulss    xmm0,xmm0,dword ptr [rcx]
       vmovss    xmm1,dword ptr [rax+4]
       vmulss    xmm1,xmm1,dword ptr [rcx+4]
       vaddss    xmm0,xmm0,xmm1
       vmovss    xmm1,dword ptr [rax+8]
       vmulss    xmm1,xmm1,dword ptr [rcx+8]
       vaddss    xmm0,xmm0,xmm1
       vmovss    xmm1,dword ptr [rax+0C]
       vmulss    xmm1,xmm1,dword ptr [rcx+0C]
       vaddss    xmm0,xmm0,xmm1
       vandps    xmm0,xmm0,[7FFC5B1441B0]
       vbroadcastss xmm1,dword ptr [7FFC5B1441C0]
       vminss    xmm0,xmm1,xmm0
       call      00007FFCBAD606C0
       vmulss    xmm0,xmm0,dword ptr [7FFC5B1441C4]
       add       rsp,28
       ret
; Total bytes of code 104
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Exponential()
       push      rbx
       sub       rsp,60
       vzeroupper
       vmovaps   [rsp+50],xmm6
       vmovaps   [rsp+40],xmm7
       vmovaps   [rsp+30],xmm8
       vmovaps   [rsp+20],xmm9
       mov       rbx,rdx
       vmovss    xmm6,dword ptr [rcx+14]
       vmovss    xmm7,dword ptr [rcx+18]
       vmovss    xmm8,dword ptr [rcx+1C]
       vmulss    xmm0,xmm6,xmm6
       vmulss    xmm1,xmm7,xmm7
       vaddss    xmm0,xmm0,xmm1
       vmulss    xmm1,xmm8,xmm8
       vaddss    xmm9,xmm0,xmm1
       vsqrtss   xmm9,xmm9,xmm9
       vmovaps   xmm0,xmm9
       call      00007FFCBAD607C0
       vandps    xmm1,xmm0,[7FFC5B174410]
       vucomiss  xmm1,dword ptr [7FFC5B174420]
       jb        short M00_L01
       vdivss    xmm0,xmm0,xmm9
       vmulss    xmm6,xmm0,xmm6
       vmulss    xmm7,xmm0,xmm7
       vmulss    xmm8,xmm0,xmm8
M00_L00:
       vmovaps   xmm0,xmm9
       call      00007FFCBAD60710
       vmovss    dword ptr [rbx],xmm6
       vmovss    dword ptr [rbx+4],xmm7
       vmovss    dword ptr [rbx+8],xmm8
       vmovss    dword ptr [rbx+0C],xmm0
       mov       rax,rbx
       vmovaps   xmm6,[rsp+50]
       vmovaps   xmm7,[rsp+40]
       vmovaps   xmm8,[rsp+30]
       vmovaps   xmm9,[rsp+20]
       add       rsp,60
       pop       rbx
       ret
M00_L01:
       jmp       short M00_L00
; Total bytes of code 186
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Lerp()
       sub       rsp,78
       vmovaps   [rsp+60],xmm6
       vmovaps   [rsp+50],xmm7
       vmovaps   [rsp+40],xmm8
       vmovaps   [rsp+30],xmm9
       vmovaps   [rsp+20],xmm10
       vmovaps   [rsp+10],xmm11
       vmovaps   [rsp],xmm12
       vmovss    xmm0,dword ptr [rcx+14]
       vmovss    xmm1,dword ptr [rcx+18]
       vmovss    xmm2,dword ptr [rcx+1C]
       vmovss    xmm3,dword ptr [rcx+20]
       vmovss    xmm4,dword ptr [rcx+24]
       vmovss    xmm5,dword ptr [rcx+28]
       vmovss    xmm6,dword ptr [rcx+2C]
       vmovss    xmm7,dword ptr [rcx+30]
       vmovss    xmm8,dword ptr [rcx+8]
       vmovss    xmm9,dword ptr [7FFC5B1349F0]
       vsubss    xmm10,xmm9,xmm8
       vmulss    xmm11,xmm0,xmm4
       vmulss    xmm12,xmm1,xmm5
       vaddss    xmm11,xmm11,xmm12
       vmulss    xmm12,xmm2,xmm6
       vaddss    xmm11,xmm11,xmm12
       vmulss    xmm12,xmm3,xmm7
       vaddss    xmm11,xmm11,xmm12
       vxorps    xmm12,xmm12,xmm12
       vucomiss  xmm11,xmm12
       jb        near ptr M00_L02
       vmulss    xmm0,xmm10,xmm0
       vmulss    xmm4,xmm8,xmm4
       vaddss    xmm0,xmm0,xmm4
       vmulss    xmm1,xmm10,xmm1
       vmulss    xmm5,xmm8,xmm5
       vaddss    xmm1,xmm1,xmm5
       vmulss    xmm2,xmm10,xmm2
       vmulss    xmm4,xmm8,xmm6
       vaddss    xmm2,xmm2,xmm4
       vmulss    xmm3,xmm10,xmm3
       vmulss    xmm4,xmm8,xmm7
       vaddss    xmm3,xmm3,xmm4
M00_L00:
       vmulss    xmm4,xmm0,xmm0
       vmulss    xmm5,xmm1,xmm1
       vaddss    xmm4,xmm4,xmm5
       vmulss    xmm5,xmm2,xmm2
       vaddss    xmm4,xmm4,xmm5
       vmulss    xmm5,xmm3,xmm3
       vaddss    xmm4,xmm4,xmm5
       vsqrtss   xmm4,xmm4,xmm4
       vucomiss  xmm4,dword ptr [7FFC5B1349F4]
       jbe       short M00_L01
       vdivss    xmm4,xmm9,xmm4
       vmulss    xmm0,xmm0,xmm4
       vmulss    xmm1,xmm1,xmm4
       vmulss    xmm2,xmm2,xmm4
       vmulss    xmm3,xmm3,xmm4
M00_L01:
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       vmovss    dword ptr [rdx+0C],xmm3
       mov       rax,rdx
       vmovaps   xmm6,[rsp+60]
       vmovaps   xmm7,[rsp+50]
       vmovaps   xmm8,[rsp+40]
       vmovaps   xmm9,[rsp+30]
       vmovaps   xmm10,[rsp+20]
       vmovaps   xmm11,[rsp+10]
       vmovaps   xmm12,[rsp]
       add       rsp,78
       ret
M00_L02:
       vmulss    xmm0,xmm10,xmm0
       vmulss    xmm4,xmm8,xmm4
       vsubss    xmm0,xmm0,xmm4
       vmulss    xmm1,xmm10,xmm1
       vmulss    xmm4,xmm8,xmm5
       vsubss    xmm1,xmm1,xmm4
       vmulss    xmm2,xmm10,xmm2
       vmulss    xmm4,xmm8,xmm6
       vsubss    xmm2,xmm2,xmm4
       vmulss    xmm3,xmm10,xmm3
       vmulss    xmm4,xmm8,xmm7
       vsubss    xmm3,xmm3,xmm4
       jmp       near ptr M00_L00
; Total bytes of code 381
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Logarithm()
       push      rbx
       sub       rsp,60
       vzeroupper
       vmovaps   [rsp+50],xmm6
       vmovaps   [rsp+40],xmm7
       vmovaps   [rsp+30],xmm8
       mov       rbx,rdx
       vmovss    xmm6,dword ptr [rcx+14]
       vmovss    xmm7,dword ptr [rcx+18]
       vmovss    xmm8,dword ptr [rcx+1C]
       vmovss    xmm0,dword ptr [rcx+20]
       vandps    xmm1,xmm0,[7FFC5B144410]
       vmovss    xmm2,dword ptr [7FFC5B144420]
       vucomiss  xmm2,xmm1
       jbe       short M00_L01
       call      00007FFCBAD606C0
       vmovss    dword ptr [rsp+2C],xmm0
       call      00007FFCBAD607C0
       vandps    xmm1,xmm0,[7FFC5B144410]
       vucomiss  xmm1,dword ptr [7FFC5B144424]
       jb        short M00_L01
       vmovss    xmm1,dword ptr [rsp+2C]
       vdivss    xmm0,xmm1,xmm0
       vmulss    xmm6,xmm6,xmm0
       vmulss    xmm7,xmm7,xmm0
       vmulss    xmm8,xmm8,xmm0
M00_L00:
       vmovss    dword ptr [rbx],xmm6
       vmovss    dword ptr [rbx+4],xmm7
       vmovss    dword ptr [rbx+8],xmm8
       xor       eax,eax
       mov       [rbx+0C],eax
       mov       rax,rbx
       vmovaps   xmm6,[rsp+50]
       vmovaps   xmm7,[rsp+40]
       vmovaps   xmm8,[rsp+30]
       add       rsp,60
       pop       rbx
       ret
M00_L01:
       jmp       short M00_L00
; Total bytes of code 175
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.RotationX()
       push      rbx
       sub       rsp,40
       vzeroupper
       vmovaps   [rsp+30],xmm6
       mov       rbx,rdx
       vmovss    xmm0,dword ptr [rcx+8]
       vmulss    xmm0,xmm0,dword ptr [7FFC5B174318]
       vmovss    dword ptr [rsp+2C],xmm0
       call      00007FFCBAD607C0
       vmovaps   xmm6,xmm0
       vmovss    xmm0,dword ptr [rsp+2C]
       call      00007FFCBAD60710
       vmovss    dword ptr [rbx],xmm6
       xor       eax,eax
       mov       [rbx+4],rax
       vmovss    dword ptr [rbx+0C],xmm0
       mov       rax,rbx
       vmovaps   xmm6,[rsp+30]
       add       rsp,40
       pop       rbx
       ret
; Total bytes of code 86
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.RotationY()
       push      rbx
       sub       rsp,40
       vzeroupper
       vmovaps   [rsp+30],xmm6
       mov       rbx,rdx
       vmovss    xmm0,dword ptr [rcx+0C]
       vmulss    xmm0,xmm0,dword ptr [7FFC5B134318]
       vmovss    dword ptr [rsp+2C],xmm0
       call      00007FFCBAD607C0
       vmovaps   xmm6,xmm0
       vmovss    xmm0,dword ptr [rsp+2C]
       call      00007FFCBAD60710
       vmovss    dword ptr [rbx],xmm6
       xor       eax,eax
       mov       [rbx+4],rax
       vmovss    dword ptr [rbx+0C],xmm0
       mov       rax,rbx
       vmovaps   xmm6,[rsp+30]
       add       rsp,40
       pop       rbx
       ret
; Total bytes of code 86
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.RotationZ()
       push      rbx
       sub       rsp,40
       vzeroupper
       vmovaps   [rsp+30],xmm6
       mov       rbx,rdx
       vmovss    xmm0,dword ptr [rcx+10]
       vmulss    xmm0,xmm0,dword ptr [7FFC5B134318]
       vmovss    dword ptr [rsp+2C],xmm0
       call      00007FFCBAD607C0
       vmovaps   xmm6,xmm0
       vmovss    xmm0,dword ptr [rsp+2C]
       call      00007FFCBAD60710
       vmovss    dword ptr [rbx],xmm6
       xor       eax,eax
       mov       [rbx+4],rax
       vmovss    dword ptr [rbx+0C],xmm0
       mov       rax,rbx
       vmovaps   xmm6,[rsp+30]
       add       rsp,40
       pop       rbx
       ret
; Total bytes of code 86
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.RotationYawPitchRoll()
       push      rbx
       sub       rsp,60
       vzeroupper
       vmovaps   [rsp+50],xmm6
       vmovaps   [rsp+40],xmm7
       mov       rbx,rdx
       vmovss    xmm0,dword ptr [rcx+8]
       vmovss    xmm1,dword ptr [rcx+0C]
       vmovss    xmm2,dword ptr [rcx+10]
       vmovss    xmm3,dword ptr [7FFC5B134548]
       vmulss    xmm1,xmm1,xmm3
       vmovss    dword ptr [rsp+3C],xmm1
       vmulss    xmm0,xmm0,xmm3
       vmovss    dword ptr [rsp+38],xmm0
       vmulss    xmm6,xmm2,xmm3
       vmovaps   xmm0,xmm6
       call      00007FFCBAD607C0
       vmovaps   xmm7,xmm0
       vmovaps   xmm0,xmm6
       call      00007FFCBAD60710
       vmovaps   xmm6,xmm0
       vmovss    xmm0,dword ptr [rsp+3C]
       call      00007FFCBAD607C0
       vmovss    dword ptr [rsp+34],xmm0
       vmovss    xmm0,dword ptr [rsp+3C]
       call      00007FFCBAD60710
       vmovss    dword ptr [rsp+30],xmm0
       vmovss    xmm0,dword ptr [rsp+38]
       call      00007FFCBAD607C0
       vmovss    dword ptr [rsp+2C],xmm0
       vmovss    xmm0,dword ptr [rsp+38]
       call      00007FFCBAD60710
       vmovss    xmm1,dword ptr [rsp+30]
       vmulss    xmm2,xmm0,xmm1
       vmovss    xmm3,dword ptr [rsp+2C]
       vmovss    xmm4,dword ptr [rsp+34]
       vmulss    xmm5,xmm3,xmm4
       vmulss    xmm0,xmm0,xmm4
       vmulss    xmm4,xmm0,xmm6
       vmulss    xmm1,xmm3,xmm1
       vmulss    xmm3,xmm1,xmm7
       vaddss    xmm3,xmm4,xmm3
       vmulss    xmm1,xmm1,xmm6
       vmulss    xmm0,xmm0,xmm7
       vsubss    xmm0,xmm1,xmm0
       vmulss    xmm1,xmm2,xmm7
       vmulss    xmm4,xmm5,xmm6
       vsubss    xmm1,xmm1,xmm4
       vmulss    xmm2,xmm2,xmm6
       vmulss    xmm4,xmm5,xmm7
       vaddss    xmm2,xmm2,xmm4
       vmovss    dword ptr [rbx],xmm3
       vmovss    dword ptr [rbx+4],xmm0
       vmovss    dword ptr [rbx+8],xmm1
       vmovss    dword ptr [rbx+0C],xmm2
       mov       rax,rbx
       vmovaps   xmm6,[rsp+50]
       vmovaps   xmm7,[rsp+40]
       add       rsp,60
       pop       rbx
       ret
; Total bytes of code 280
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Slerp()
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,78
       vzeroupper
       vmovaps   [rsp+60],xmm6
       vmovaps   [rsp+50],xmm7
       vmovaps   [rsp+40],xmm8
       vmovaps   [rsp+30],xmm9
       mov       rbx,rdx
       lea       rsi,[rcx+14]
       lea       rdi,[rcx+24]
       vmovss    xmm6,dword ptr [rcx+8]
       vmovss    xmm0,dword ptr [rsi]
       vmulss    xmm0,xmm0,dword ptr [rdi]
       vmovss    xmm1,dword ptr [rsi+4]
       vmulss    xmm1,xmm1,dword ptr [rdi+4]
       vaddss    xmm0,xmm0,xmm1
       vmovss    xmm1,dword ptr [rsi+8]
       vmulss    xmm1,xmm1,dword ptr [rdi+8]
       vaddss    xmm0,xmm0,xmm1
       vmovss    xmm1,dword ptr [rsi+0C]
       vmulss    xmm1,xmm1,dword ptr [rdi+0C]
       vaddss    xmm0,xmm0,xmm1
       vmovss    dword ptr [rsp+2C],xmm0
       vandps    xmm1,xmm0,[7FFC5B164860]
       vucomiss  xmm1,dword ptr [7FFC5B164870]
       ja        near ptr M00_L04
       vmovaps   xmm0,xmm1
       call      00007FFCBAD606C0
       vmovss    dword ptr [rsp+28],xmm0
       call      00007FFCBAD607C0
       vmovss    xmm7,dword ptr [7FFC5B164874]
       vdivss    xmm8,xmm7,xmm0
       vsubss    xmm0,xmm7,xmm6
       vmulss    xmm0,xmm0,dword ptr [rsp+28]
       call      00007FFCBAD607C0
       vmulss    xmm9,xmm0,xmm8
       vmulss    xmm0,xmm6,dword ptr [rsp+28]
       call      00007FFCBAD607C0
       vmulss    xmm7,xmm0,xmm8
       vxorps    xmm0,xmm0,xmm0
       vmovss    xmm1,dword ptr [rsp+2C]
       vucomiss  xmm0,xmm1
       ja        short M00_L01
       vxorps    xmm0,xmm0,xmm0
       vucomiss  xmm1,xmm0
       ja        short M00_L00
       vxorps    xmm0,xmm0,xmm0
       vucomiss  xmm1,xmm0
       jp        near ptr M00_L05
       jne       near ptr M00_L05
       xor       ebp,ebp
       jmp       short M00_L02
M00_L00:
       mov       ebp,1
       jmp       short M00_L02
M00_L01:
       mov       ebp,0FFFFFFFF
M00_L02:
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2ss xmm0,xmm0,ebp
       vmulss    xmm7,xmm7,xmm0
M00_L03:
       vmulss    xmm0,xmm9,dword ptr [rsi]
       vmulss    xmm1,xmm7,dword ptr [rdi]
       vaddss    xmm0,xmm0,xmm1
       vmulss    xmm1,xmm9,dword ptr [rsi+4]
       vmulss    xmm2,xmm7,dword ptr [rdi+4]
       vaddss    xmm1,xmm1,xmm2
       vmulss    xmm2,xmm9,dword ptr [rsi+8]
       vmulss    xmm3,xmm7,dword ptr [rdi+8]
       vaddss    xmm2,xmm2,xmm3
       vmulss    xmm3,xmm9,dword ptr [rsi+0C]
       vmulss    xmm4,xmm7,dword ptr [rdi+0C]
       vaddss    xmm3,xmm3,xmm4
       vmovss    dword ptr [rbx],xmm0
       vmovss    dword ptr [rbx+4],xmm1
       vmovss    dword ptr [rbx+8],xmm2
       vmovss    dword ptr [rbx+0C],xmm3
       mov       rax,rbx
       vmovaps   xmm6,[rsp+60]
       vmovaps   xmm7,[rsp+50]
       vmovaps   xmm8,[rsp+40]
       vmovaps   xmm9,[rsp+30]
       add       rsp,78
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       ret
M00_L04:
       vmovss    xmm7,dword ptr [7FFC5B164874]
       vsubss    xmm9,xmm7,xmm6
       vmovss    xmm0,dword ptr [rsp+2C]
       call      qword ptr [7FFC5B595170]; System.Math.Sign(Single)
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2ss xmm0,xmm0,eax
       vmulss    xmm7,xmm0,xmm6
       jmp       near ptr M00_L03
M00_L05:
       mov       rcx,offset MT_System.ArithmeticException
       call      CORINFO_HELP_NEWSFAST
       mov       rbp,rax
       call      qword ptr [7FFC5B597480]
       mov       rdx,rax
       mov       rcx,rbp
       call      qword ptr [7FFC5B597498]
       mov       rcx,rbp
       call      CORINFO_HELP_THROW
       int       3
; Total bytes of code 466
```
```assembly
; System.Math.Sign(Single)
       push      rbx
       sub       rsp,20
       vxorps    xmm1,xmm1,xmm1
       vucomiss  xmm1,xmm0
       ja        short M01_L01
       vxorps    xmm1,xmm1,xmm1
       vucomiss  xmm0,xmm1
       ja        short M01_L00
       vxorps    xmm1,xmm1,xmm1
       vucomiss  xmm0,xmm1
       jp        short M01_L02
       jne       short M01_L02
       mov       rcx,7FFC5B5E7080
       call      CORINFO_HELP_COUNTPROFILE32
       xor       eax,eax
       add       rsp,20
       pop       rbx
       ret
M01_L00:
       mov       rcx,7FFC5B5E707C
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,1
       add       rsp,20
       pop       rbx
       ret
M01_L01:
       mov       rcx,7FFC5B5E7078
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,0FFFFFFFF
       add       rsp,20
       pop       rbx
       ret
M01_L02:
       mov       rcx,offset MT_System.ArithmeticException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       call      qword ptr [7FFC5B597480]
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FFC5B597498]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
; Total bytes of code 157
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.RotateTowards()
       push      rdi
       push      rsi
       push      rbp
       push      rbx
       sub       rsp,68
       vzeroupper
       vmovaps   [rsp+50],xmm6
       vmovaps   [rsp+40],xmm7
       vmovaps   [rsp+30],xmm8
       mov       rbx,rdx
       lea       rsi,[rcx+14]
       lea       rdi,[rcx+24]
       vmovss    xmm6,dword ptr [rcx+8]
       vmovss    xmm0,dword ptr [rsi]
       vmulss    xmm0,xmm0,dword ptr [rdi]
       vmovss    xmm1,dword ptr [rsi+4]
       vmulss    xmm1,xmm1,dword ptr [rdi+4]
       vaddss    xmm0,xmm0,xmm1
       vmovss    xmm1,dword ptr [rsi+8]
       vmulss    xmm1,xmm1,dword ptr [rdi+8]
       vaddss    xmm0,xmm0,xmm1
       vmovss    xmm1,dword ptr [rsi+0C]
       vmulss    xmm1,xmm1,dword ptr [rdi+0C]
       vaddss    xmm0,xmm0,xmm1
       vmovss    dword ptr [rsp+20],xmm0
       vandps    xmm1,xmm0,[7FFC5B144BA0]
       vmovss    dword ptr [rsp+24],xmm1
       vbroadcastss xmm7,dword ptr [7FFC5B144BB0]
       vminss    xmm2,xmm7,xmm1
       vmovaps   xmm0,xmm2
       call      00007FFCBAD606C0
       vmulss    xmm0,xmm0,dword ptr [7FFC5B144BB4]
       vxorps    xmm1,xmm1,xmm1
       vucomiss  xmm0,xmm1
       jp        short M00_L00
       je        near ptr M00_L08
M00_L00:
       vdivss    xmm0,xmm6,xmm0
       vminss    xmm6,xmm7,xmm0
       vmovss    xmm0,dword ptr [rsp+20]
       vmovss    dword ptr [rsp+2C],xmm0
       vmovss    xmm1,dword ptr [rsp+24]
       vucomiss  xmm1,dword ptr [7FFC5B144BB8]
       ja        near ptr M00_L06
       vmovaps   xmm0,xmm1
       call      00007FFCBAD606C0
       vmovss    dword ptr [rsp+28],xmm0
       call      00007FFCBAD607C0
       vmovss    xmm1,dword ptr [7FFC5B144BB0]
       vdivss    xmm7,xmm1,xmm0
       vsubss    xmm0,xmm1,xmm6
       vmulss    xmm0,xmm0,dword ptr [rsp+28]
       call      00007FFCBAD607C0
       vmulss    xmm8,xmm0,xmm7
       vmulss    xmm0,xmm6,dword ptr [rsp+28]
       call      00007FFCBAD607C0
       vmulss    xmm6,xmm0,xmm7
       vxorps    xmm0,xmm0,xmm0
       vmovss    xmm1,dword ptr [rsp+2C]
       vucomiss  xmm0,xmm1
       ja        short M00_L02
       vxorps    xmm0,xmm0,xmm0
       vucomiss  xmm1,xmm0
       ja        short M00_L01
       vxorps    xmm0,xmm0,xmm0
       vucomiss  xmm1,xmm0
       jp        near ptr M00_L07
       jne       near ptr M00_L07
       xor       ebp,ebp
       jmp       short M00_L03
M00_L01:
       mov       ebp,1
       jmp       short M00_L03
M00_L02:
       mov       ebp,0FFFFFFFF
M00_L03:
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2ss xmm0,xmm0,ebp
       vmulss    xmm6,xmm6,xmm0
M00_L04:
       vmulss    xmm0,xmm8,dword ptr [rsi]
       vmulss    xmm1,xmm6,dword ptr [rdi]
       vaddss    xmm0,xmm0,xmm1
       vmulss    xmm1,xmm8,dword ptr [rsi+4]
       vmulss    xmm2,xmm6,dword ptr [rdi+4]
       vaddss    xmm1,xmm1,xmm2
       vmulss    xmm2,xmm8,dword ptr [rsi+8]
       vmulss    xmm3,xmm6,dword ptr [rdi+8]
       vaddss    xmm2,xmm2,xmm3
       vmulss    xmm3,xmm8,dword ptr [rsi+0C]
       vmulss    xmm4,xmm6,dword ptr [rdi+0C]
       vaddss    xmm3,xmm3,xmm4
M00_L05:
       vmovss    dword ptr [rbx],xmm0
       vmovss    dword ptr [rbx+4],xmm1
       vmovss    dword ptr [rbx+8],xmm2
       vmovss    dword ptr [rbx+0C],xmm3
       mov       rax,rbx
       vmovaps   xmm6,[rsp+50]
       vmovaps   xmm7,[rsp+40]
       vmovaps   xmm8,[rsp+30]
       add       rsp,68
       pop       rbx
       pop       rbp
       pop       rsi
       pop       rdi
       ret
M00_L06:
       vmovss    xmm1,dword ptr [7FFC5B144BB0]
       vsubss    xmm8,xmm1,xmm6
       vmovss    xmm0,dword ptr [rsp+2C]
       call      qword ptr [7FFC5B5750B0]; System.Math.Sign(Single)
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2ss xmm0,xmm0,eax
       vmulss    xmm6,xmm0,xmm6
       jmp       near ptr M00_L04
M00_L07:
       mov       rcx,offset MT_System.ArithmeticException
       call      CORINFO_HELP_NEWSFAST
       mov       rbp,rax
       call      qword ptr [7FFC5B5774B0]
       mov       rdx,rax
       mov       rcx,rbp
       call      qword ptr [7FFC5B5774C8]
       mov       rcx,rbp
       call      CORINFO_HELP_THROW
       int       3
M00_L08:
       vmovss    xmm0,dword ptr [rdi]
       vmovss    xmm1,dword ptr [rdi+4]
       vmovss    xmm2,dword ptr [rdi+8]
       vmovss    xmm3,dword ptr [rdi+0C]
       jmp       near ptr M00_L05
; Total bytes of code 554
```
```assembly
; System.Math.Sign(Single)
       push      rbx
       sub       rsp,20
       vxorps    xmm1,xmm1,xmm1
       vucomiss  xmm1,xmm0
       ja        short M01_L01
       vxorps    xmm1,xmm1,xmm1
       vucomiss  xmm0,xmm1
       ja        short M01_L00
       vxorps    xmm1,xmm1,xmm1
       vucomiss  xmm0,xmm1
       jp        short M01_L02
       jne       short M01_L02
       mov       rcx,7FFC5B5C7080
       call      CORINFO_HELP_COUNTPROFILE32
       xor       eax,eax
       add       rsp,20
       pop       rbx
       ret
M01_L00:
       mov       rcx,7FFC5B5C707C
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,1
       add       rsp,20
       pop       rbx
       ret
M01_L01:
       mov       rcx,7FFC5B5C7078
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,0FFFFFFFF
       add       rsp,20
       pop       rbx
       ret
M01_L02:
       mov       rcx,offset MT_System.ArithmeticException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       call      qword ptr [7FFC5B5774B0]
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FFC5B5774C8]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
; Total bytes of code 157
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Squad()
       push      rsi
       push      rbx
       sub       rsp,118
       vzeroupper
       vmovaps   [rsp+100],xmm6
       vmovaps   [rsp+0F0],xmm7
       vmovaps   [rsp+0E0],xmm8
       vmovaps   [rsp+0D0],xmm9
       vmovaps   [rsp+0C0],xmm10
       vmovaps   [rsp+0B0],xmm11
       vmovaps   [rsp+0A0],xmm12
       vmovaps   [rsp+90],xmm13
       vmovaps   [rsp+80],xmm14
       vmovaps   [rsp+70],xmm15
       mov       rbx,rdx
       vmovss    xmm6,dword ptr [rcx+14]
       vmovss    xmm7,dword ptr [rcx+18]
       vmovss    xmm8,dword ptr [rcx+1C]
       vmovss    xmm9,dword ptr [rcx+20]
       vmovss    xmm10,dword ptr [rcx+24]
       vmovss    xmm11,dword ptr [rcx+28]
       vmovss    dword ptr [rsp+44],xmm11
       vmovss    xmm12,dword ptr [rcx+2C]
       vmovss    dword ptr [rsp+40],xmm12
       vmovss    xmm13,dword ptr [rcx+30]
       vmovss    dword ptr [rsp+3C],xmm13
       vmovss    xmm14,dword ptr [rcx+34]
       vmovss    xmm15,dword ptr [rcx+38]
       vmovss    xmm0,dword ptr [rcx+3C]
       vmovss    dword ptr [rsp+38],xmm0
       vmovss    xmm1,dword ptr [rcx+40]
       vmovss    dword ptr [rsp+34],xmm1
       vmovss    xmm2,dword ptr [rcx+44]
       vmovss    dword ptr [rsp+30],xmm2
       vmovss    xmm3,dword ptr [rcx+48]
       vmovss    dword ptr [rsp+2C],xmm3
       vmovss    xmm4,dword ptr [rcx+4C]
       vmovss    dword ptr [rsp+28],xmm4
       vmovss    xmm5,dword ptr [rcx+50]
       vmovss    dword ptr [rsp+24],xmm5
       vmovss    xmm13,dword ptr [rcx+8]
       vmulss    xmm12,xmm6,xmm2
       vmulss    xmm11,xmm7,xmm3
       vaddss    xmm11,xmm12,xmm11
       vmulss    xmm12,xmm8,xmm4
       vaddss    xmm11,xmm11,xmm12
       vmulss    xmm12,xmm9,xmm5
       vaddss    xmm11,xmm11,xmm12
       vandps    xmm12,xmm11,[7FFC5B175210]
       vucomiss  xmm12,dword ptr [7FFC5B175220]
       ja        near ptr M00_L12
       vmovaps   xmm0,xmm12
       call      00007FFCBAD606C0
       vmovss    dword ptr [rsp+6C],xmm0
       call      00007FFCBAD607C0
       vmovss    xmm12,dword ptr [7FFC5B175224]
       vdivss    xmm0,xmm12,xmm0
       vmovss    dword ptr [rsp+68],xmm0
       vsubss    xmm1,xmm12,xmm13
       vmovss    dword ptr [rsp+20],xmm1
       vmovss    xmm2,dword ptr [rsp+6C]
       vmulss    xmm0,xmm1,xmm2
       call      00007FFCBAD607C0
       vmulss    xmm0,xmm0,dword ptr [rsp+68]
       vmovss    dword ptr [rsp+64],xmm0
       vmulss    xmm0,xmm13,dword ptr [rsp+6C]
       call      00007FFCBAD607C0
       vmulss    xmm0,xmm0,dword ptr [rsp+68]
       vxorps    xmm1,xmm1,xmm1
       vucomiss  xmm1,xmm11
       ja        short M00_L01
       vxorps    xmm1,xmm1,xmm1
       vucomiss  xmm11,xmm1
       ja        short M00_L00
       vxorps    xmm1,xmm1,xmm1
       vucomiss  xmm11,xmm1
       jp        near ptr M00_L13
       jne       near ptr M00_L13
       xor       esi,esi
       jmp       short M00_L02
M00_L00:
       mov       esi,1
       jmp       short M00_L02
M00_L01:
       mov       esi,0FFFFFFFF
M00_L02:
       vxorps    xmm1,xmm1,xmm1
       vcvtsi2ss xmm1,xmm1,esi
       vmulss    xmm0,xmm0,xmm1
M00_L03:
       vmovss    xmm11,dword ptr [rsp+64]
       vmulss    xmm1,xmm11,xmm6
       vmulss    xmm2,xmm0,dword ptr [rsp+30]
       vaddss    xmm6,xmm1,xmm2
       vmulss    xmm1,xmm11,xmm7
       vmulss    xmm2,xmm0,dword ptr [rsp+2C]
       vaddss    xmm7,xmm1,xmm2
       vmulss    xmm1,xmm11,xmm8
       vmulss    xmm2,xmm0,dword ptr [rsp+28]
       vaddss    xmm8,xmm1,xmm2
       vmulss    xmm1,xmm11,xmm9
       vmulss    xmm0,xmm0,dword ptr [rsp+24]
       vaddss    xmm9,xmm1,xmm0
       vmulss    xmm0,xmm10,xmm14
       vmovss    xmm11,dword ptr [rsp+44]
       vmulss    xmm1,xmm11,xmm15
       vaddss    xmm0,xmm0,xmm1
       vmovss    xmm1,dword ptr [rsp+40]
       vmulss    xmm3,xmm1,dword ptr [rsp+38]
       vaddss    xmm0,xmm0,xmm3
       vmovss    xmm3,dword ptr [rsp+3C]
       vmulss    xmm5,xmm3,dword ptr [rsp+34]
       vaddss    xmm0,xmm0,xmm5
       vmovss    dword ptr [rsp+60],xmm0
       vandps    xmm5,xmm0,[7FFC5B175210]
       vucomiss  xmm5,dword ptr [7FFC5B175220]
       ja        near ptr M00_L14
       vmovaps   xmm0,xmm5
       call      00007FFCBAD606C0
       vmovss    dword ptr [rsp+5C],xmm0
       call      00007FFCBAD607C0
       vdivss    xmm0,xmm12,xmm0
       vmovss    dword ptr [rsp+58],xmm0
       vmovss    xmm2,dword ptr [rsp+5C]
       vmulss    xmm0,xmm2,dword ptr [rsp+20]
       call      00007FFCBAD607C0
       vmulss    xmm0,xmm0,dword ptr [rsp+58]
       vmovss    dword ptr [rsp+54],xmm0
       vmulss    xmm0,xmm13,dword ptr [rsp+5C]
       call      00007FFCBAD607C0
       vmulss    xmm0,xmm0,dword ptr [rsp+58]
       vxorps    xmm1,xmm1,xmm1
       vmovss    xmm2,dword ptr [rsp+60]
       vucomiss  xmm1,xmm2
       ja        short M00_L05
       vxorps    xmm1,xmm1,xmm1
       vucomiss  xmm2,xmm1
       ja        short M00_L04
       vxorps    xmm1,xmm1,xmm1
       vucomiss  xmm2,xmm1
       jp        near ptr M00_L15
       jne       near ptr M00_L15
       xor       esi,esi
       jmp       short M00_L06
M00_L04:
       mov       esi,1
       jmp       short M00_L06
M00_L05:
       mov       esi,0FFFFFFFF
M00_L06:
       vxorps    xmm1,xmm1,xmm1
       vcvtsi2ss xmm1,xmm1,esi
       vmulss    xmm0,xmm0,xmm1
M00_L07:
       vmovss    xmm1,dword ptr [rsp+54]
       vmulss    xmm2,xmm1,xmm10
       vmulss    xmm3,xmm0,xmm14
       vaddss    xmm10,xmm2,xmm3
       vmulss    xmm2,xmm1,dword ptr [rsp+44]
       vmulss    xmm3,xmm0,xmm15
       vaddss    xmm11,xmm2,xmm3
       vmulss    xmm2,xmm1,dword ptr [rsp+40]
       vmulss    xmm3,xmm0,dword ptr [rsp+38]
       vaddss    xmm14,xmm2,xmm3
       vmulss    xmm1,xmm1,dword ptr [rsp+3C]
       vmulss    xmm0,xmm0,dword ptr [rsp+34]
       vaddss    xmm15,xmm1,xmm0
       vaddss    xmm0,xmm13,xmm13
       vmulss    xmm13,xmm0,dword ptr [rsp+20]
       vmulss    xmm0,xmm6,xmm10
       vmulss    xmm1,xmm7,xmm11
       vaddss    xmm0,xmm0,xmm1
       vmulss    xmm1,xmm8,xmm14
       vaddss    xmm0,xmm0,xmm1
       vmulss    xmm1,xmm9,xmm15
       vaddss    xmm0,xmm0,xmm1
       vmovss    dword ptr [rsp+50],xmm0
       vandps    xmm1,xmm0,[7FFC5B175210]
       vucomiss  xmm1,dword ptr [7FFC5B175220]
       ja        near ptr M00_L16
       vmovaps   xmm0,xmm1
       call      00007FFCBAD606C0
       vmovss    dword ptr [rsp+4C],xmm0
       call      00007FFCBAD607C0
       vdivss    xmm0,xmm12,xmm0
       vmovss    dword ptr [rsp+48],xmm0
       vsubss    xmm1,xmm12,xmm13
       vmulss    xmm0,xmm1,dword ptr [rsp+4C]
       call      00007FFCBAD607C0
       vmulss    xmm12,xmm0,dword ptr [rsp+48]
       vmulss    xmm0,xmm13,dword ptr [rsp+4C]
       call      00007FFCBAD607C0
       vmulss    xmm13,xmm0,dword ptr [rsp+48]
       vxorps    xmm0,xmm0,xmm0
       vmovss    xmm1,dword ptr [rsp+50]
       vucomiss  xmm0,xmm1
       ja        short M00_L09
       vxorps    xmm0,xmm0,xmm0
       vucomiss  xmm1,xmm0
       ja        short M00_L08
       vxorps    xmm0,xmm0,xmm0
       vucomiss  xmm1,xmm0
       jp        near ptr M00_L17
       jne       near ptr M00_L17
       xor       esi,esi
       jmp       short M00_L10
M00_L08:
       mov       esi,1
       jmp       short M00_L10
M00_L09:
       mov       esi,0FFFFFFFF
M00_L10:
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2ss xmm0,xmm0,esi
       vmulss    xmm13,xmm13,xmm0
M00_L11:
       vmulss    xmm0,xmm12,xmm6
       vmulss    xmm1,xmm13,xmm10
       vaddss    xmm0,xmm0,xmm1
       vmulss    xmm1,xmm12,xmm7
       vmulss    xmm2,xmm13,xmm11
       vaddss    xmm1,xmm1,xmm2
       vmulss    xmm2,xmm12,xmm8
       vmulss    xmm3,xmm13,xmm14
       vaddss    xmm2,xmm2,xmm3
       vmulss    xmm3,xmm12,xmm9
       vmulss    xmm4,xmm13,xmm15
       vaddss    xmm3,xmm3,xmm4
       vmovss    dword ptr [rbx],xmm0
       vmovss    dword ptr [rbx+4],xmm1
       vmovss    dword ptr [rbx+8],xmm2
       vmovss    dword ptr [rbx+0C],xmm3
       mov       rax,rbx
       vmovaps   xmm6,[rsp+100]
       vmovaps   xmm7,[rsp+0F0]
       vmovaps   xmm8,[rsp+0E0]
       vmovaps   xmm9,[rsp+0D0]
       vmovaps   xmm10,[rsp+0C0]
       vmovaps   xmm11,[rsp+0B0]
       vmovaps   xmm12,[rsp+0A0]
       vmovaps   xmm13,[rsp+90]
       vmovaps   xmm14,[rsp+80]
       vmovaps   xmm15,[rsp+70]
       add       rsp,118
       pop       rbx
       pop       rsi
       ret
M00_L12:
       vmovss    xmm12,dword ptr [7FFC5B175224]
       vsubss    xmm1,xmm12,xmm13
       vmovss    dword ptr [rsp+20],xmm1
       vmovss    dword ptr [rsp+64],xmm1
       vmovaps   xmm0,xmm11
       call      qword ptr [7FFC5B5A51A0]; System.Math.Sign(Single)
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2ss xmm0,xmm0,eax
       vmulss    xmm0,xmm0,xmm13
       jmp       near ptr M00_L03
M00_L13:
       mov       rcx,offset MT_System.ArithmeticException
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       call      qword ptr [7FFC5B5A74B0]
       mov       rdx,rax
       mov       rcx,rsi
       call      qword ptr [7FFC5B5A74C8]
       mov       rcx,rsi
       call      CORINFO_HELP_THROW
       int       3
M00_L14:
       vmovss    xmm5,dword ptr [rsp+20]
       vmovaps   xmm11,xmm5
       vmovss    xmm0,dword ptr [rsp+60]
       call      qword ptr [7FFC5B5A51A0]; System.Math.Sign(Single)
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2ss xmm0,xmm0,eax
       vmulss    xmm0,xmm0,xmm13
       vmovss    dword ptr [rsp+54],xmm11
       jmp       near ptr M00_L07
M00_L15:
       mov       rcx,offset MT_System.ArithmeticException
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       call      qword ptr [7FFC5B5A74B0]
       mov       rdx,rax
       mov       rcx,rsi
       call      qword ptr [7FFC5B5A74C8]
       mov       rcx,rsi
       call      CORINFO_HELP_THROW
       int       3
M00_L16:
       vsubss    xmm12,xmm12,xmm13
       vmovss    xmm0,dword ptr [rsp+50]
       call      qword ptr [7FFC5B5A51A0]; System.Math.Sign(Single)
       vxorps    xmm0,xmm0,xmm0
       vcvtsi2ss xmm0,xmm0,eax
       vmulss    xmm13,xmm0,xmm13
       jmp       near ptr M00_L11
M00_L17:
       mov       rcx,offset MT_System.ArithmeticException
       call      CORINFO_HELP_NEWSFAST
       mov       rsi,rax
       call      qword ptr [7FFC5B5A74B0]
       mov       rdx,rax
       mov       rcx,rsi
       call      qword ptr [7FFC5B5A74C8]
       mov       rcx,rsi
       call      CORINFO_HELP_THROW
       int       3
; Total bytes of code 1475
```
```assembly
; System.Math.Sign(Single)
       push      rbx
       sub       rsp,20
       vxorps    xmm1,xmm1,xmm1
       vucomiss  xmm1,xmm0
       ja        short M01_L01
       vxorps    xmm1,xmm1,xmm1
       vucomiss  xmm0,xmm1
       ja        short M01_L00
       vxorps    xmm1,xmm1,xmm1
       vucomiss  xmm0,xmm1
       jp        short M01_L02
       jne       short M01_L02
       mov       rcx,7FFC5B5F7080
       call      CORINFO_HELP_COUNTPROFILE32
       xor       eax,eax
       add       rsp,20
       pop       rbx
       ret
M01_L00:
       mov       rcx,7FFC5B5F707C
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,1
       add       rsp,20
       pop       rbx
       ret
M01_L01:
       mov       rcx,7FFC5B5F7078
       call      CORINFO_HELP_COUNTPROFILE32
       mov       eax,0FFFFFFFF
       add       rsp,20
       pop       rbx
       ret
M01_L02:
       mov       rcx,offset MT_System.ArithmeticException
       call      CORINFO_HELP_NEWSFAST
       mov       rbx,rax
       call      qword ptr [7FFC5B5A74B0]
       mov       rdx,rax
       mov       rcx,rbx
       call      qword ptr [7FFC5B5A74C8]
       mov       rcx,rbx
       call      CORINFO_HELP_THROW
       int       3
; Total bytes of code 157
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Operator_Add()
       lea       rax,[rcx+14]
       add       rcx,24
       vmovss    xmm0,dword ptr [rax]
       vaddss    xmm0,xmm0,dword ptr [rcx]
       vmovss    xmm1,dword ptr [rax+4]
       vaddss    xmm1,xmm1,dword ptr [rcx+4]
       vmovss    xmm2,dword ptr [rax+8]
       vaddss    xmm2,xmm2,dword ptr [rcx+8]
       vmovss    xmm3,dword ptr [rax+0C]
       vaddss    xmm3,xmm3,dword ptr [rcx+0C]
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
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Operator_Subtract()
       lea       rax,[rcx+14]
       add       rcx,24
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
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Operator_Negate()
       add       rcx,14
       vmovss    xmm0,dword ptr [rcx]
       vxorps    xmm0,xmm0,[7FFC5B144270]
       vmovss    xmm1,dword ptr [rcx+4]
       vxorps    xmm1,xmm1,[7FFC5B144270]
       vmovss    xmm2,dword ptr [rcx+8]
       vxorps    xmm2,xmm2,[7FFC5B144270]
       vmovss    xmm3,dword ptr [rcx+0C]
       vxorps    xmm3,xmm3,[7FFC5B144270]
       vmovss    dword ptr [rdx],xmm0
       vmovss    dword ptr [rdx+4],xmm1
       vmovss    dword ptr [rdx+8],xmm2
       vmovss    dword ptr [rdx+0C],xmm3
       mov       rax,rdx
       ret
; Total bytes of code 78
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Operator_Multiply()
       sub       rsp,68
       vmovaps   [rsp+50],xmm6
       vmovaps   [rsp+40],xmm7
       vmovaps   [rsp+30],xmm8
       vmovaps   [rsp+20],xmm9
       vmovaps   [rsp+10],xmm10
       vmovaps   [rsp],xmm11
       lea       rax,[rcx+14]
       add       rcx,24
       vmovss    xmm0,dword ptr [rax]
       vmovss    xmm1,dword ptr [rax+4]
       vmovss    xmm2,dword ptr [rax+8]
       vmovss    xmm3,dword ptr [rax+0C]
       vmovss    xmm4,dword ptr [rcx]
       vmovss    xmm5,dword ptr [rcx+4]
       vmovss    xmm6,dword ptr [rcx+8]
       vmovss    xmm7,dword ptr [rcx+0C]
       vmulss    xmm8,xmm4,xmm3
       vmulss    xmm9,xmm0,xmm7
       vaddss    xmm8,xmm8,xmm9
       vmulss    xmm9,xmm5,xmm2
       vaddss    xmm8,xmm8,xmm9
       vmulss    xmm9,xmm6,xmm1
       vsubss    xmm8,xmm8,xmm9
       vmulss    xmm9,xmm5,xmm3
       vmulss    xmm10,xmm1,xmm7
       vaddss    xmm9,xmm9,xmm10
       vmulss    xmm10,xmm6,xmm0
       vaddss    xmm9,xmm9,xmm10
       vmulss    xmm10,xmm4,xmm2
       vsubss    xmm9,xmm9,xmm10
       vmulss    xmm10,xmm6,xmm3
       vmulss    xmm11,xmm2,xmm7
       vaddss    xmm10,xmm10,xmm11
       vmulss    xmm11,xmm4,xmm1
       vaddss    xmm10,xmm10,xmm11
       vmulss    xmm11,xmm5,xmm0
       vsubss    xmm10,xmm10,xmm11
       vmulss    xmm0,xmm4,xmm0
       vmulss    xmm1,xmm5,xmm1
       vaddss    xmm0,xmm0,xmm1
       vmulss    xmm1,xmm6,xmm2
       vaddss    xmm0,xmm0,xmm1
       vmulss    xmm1,xmm7,xmm3
       vsubss    xmm0,xmm1,xmm0
       vmovss    dword ptr [rdx],xmm8
       vmovss    dword ptr [rdx+4],xmm9
       vmovss    dword ptr [rdx+8],xmm10
       vmovss    dword ptr [rdx+0C],xmm0
       mov       rax,rdx
       vmovaps   xmm6,[rsp+50]
       vmovaps   xmm7,[rsp+40]
       vmovaps   xmm8,[rsp+30]
       vmovaps   xmm9,[rsp+20]
       vmovaps   xmm10,[rsp+10]
       vmovaps   xmm11,[rsp]
       add       rsp,68
       ret
; Total bytes of code 268
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Operator_Equals()
       lea       rax,[rcx+14]
       vmovss    xmm0,dword ptr [rcx+24]
       vmovss    xmm1,dword ptr [rcx+28]
       vmovss    xmm2,dword ptr [rcx+2C]
       vmovss    xmm3,dword ptr [rcx+30]
       vsubss    xmm0,xmm0,dword ptr [rax]
       vandps    xmm0,xmm0,[7FFC5B154230]
       vmovss    xmm4,dword ptr [7FFC5B154240]
       vucomiss  xmm4,xmm0
       ja        short M00_L02
M00_L00:
       xor       eax,eax
M00_L01:
       ret
M00_L02:
       vsubss    xmm0,xmm1,dword ptr [rax+4]
       vandps    xmm0,xmm0,[7FFC5B154230]
       vmovss    xmm1,dword ptr [7FFC5B154240]
       vucomiss  xmm1,xmm0
       jbe       short M00_L00
       vsubss    xmm0,xmm2,dword ptr [rax+8]
       vandps    xmm0,xmm0,[7FFC5B154230]
       vmovss    xmm1,dword ptr [7FFC5B154240]
       vucomiss  xmm1,xmm0
       jbe       short M00_L00
       vsubss    xmm0,xmm3,dword ptr [rax+0C]
       vandps    xmm0,xmm0,[7FFC5B154230]
       vmovss    xmm1,dword ptr [7FFC5B154240]
       vucomiss  xmm1,xmm0
       seta      al
       movzx     eax,al
       jmp       short M00_L01
; Total bytes of code 140
```

## .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3 (Job: ShortRun(IterationCount=5, LaunchCount=1, WarmupCount=5))

```assembly
; Stride.Core.Mathematics.Benchmarks.QuaternionBenchmarks.Operator_NotEquals()
       lea       rax,[rcx+14]
       vmovss    xmm0,dword ptr [rcx+24]
       vmovss    xmm1,dword ptr [rcx+28]
       vmovss    xmm2,dword ptr [rcx+2C]
       vmovss    xmm3,dword ptr [rcx+30]
       vsubss    xmm0,xmm0,dword ptr [rax]
       vandps    xmm0,xmm0,[7FFC5B134240]
       vmovss    xmm4,dword ptr [7FFC5B134250]
       vucomiss  xmm4,xmm0
       ja        short M00_L01
M00_L00:
       mov       eax,1
       ret
M00_L01:
       vsubss    xmm0,xmm1,dword ptr [rax+4]
       vandps    xmm0,xmm0,[7FFC5B134240]
       vmovss    xmm1,dword ptr [7FFC5B134250]
       vucomiss  xmm1,xmm0
       jbe       short M00_L00
       vsubss    xmm0,xmm2,dword ptr [rax+8]
       vandps    xmm0,xmm0,[7FFC5B134240]
       vmovss    xmm1,dword ptr [7FFC5B134250]
       vucomiss  xmm1,xmm0
       jbe       short M00_L00
       vsubss    xmm0,xmm3,dword ptr [rax+0C]
       vandps    xmm0,xmm0,[7FFC5B134240]
       vmovss    xmm1,dword ptr [7FFC5B134250]
       vucomiss  xmm1,xmm0
       seta      al
       movzx     eax,al
       test      eax,eax
       sete      al
       movzx     eax,al
       ret
; Total bytes of code 150
```

