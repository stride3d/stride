# Shaders: SDSL parser and compiler

- An SDSL mistake is an error, not a warning, even for a keyword or attribute that does nothing. Warnings are only for lints on valid HLSL, such as operator precedence. When a machine can fix the mistake in user shaders, also add an upgrader step.
- SDSL follows the strict rules of DXC, recent C/C++ and C#, not the loose rules of FXC. Where SDSL already differs from HLSL, it may choose a different spelling (for example `SlotChannel.R`, as in C#).
- Code that Stride 4.3 accepted and the new compiler rejects comes back only when it is still valid: check it with DXC (`dxc.exe -T ps_6_0`). A construct that only FXC accepts (such as `StructuredBuffer.Length`) gets a clear error that gives the fix.
- When a change alters the compiler's output, bump a version in [ShaderCompilerVersion.cs](Stride.Shaders.Effects/ShaderCompilerVersion.cs). Effect caches are keyed on the shader inputs only, so without the bump users keep the old bytecode and the fix seems to do nothing.
  - `Shader`: the SPIR-V of a shader class or the `.spv` file changes. This clears every cache.
  - `Effect`: only what is derived from the SPIR-V changes (reflection, bytecode for one API).
- A language change or a new diagnostic comes with its documentation in [stride-docs](https://github.com/stride3d/stride-docs). Diagnostic pages follow the `en/diagnostics/STRDIAG*.md` format there.
