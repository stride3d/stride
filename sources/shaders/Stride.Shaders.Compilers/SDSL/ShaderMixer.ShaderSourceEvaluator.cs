using CommunityToolkit.HighPerformance;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Parsing.SDSL.AST;
using Stride.Shaders.Spirv;
using Stride.Shaders.Spirv.Building;
using Stride.Shaders.Spirv.Core;

namespace Stride.Shaders.Compilers.SDSL;

using static Stride.Shaders.Spirv.Specification;

public partial class ShaderMixer
{
    /// <summary>
    /// Expands inheritance (including implicit and transitive ones) and composition (including shaders that should be merged at stage level).
    /// </summary>
    private ShaderMixinInstantiation EvaluateInheritanceAndCompositions(IExternalShaderLoader shaderLoader, SpirvContext context, ShaderMixinSource? parent, ShaderSource shaderSource, Action<ShaderClassInstantiation>? promoteToParent = null, HashSet<string>? needsFullImport = null)
    {
        var mixinList = new List<ShaderClassInstantiation>();

        Span<ShaderMacro> macros = (shaderSource, parent) switch
        {
            (ShaderMixinSource mixinSource, _) => mixinSource.Macros.AsSpan(),
            (_, not null) => parent.Macros.AsSpan(),
            _ => Span<ShaderMacro>.Empty,
        };

        var shaderMixinSource = shaderSource switch
        {
            ShaderMixinSource mixinSource2 => mixinSource2,
            ShaderClassSource classSource => new ShaderMixinSource { Mixins = { classSource } },
            _ => throw new NotSupportedException($"Unsupported shader source type: {shaderSource.GetType().Name}"),
        };

        var compositions = new Dictionary<string, ShaderMixinInstantiation[]>();
        var result = new ShaderMixinInstantiation(new(), compositions);

        foreach (var mixinToMerge in shaderMixinSource.Mixins)
        {
            var shaderBuffer = SpirvBuilder.GetOrLoadShader(shaderLoader, mixinToMerge.ClassName, mixinToMerge.GenericArguments, macros);

            var mixinToMerge2 = new ShaderClassInstantiation(mixinToMerge.ClassName, []);
            mixinToMerge2.Buffer = shaderBuffer;
            // Copy back updated shader name (in case it had generic parameters)
            foreach (var i in shaderBuffer.Buffer)
            {
                if (i.Op == Op.OpShaderSDSL && (OpShaderSDSL)i is { } shaderInstruction)
                {
                    mixinToMerge2.ClassName = shaderInstruction.ShaderName;
                    break;
                }
            }

            SpirvBuilder.BuildInheritanceListIncludingSelf(shaderLoader, context, mixinToMerge2, macros, mixinList, ResolveStep.Mix);
        }

        ProcessClasses(shaderLoader, context, mixinList, shaderMixinSource, result, compositions, promoteToParent, needsFullImport);

        return result;
    }

    /// <summary>
    /// Processes a flat mixin list (already expanded with inheritance) to produce the final ShaderMixinInstantiation.
    ///
    /// Algorithm:
    ///   1. Pre-scan: identify shaders that need full (non-stage-only) import at root level,
    ///      based on NeedsFullImport flags set by the parser when a stage method calls non-stage members.
    ///   2. Build a promoteToParent callback for compositions to promote stage shaders to the parent level.
    ///      At root level (promoteToParent == null), this creates a closure that adds shaders to the root result,
    ///      choosing between stage-only, full import, or pull-forward based on needsFullImport and mixinList membership.
    ///   3. Main loop: iterate each shader in mixinList, detect stage members and compositions,
    ///      recursively process compositions, then promote stage/needsFullImport shaders to parent.
    ///   4. Post-processing (root only): upgrade any stage-only imports to full imports if compositions
    ///      discovered during step 3 that their non-stage members are needed.
    /// </summary>
    private void ProcessClasses(IExternalShaderLoader shaderLoader, SpirvContext context, List<ShaderClassInstantiation> mixinList, ShaderMixinSource shaderMixinSource, ShaderMixinInstantiation result, Dictionary<string, ShaderMixinInstantiation[]> compositions, Action<ShaderClassInstantiation>? promoteToParent = null, HashSet<string>? needsFullImport = null)
    {
        // --- Step 1: Pre-scan for shaders needing full import ---
        // Two sources:
        //   - OpMixinInheritSDSL with NeedsFullImport: parent shader whose non-stage members are called by a child's stage method
        //   - OpFunctionMetadataSDSL with ReferencesNonStage: shader's own non-stage members are called by its own stage method
        // The set is shared across root and composition calls so that compositions can contribute.
        needsFullImport ??= new HashSet<string>();
        foreach (var shader in mixinList)
        {
            ScanNeedsFullImport(shader, needsFullImport);
        }

        // --- Step 2: Build the promote-to-parent callback ---
        // At root level, we create a closure that decides how to add shaders to the root mixin list.
        // At composition level, we reuse the parent's callback directly.
        var promoteToParentForCompositions = promoteToParent;
        if (promoteToParentForCompositions == null)
        {
            promoteToParentForCompositions = shaderName =>
            {
                var stageOnlyVariant = new ShaderClassInstantiation(shaderName.ClassName, shaderName.GenericArguments, ImportStageOnly: true) { Buffer = shaderName.Buffer, Symbol = shaderName.Symbol };

                // Skip if already added (either standard or stage-only)
                if (result.Mixins.Contains(shaderName) || result.Mixins.Contains(stageOnlyVariant))
                    return;

                if (mixinList.Contains(shaderName))
                {
                    var currentlyMixedList = result.Mixins[..];
                    SpirvBuilder.BuildInheritanceListIncludingSelf(shaderLoader, context, shaderName, shaderMixinSource.Macros.AsSpan(), currentlyMixedList, ResolveStep.Mix);

                    var newShadersToMergeNow = currentlyMixedList[result.Mixins.Count..];
                    result.Mixins.AddRange(newShadersToMergeNow);
                }
                else if (needsFullImport.Contains(shaderName.ClassName))
                {
                    // A fully-imported shader needs its parent shaders also fully imported,
                    // because its non-stage code may call non-stage methods from parents.
                    // Build the inheritance chain and add any missing parents (upgrading stage-only to full).
                    var inheritanceList = new List<ShaderClassInstantiation>();
                    SpirvBuilder.BuildInheritanceListIncludingSelf(shaderLoader, context, shaderName, shaderMixinSource.Macros.AsSpan(), inheritanceList, ResolveStep.Mix);
                    foreach (var ancestor in inheritanceList)
                    {
                        var ancestorStageOnly = new ShaderClassInstantiation(ancestor.ClassName, ancestor.GenericArguments, ImportStageOnly: true) { Buffer = ancestor.Buffer, Symbol = ancestor.Symbol };
                        // Upgrade stage-only to full import
                        var stageOnlyIndex = result.Mixins.IndexOf(ancestorStageOnly);
                        if (stageOnlyIndex >= 0)
                        {
                            result.Mixins[stageOnlyIndex] = ancestor;
                        }
                        else if (!result.Mixins.Contains(ancestor))
                        {
                            result.Mixins.Add(ancestor);
                        }
                    }
                }
                else
                {
                    result.Mixins.Add(stageOnlyVariant);
                }
            };
        }

        // --- Step 3: Main loop — detect stage members, process compositions, promote to parent ---
        for (int shaderIndex = 0; shaderIndex < mixinList.Count; shaderIndex++)
        {
            var shaderName = mixinList[shaderIndex];

            // May already be present if promoteToParent pulled it forward earlier
            if (result.Mixins.Contains(shaderName))
                continue;

            var shaderBuffers = shaderName.Buffer ?? throw new InvalidOperationException($"Shader buffers not loaded for {shaderName.ClassName}");

            bool hasStage = HasStageMembersOrCompositions(shaderBuffers);

            // Discover and recursively process compositions
            ProcessCompositions(shaderLoader, context, shaderBuffers, shaderMixinSource, compositions, promoteToParentForCompositions, needsFullImport);

            // Promote to parent level if this shader has stage members or needs full import
            if (hasStage || needsFullImport.Contains(shaderName.ClassName))
                promoteToParent?.Invoke(shaderName);

            // Add to result *after* compositions are processed — a composition might add a "stage" inheritance
            // that defines a struct, and that must come before the composition shader that uses it.
            result.Mixins.Add(shaderName);
        }

        // --- Step 4: Post-processing (root only) — upgrade stage-only to full import ---
        // Compositions processed in step 3 may have added entries to needsFullImport.
        // Shaders already added as stage-only need upgrading.
        if (promoteToParent == null)
        {
            for (int i = 0; i < result.Mixins.Count; i++)
            {
                var mixin = result.Mixins[i];
                if (mixin.ImportStageOnly && needsFullImport.Contains(mixin.ClassName))
                {
                    result.Mixins[i] = new ShaderClassInstantiation(mixin.ClassName, mixin.GenericArguments) { Buffer = mixin.Buffer, Symbol = mixin.Symbol };
                }
            }
        }
    }

    /// <summary>
    /// Scans a shader's SPIR-V buffer for NeedsFullImport flags and adds matching shader names to the set.
    /// </summary>
    private static void ScanNeedsFullImport(ShaderClassInstantiation shader, HashSet<string> needsFullImport)
    {
        var shaderBuffers = shader.Buffer ?? throw new InvalidOperationException($"Shader buffers not loaded for {shader.ClassName}");
        foreach (var i in shaderBuffers.Context)
        {
            if (i.Op == Op.OpMixinInheritSDSL && (OpMixinInheritSDSL)i is { } inherit
                && (inherit.Flags & MixinInheritFlagsMask.NeedsFullImport) != 0
                && shaderBuffers.Context.ReverseTypes.TryGetValue(inherit.Shader, out var inheritType) && inheritType is ShaderSymbol lss)
            {
                needsFullImport.Add(lss.Name);
            }
        }
        foreach (var i in shaderBuffers.Buffer)
        {
            if (i.Op == Op.OpFunctionMetadataSDSL && (OpFunctionMetadataSDSL)i is { } fi
                && (fi.Flags & FunctionFlagsMask.ReferencesNonStage) != 0)
            {
                needsFullImport.Add(shader.ClassName);
                break;
            }
        }
    }

    /// <summary>
    /// Checks whether a shader buffer contains stage members (structs/streams, stage variables, or stage functions).
    /// Also detects composition variables (returns true via hasStage if struct declarations exist).
    /// </summary>
    private static bool HasStageMembersOrCompositions(ShaderBuffers shader)
    {
        bool hasStage = false;
        foreach (var i in shader.Context)
        {
            if (i.Op == Op.OpTypeStruct)
            {
                hasStage = true;
            }
        }

        foreach (var i in shader.Buffer)
        {
            if (i.Op == Op.OpVariableSDSL && (OpVariableSDSL)i is { } variable && variable.StorageClass != Specification.StorageClass.Function)
            {
                hasStage |= (variable.Flags & VariableFlagsMask.Stage) != 0;
            }

            if (i.Op == Op.OpFunctionMetadataSDSL && (OpFunctionMetadataSDSL)i is { } functionInfo)
            {
                hasStage |= (functionInfo.Flags & FunctionFlagsMask.Stage) != 0;
            }
        }

        return hasStage;
    }

    /// <summary>
    /// Discovers composition variables in a shader buffer and recursively evaluates them.
    /// </summary>
    private void ProcessCompositions(IExternalShaderLoader shaderLoader, SpirvContext context, ShaderBuffers shader, ShaderMixinSource shaderMixinSource, Dictionary<string, ShaderMixinInstantiation[]> compositions, Action<ShaderClassInstantiation> promoteToParent, HashSet<string> needsFullImport)
    {
        foreach (var i in shader.Buffer)
        {
            if (i.Op != Op.OpVariableSDSL)
                continue;

            var variable = (OpVariableSDSL)i;
            if (variable.StorageClass == Specification.StorageClass.Function)
                continue;

            var variableType = shader.Context.ReverseTypes[variable.ResultType];
            if (variableType is not PointerType pointer || pointer.BaseType is not (ShaderSymbol or ArrayType { BaseType: ShaderSymbol }))
                continue;

            var variableName = shader.Context.Names[variable.ResultId];

            // Use the composition from ShaderMixinSource if specified, otherwise use the default type
            if (!shaderMixinSource.Compositions.TryGetValue(variableName, out var compositionMixin))
            {
                if (pointer.BaseType is ShaderSymbol shaderSymbol)
                    compositionMixin = new ShaderMixinSource { Mixins = { new ShaderClassSource(shaderSymbol.Name) } };
                else if (pointer.BaseType is ArrayType { BaseType: ShaderSymbol })
                    compositionMixin = new ShaderArraySource();
                else
                    throw new NotImplementedException();
            }

            if (compositionMixin is ShaderArraySource shaderArraySource)
            {
                var variableCompositions = new List<ShaderMixinInstantiation>();
                foreach (var value in shaderArraySource.Values)
                    variableCompositions.Add(EvaluateInheritanceAndCompositions(shaderLoader, context, shaderMixinSource, value, promoteToParent, needsFullImport));
                compositions[variableName] = [.. variableCompositions];
            }
            else
            {
                var variableComposition = EvaluateInheritanceAndCompositions(shaderLoader, context, shaderMixinSource, compositionMixin, promoteToParent, needsFullImport);
                compositions[variableName] = [variableComposition];
            }
        }
    }

    private void PropagateMacrosRecursively(ShaderSource child, ShaderMixinSource? parent = null)
    {
        var existingMacros = new HashSet<string>();
        if (child is ShaderMixinSource mixinChild)
        {
            foreach (var macro in mixinChild.Macros)
            {
                existingMacros.Add(macro.Name);
            }
            if (parent != null)
            {
                foreach (var macro in parent.Macros)
                {
                    if (!existingMacros.Contains(macro.Name))
                        mixinChild.AddMacro(macro.Name, macro.Definition);
                }
            }

            // Recurse
            foreach (var mixin in mixinChild.Mixins)
            {
                PropagateMacrosRecursively(mixin, mixinChild);
            }
            foreach (var composition in mixinChild.Compositions)
            {
                PropagateMacrosRecursively(composition.Value, mixinChild);
            }
        }
        else if (child is ShaderArraySource arrayChild)
        {
            foreach (var mixin in arrayChild)
            {
                PropagateMacrosRecursively(mixin, parent);
            }
        }
    }
}
