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

public record struct SDSLC(IExternalShaderLoader ShaderLoader)
{
    public readonly bool Compile(string filename, string code, ObjectId hash, ReadOnlySpan<ShaderMacro> macros, ILogger log, [MaybeNullWhen(false)] out ShaderBuffers lastBuffer)
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
                SymbolTable table = new(compiler.Context)
                {
                    ShaderLoader = ShaderLoader,
                    CurrentMacros = [.. macros],
                };

                // Add OpSource
                var filenameId = compiler.Context.Add(new OpString(compiler.Context.Bound++, filename)).ResultId;
                // TODO: Add SourceLanguage.SDSL
                compiler.Context.Add(new OpSource(Spirv.Specification.SourceLanguage.Unknown, 0, filenameId, null));
                compiler.Context.Add(new OpSourceHashSDSL(filenameId, (int)hash.Hash1, (int)hash.Hash2, (int)hash.Hash3, (int)hash.Hash4));
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
}
