// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core;
using Stride.Core.IO;
using Stride.Engine.Design;
using Stride.Rendering;

namespace Stride.Shaders.Compiler
{
    public static class EffectCompilerFactory
    {
        public static IEffectCompiler CreateEffectCompiler(
            IVirtualFileProvider fileProvider, 
            EffectSystem effectSystem = null, 
            string packageName = null, 
            EffectCompilationMode effectCompilationMode = EffectCompilationMode.Local, 
            bool recordEffectRequested = false, 
            TaskSchedulerSelector taskSchedulerSelector = null,
            DatabaseFileProvider database = null)
        {
            EffectCompilerBase compiler = null;

            if ((effectCompilationMode & EffectCompilationMode.Local) != 0)
            {
                // Local allowed and available, let's use that
                compiler = new EffectCompiler(fileProvider)
                {
                    SourceDirectories = { EffectCompilerBase.DefaultSourceShaderFolder },
                };
            }

            // Select database - needed for caching
            var selectedDatabase = database ?? fileProvider as DatabaseFileProvider;

            // Nothing to do remotely
            bool needRemoteCompiler = (compiler == null && (effectCompilationMode & EffectCompilationMode.Remote) != 0);
            if (needRemoteCompiler || recordEffectRequested)
            {
                // Create the object that handles the connection
                var shaderCompilerTarget = new RemoteEffectCompilerClient(packageName);

                if (recordEffectRequested)
                {
                    // Let's notify effect compiler server for each new effect requested
                    effectSystem.EffectUsed += shaderCompilerTarget.NotifyEffectUsed;
                }

                // Use remote only if nothing else was found before (i.e. a local compiler)
                if (needRemoteCompiler)
                {
                    // Create a remote compiler
                    compiler = new RemoteEffectCompiler(fileProvider, selectedDatabase, shaderCompilerTarget);
                }
                else
                {
                    // Otherwise, EffectSystem takes ownership of shaderCompilerTarget
                    shaderCompilerTarget.DisposeBy(effectSystem);
                }
            }

            // Local not possible or allowed, and remote not allowed either => switch back to null compiler
            if (compiler == null)
            {
                compiler = new NullEffectCompiler(fileProvider, selectedDatabase);
            }

            return new EffectCompilerCache(compiler, selectedDatabase, taskSchedulerSelector);
        }
    }
}
