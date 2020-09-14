// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;
using Stride.Rendering;

namespace Stride.Shaders.Compiler
{
    public delegate TaskScheduler TaskSchedulerSelector(ShaderMixinSource mixinTree, EffectCompilerParameters? compilerParameters);

    /// <summary>
    /// Checks if an effect has already been compiled in its cache before deferring to a real <see cref="IEffectCompiler"/>.
    /// </summary>
    [DataSerializerGlobal(null, typeof(KeyValuePair<HashSourceCollection, EffectBytecode>))]
    public class EffectCompilerCache : EffectCompilerChain
    {
        private static readonly Logger Log = GlobalLogger.GetLogger("EffectCompilerCache");
        private readonly Dictionary<ObjectId, KeyValuePair<EffectBytecode, EffectBytecodeCacheLoadSource>> bytecodes = new Dictionary<ObjectId, KeyValuePair<EffectBytecode, EffectBytecodeCacheLoadSource>>();
        private readonly HashSet<ObjectId> bytecodesByPassingStorage = new HashSet<ObjectId>();
        private const string CompiledShadersKey = "__shaders_bytecode__";

        private readonly Dictionary<ObjectId, Task<EffectBytecodeCompilerResult>> compilingShaders = new Dictionary<ObjectId, Task<EffectBytecodeCompilerResult>>();
        private readonly DatabaseFileProvider database;
        private readonly TaskSchedulerSelector taskSchedulerSelector;

        private int effectCompileCount;

        public bool CompileEffectAsynchronously { get; set; }

        /// <summary>
        /// If we have to compile a new shader, what kind of cache are we building?
        /// </summary>
        public EffectBytecodeCacheLoadSource CurrentCache { get; set; } = EffectBytecodeCacheLoadSource.DynamicCache;

        public EffectCompilerCache(EffectCompilerBase compiler, DatabaseFileProvider database, TaskSchedulerSelector taskSchedulerSelector = null) : base(compiler)
        {
            CompileEffectAsynchronously = true;
            this.database = database ?? throw new ArgumentNullException(nameof(database), "Using the cache requires a database.");
            this.taskSchedulerSelector = taskSchedulerSelector;
        }

        public override void ResetCache(HashSet<string> modifiedShaders)
        {
            // remove old shaders from cache
            lock (bytecodes)
            {
                base.ResetCache(modifiedShaders);
                RemoveObsoleteStoredResults(modifiedShaders);
            }
        }

        public override TaskOrResult<EffectBytecodeCompilerResult> Compile(ShaderMixinSource mixin, EffectCompilerParameters effectParameters, CompilerParameters compilerParameters)
        {
            var usedParameters = compilerParameters;
            var mixinObjectId = ShaderMixinObjectId.Compute(mixin, usedParameters.EffectParameters);

            // Final url of the compiled bytecode
            var compiledUrl = string.Format("{0}/{1}", CompiledShadersKey, mixinObjectId);

            var bytecode = new KeyValuePair<EffectBytecode, EffectBytecodeCacheLoadSource>(null, EffectBytecodeCacheLoadSource.JustCompiled);
            lock (bytecodes)
            {                
                // ------------------------------------------------------------------------------------------------------------
                // 1) Try to load latest bytecode
                // ------------------------------------------------------------------------------------------------------------
                ObjectId bytecodeId;
                if (database.ContentIndexMap.TryGetValue(compiledUrl, out bytecodeId))
                {
                    bytecode = LoadEffectBytecode(database, bytecodeId);
                }

                // On non Windows platform, we are expecting to have the bytecode stored directly
                if (Compiler is NullEffectCompiler && bytecode.Key == null)
                {
                    var stringBuilder = new StringBuilder();
                    stringBuilder.AppendFormat("Unable to find compiled shaders [{0}] for mixin [{1}] with parameters [{2}]", compiledUrl, mixin, usedParameters.ToStringPermutationsDetailed());
                    Log.Error(stringBuilder.ToString());
                    throw new InvalidOperationException(stringBuilder.ToString());
                }

                // ------------------------------------------------------------------------------------------------------------
                // 2) Try to load from database cache
                // ------------------------------------------------------------------------------------------------------------
                if (bytecode.Key == null && database.ObjectDatabase.Exists(mixinObjectId))
                {
                    using (var stream = database.ObjectDatabase.OpenStream(mixinObjectId))
                    {
                        // We have an existing stream, make sure the shader is compiled
                        var objectIdBuffer = new byte[ObjectId.HashSize];
                        if (stream.Read(objectIdBuffer, 0, ObjectId.HashSize) == ObjectId.HashSize)
                        {
                            var newBytecodeId = new ObjectId(objectIdBuffer);
                            bytecode = LoadEffectBytecode(database, newBytecodeId);

                            if (bytecode.Key != null)
                            {
                                // If we successfully retrieved it from cache, add it to index map so that it won't be collected and available for faster lookup 
                                database.ContentIndexMap[compiledUrl] = newBytecodeId;
                            }
                        }
                    }
                }
            }

            if (bytecode.Key != null)
            {
                return new EffectBytecodeCompilerResult(bytecode.Key, bytecode.Value);
            }

            // ------------------------------------------------------------------------------------------------------------
            // 3) Compile the shader
            // ------------------------------------------------------------------------------------------------------------
            lock (compilingShaders)
            {
                Task<EffectBytecodeCompilerResult> compilingShaderTask;
                if (compilingShaders.TryGetValue(mixinObjectId, out compilingShaderTask))
                {
                    // Note: Task might still be compiling
                    return compilingShaderTask;
                }

                // Compile the mixin in a Task
                if (CompileEffectAsynchronously)
                {
                    var compilerParametersCopy = compilerParameters != null ? new CompilerParameters(compilerParameters) : null;
                    var resultTask = Task.Factory.StartNew(() => CompileBytecode(mixin, effectParameters, compilerParametersCopy, mixinObjectId, database, compiledUrl), CancellationToken.None, TaskCreationOptions.None, taskSchedulerSelector != null ? taskSchedulerSelector(mixin, compilerParametersCopy.EffectParameters) : TaskScheduler.Default);

                    compilingShaders.Add(mixinObjectId, resultTask);

                    return resultTask;
                }
                else
                {
                    return CompileBytecode(mixin, effectParameters, compilerParameters, mixinObjectId, database, compiledUrl);
                }
            }
        }

        private EffectBytecodeCompilerResult CompileBytecode(ShaderMixinSource mixinTree, EffectCompilerParameters effectParameters, CompilerParameters compilerParameters, ObjectId mixinObjectId, DatabaseFileProvider database, string compiledUrl)
        {
            // Open the database for writing
            var log = new LoggerResult();
            var effectLog = GlobalLogger.GetLogger("EffectCompilerCache");

            // Note: this compiler is expected to not be async and directly write stuff in localLogger
            var compiledShader = base.Compile(mixinTree, effectParameters, compilerParameters).WaitForResult();
            compiledShader.CompilationLog.CopyTo(log);
            
            // If there are any errors, return immediately
            if (log.HasErrors)
            {
                lock (compilingShaders)
                {
                    compilingShaders.Remove(mixinObjectId);
                }

                log.CopyTo(effectLog);
                return new EffectBytecodeCompilerResult(null, log);
            }

            // Compute the bytecodeId
            var newBytecodeId = compiledShader.Bytecode.ComputeId();

            // Check if we really need to store the bytecode
            lock (bytecodes)
            {
                // Using custom serialization to the database to store an object with a custom id
                // TODO: Check if we really need to write the bytecode everytime even if id is not changed
                var memoryStream = new MemoryStream();
                compiledShader.Bytecode.WriteTo(memoryStream);
                
                // Write current cache at the end (not part of the pure bytecode, but we use this as meta info)
                var writer = new BinarySerializationWriter(memoryStream);
                writer.Write(CurrentCache);

                memoryStream.Position = 0;
                database.ObjectDatabase.Write(memoryStream, newBytecodeId, true);
                database.ContentIndexMap[compiledUrl] = newBytecodeId;

                // Save bytecode Id to the database cache as well
                memoryStream.SetLength(0);
                memoryStream.Write((byte[])newBytecodeId, 0, ObjectId.HashSize);
                memoryStream.Position = 0;
                database.ObjectDatabase.Write(memoryStream, mixinObjectId, true);

                if (!bytecodes.ContainsKey(newBytecodeId))
                {
                    log.Verbose($"New effect compiled #{effectCompileCount} [{mixinObjectId}] (db: {newBytecodeId})\r\n{compilerParameters?.ToStringPermutationsDetailed()}");
                    Interlocked.Increment(ref effectCompileCount);

                    // Replace or add new bytecode
                    bytecodes[newBytecodeId] = new KeyValuePair<EffectBytecode, EffectBytecodeCacheLoadSource>(compiledShader.Bytecode, EffectBytecodeCacheLoadSource.JustCompiled);
                }
            }

            lock (compilingShaders)
            {
                compilingShaders.Remove(mixinObjectId);
            }

            log.CopyTo(effectLog);
            return compiledShader;
        }

        private KeyValuePair<EffectBytecode, EffectBytecodeCacheLoadSource> LoadEffectBytecode(DatabaseFileProvider database, ObjectId bytecodeId)
        {
            KeyValuePair<EffectBytecode, EffectBytecodeCacheLoadSource> bytecodePair;

            if (!bytecodes.TryGetValue(bytecodeId, out bytecodePair))
            {
                if (!bytecodesByPassingStorage.Contains(bytecodeId) && database.ObjectDatabase.Exists(bytecodeId))
                {
                    using (var stream = database.ObjectDatabase.OpenStream(bytecodeId))
                    {
                        var bytecode = EffectBytecode.FromStream(stream);

                        // Try to read an integer that would specify what kind of cache it belongs to (if undefined because of old versions, mark it as dynamic cache)
                        var cacheSource = EffectBytecodeCacheLoadSource.DynamicCache;
                        if (stream.Position < stream.Length)
                        {
                            var binaryReader = new BinarySerializationReader(stream);
                            cacheSource = (EffectBytecodeCacheLoadSource)binaryReader.ReadInt32();
                        }
                        bytecodePair = new KeyValuePair<EffectBytecode, EffectBytecodeCacheLoadSource>(bytecode, cacheSource);
                    }
                }
                if (bytecodePair.Key != null)
                {
                    bytecodes.Add(bytecodeId, bytecodePair);
                }
            }

            // Always check that the bytecode is in sync with hash sources on all platforms
            if (bytecodePair.Key != null && IsBytecodeObsolete(bytecodePair.Key))
            {
                bytecodes.Remove(bytecodeId);
                bytecodePair = new KeyValuePair<EffectBytecode, EffectBytecodeCacheLoadSource>(null, EffectBytecodeCacheLoadSource.JustCompiled);
            }

            return bytecodePair;
        }

        private void RemoveObsoleteStoredResults(HashSet<string> modifiedShaders)
        {
            // TODO: avoid List<ObjectId> creation?
            var keysToRemove = new List<ObjectId>();
            foreach (var bytecodePair in bytecodes)
            {
                if (IsBytecodeObsolete(bytecodePair.Value.Key, modifiedShaders))
                    keysToRemove.Add(bytecodePair.Key);
            }

            foreach (var key in keysToRemove)
            {
                bytecodes.Remove(key);
                bytecodesByPassingStorage.Add(key);
            }
        }

        private bool IsBytecodeObsolete(EffectBytecode bytecode, HashSet<string> modifiedShaders)
        {
            // Don't use linq
            foreach (KeyValuePair<string, ObjectId> x in bytecode.HashSources)
            {
                if (modifiedShaders.Contains(x.Key)) return true;
            }
            return false;
        }

        private bool IsBytecodeObsolete(EffectBytecode bytecode)
        {
            foreach (var hashSource in bytecode.HashSources)
            {
                if (GetShaderSourceHash(hashSource.Key) != hashSource.Value)
                {
                    return true;
                }
            }
            return false;
        }
   }
}
