// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Storage;
using Stride.Rendering.Materials;
using Stride.Shaders;
using Stride.Shaders.Compiler;

namespace Stride.Engine.Tests;

/// <summary>
/// Holds back the compilation of effects that use a tessellation shader until <see cref="Release"/> or <see cref="Fail"/>.
/// </summary>
internal class GatedEffectCompiler : EffectCompilerChain
{
    private readonly List<(TaskOrResult<EffectBytecodeCompilerResult> Inner, TaskCompletionSource<EffectBytecodeCompilerResult> Gate)> held = new();

    public volatile bool Hold;

    public int HeldCount { get { lock (held) return held.Count; } }

    public GatedEffectCompiler(EffectCompilerBase compiler) : base(compiler)
    {
    }

    public override TaskOrResult<EffectBytecodeCompilerResult> Compile(ShaderMixinSource mixinTree, EffectCompilerParameters effectParameters, CompilerParameters compilerParameters, ObjectId effectInputHash)
    {
        var result = base.Compile(mixinTree, effectParameters, compilerParameters, effectInputHash);
        if (!Hold || !compilerParameters.ContainsKey(MaterialKeys.TessellationShader))
            return result;

        var gate = new TaskCompletionSource<EffectBytecodeCompilerResult>();
        lock (held)
            held.Add((result, gate));
        return gate.Task;
    }

    public void Release()
    {
        Hold = false;
        lock (held)
        {
            foreach (var (inner, gate) in held)
            {
                try
                {
                    gate.TrySetResult(inner.WaitForResult());
                }
                catch (Exception e)
                {
                    gate.TrySetException(e);
                }
            }
        }
    }

    public void Fail()
    {
        Hold = false;
        lock (held)
        {
            foreach (var (_, gate) in held)
                gate.TrySetException(new InvalidOperationException("The effect failed to compile"));
        }
    }
}
