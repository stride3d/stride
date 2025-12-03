using Stride.Shaders.Parsing.SDSL;
using Stride.Shaders.Spirv.Building;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Shaders.Spirv.Core;
using static Stride.Shaders.Spirv.Specification;

namespace Stride.Shaders.Compilers.SDSL
{
    internal class EffectEvaluator(IExternalShaderLoader ShaderLoader)
    {
        public ShaderSource EvaluateEffects(ShaderSource source)
        {
            switch (source)
            {
                case ShaderClassSource classSource:
                    if (classSource.GenericArguments != null && classSource.GenericArguments.Length > 0)
                        throw new NotImplementedException();

                    var buffer = SpirvBuilder.GetOrLoadShader(ShaderLoader, classSource.ClassName);
                    if (buffer[0].Op == Op.OpSDSLEffect)
                    {
                        var mixinTree = new ShaderMixinSource();
                        foreach (var instruction in buffer)
                        {
                            if (instruction.Op == Op.OpSDSLMixin && (OpSDSLMixin)instruction is { } mixinInstruction)
                            {
                                var instSource = new ShaderClassSource(mixinInstruction.Mixin);
                                var evaluatedSource = EvaluateEffects(instSource);

                                Merge(mixinTree, evaluatedSource);
                            }
                            else if (instruction.Op == Op.OpSDSLMixinCompose && (OpSDSLMixinCompose)instruction is { } mixinComposeInstruction)
                            {
                                var instSource = new ShaderClassSource(mixinComposeInstruction.Mixin);
                                var evaluatedSource = EvaluateEffects(instSource);

                                MergeComposition(mixinTree, mixinComposeInstruction.Identifier, evaluatedSource);
                            }
                        }

                        return mixinTree;
                    }

                    return classSource;
                case ShaderMixinSource mixinSource:
                    var result = new ShaderMixinSource();
                    foreach (var mixin in mixinSource.Mixins)
                    {
                        var evaluatedMixin = EvaluateEffects(mixin);
                        Merge(result, evaluatedMixin);
                    }

                    foreach (var composition in mixinSource.Compositions)
                    {
                        var evaluatedMixin = EvaluateEffects(composition.Value);
                        MergeComposition(result, composition.Key, evaluatedMixin);
                    }

                    return result;
                default:
                    throw new NotImplementedException();
            }
        }

        public void Merge(ShaderMixinSource mixinTree, ShaderSource source)
        {
            switch (source)
            {
                case ShaderClassSource classSource:
                    mixinTree.Mixins.Add(classSource);
                    break;
                case ShaderMixinSource mixinSource:
                    foreach (var mixin in mixinSource.Mixins)
                    {
                        mixinTree.Mixins.Add(mixin);
                    }

                    foreach (var composition in mixinSource.Compositions)
                    {
                        if (mixinTree.Compositions.TryGetValue(composition.Key, out var mixinTreeComposition))
                            mixinTree.Compositions.Add(composition.Key, mixinTreeComposition = new ShaderMixinSource());
                        Merge((ShaderMixinSource)mixinTreeComposition, composition.Value);
                    }

                    break;
            }
        }

        public void MergeComposition(ShaderMixinSource mixinTree, string compositionName, ShaderSource evaluatedSource)
        {
            if (!mixinTree.Compositions.TryGetValue(compositionName, out var composition))
                mixinTree.Compositions.Add(compositionName, composition = new ShaderMixinSource());

            Merge((ShaderMixinSource)composition, evaluatedSource);
        }
    }
}
