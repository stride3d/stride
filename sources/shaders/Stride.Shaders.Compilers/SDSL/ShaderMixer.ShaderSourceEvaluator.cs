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
    /// Expands inheritance (including implicit and transitive ones) and compositions, recursively. Each node gets all its classes;
    /// the module parts of the classes used in compositions are placed in the root afterwards (<see cref="CollectModules"/>).
    /// </summary>
    private ShaderMixinInstantiation EvaluateInheritanceAndCompositions(IExternalShaderLoader shaderLoader, SpirvContext context, ShaderMixinSource? parent, ShaderSource shaderSource, string? compositionPath = null, ShaderMixinSource? rootMixinSource = null, Dictionary<string, ShaderMixinInstantiation[]>? rootCompositions = null)
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
            // With the macros it is compiled with, which its default compositions get too
            ShaderClassSource classSource => new ShaderMixinSource { Mixins = { classSource }, Macros = [.. macros] },
            _ => throw new NotSupportedException($"Unsupported shader source type: {shaderSource.GetType().Name}"),
        };

        var compositions = new Dictionary<string, ShaderMixinInstantiation[]>();
        var result = new ShaderMixinInstantiation(new(), compositions);

        // Module compositions are resolved against the root, whichever node declares them
        rootMixinSource ??= shaderMixinSource;
        rootCompositions ??= compositions;

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

            var mixin = SpirvBuilder.BuildInheritanceListIncludingSelf(shaderLoader, context, mixinToMerge2, macros, mixinList, ResolveStep.Mix);
            // A module goes to the root wherever it is listed
            if (compositionPath != null && mixin.IsRootOnly && !mixin.IsModule)
                throw new InvalidOperationException(string.Format(Parsing.SDSLErrorMessages.SDSL0119, mixin.ToClassNameWithGenerics(), ReadableCompositionPath(compositionPath)));
        }

        var macrosArray = macros.ToArray();
        foreach (var shaderName in mixinList)
        {
            var shaderBuffers = shaderName.Buffer ?? throw new InvalidOperationException($"Shader buffers not loaded for {shaderName.ClassName}");

            ProcessCompositions(shaderLoader, context, shaderBuffers, shaderName.ClassName, shaderMixinSource, compositions, compositionPath, rootMixinSource, rootCompositions);

            shaderName.ModuleIdentity = ModuleIdentity(shaderLoader, shaderName, macrosArray);
            if (compositionPath != null && HasModulePart(shaderName))
            {
                shaderName.CompositionPath = compositionPath;
                var ancestors = new List<ShaderClassInstantiation>();
                SpirvBuilder.BuildInheritanceListWithoutSelf(shaderLoader, context, shaderName, macros, shaderBuffers.Context, ancestors, ResolveStep.Mix);
                shaderName.AncestorNames = ancestors.Select(x => x.ToClassNameWithGenerics()).ToList();
            }

            result.Mixins.Add(shaderName);
        }

        return result;
    }

    /// <summary>
    /// Whether a class has a module part: <c>module</c> members (variables, streams, compositions, methods) or struct types,
    /// or it is a module (<c>module X</c>) as a whole.
    /// </summary>
    private static bool HasModulePart(ShaderClassInstantiation shaderClass)
    {
        if (shaderClass.IsModule)
            return true;

        var shader = shaderClass.Buffer ?? throw new InvalidOperationException($"Shader buffers not loaded for {shaderClass.ClassName}");
        foreach (var i in shader.Context)
        {
            if (i.Op == Op.OpTypeStruct)
                return true;
        }

        foreach (var i in shader.Buffer)
        {
            if (i.Op == Op.OpVariableSDSL && (OpVariableSDSL)i is { } variable && variable.StorageClass != Specification.StorageClass.Function
                && (variable.Flags & VariableFlagsMask.Stage) != 0)
                return true;

            if (i.Op == Op.OpFunctionMetadataSDSL && (OpFunctionMetadataSDSL)i is { } functionInfo && (functionInfo.Flags & FunctionFlagsMask.Stage) != 0)
                return true;
        }

        return false;
    }

    /// <summary>
    /// The composition variables a shader declares, in declaration order.
    /// </summary>
    private static List<(string Name, bool IsModule, PointerType Type)> CompositionVariables(ShaderBuffers shader)
    {
        var result = new List<(string, bool, PointerType)>();
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

            result.Add((shader.Context.Names[variable.ResultId], (variable.Flags & VariableFlagsMask.Stage) != 0, pointer));
        }
        return result;
    }

    /// <summary>
    /// Discovers composition variables in a shader buffer and recursively evaluates them.
    /// </summary>
    private void ProcessCompositions(IExternalShaderLoader shaderLoader, SpirvContext context, ShaderBuffers shader, string shaderClassName, ShaderMixinSource shaderMixinSource, Dictionary<string, ShaderMixinInstantiation[]> compositions, string? compositionPath, ShaderMixinSource rootMixinSource, Dictionary<string, ShaderMixinInstantiation[]> rootCompositions)
    {
        foreach (var (variableName, isModule, pointer) in CompositionVariables(shader))
        {
            // A module composition is one slot for the whole effect: it belongs to its declaring shader's companion module in the root,
            // so its value comes from the root source and lands in the root compositions, whichever node declares it
            if (isModule && compositionPath != null && shaderMixinSource.Compositions.ContainsKey(variableName))
                throw new InvalidOperationException($"'{variableName}' is a `module compose` declared by '{shaderClassName}', so it is one slot for the whole effect and must be supplied at the root. It was supplied at '{ReadableCompositionPath(compositionPath)}'.");

            var valueSource = isModule ? rootMixinSource : shaderMixinSource;
            var valueCompositions = isModule ? rootCompositions : compositions;
            var valuePath = isModule ? null : compositionPath;

            // Already evaluated, at the root or through another shader inheriting the same declarer
            if (isModule && valueCompositions.ContainsKey(variableName))
                continue;

            // Use the composition from ShaderMixinSource if specified, otherwise use the default type, with the macros of the
            // node it is in (it is not in the tree, so it didn't get them from PropagateMacrosRecursively)
            if (!valueSource.Compositions.TryGetValue(variableName, out var compositionMixin))
            {
                if (pointer.BaseType is ShaderSymbol shaderSymbol)
                    compositionMixin = new ShaderMixinSource { Mixins = { new ShaderClassSource(shaderSymbol.Name) }, Macros = [.. valueSource.Macros] };
                else if (pointer.BaseType is ArrayType { BaseType: ShaderSymbol })
                    compositionMixin = new ShaderArraySource();
                else
                    throw new NotImplementedException();
            }

            if (compositionMixin is ShaderArraySource shaderArraySource)
            {
                var variableCompositions = new List<ShaderMixinInstantiation>();
                for (int index = 0; index < shaderArraySource.Values.Count; index++)
                    variableCompositions.Add(EvaluateInheritanceAndCompositions(shaderLoader, context, valueSource, shaderArraySource.Values[index], ChildCompositionPath(valuePath, $"{variableName}[{index}]"), rootMixinSource, rootCompositions));
                valueCompositions[variableName] = [.. variableCompositions];
            }
            else
            {
                var variableComposition = EvaluateInheritanceAndCompositions(shaderLoader, context, valueSource, compositionMixin, ChildCompositionPath(valuePath, variableName), rootMixinSource, rootCompositions);
                valueCompositions[variableName] = [variableComposition];
            }
        }
    }

    // The child key comes first, e.g. "Marcher.diffuseMarcher.environmentLights[0]"
    // TODO: Review: it seems like Stride compose variable the opposite way that we expect
    //       Let's change it so that it becomes {parentPath}.{key}!
    private static string ChildCompositionPath(string? parentPath, string key) => parentPath != null ? $"{key}.{parentPath}" : key;

    // For messages: the root's slot first, as written in an effect ("environmentLights[0].diffuseMarcher.Marcher" for the path above)
    private static string ReadableCompositionPath(string compositionPath) => string.Join('.', compositionPath.Split('.').Reverse());

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
