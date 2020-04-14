Stride.Core.Shader
====================

Shader source code manipulation library.
With it, you can:
* Parse HLSL and GLSL
* Implement custom AST visitors
* Semantic information: inferred type, etc...
* Transform HLSL into GLSL

## HLSL to GLSL

It can convert HLSL code to various GLSL dialect, including Core, ES 2.0, ES 3.0+ and Vulkan.

### Intrisics

| Feature                 | Status |
| ----------------------- | ------ |
| Vertex & Pixel Shaders  | ✔ |
| Geometry Shaders        | ✘ |
| Tessellation Shaders    | ✘ |
| Compute Shaders         | ✘ |
| Standard intrinsics     | ✔ except: noise |
| SM5 intrinsics          | ✘ missing: dst, ddx_coarse, ddx_fine, ddy_coarse, ddy_fine, firstbithigh, firstbitlow, countbits, f16tof32, f32tof16, fma, mad, msad4, rcp, reversebits |
| Registers               | ✔ simple remapping |
| Constant Buffer Offsets & Packing | ✘ cbuffer follow GLSL rules |
| Barriers intrinsics     | ✘ (no Compute Shaders) |
| Interlocked intrinsics  | ✘ (no Compute Shaders) |
| Shared variables & memory | ✘ (no Compute Shaders) |
| Integer reinterpret intrinsics | ✘ asuint, asint |
| Flow statements         | ✔ except: errorf, printf, abort |
| Texture objects         | ✔ |
| Buffer objects          | ✔ |
| Class/struct methods    | ✘ |
| Preprocessor            | ✔ |
| Remap VS projected coordinates | ✔ |
| Multiple Render Targets | ✔ |
| Constant Buffers        | ✔ |
| StructuredBuffer and RWStructuredBuffer | ✘ |
| RWBuffer and RWTexture  | ✘ |
| #extension directives   | ✘ not generated |

### Texture / samplers

By default, it generates "combined" samplers:

```
Texture2D texture;
SamplerState sampler;

texture.Sample(texture, sampler);
```

will generate a single `sampler2D texture_sampler`

There is also a mode to generate separate texture/samplers for platforms that support it (i.e. Vulkan).

### Known issues

* Small type inference issue with Texture object, happens when doing texture.Sample().r, where it doesn't resolve generics type properly
* Preprocessor seems to first concat then replace defines, which makes such patterns fail:
```
#define REGISTER_INDEX 1
#define REGISTER_EXPR b ## REGISTER_INDEX
cbuffer A : register(REGISTER_EXPR)
```
