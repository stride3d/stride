# Shaders: SDSL parser and compiler

- An SDSL mistake is an error, not a warning, even for a keyword or attribute that does nothing. Warnings are only for lints on valid HLSL, such as operator precedence. When a machine can fix the mistake in user shaders, also add an upgrader step.
- SDSL follows the strict rules of DXC, recent C/C++ and C#, not the loose rules of FXC. Where SDSL already differs from HLSL, it may choose a different spelling (for example `SlotChannel.R`, as in C#).
- Code that Stride 4.3 accepted and the new compiler rejects comes back only when it is still valid: check it with DXC (`dxc.exe -T ps_6_0`). A construct that only FXC accepts (such as `StructuredBuffer.Length`) gets a clear error that gives the fix.
- When a change alters the compiler's output, bump a version in [ShaderCompilerVersion.cs](Stride.Shaders.Effects/ShaderCompilerVersion.cs). Effect caches are keyed on the shader inputs only, so without the bump users keep the old bytecode and the fix seems to do nothing.
  - `Shader`: the SPIR-V of a shader class or the `.spv` file changes. This clears every cache.
  - `Effect`: only what is derived from the SPIR-V changes (reflection, bytecode for one API).
- When you review a pull request whose compiled shader output changed (the `Shader corpus` jobs, see [the corpus README](Stride.Shaders.Tests/Corpus/README.md#in-ci)), check that every change has a cause in the diff. Start from the changed lines found in many permutations: match each group to a change of the pull request (a constant, a function, a new member, a compiler change), and check that the lines do what that change says. Then:
  - A permutation that `now fails` is a blocking problem, unless the pull request removes that feature on purpose. Name the effect and the error.
  - A `reflection` change in a pull request that adds, removes or moves no resource or constant buffer member can break games at run time: ask about it. So can an effect that the changes should not reach, or changed lines that match no change.
  - Changes that keep the same code once legalized or optimized don't change what runs: say nothing about them.
  - A failing `Shader output` job only waits for the `shader-output-change` label, which a maintainer adds once the changes are explained. It is not a bug of the pull request.
- A language change or a new diagnostic comes with its documentation in [stride-docs](https://github.com/stride3d/stride-docs). Diagnostic pages follow the `en/diagnostics/STRDIAG*.md` format there.
