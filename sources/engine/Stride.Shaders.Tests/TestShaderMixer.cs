// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

using Stride.Rendering;
using Stride.Engine.Shaders.Mixins;
using Stride.Core.IO;
using Stride.Shaders.Compiler;
using Stride.Core.Shaders.Ast;

namespace Stride.Core.Shaders.Tests
{
    public class TestShaderMixer
    {
        private ShaderSourceManager sourceManager;
        private ShaderLoader shaderLoader;

        public TestShaderMixer()
        {
            sourceManager = new ShaderSourceManager();
            sourceManager.LookupDirectoryList.Add(@"..\..\Shaders");
            shaderLoader = new ShaderLoader(sourceManager);
        }

        [Fact]
        public void TestRenameBasic() // simple mix with inheritance
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("Parent"),
                    new ShaderClassSource("Child")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.False(mcm.ErrorWarningLog.HasErrors);

            var mixer = new StrideShaderMixer(mcm.Mixins["Child"], mcm.Mixins, null);
            mixer.Mix();

            //var childMixinInfo = mcm.Mixins["Child"].ParsingInfo;
            //Assert.Equal("Child_AddBaseValue", childMixinInfo.MethodDeclarations.First().Name.Text);
            //Assert.Equal("Parent_AddBaseValue", (childMixinInfo.BaseMethodCalls.First().Target as VariableReferenceExpression).Name.Text);
            //Assert.Equal("Parent_baseValue", childMixinInfo.VariableReferenceExpressions[0].Name.Text);
        }

        [Fact]
        public void TestRenameStatic() // mix with call to a static method
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("StaticMixin"),
                    new ShaderClassSource("StaticCallMixin")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.False(mcm.ErrorWarningLog.HasErrors);

            var mixer = new StrideShaderMixer(mcm.Mixins["StaticCallMixin"], mcm.Mixins, null);
            mixer.Mix();

            //var staticCallMixinInfo = mcm.Mixins["StaticCallMixin"].ParsingInfo;
            //Assert.Equal("StaticMixin_staticCall", (staticCallMixinInfo.MethodCalls.First().Target as VariableReferenceExpression).Name.Text);
            //Assert.Equal("StaticMixin_staticMember", ((staticCallMixinInfo.StaticMemberReferences.First().Node as UnaryExpression).Expression as VariableReferenceExpression).Name.Text);
        }

        [Fact]
        public void TestBasicExternMix() // mix with an extern class
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("ExternMixin"),
                    new ShaderClassSource("ExternTest")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.False(mcm.ErrorWarningLog.HasErrors);

            var mcmFinal = mcm.DeepClone();
            var externDictionary = new Dictionary<Variable, List<ModuleMixin>>();
            externDictionary.Add(mcmFinal.Mixins["ExternTest"].VariableDependencies.First().Key, new List<ModuleMixin>{ mcm.Mixins["ExternMixin"].DeepClone() });

            var mixer = new StrideShaderMixer(mcmFinal.Mixins["ExternTest"], mcmFinal.Mixins, externDictionary);
            mixer.Mix();

            //var externTestMixinInfo = mcmFinal.Mixins["ExternTest"].ParsingInfo;
            //Assert.Equal("ExternTest_myExtern_ExternMixin_externFunc", (externTestMixinInfo.ExternMethodCalls.First().MethodInvocation.Target as VariableReferenceExpression).Name.Text);
            //Assert.Equal("ExternTest_myExtern_ExternMixin_externMember", ((externTestMixinInfo.ExternMemberReferences.First().Node as ReturnStatement).Value as VariableReferenceExpression).Name.Text);
        }

        [Fact]
        public void TestDeepMix() // mix with multiple levels of extern classes
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("ExternMixin"),
                    new ShaderClassSource("DeepExtern"),
                    new ShaderClassSource("DeepExternTest")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.False(mcm.ErrorWarningLog.HasErrors);

            var mcmFinal = mcm.DeepClone();
            var depext = mcm.Mixins["DeepExtern"].DeepClone();
            var deepDictionary = new Dictionary<Variable, List<ModuleMixin>>();
            deepDictionary.Add(mcmFinal.Mixins["DeepExternTest"].VariableDependencies.First().Key, new List<ModuleMixin> { depext });
            deepDictionary.Add(depext.VariableDependencies.First().Key, new List<ModuleMixin> { mcm.Mixins["ExternMixin"].DeepClone() });
            var mixer = new StrideShaderMixer(mcmFinal.Mixins["DeepExternTest"], mcmFinal.Mixins, deepDictionary);
            mixer.Mix();

            //var externDeepTest = mcmFinal.Mixins["DeepExternTest"].ParsingInfo;
            //Assert.Equal("DeepExternTest_myExtern_DeepExtern_myExtern_ExternMixin_externFunc", (externDeepTest.ExternMethodCalls.First().MethodInvocation.Target as VariableReferenceExpression).Name.Text);
            //Assert.Equal("DeepExternTest_myExtern_DeepExtern_myExtern_ExternMixin_externMember", ((externDeepTest.ExternMemberReferences.First().Node as ReturnStatement).Value as VariableReferenceExpression).Name.Text);
        }

        [Fact]
        public void TestMultipleStatic() // check that static calls only written once
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("StaticMixin"),
                    new ShaderClassSource("StaticCallMixin"),
                    new ShaderClassSource("TestMultipleStatic"),
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.False(mcm.ErrorWarningLog.HasErrors);

            var mcmFinal = mcm.DeepClone();
            var extDictionary = new Dictionary<Variable, List<ModuleMixin>>();
            extDictionary.Add(mcmFinal.Mixins["TestMultipleStatic"].VariableDependencies.First().Key, new List<ModuleMixin>{ mcm.Mixins["StaticCallMixin"].DeepClone() });
            var mixer = new StrideShaderMixer(mcmFinal.Mixins["TestMultipleStatic"], mcmFinal.Mixins, extDictionary);
            mixer.Mix();

            //Assert.Equal(1, mixer.MixedShader.Members.OfType<MethodDeclaration>().Count(x => x.Name.Text == "StaticMixin_staticCall"));
            //Assert.Equal(1, mixer.MixedShader.Members.OfType<Variable>().Count(x => x.Name.Text == "StaticMixin_staticMember"));
        }

        [Fact]
        public void TestStageCall()
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("StageBase"),
                    new ShaderClassSource("StageCallExtern"),
                    new ShaderClassSource("StaticStageCallTest")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.False(mcm.ErrorWarningLog.HasErrors);

            var mcmFinal = mcm.DeepClone();
            var extDictionary = new Dictionary<Variable, List<ModuleMixin>>();
            extDictionary.Add(mcmFinal.Mixins["StaticStageCallTest"].VariableDependencies.First().Key, new List<ModuleMixin>{mcm.Mixins["StageCallExtern"].DeepClone()});
            var mixer = new StrideShaderMixer(mcmFinal.Mixins["StaticStageCallTest"], mcmFinal.Mixins, extDictionary);
            mixer.Mix();

            //var extPI = mcmExtern.Mixins["StageCallExtern"].ParsingInfo;
            //var finalPI = mcmFinal.Mixins["StaticStageCallTest"].ParsingInfo;

            //Assert.Equal(1, extPI.StageMethodCalls.Count);
            //Assert.Equal(1, finalPI.MethodDeclarations.Count);
            //Assert.Equal(finalPI.MethodDeclarations[0], extPI.StageMethodCalls[0].Target.TypeInference.Declaration);
            //Assert.Equal(finalPI.MethodDeclarations[0].Name.Text, (extPI.StageMethodCalls[0].Target as VariableReferenceExpression).Name.Text);

            //Assert.Equal(1, extPI.VariableReferenceExpressions.Count);
            //Assert.Equal(2, finalPI.Variables.Count);
            //Assert.Equal(finalPI.Variables[1], extPI.VariableReferenceExpressions[0].TypeInference.Declaration);
            //Assert.Equal(finalPI.Variables[1].Name.Text, extPI.VariableReferenceExpressions[0].TypeInference.Declaration.Name.Text);
        }

        [Fact]
        public void TestMergeSemantics()
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("SemanticTest")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.False(mcm.ErrorWarningLog.HasErrors);

            var mixer = new StrideShaderMixer(mcm.Mixins["SemanticTest"], mcm.Mixins, null);
            mixer.Mix();

            //Assert.Equal(1, mixer.MixedShader.Members.OfType<Variable>().Count());
        }

        [Fact]
        public void TestStreams()
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("StreamTest")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.False(mcm.ErrorWarningLog.HasErrors);

            var mixer = new StrideShaderMixer(mcm.Mixins["StreamTest"], mcm.Mixins, null);
            mixer.Mix();
        }

        [Fact]
        public void TestStageAssignement()
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("StageValueReference"),
                    new ShaderClassSource("StageValueTest")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.False(mcm.ErrorWarningLog.HasErrors);

            var mcmFinal = mcm.DeepClone();
            var extDictionary = new Dictionary<Variable, List<ModuleMixin>>();
            extDictionary.Add(mcmFinal.Mixins["StageValueTest"].VariableDependencies.First().Key, new List<ModuleMixin>{ mcm.Mixins["StageValueReference"].DeepClone() });
            var mixerFinal = new StrideShaderMixer(mcmFinal.Mixins["StageValueTest"], mcmFinal.Mixins, extDictionary);
            mixerFinal.Mix();
        }

        [Fact]
        public void TestClone()
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("CloneTestBase"),
                    new ShaderClassSource("CloneTestRoot"),
                    new ShaderClassSource("CloneTestExtern")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.False(mcm.ErrorWarningLog.HasErrors);

            var mcmFinal = mcm.DeepClone();
            var extDictionary = new Dictionary<Variable, List<ModuleMixin>>();
            var keys = mcmFinal.Mixins["CloneTestRoot"].VariableDependencies.Keys.ToList();
            extDictionary.Add(keys[0], new List<ModuleMixin>{ mcm.Mixins["CloneTestExtern"].DeepClone() });
            extDictionary.Add(keys[1], new List<ModuleMixin>{ mcm.Mixins["CloneTestExtern"].DeepClone() });
            var mixerFinal = new StrideShaderMixer(mcmFinal.Mixins["CloneTestRoot"], mcmFinal.Mixins, extDictionary);
            mixerFinal.Mix();
        }

        [Fact]
        public void TestBaseThis()
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("BaseTestChild"),
                    new ShaderClassSource("BaseTestInter"),
                    new ShaderClassSource("BaseTestParent")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.False(mcm.ErrorWarningLog.HasErrors);

            var mixerFinal = new StrideShaderMixer(mcm.Mixins["BaseTestChild"], mcm.Mixins, null);
            mixerFinal.Mix();
        }

        [Fact]
        public void TestForEachStatementExpand()
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("ForEachTest")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.False(mcm.ErrorWarningLog.HasErrors);

            var mixerFinal = new StrideShaderMixer(mcm.Mixins["ForEachTest"], mcm.Mixins, null);
            mixerFinal.Mix();
        }

        [Fact]
        public void TestStreamSolver()
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("StreamChild"),
                    new ShaderClassSource("StreamParent0"),
                    new ShaderClassSource("StreamParent1")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.False(mcm.ErrorWarningLog.HasErrors);

            var mixerFinal = new StrideShaderMixer(mcm.Mixins["StreamChild"], mcm.Mixins, null);
            mixerFinal.Mix();
        }

        [Fact]
        public void TestNonStageStream()
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("NonStageStreamTest"),
                    new ShaderClassSource("StreamParent2"),
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.False(mcm.ErrorWarningLog.HasErrors);

            var mcmFinal = mcm.DeepClone();
            var extDictionary = new Dictionary<Variable, List<ModuleMixin>>();
            var keys = mcmFinal.Mixins["NonStageStreamTest"].VariableDependencies.Keys.ToList();
            extDictionary.Add(keys[0], new List<ModuleMixin> { mcm.Mixins["StreamParent2"].DeepClone() });
            extDictionary.Add(keys[1], new List<ModuleMixin> { mcm.Mixins["StreamParent2"].DeepClone() });
            var mixerFinal = new StrideShaderMixer(mcmFinal.Mixins["NonStageStreamTest"], mcmFinal.Mixins, extDictionary);
            mixerFinal.Mix();
        }

        [Fact]
        public void TestStreamSolverExtern()
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("StreamChild"),
                    new ShaderClassSource("StreamParent0"),
                    new ShaderClassSource("StreamParent1"),
                    new ShaderClassSource("StreamSolverExternTest")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.False(mcm.ErrorWarningLog.HasErrors);

            var mcmFinal = mcm.DeepClone();

            var extDictionary = new Dictionary<Variable, List<ModuleMixin>>();
            extDictionary.Add(mcmFinal.Mixins["StreamSolverExternTest"].VariableDependencies.First().Key, new List<ModuleMixin>{ mcm.Mixins["StreamChild"].DeepClone() });
            var mixerFinal = new StrideShaderMixer(mcmFinal.Mixins["StreamSolverExternTest"], mcmFinal.Mixins, extDictionary);
            mixerFinal.Mix();
        }

        [Fact]
        public void TestExternArray() // check behavior with a array of compositions
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("ExternMixin"),
                    new ShaderClassSource("TestExternArray")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.False(mcm.ErrorWarningLog.HasErrors);

            var mcmFinal = mcm.DeepClone();

            var extDictionary = new Dictionary<Variable, List<ModuleMixin>>();
            var mixins = new List<ModuleMixin> { mcm.Mixins["ExternMixin"].DeepClone(), mcm.Mixins["ExternMixin"].DeepClone() };
            extDictionary.Add(mcmFinal.Mixins["TestExternArray"].VariableDependencies.First().Key, mixins);
            var mixerFinal = new StrideShaderMixer(mcmFinal.Mixins["TestExternArray"], mcmFinal.Mixins, extDictionary);
            mixerFinal.Mix();
        }

        [Fact]
        public void TestConstantBuffer() // check behavior with a array of compositions
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("ConstantBufferTest")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.False(mcm.ErrorWarningLog.HasErrors);

            var mixerFinal = new StrideShaderMixer(mcm.Mixins["ConstantBufferTest"], mcm.Mixins, null);
            mixerFinal.Mix();
        }

        [Fact]
        public void TestComputeShader() // check behavior with a array of compositions
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("TestComputeShader")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.False(mcm.ErrorWarningLog.HasErrors);

            var mixerFinal = new StrideShaderMixer(mcm.Mixins["TestComputeShader"], mcm.Mixins, null);
            mixerFinal.Mix();
        }
    }
}
*/
