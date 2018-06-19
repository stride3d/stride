// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using NUnit.Framework;

using Xenko.Core.IO;
using Xenko.Core.Serialization.Assets;
using Xenko.Core.Storage;
using Xenko.Shaders;
using Xenko.Shaders.Parser;
using Xenko.Shaders.Parser.Ast;
using Xenko.Shaders.Parser.Mixins;

namespace Xenko.Engine.Tests
{
    [TestFixture]
    class TestShaderParsing
    {
        private ShaderSourceManager sourceManager;
        private ShaderLoader shaderLoader;
        private ShaderMixinParser shaderMixinParser;

        [SetUp]
        public void Init()
        {
            // Create and mount database file system
            var objDatabase = new ObjectDatabase("/data/db", "index", "/local/db");
            var databaseFileProvider = new DatabaseFileProvider(objDatabase);
            AssetManager.GetFileProvider = () => databaseFileProvider;

            sourceManager = new ShaderSourceManager();
            sourceManager.LookupDirectoryList.Add(@"shaders");
            shaderLoader = new ShaderLoader(sourceManager);
            shaderMixinParser = new ShaderMixinParser(AssetManager.FileProvider);
            shaderMixinParser.SourceManager.LookupDirectoryList.Add(@"shaders");
        }

        [Test]
        public void TestSimpleRead() // check that the list is correctly filled
        {
            var moduleMixin = GetAnalyzedMixin("BasicMixin");
            var mixin = moduleMixin.Mixin;
            
            Assert.AreEqual(1, mixin.ParsingInfo.ClassReferences.VariablesReferences.Count(x => x.Key.Qualifiers.Contains(XenkoStorageQualifier.Stage)));
            Assert.AreEqual(1, mixin.ParsingInfo.ClassReferences.VariablesReferences.Count(x => x.Key.Qualifiers.Contains(XenkoStorageQualifier.Stream)));
            Assert.AreEqual(3, mixin.ParsingInfo.ClassReferences.VariablesReferences.Count);
            Assert.AreEqual(0, mixin.ParsingInfo.ClassReferences.MethodsReferences.Count(x => x.Key.Qualifiers.Contains(XenkoStorageQualifier.Clone)));
            Assert.AreEqual(0, mixin.BaseMixins.Count);
            Assert.AreEqual(1, mixin.ParsingInfo.ClassReferences.MethodsReferences.Count(x => x.Key.Qualifiers.Contains(XenkoStorageQualifier.Stage)));
            Assert.AreEqual(3, mixin.ParsingInfo.ClassReferences.MethodsReferences.Count);
        }
        
        [Test]
        public void TestInternalReference() // check that the list is correctly filled
        {
            var moduleMixin = GetAnalyzedMixin("Parent");
            var mixin = moduleMixin.Mixin;
            Assert.IsNotNull(mixin);

            Assert.AreEqual(2, mixin.ParsingInfo.ClassReferences.VariablesReferences.Count);
            Assert.AreEqual(1, mixin.ParsingInfo.ClassReferences.MethodsReferences.Count);
            //Assert.AreEqual(mixin.ParsingInfo.ClassReferences.VariablesReferences.Select(x => x.Key).FirstOrDefault(), mixin.ParsingInfo.VariableReferenceExpressions.FirstOrDefault(x => x.Name.Text == "baseValue").TypeInference.Declaration);
        }
        
        [Test]
        public void TestInheritance() // check that the base call is correct
        {
            var moduleMixinChild = GetAnalyzedMixin("Child");
            var mixinChild = moduleMixinChild.Mixin;

            Assert.AreEqual(1, mixinChild.BaseMixins.Count);

            var varDecl = GetAnalyzedMixin("Parent").Mixin.ParsingInfo.ClassReferences.VariablesReferences.Select(x => x.Key).FirstOrDefault();
            Assert.AreEqual(1, mixinChild.ParsingInfo.ClassReferences.VariablesReferences.Where(x => x.Key == varDecl).Select(x => x.Value).Count());
        }

        [Test]
        public void TestNameConflictSolved() // check that infered declaration are correct
        {
            var moduleMixinBase1 = GetAnalyzedMixin("BasicMixin");
            var moduleMixinBase2 = GetAnalyzedMixin("BasicMixin2");
            var moduleMixinSolved = GetAnalyzedMixin("MixinNoNameClash");

            var def1 = moduleMixinBase1.Mixin.VirtualTable.Variables.FirstOrDefault(x => x.Variable.Name == "myFloat").Variable;
            var def2 = moduleMixinBase2.Mixin.VirtualTable.Variables.FirstOrDefault(x => x.Variable.Name == "myFloat").Variable;

            Assert.AreEqual(1, moduleMixinSolved.Mixin.ParsingInfo.ClassReferences.VariablesReferences.Where(x => x.Key == def1).Select(x => x.Value).Count());
            Assert.AreEqual(1, moduleMixinSolved.Mixin.ParsingInfo.ClassReferences.VariablesReferences.Where(x => x.Key == def2).Select(x => x.Value).Count());
        }
        
        [Test]
        public void TestBaseLink() // check that infered declaration are correct
        {
            // TODO: redo this test
            var moduleMixinChild = GetAnalyzedMixin("BaseTestChild");
            var moduleMixinInter = GetAnalyzedMixin("BaseTestInter");
            var moduleMixinParent = GetAnalyzedMixin("BaseTestParent");

            Assert.AreEqual(2, moduleMixinParent.Mixin.LocalVirtualTable.Methods.Count);
            var baseMethod1Def = moduleMixinParent.Mixin.LocalVirtualTable.Methods.ToList()[0].Method;
            var baseMethod2Def = moduleMixinParent.Mixin.LocalVirtualTable.Methods.ToList()[1].Method;

            Assert.AreEqual(1, moduleMixinInter.Mixin.LocalVirtualTable.Methods.Count);
            var overrideMethod1Def = moduleMixinInter.Mixin.LocalVirtualTable.Methods.ToList()[0].Method;

            Assert.AreEqual(2, moduleMixinChild.Mixin.LocalVirtualTable.Methods.Count);
            var overrideFinalMethod1Def = moduleMixinChild.Mixin.LocalVirtualTable.Methods.ToList()[0].Method;
            var overrideFinalMethod2Def = moduleMixinChild.Mixin.LocalVirtualTable.Methods.ToList()[0].Method;

            Assert.AreEqual(1, moduleMixinInter.Mixin.ParsingInfo.BaseMethodCalls.Count);
            var baseOverrideMethod1Call = moduleMixinInter.Mixin.ParsingInfo.BaseMethodCalls.First();
            Assert.AreEqual(baseMethod1Def, baseOverrideMethod1Call.TypeInference.Declaration);

            Assert.AreEqual(3, moduleMixinInter.Mixin.ParsingInfo.ClassReferences.MethodsReferences.Select(x => x.Key).Count());
            Assert.AreEqual(1, moduleMixinInter.Mixin.ParsingInfo.ThisMethodCalls.Count);

            Assert.AreEqual(2, moduleMixinChild.Mixin.ParsingInfo.BaseMethodCalls.Count);
            var baseFinalMethod1Call = moduleMixinChild.Mixin.ParsingInfo.BaseMethodCalls.ToList()[0];
            var baseFinalMethod2Call = moduleMixinChild.Mixin.ParsingInfo.BaseMethodCalls.ToList()[1];
            //Assert.AreEqual(overrideMethod1Def, baseFinalMethod1Call.TypeInference.Declaration);
            //Assert.AreEqual(baseMethod2Def, baseFinalMethod2Call.TypeInference.Declaration);

            Assert.AreEqual(1, moduleMixinChild.Mixin.ParsingInfo.ThisMethodCalls.Count);
        }
        
        /*[Test]
        public void TestExtern() // check type inference of the extern method call
        {
            var moduleMixin = GetAnalyzedMixin("ExternMixin");
            var moduleMixinTest = GetAnalyzedMixin("ExternTest");

            Assert.IsNotNull(moduleMixin.Mixin);
            Assert.IsNotNull(moduleMixinTest.Mixin);

            Assert.AreEqual(1, moduleMixinTest.Mixin.ParsingInfo.ClassReferences.VariablesReferences.Count(x => x.Key.Qualifiers.Contains(XenkoStorageQualifier.Extern)));
            var externVar = moduleMixinTest.Mixin.ParsingInfo.ClassReferences.VariablesReferences.Select(x => x.Key).FirstOrDefault();

            var externDef = moduleMixin.Mixin.Shader;
            var externMethodDef = externDef.Members.OfType<MethodDeclaration>().FirstOrDefault();
            var externVariableDef = externDef.Members.OfType<Variable>().FirstOrDefault();

            Assert.AreEqual(1, moduleMixinTest.Mixin.ParsingInfo.ExternReferences.MethodsReferences.Select(x => x.Key).Count());
            //Assert.AreEqual(externVar, moduleMixinTest.Mixin.ParsingInfo.ExternReferences.VariablesReferences.FirstOrDefault().Key);

            Assert.AreEqual(1, moduleMixinTest.Mixin.ParsingInfo.ExternReferences.VariablesReferences.Count);
            Assert.AreEqual(1, moduleMixinTest.Mixin.ParsingInfo.ExternReferences.MethodsReferences.Count);
            Assert.IsTrue(moduleMixinTest.Mixin.ParsingInfo.ExternReferences.VariablesReferences.All(x => x.Key == externVar));

            Assert.AreEqual(1,moduleMixinTest.Mixin.ParsingInfo.ExternReferences.VariablesReferences.Select(x => x.Key).Count());
            Assert.AreEqual(externVariableDef, moduleMixinTest.Mixin.ParsingInfo.ExternReferences.VariablesReferences.Select(x => x.Key).FirstOrDefault());
        }*/
        /*
        [Test]
        public void TestDeepExtern() // check type inference of the deep extern method call
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("ExternMixin"),
                    new ShaderClassSource("DeepExtern"),
                    new ShaderClassSource("DeepExternTest")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.IsFalse(mcm.ErrorWarningLog.HasErrors);

            Assert.AreEqual(2, mcm.Mixins["DeepExternTest"].ParsingInfo.ClassReferences.VariablesReferences.Count(x => x.Key.Qualifiers.Contains(XenkoStorageQualifier.Extern)));
            var externVar = mcm.Mixins["DeepExternTest"].ParsingInfo.ClassReferences.VariablesReferences.Select(x => x.Key).FirstOrDefault();

            Assert.AreEqual(1, mcm.Mixins["DeepExtern"].ParsingInfo.ClassReferences.VariablesReferences.Count(x => x.Key.Qualifiers.Contains(XenkoStorageQualifier.Extern)));
            var externVar2 = mcm.Mixins["DeepExtern"].ParsingInfo.ClassReferences.VariablesReferences.Select(x => x.Key).FirstOrDefault();

            var externDef = mcm.Mixins["ExternMixin"].Shader;
            var externMethodDef = externDef.Members.OfType<MethodDeclaration>().FirstOrDefault();
            var externVariableDef = externDef.Members.OfType<Variable>().FirstOrDefault();

            Assert.AreEqual(1, mcm.Mixins["DeepExternTest"].ParsingInfo.ExternReferences.MethodsReferences.Select(x => x.Key).Count());
            Assert.AreEqual(externMethodDef, mcm.Mixins["DeepExternTest"].ParsingInfo.ExternReferences.MethodsReferences.Select(x => x.Key).FirstOrDefault());

            //Assert.AreEqual(2, mcm.Mixins["DeepExternTest"].ParsingInfo.ExternReferenceExpressions.Count);
            //Assert.IsTrue(mcm.Mixins["DeepExternTest"].ParsingInfo.ExternReferenceExpressions.All(x => x.TypeInference.Declaration == externVar));

            Assert.AreEqual(1, mcm.Mixins["DeepExternTest"].ParsingInfo.ExternReferences.VariablesReferences.Select(x => x.Key).Count());
            Assert.AreEqual(externVariableDef, mcm.Mixins["DeepExternTest"].ParsingInfo.ExternReferences.VariablesReferences.Select(x => x.Key).FirstOrDefault());
        }
        [Test]
        public void TestExternArray() // check behavior with a array of compositions
        {
            var moduleMixin = GetAnalyzedMixin("TestExternArray");
        }
        */
        [Test]
        public void TestStaticCall() // check that the type inference is correct
        {
            var moduleMixin = GetAnalyzedMixin("StaticMixin");
            var moduleMixinCall = GetAnalyzedMixin("StaticCallMixin");

            var methodDef = moduleMixin.Mixin.ParsingInfo.ClassReferences.MethodsReferences.Select(x => x.Key).FirstOrDefault();
            var variableDef = moduleMixin.Mixin.ParsingInfo.ClassReferences.VariablesReferences.Select(x => x.Key).FirstOrDefault();
            var methodCall = moduleMixinCall.Mixin.ParsingInfo.StaticReferences.MethodsReferences.FirstOrDefault().Value.FirstOrDefault();

            Assert.AreNotEqual(null, methodCall);
            Assert.AreEqual(methodDef, methodCall.TypeInference.Declaration);
            Assert.AreEqual(1, moduleMixinCall.Mixin.ParsingInfo.StaticReferences.VariablesReferences.Select(x => x.Key).Count());
            Assert.AreEqual(variableDef, moduleMixinCall.Mixin.ParsingInfo.StaticReferences.VariablesReferences.First().Value.First().Expression.TypeInference.Declaration);
        }
        /*
        [Test]
        public void TestGenerics() // test the behavior with generics
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("TestGenerics", new object[] { 1.0f } )
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            // TODO: change test - Irrelevant
            Assert.DoesNotThrow(mcm.Run);
            Assert.IsFalse(mcm.ErrorWarningLog.HasErrors);
        }
        /*
        [Test]
        public void TestGenericsCall() // test the behavior with inheritance and generics
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("GenericCall"),
                    new ShaderClassSource("TestGenerics"),
                    new ShaderClassSource("TestGenerics", new object[] { 1.0 }),
                    new ShaderClassSource("TestGenerics", new object[] { 2.0 }),
                    new ShaderClassSource("TestGenerics", new object[] { "2.000000" }), // TODO: prevent this class to be loaded twice
                    new ShaderClassSource("GenericTexcoord", new object[] { "TEXCOORD0" }),
                    new ShaderClassSource("GenericExtern")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.IsFalse(mcm.ErrorWarningLog.HasErrors);

            Assert.AreEqual(7, mcm.ClassSources.Count);
            //Assert.AreEqual(2, mcm.UninstanciatedMixinInfos.Count);

            Assert.AreEqual(3, mcm.ClassSources.OfType<ShaderClassSource>().Count(x => x.ClassName == "TestGenerics"));
            Assert.AreEqual(2, mcm.ClassSources.OfType<ShaderClassSource>().Count(x => x.ClassName == "GenericTexcoord"));
            Assert.AreEqual(1, mcm.ClassSources.OfType<ShaderClassSource>().Count(x => x.ClassName == "GenericCall"));
            Assert.AreEqual(1, mcm.ClassSources.OfType<ShaderClassSource>().Count(x => x.ClassName == "GenericExtern"));


            //////////////////////////////////////////////////////////////////////////////////////////

            // test generics with more complex identifier (IdentifierDot)
            var shaderClassSourceList2 = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("TestGenericComplex"),
                    new ShaderClassSource("TestGenericMacro"),
                    new ShaderClassSource("StaticMixin")
                };
            var mcm2 = new ShaderCompilationContext(shaderClassSourceList2, shaderLoader.LoadClassSource);
            mcm2.Run();

            Assert.IsFalse(mcm2.ErrorWarningLog.HasErrors);
        }
        
        [Test]
        public void TestStageValueInitializer() // test "= stage"
        {
            // TODO: hangs
            var moduleMixinRef = GetAnalyzedMixin("StageValueReference");

            Assert.AreEqual(1, moduleMixinRef.Mixin.ParsingInfo.StageInitializedVariables.Count);
        }
        */
        [Test]
        public void TestStreams() // test streams input/output - nothing for now
        {
            var moduleMixin = GetAnalyzedMixin("TestStreams");

            Assert.AreEqual(1, moduleMixin.Mixin.ParsingInfo.ClassReferences.VariablesReferences.Select(x => x.Key).Count());
            Assert.AreEqual(1, moduleMixin.Mixin.ParsingInfo.ClassReferences.VariablesReferences.FirstOrDefault().Value.Count);
        }
        
        [Test]
        public void TestExternClone() // test extern cloning and correct redirection - do nothing for now
        {
            var moduleMixin = GetAnalyzedMixin("ExternCloneTest");
        }

        [Test]
        public void TestStructure() // test structure type inference
        {
            var moduleMixinStr = GetAnalyzedMixin("TestStructure");
            var moduleMixinStrIn = GetAnalyzedMixin("TestStructInheritance");

            Assert.AreEqual(1, moduleMixinStr.Mixin.ParsingInfo.StructureDefinitions.Count);
            Assert.AreEqual(1, moduleMixinStr.Mixin.LocalVirtualTable.StructureTypes.Count);
            Assert.AreEqual(1, moduleMixinStr.Mixin.VirtualTable.StructureTypes.Count);

            Assert.AreEqual(0, moduleMixinStrIn.Mixin.ParsingInfo.StructureDefinitions.Count);
            Assert.AreEqual(0, moduleMixinStrIn.Mixin.LocalVirtualTable.StructureTypes.Count);
            Assert.AreEqual(1, moduleMixinStrIn.Mixin.VirtualTable.StructureTypes.Count);
        }
        /*
        [Test]
        public void TestGeometryShader() // test structures inheritance in geometry shader
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("GeometryShaderTest"),
                    new ShaderClassSource("TestStructure")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.IsFalse(mcm.ErrorWarningLog.HasErrors);
        }*/
        
        [Test]
        public void TestTessellation() // test tessellation shader, patchstream
        {
            var moduleMixin = GetAnalyzedMixin("TessellationTest");
            Assert.AreEqual(2, moduleMixin.Mixin.ParsingInfo.ClassReferences.VariablesReferences.Count(x => x.Key.Qualifiers.Contains(XenkoStorageQualifier.PatchStream)));
        }
        
        [Test]
        public void TestStageCall()
        {
            var moduleMixin = GetAnalyzedMixin("StageCallExtern");
            Assert.AreEqual(1, moduleMixin.Mixin.ParsingInfo.StageMethodCalls.Count);
        }
        
        [Test]
        public void TestForEachStatement()
        {
            var moduleMixin = GetAnalyzedMixin("ForEachTest");
            Assert.AreEqual(1, moduleMixin.Mixin.ParsingInfo.ForEachStatements.Count);
        }
        /*
        [Test]
        public void TestErrors()
        {
            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            // check that a cyclic definition throws an error
            var shaderClassSourceCyclic = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("CyclicTest")
                };
            var mcmCyclic = new ShaderCompilationContext(shaderClassSourceCyclic, shaderLoader.LoadClassSource);
            mcmCyclic.Run();

            Assert.AreEqual(1, mcmCyclic.ErrorWarningLog.Messages.Count);
            Assert.That(mcmCyclic.ErrorWarningLog.Messages[0].Code, Is.EqualTo(XenkoMessageCode.ErrorCyclicDependency.Code));

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            // check that a missing mixin throws an error
            var shaderClassSourceMissing = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("Child")
                };
            var mcmMissing = new ShaderCompilationContext(shaderClassSourceMissing, shaderLoader.LoadClassSource);
            mcmMissing.Run();

            Assert.AreEqual(1, mcmMissing.ErrorWarningLog.Messages.Count);
            Assert.That(mcmMissing.ErrorWarningLog.Messages[0].Code, Is.EqualTo(XenkoMessageCode.ErrorDependencyNotInModule.Code));

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            // should throw an error: missing override keyword
            var shaderClassSourceOverride = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("Parent"),
                    new ShaderClassSource("ChildError")
                };
            var mcmOverride = new ShaderCompilationContext(shaderClassSourceOverride, shaderLoader.LoadClassSource);
            mcmOverride.Run();

            Assert.AreEqual(1, mcmOverride.ErrorWarningLog.Messages.Count);
            Assert.That(mcmOverride.ErrorWarningLog.Messages[0].Code, Is.EqualTo(XenkoMessageCode.ErrorMissingOverride.Code));

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            // should create an error: name ambiguous
            var shaderClassSourceAmbiguous = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("BasicMixin"), 
                    new ShaderClassSource("BasicMixin2"),
                    new ShaderClassSource("MixinNameClash")
                };
            var mcmAmbiguous = new ShaderCompilationContext(shaderClassSourceAmbiguous, shaderLoader.LoadClassSource);
            mcmAmbiguous.Run();

            Assert.AreEqual(1, mcmAmbiguous.ErrorWarningLog.Messages.Count);
            Assert.That(mcmAmbiguous.ErrorWarningLog.Messages[0].Code, Is.EqualTo(XenkoMessageCode.ErrorVariableNameAmbiguity.Code));

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            // test what happens if an interface is declared
            var shaderClassSourceInterface = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("InterfaceTest")
                };
            var mcmInterface = new ShaderCompilationContext(shaderClassSourceInterface, shaderLoader.LoadClassSource);
            mcmInterface.Run();

            Assert.IsTrue(mcmInterface.ErrorWarningLog.HasErrors);
            Assert.AreEqual(1, mcmInterface.ErrorWarningLog.Messages.Count);

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            // test what happens when a mixin is a parameter or a return value inside a function
            var shaderClassSourceParamReturn = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("ExternMixin"),
                    new ShaderClassSource("MixinFunctionParamaterTest")
                };
            var mcmParamReturn = new ShaderCompilationContext(shaderClassSourceParamReturn, shaderLoader.LoadClassSource);
            mcmParamReturn.Run();

            Assert.AreEqual(3, mcmParamReturn.ErrorWarningLog.Messages.Count);

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            // test that it is impossible to put a shaderclass as a generic value
            var shaderClassSourceGenerics = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("StructuredBufferTest"),
                    new ShaderClassSource("StaticMixin")
                };
            var mcmGenerics = new ShaderCompilationContext(shaderClassSourceGenerics, shaderLoader.LoadClassSource);
            mcmGenerics.Run();

            Assert.IsTrue(mcmGenerics.ErrorWarningLog.HasErrors);

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            var shaderClassSourceFunc = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("TestErrors"),
                    new ShaderClassSource("ExternMixin")
                };
            var mcmFunc = new ShaderCompilationContext(shaderClassSourceFunc, shaderLoader.LoadClassSource);
            mcmFunc.Run();

            // TODO: separate tests or check messages/error code
            Assert.AreEqual(27, mcmFunc.ErrorWarningLog.Messages.Count);

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            var shaderClassSourceStream = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("StreamError")
                };
            var mcmStream = new ShaderCompilationContext(shaderClassSourceStream, shaderLoader.LoadClassSource);
            mcmStream.Run();

            Assert.AreEqual(1, mcmStream.ErrorWarningLog.Messages.Count);
            Assert.That(mcmStream.ErrorWarningLog.Messages[0].Code, Is.EqualTo(XenkoMessageCode.ErrorInOutStream.Code));
        }

        [Test]
        public void TestShaderLibrary()
        {
            var shaderClassSourceList = new HashSet<string>
                {
                    "BaseTestParent",
                    "BaseTestParent",
                    "StaticMixin",
                    "MacroTest",
                    "MacroTestChild"
                };

            var lib = new XenkoShaderLibrary(shaderClassSourceList);
            lib.LoadClass = shaderLoader.LoadClassSource;

            Assert.AreEqual(4, lib.AvailableShaders.Count);
            Assert.AreEqual(0, lib.MixinInfos.Count);
            var context = lib.GetContextFromMacros(new ShaderMacro[] { new ShaderMacro("MAC0", "0") });
            Assert.AreEqual(4, context.Count);
            Assert.AreEqual(4, lib.AvailableShaders.Count);
            Assert.AreEqual(4, lib.MixinInfos.Count);
            Assert.AreEqual(4, lib.MixinInfos.Select(x => x.Mixin).Distinct().Count());

            context = lib.GetContextFromMacros(new ShaderMacro[] { new ShaderMacro("MAC1", "1") });
            Assert.AreEqual(4, context.Count);
            Assert.AreEqual(4, lib.AvailableShaders.Count);
            Assert.AreEqual(8, lib.MixinInfos.Count);
            Assert.AreEqual(4, lib.MixinInfos.Select(x => x.Mixin).Distinct().Count());

            context = lib.GetContextFromMacros(new ShaderMacro[] { new ShaderMacro("MAC0", "1") });
            Assert.AreEqual(4, context.Count);
            Assert.AreEqual(4, lib.AvailableShaders.Count);
            Assert.AreEqual(12, lib.MixinInfos.Count);
            Assert.AreEqual(4, lib.MixinInfos.Select(x => x.Mixin).Distinct().Count());

            context = lib.GetContextFromMacros(new ShaderMacro[] { new ShaderMacro("MAC0", "0") });
            Assert.AreEqual(4, context.Count);
            Assert.AreEqual(4, lib.AvailableShaders.Count);
            Assert.AreEqual(12, lib.MixinInfos.Count);
            Assert.AreEqual(4, lib.MixinInfos.Select(x => x.Mixin).Distinct().Count());

            context = lib.GetContextFromMacros(new ShaderMacro[] { });
            Assert.AreEqual(4, context.Count);
            Assert.AreEqual(4, lib.AvailableShaders.Count);
            Assert.AreEqual(16, lib.MixinInfos.Count);
            Assert.AreEqual(4, lib.MixinInfos.Select(x => x.Mixin).Distinct().Count());

            context = lib.GetContextFromMacros(new ShaderMacro[] { new ShaderMacro("MACRO_TEST", "int") });
            Assert.AreEqual(4, context.Count);
            Assert.AreEqual(4, lib.AvailableShaders.Count);
            Assert.AreEqual(20, lib.MixinInfos.Count);
            Assert.AreEqual(5, lib.MixinInfos.Select(x => x.Mixin).Distinct().Count());

            context = lib.GetContextFromMacros(new ShaderMacro[] { new ShaderMacro("MACRO_TEST", "float") });
            Assert.AreEqual(4, context.Count);
            Assert.AreEqual(4, lib.AvailableShaders.Count);
            Assert.AreEqual(24, lib.MixinInfos.Count);
            Assert.AreEqual(6, lib.MixinInfos.Select(x => x.Mixin).Distinct().Count()); // TODO: should be 4!
  
        }

        [Test]
        public void TestSingle()
        {
            VirtualFileSystem.MountFileSystem("/assets/shaders", "../../../../../shaders");
            
            var className = "ComputeColorFixed";
            Console.WriteLine(@"Loading effect " + className);
            
            var effectCompiler = EffectCompiler.New(EffectCompilerTarget.Direct3D11, GraphicsProfile.Level_11_1) as EffectCompilerHlsl;
            
            if (effectCompiler != null)
            {
                var mixin = new ShaderMixinSource();
                mixin.Mixins.Add(new ShaderClassSource(className, "float4(0.0,1.0,      0.5, 2.0)"));
                effectCompiler.CompileEffectShaderPass("test.hlsl", mixin, null);
            }
        }

        private void TestSingleClass(EffectCompilerHlsl effectCompiler, string className)
        {
            var mixin = new ShaderMixinSource();
            mixin.Mixins.Add(new ShaderClassSource(className));
            try
            {
                if (className == "LightMultiDirectionalShadingPerPixel")
                    mixin.Mixins[0].GenericParameters = new object[] {4};
                effectCompiler.CompileEffectShaderPass("test.hlsl", mixin, null);
            }
            catch (Exception exp)
            {
                Console.WriteLine(@"---EXCEPTION---");
                Console.WriteLine(exp.Message);
            }
        }

        [Test]
        public void TestWarnings()
        {
            //VirtualFileSystem.MountFileSystem("/assets/shaders", "../../../../../shaders");
            VirtualFileSystem.MountFileSystem("/assets/shaders", "C:\\Users\\aurelien.serandour\\Desktop\\Shaders");
            foreach (var file in VirtualFileSystem.ListFiles("/assets/shaders", "*.xksl", VirtualSearchOption.TopDirectoryOnly).Result)
            {
                var fileParts = file.Split('.', '/');
                var className = fileParts[fileParts.Length - 2];
                Console.WriteLine();
                Console.WriteLine(@"Loading effect " + className);
                var effectCompiler = EffectCompiler.New(EffectCompilerTarget.Direct3D11, GraphicsProfile.Level_11_1) as EffectCompilerHlsl;

                if (effectCompiler != null)
                    TestSingleClass(effectCompiler, className);
            }
            
            var effectCompiler0 = EffectCompiler.New(EffectCompilerTarget.Direct3D11, GraphicsProfile.Level_11_1) as EffectCompilerHlsl;
            TestSingleClass(effectCompiler0, "PostEffectFXAA");
        }


        [Test]
        public void TestMayaWarnings()
        {
            VirtualFileSystem.MountFileSystem("/assets/shaders", "../../../../../shaders");
            //VirtualFileSystem.MountFileSystem("/assets/shaders", "C:\\Users\\aurelien.serandour\\Desktop\\Shaders\\Maya");
            foreach (var file in VirtualFileSystem.ListFiles("/assets/shaders", "*.xksl", VirtualSearchOption.TopDirectoryOnly).Result)
            {
                var fileParts = file.Split('.', '/');
                var className = fileParts[fileParts.Length - 2];
                Console.WriteLine();
                Console.WriteLine(@"Loading effect " + className);
                var effectCompiler = EffectCompiler.New(EffectCompilerTarget.Direct3D11, GraphicsProfile.Level_11_1) as EffectCompilerHlsl;

                if (effectCompiler != null)
                    TestSingleClass(effectCompiler, className);
            }
        }*/

        public ModuleMixinInfo GetAnalyzedMixin(string mixinName)
        {
            var source = new ShaderMixinSource();
            source.Mixins.Add(new ShaderClassSource(mixinName));
            shaderMixinParser.Parse(source, new Xenko.Shaders.ShaderMacro[0]);

            var moduleMixin = shaderMixinParser.GetMixin(mixinName);
            Assert.IsNotNull(moduleMixin);
            Assert.IsFalse(moduleMixin.Log.HasErrors);
            Assert.IsNotNull(moduleMixin.Mixin);

            return moduleMixin;
        }
    }
}
