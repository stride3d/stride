using Stride.Shaders.Core;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.Analysis;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;
using Stride.Shaders.Spirv.Core.Buffers;
using Stride.Shaders.Spirv.Processing;
using Stride.Shaders.Spirv.Tools;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Stride.Core.Diagnostics;
using Stride.Core.Storage;
using Stride.Shaders.Parsing.SDFX.AST;

namespace Stride.Shaders.Compilers.SDSL;

public record struct CompileOptions()
{
    /// <summary>Whether to register the compiled shader in the cache.</summary>
    public bool RegisterInCache { get; init; } = true;
    /// <summary>Whether to emit OpSourceHashSDSL for cache validation.</summary>
    public bool EmitSourceHash { get; init; } = true;
    /// <summary>Original source code before preprocessing, used for OpSource debug info. Falls back to preprocessed code if null.</summary>
    public string? OriginalCode { get; init; }
}

public record struct SDSLC(IExternalShaderLoader ShaderLoader)
{
    public readonly bool Compile(string? filename, string code, ObjectId hash, ReadOnlySpan<ShaderMacro> macros, ILogger log, [MaybeNullWhen(false)] out ShaderBuffers lastBuffer, CompileOptions options = default)
    {
        lastBuffer = default;

        var parsed = SDSLParser.Parse(code);
        if (parsed.Errors.Count > 0)
        {
            foreach (var error in parsed.Errors)
                log.Error(error.ToString());
            return false;
        }
        if (parsed.AST is not ShaderFile sf)
            return false;

        // TODO: support namespace
        var declarations = sf.Namespaces.SelectMany(x => x.Declarations).Concat(sf.RootDeclarations);
        foreach (var declaration in declarations)
        {
            if (declaration is ShaderClass shader)
            {
                var compiler = new CompilerUnit();
                SymbolTable table = new(compiler.Context, ShaderLoader)
                {
                    CurrentMacros = [.. macros],
                };

                // Add debug source info (OpString/OpSource for debug mapping, OpSourceHashSDSL for cache validation)
                if (filename != null)
                {
                    var filenameId = compiler.Context.Add(new OpString(compiler.Context.Bound++, filename)).ResultId;
                    // TODO: Add SourceLanguage.SDSL
                    compiler.Context.Add(new OpSource(Spirv.Specification.SourceLanguage.Unknown, 0, filenameId, options.OriginalCode ?? code));
                    if (options.EmitSourceHash)
                        compiler.Context.Add(new OpSourceHashSDSL(filenameId, (int)hash.Hash1, (int)hash.Hash2, (int)hash.Hash3, (int)hash.Hash4));
                    compiler.SourceFileId = filenameId;
                }
                // TODO: Do we want to record macros with a custom OpMacroSDSL? (mostly for debug purposes)

                compiler.Macros.AddRange(macros);
                bool hasErrors = false;

                try
                {
                    shader.Compile(table, compiler);
                }
                catch (Exception e)
                {
                    log.Error(e.Message, e);
                    hasErrors = true;
                }

                // Collect dependency hashes from all shaders loaded during compilation.
                // Each loaded shader's buffer has its own OpSourceHashSDSL — copy them into this shader's context.
                var seenHashes = new HashSet<ObjectId>();
                // Add own hash so we don't duplicate it
                seenHashes.Add(hash);
                foreach (var loadedShader in table.DeclaredShaders.Values)
                {
                    if (loadedShader.Name != shader.Name
                        && ShaderLoader.Cache.TryLoadFromCache(loadedShader.Name, null, macros, out var depBuffer, out _))
                    {
                        CopySourceHashes(depBuffer, compiler.Context, seenHashes);
                    }
                }

                foreach (var info in table.Infos)
                    log.Info(info.ToString());

                foreach (var warning in table.Warnings)
                    log.Warning(warning.ToString());

                if (table.Errors.Count > 0)
                {
                    foreach (var error in table.Errors)
                        log.Error(error.ToString());
                    hasErrors = true;
                }

                if (hasErrors)
                    return false;

                lastBuffer = compiler.ToShaderBuffers();

                // Ensure all names and types from OpName/OpType instructions are registered
                // in the context dictionaries. The compiler may not explicitly register everything
                // (e.g. names for imported IDs, or types from InsertWithoutDuplicates).
                ShaderClass.ProcessNameAndTypes(lastBuffer.Context, new ShaderLoaderImporter(ShaderLoader));

                if (options.RegisterInCache)
                    ShaderLoader.Cache.RegisterShader(shader.Name, null, macros, lastBuffer, hash);
            }
            else if (declaration is ShaderEffect or EffectParameters)
            {
                // Ignore (using C# codegen for now)
            }
            else
            {
                log.Error($"Compiling declaration [{declaration.GetType()}] is not implemented");
                return false;
            }
        }

        return lastBuffer != null;
    }

    /// <summary>
    /// Copies OpSourceHashSDSL entries from a dependency's buffer into the target context, skipping duplicates.
    /// </summary>
    private static void CopySourceHashes(ShaderBuffers source, SpirvContext target, HashSet<ObjectId> seenHashes)
    {
        foreach (var inst in source.Context)
        {
            if (inst.Op != Spirv.Specification.Op.OpSourceHashSDSL)
                continue;

            var sourceHash = (OpSourceHashSDSL)inst;
            var depHash = new ObjectId((uint)sourceHash.Hash1, (uint)sourceHash.Hash2, (uint)sourceHash.Hash3, (uint)sourceHash.Hash4);
            if (!seenHashes.Add(depHash))
                continue;

            // Find the filename string from the source context
            foreach (var s in source.Context)
            {
                if (s.Op == Spirv.Specification.Op.OpString && ((OpString)s).ResultId == sourceHash.File)
                {
                    var depFilenameId = target.Add(new OpString(target.Bound++, ((OpString)s).Value)).ResultId;
                    target.Add(new OpSourceHashSDSL(depFilenameId, sourceHash.Hash1, sourceHash.Hash2, sourceHash.Hash3, sourceHash.Hash4));
                    break;
                }
            }
        }
    }
}
