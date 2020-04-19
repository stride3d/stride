// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Stride.Core.Serialization;
using Stride.Core.Shaders.Grammar.Hlsl;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;
using Stride.Core.Shaders.Parser;
using Stride.Core.Shaders.Parser.Hlsl;
using Stride.Core.Shaders.Properties;
using Stride.Core.Shaders.Utility;
using ParameterQualifier = Stride.Core.Shaders.Ast.ParameterQualifier;
using Stride.Core.Shaders.Visitor;

namespace Stride.Core.Shaders.Analysis.Hlsl
{
    /// <summary>
    /// A Type reference analysis is building type references.
    /// </summary>
    [DataSerializerGlobal(null, typeof(List<MatrixType.Indexer>))]
    public class HlslSemanticAnalysis : SemanticAnalysis
    {
        private static readonly Object lockInit = new Object();
        private static bool builtinsInitialized = false;
        protected static readonly List<IDeclaration> defaultDeclarations = new List<IDeclaration>();
        private static readonly Dictionary<string, TypeBase> BuiltinObjects = new Dictionary<string, TypeBase>();
        private static readonly Dictionary<GenericInstanceKey, TypeBase> InstanciatedTypes = new Dictionary<GenericInstanceKey, TypeBase>();
        
        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="HlslSemanticAnalysis"/> class.
        /// </summary>
        /// <param name="result">
        /// The result.
        /// </param>
        protected HlslSemanticAnalysis(ParsingResult result) : base(result)
        {
        }

        #endregion

        #region Public Methods

        private static readonly string SwizzleTag = "MatrixSwizzleDecode";


        /// <summary>
        /// Decodes the swizzle.
        /// </summary>
        /// <param name="memberReference">The member reference.</param>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        public static List<MatrixType.Indexer> MatrixSwizzleDecode(MemberReferenceExpression memberReference, ParsingResult result = null)
        {
            string components = memberReference.Member.Text;

            var matrixDecl = (MatrixType)(memberReference.Target.TypeInference.TargetType);

            var span = matrixDecl.Span;

            var swizzles = (List<MatrixType.Indexer>)memberReference.GetTag(SwizzleTag);
            if (swizzles != null)
                return swizzles;

            swizzles = new List<MatrixType.Indexer>();

            if (components.StartsWith("_"))
            {
                string[] splitComponents;
                int indexOffset = 0;
                if (components.StartsWith("_m"))
                {
                    splitComponents = components.Split(new[] { "_m" }, StringSplitOptions.RemoveEmptyEntries);
                }
                else
                {
                    splitComponents = components.Split(new[] { "_" }, StringSplitOptions.RemoveEmptyEntries);
                    indexOffset = 1;
                }

                int dimension = 0;

                if (splitComponents.Length == 0 && result != null)
                {
                    result.Error(MessageCode.ErrorMatrixInvalidMemberReference, span, components);
                }

                foreach (var splitComponent in splitComponents)
                {
                    if (splitComponent.Length != 2 || !IsValidIndex(span, splitComponent[0], indexOffset, indexOffset + 3) || !IsValidIndex(span, splitComponent[1], indexOffset, indexOffset + 3))
                    {
                        swizzles.Clear();
                        break;
                    }

                    swizzles.Add(new MatrixType.Indexer(int.Parse(splitComponent[0].ToString()) - indexOffset, int.Parse(splitComponent[1].ToString()) - indexOffset));
                    dimension++;
                }
            }

            memberReference.SetTag(SwizzleTag, swizzles);
            return swizzles;
        }

        public override Node Visit(AsmExpression asmExpression)
        {
            return asmExpression;
        }

        public override Node Visit(Ast.Hlsl.Annotations annotations)
        {
            return annotations;
        }

        public override Node Visit(CastExpression castExpression)
        {
            base.Visit(castExpression);

            var targetType = castExpression.Target.ResolveType();
            castExpression.TypeInference = (TypeInference)castExpression.Target.TypeInference.Clone();
            if (castExpression.TypeInference.TargetType == null)
                castExpression.TypeInference.TargetType = targetType;

            return castExpression;
        }

        public override Node Visit(MethodInvocationExpression expression)
        {
            var methodAsVariable = expression.Target as VariableReferenceExpression;

            // We are not parsing CompileShader methods
            if (methodAsVariable != null)
            {
                switch (methodAsVariable.Name.Text)
                {
                    case "ConstructGSWithSO":
                    case "CompileShader":
                        return expression;
                }
            }            
            
            return base.Visit(expression);
        }


        public override Node Visit(CompileExpression compileExpression)
        {
            // base.Visit(compileExpression);
            //Warning("TypeInference on CompileExpression is not handled", compileExpression.Span);
            return compileExpression;
        }

        public override Node Visit(StateExpression stateExpression)
        {
            // base.Visit(stateExpression);
            // Warning("TypeInference on StateExpression is not handled", stateExpression.Span);
            return stateExpression;
        }

        public override Node Visit(Technique technique)
        {
            // Force to not visit a techniques
            return technique;
        }


        /// <summary>
        /// Visits the specified type name.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        public override Node Visit(GenericType genericType)
        {
            base.Visit(genericType);

            string genericName = genericType.Name.Text;

            // Get the case insenti
            var value = TextureType.Parse(genericName);
            if (value != null)
                genericName = value.Name.Text;

            TypeBase typeBase;
            if (BuiltinObjects.TryGetValue(genericName, out typeBase))
            {
                genericType.TypeInference.TargetType = GetGenericInstance(genericName, genericType, typeBase);
            }

            return genericType;
        }

        protected override IEnumerable<IDeclaration> FindDeclarationsFromObject(TypeBase typeBase, string memberName)
        {
            if (typeBase is ClassType)
            {
                var classType = (ClassType)typeBase;

                foreach (var declaration in classType.Members.OfType<IDeclaration>().Where(node => node.Name == memberName))
                    yield return declaration;

                foreach (var baseType in classType.BaseClasses)
                    foreach (var baseDeclaration in FindDeclarationsFromObject(baseType.ResolveType(), memberName))
                        yield return baseDeclaration;
            }
            else if (typeBase is InterfaceType)
            {
                var interfaceType = (InterfaceType)typeBase;

                foreach (var declaration in interfaceType.Methods.OfType<IDeclaration>().Where(node => node.Name == memberName))
                    yield return declaration;
            }
            /*else if (typeBase is StructType)
            {
                var structType = (StructType)typeBase;
                foreach (var declaration in structType.Fields.Where(node => node.Name == memberName))
                    yield return declaration;
            }*/
        }

        protected override void ProcessMethodInvocation(MethodInvocationExpression expression, string methodName, List<IDeclaration> declarations)
        {
            // Check for typedef method 
            if (methodName != null)
            {
                var varExp = expression.Target as VariableReferenceExpression;

                if (varExp != null)
                {
                    var typeDefDeclarator = declarations.OfType<Typedef>().FirstOrDefault();
                    if (typeDefDeclarator != null)
                    {
                        varExp.TypeInference.Declaration = typeDefDeclarator;
                        varExp.TypeInference.TargetType = typeDefDeclarator.ResolveType();

                        expression.TypeInference.Declaration = typeDefDeclarator;
                        expression.TypeInference.TargetType = typeDefDeclarator.ResolveType();
                        return;
                    }

                    //var builtInFunction = defaultDeclarations.FirstOrDefault(x => x.Name.Text == varExp.Name.Text && TestParametersType(expression, x)) as MethodDeclaration;
                    var builtInFunction = defaultDeclarations.FirstOrDefault(x => (x as MethodDeclaration != null) && (x as MethodDeclaration).IsSameSignature(expression)) as MethodDeclaration;
                    if (builtInFunction != null)
                    {
                        varExp.TypeInference.Declaration = builtInFunction;
                        varExp.TypeInference.TargetType = builtInFunction.ReturnType.ResolveType();

                        expression.TypeInference.Declaration = builtInFunction;
                        expression.TypeInference.TargetType = builtInFunction.ReturnType.ResolveType();
                        return;
                    }
                }
            }
            
            base.ProcessMethodInvocation(expression, methodName, declarations);
        }

        public override Node Visit(TextureType textureType)
        {
            base.Visit(textureType);

            AssociatePredefinedObjects(textureType);

            return textureType;
        }

        private void AssociatePredefinedObjects(TypeBase typebase)
        {
            // Use the returned name in order to support case insensitive names 
            TypeBase predefinedType;
            if (typebase.TypeInference.TargetType == null && BuiltinObjects.TryGetValue(typebase.Name.Text, out predefinedType))
            {
                var textureType = new GenericType(null, 1);
                textureType.Parameters[0] = VectorType.Float4;

                typebase.TypeInference.TargetType = GetGenericInstance(typebase.Name.Text, textureType, predefinedType);
            }
        }

        /// <summary>
        /// Visits the specified type name.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        public override Node Visit(TypeName typeName)
        {
            base.Visit(typeName);

            var name = typeName.Name.Text;

            // Substitute case insensitive types to case sensitive types
            // TODO this is temporary. We need to found a better workaround.
            TypeBase value = TextureType.Parse(name);
            if (value != null)
            {
                AssociatePredefinedObjects(value);
                return value;
            }

            value = StreamTypeName.Parse(name);
            if (value != null)
            {
                AssociatePredefinedObjects(value);
                return value;
            }

            value = SamplerType.Parse(name);
            if (value != null)
                return value;

            value = StateType.Parse(name);
            if (value != null)
                return value;

            // Replace shader objects
            if (name == "VertexShader" || name == "GeometryShader" || name == "PixelShader")
                return new ObjectType(name);

            // Else call the base
            return base.Visit(typeName);
        }

        private static bool IsValidIndex(SourceSpan span, char valueChar, int min, int max, ParsingResult result = null)
        {
            int value;
            var isParseOk = int.TryParse(valueChar.ToString(), out value);

            if (!isParseOk || value < min || value > max)
            {
                if (result != null)
                    result.Error(MessageCode.ErrorMatrixInvalidIndex, span, valueChar, min, max);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Visits the specified member reference.
        /// </summary>
        /// <param name="memberReference">The member reference.</param>
        protected override void CommonVisit(MemberReferenceExpression memberReference)
        {
            var thisType = memberReference.Target.TypeInference.TargetType;
            
            if (thisType is MatrixType)
            {
                FindMemberTypeReference((MatrixType)thisType, memberReference);
            }
            else
            {
                base.CommonVisit(memberReference);
            }
        }

        /// <summary>
        /// Finds the member type reference.
        /// </summary>
        /// <param name="matrixType">Type of the matrix.</param>
        /// <param name="memberReference">The member reference.</param>
        protected virtual void FindMemberTypeReference(MatrixType matrixType, MemberReferenceExpression memberReference)
        {
            var components = memberReference.Member.Text;
            var span = memberReference.Span;

            // A matrix contains values organized in rows and columns, which can be accessed using the structure operator "." followed by one of two naming sets:
            //    The zero-based row-column position:
            //        _m00, _m01, _m02, _m03
            //        _m10, _m11, _m12, _m13
            //        _m20, _m21, _m22, _m23
            //        _m30, _m31, _m32, _m33
            //    The one-based row-column position:
            //        _11, _12, _13, _14
            //        _21, _22, _23, _24
            //        _31, _32, _33, _34
            //        _41, _42, _43, _44
            var swizzles = MatrixSwizzleDecode(memberReference, ParsingResult);

            if (swizzles.Count > 0)
            {
                var itemType = matrixType.Type.ResolveType();
                memberReference.TypeInference.TargetType = swizzles.Count == 1 ? itemType : new VectorType((ScalarType)itemType, swizzles.Count);
            }
        }

        #endregion

        #region Methods

        private static TypeBase GetGenericInstance(string typename, GenericBaseType genericType, TypeBase predefinedType)
        {
            var key = new GenericInstanceKey(typename, genericType.Parameters);

            TypeBase instanciatedType;
            lock (InstanciatedTypes)
            {
                if (!InstanciatedTypes.TryGetValue(key, out instanciatedType))
                {
                    instanciatedType = genericType.MakeGenericInstance(predefinedType);
                    InstanciatedTypes.Add(key, instanciatedType);
                }
            }
            return instanciatedType;
        }

        protected static void InitializeBuiltins()
        {
            foreach (var function in Function.Functions)
            {
                foreach (var p in EnumerateParameters(function.Parameters[0]))
                {
                    var returnType = function.Return(function, new[] { p });
                    var parameterTypes = function.ParamList(function, new[] { p });

                    var methodDeclaration = new MethodDeclaration();
                    methodDeclaration.IsBuiltin = true;
                    methodDeclaration.Name = new Identifier(function.Name);
                    methodDeclaration.ReturnType = returnType;

                    foreach (var parameterType in parameterTypes)
                        methodDeclaration.Parameters.Add( new Ast.Parameter { DeclaringMethod = methodDeclaration, Type = parameterType } );

                    defaultDeclarations.Add(methodDeclaration);
                }
            }

            defaultDeclarations.AddRange(declaredMethods);

            foreach (var methodDeclaration in declaredMethods)
            {
                var newMethodDeclaration = new MethodDeclaration();
                newMethodDeclaration.IsBuiltin = true;
                newMethodDeclaration.Name = new Identifier(methodDeclaration.Name);
                newMethodDeclaration.ReturnType = methodDeclaration.ReturnType;

                foreach (var parameter in methodDeclaration.Parameters)
                {
                    var parameterType = parameter.Type;
                    if (parameterType.IsSamplerType())
                    {
                        parameterType = SamplerType.Sampler;
                    }

                    newMethodDeclaration.Parameters.Add(new Ast.Parameter { DeclaringMethod = newMethodDeclaration, Type = parameterType });
                }
                defaultDeclarations.Add(newMethodDeclaration);
            }

            // adding remaining functions that doesn't have multiple versions
            defaultDeclarations.Add(GenericMethod("AllMemoryBarrier", TypeBase.Void));
            defaultDeclarations.Add(GenericMethod("AllMemoryBarrierWithGroupSync", TypeBase.Void));
            defaultDeclarations.Add(GenericMethod("D3DCOLORtoUBYTE4", VectorType.Int4, GenericParam("x", VectorType.Float4)));
            defaultDeclarations.Add(GenericMethod("DeviceMemoryBarrier", TypeBase.Void));
            defaultDeclarations.Add(GenericMethod("DeviceMemoryBarrierWithGroupSync", TypeBase.Void));
            defaultDeclarations.Add(GenericMethod("GetRenderTargetSampleCount", ScalarType.UInt));
            defaultDeclarations.Add(GenericMethod("GetRenderTargetSamplePosition", ScalarType.UInt, GenericParam("x", ScalarType.Int)));
            defaultDeclarations.Add(GenericMethod("GroupMemoryBarrier", TypeBase.Void));
        }

        public static List<IDeclaration> ParseBuiltin(string builtins, string fileName)
        {
            var builtinDeclarations = new List<IDeclaration>();

            var result = HlslParser.TryPreProcessAndParse(builtins, fileName);

            // Check that we parse builtins successfully.
            var shader = ShaderParser.Check(result, fileName);

            foreach (var declaration in shader.Declarations)
            {
                var classType = declaration as ClassType;
                if (classType != null)
                {
                    classType.Name.Text = classType.Name.Text.Trim('_');
                    BuiltinObjects.Add(classType.Name.Text, classType);
                    classType.IsBuiltIn = true;
                }
                else if (declaration is IDeclaration)
                {
                    var methodDeclaration = declaration as MethodDeclaration;
                    if (methodDeclaration != null)
                        methodDeclaration.IsBuiltin = true;
                    builtinDeclarations.Add((IDeclaration)declaration);
                }
            }

            var tempAnalysis = new HlslSemanticAnalysis(result);
            tempAnalysis.Run();

            return builtinDeclarations;
        }

        /// <summary>
        /// Create the required declarations for hlsl parsing
        /// </summary>
        /// <param name="builtinDeclarations">the list of declarations</param>
        protected void SetupHlslAnalyzer(List<IDeclaration> builtinDeclarations = null)
        {
            StaticInitializeBuiltins();

            // Add all default declarations
            ScopeStack.Peek().AddDeclarations(defaultDeclarations);

            if (builtinDeclarations != null)
            {
                this.ScopeStack.Peek().AddDeclarations(builtinDeclarations);

                // Tag all method declared as user defined
                foreach (var builtinDeclaration in builtinDeclarations)
                {
                    var methodDeclaration = builtinDeclaration as MethodDeclaration;
                    if (methodDeclaration != null)
                        methodDeclaration.SetTag(TagBuiltinUserDefined, true);
                }
            }
        }

        /// <summary>
        /// Fill the clone context with the elements of the default declarations
        /// </summary>
        /// <param name="cloneContext">the CloneContext</param>
        public static void FillCloneContext(CloneContext cloneContext)
        {
            StaticInitializeBuiltins();

            foreach (var decl in defaultDeclarations)
                DeepCloner.DeepCollect(decl, cloneContext);

            foreach (var bInObj in BuiltinObjects)
                DeepCloner.DeepCollect(bInObj.Value, cloneContext);

            UpdateCloneContext(cloneContext);
        }

        /// <summary>
        /// Update the clone context with the new instanciated classes
        /// </summary>
        /// <param name="cloneContext">the CloneContext</param>
        public static void UpdateCloneContext(CloneContext cloneContext)
        {
            lock (InstanciatedTypes)
            {
                foreach (var instType in InstanciatedTypes)
                {
                    if (instType.Key.GenericParameters.Any(x => x is TypeName))
                        continue;
                    DeepCloner.DeepCollect(instType.Value, cloneContext);
                }
            }
        }

        public static void Run(ParsingResult toParse, List<IDeclaration> builtinDeclarations = null)
        {
            var analysis = new HlslSemanticAnalysis(toParse);
            analysis.SetupHlslAnalyzer(builtinDeclarations);
            analysis.Run();
        }

        private static void StaticInitializeBuiltins()
        {
            lock (lockInit)
            {
                if (!builtinsInitialized)
                {
                    // Add builtins
                    defaultDeclarations.AddRange(ParseBuiltin(Resources.HlslDeclarations, "internal_hlsl_declarations.hlsl"));
                    InitializeBuiltins();
                    builtinsInitialized = true;
                }
            }
        }

        private static MethodDeclaration[] declaredMethods = new MethodDeclaration[]
            {
                // -----------------------------------------
                // tex1D functions
                // -----------------------------------------

                // ret tex1D(s, t) : http://msdn.microsoft.com/en-us/library/windows/desktop/bb509672%28v=VS.85%29.aspx
                GenericMethod("tex1D", VectorType.Float4, GenericParam("s", SamplerType.Sampler1D), GenericParam("t", ScalarType.Float)),
                 
                // ret tex1D(s, t, ddx, ddy) http://msdn.microsoft.com/en-us/library/windows/desktop/ff471388%28v=VS.85%29.aspx
                GenericMethod("tex1D", VectorType.Float4, GenericParam("s", SamplerType.Sampler1D), GenericParam("t", ScalarType.Float), GenericParam("ddx", ScalarType.Float), GenericParam("ddy", ScalarType.Float)),

                // ret tex1Dbias(s, t) : http://msdn.microsoft.com/en-us/library/windows/desktop/bb509673%28v=VS.85%29.aspx
                GenericMethod("tex1Dbias", VectorType.Float4, GenericParam("s", SamplerType.Sampler1D), GenericParam("t", VectorType.Float4)),

                // ret tex1Dgrad(s, t, ddx, ddy) http://msdn.microsoft.com/en-us/library/windows/desktop/bb509674%28v=VS.85%29.aspx
                GenericMethod("tex1Dgrad", VectorType.Float4, GenericParam("s", SamplerType.Sampler1D), GenericParam("t", ScalarType.Float), GenericParam("ddx", ScalarType.Float), GenericParam("ddy", ScalarType.Float)),

                // ret tex1Dlod(s, t) http://msdn.microsoft.com/en-us/library/windows/desktop/bb509675%28v=VS.85%29.aspx
                GenericMethod("tex1Dlod", VectorType.Float4, GenericParam("s", SamplerType.Sampler1D), GenericParam("t", VectorType.Float4)),

                // ret tex1Dproj(s, t) http://msdn.microsoft.com/en-us/library/windows/desktop/bb509676%28v=VS.85%29.aspx
                GenericMethod("tex1Dproj", VectorType.Float4, GenericParam("s", SamplerType.Sampler1D), GenericParam("t", VectorType.Float4)),
                
                // -----------------------------------------
                // tex2D functions
                // -----------------------------------------

                // ret tex2D(s, t) : http://msdn.microsoft.com/en-us/library/windows/desktop/bb509677%28v=VS.85%29.aspx
                GenericMethod("tex2D", VectorType.Float4, GenericParam("s", SamplerType.Sampler2D), GenericParam("t", VectorType.Float2)),
                 
                // ret tex2D(s, t, ddx, ddy) http://msdn.microsoft.com/en-us/library/windows/desktop/ff471389%28v=VS.85%29.aspx
                GenericMethod("tex2D", VectorType.Float4, GenericParam("s", SamplerType.Sampler2D), GenericParam("t", VectorType.Float2), GenericParam("ddx", VectorType.Float2), GenericParam("ddy", VectorType.Float2)),

                // ret tex2Dbias(s, t) : http://msdn.microsoft.com/en-us/library/windows/desktop/bb509678%28v=VS.85%29.aspx
                GenericMethod("tex2Dbias", VectorType.Float4, GenericParam("s", SamplerType.Sampler2D), GenericParam("t", VectorType.Float4)),

                // ret tex2Dgrad(s, t, ddx, ddy) http://msdn.microsoft.com/en-us/library/windows/desktop/bb509679%28v=VS.85%29.aspx
                GenericMethod("tex2Dgrad", VectorType.Float4, GenericParam("s", SamplerType.Sampler2D), GenericParam("t", VectorType.Float2), GenericParam("ddx", VectorType.Float2), GenericParam("ddy", VectorType.Float2)),

                // ret tex2Dlod(s, t) http://msdn.microsoft.com/en-us/library/windows/desktop/bb509680%28v=VS.85%29.aspx
                GenericMethod("tex2Dlod", VectorType.Float4, GenericParam("s", SamplerType.Sampler2D), GenericParam("t", VectorType.Float4)),

                // ret tex2Dproj(s, t) http://msdn.microsoft.com/en-us/library/windows/desktop/bb509681%28v=VS.85%29.aspx
                GenericMethod("tex2Dproj", VectorType.Float4, GenericParam("s", SamplerType.Sampler2D), GenericParam("t", VectorType.Float4)),

                // -----------------------------------------
                // tex3D functions
                // -----------------------------------------

                // ret tex3D(s, t) : http://msdn.microsoft.com/en-us/library/windows/desktop/bb509682%28v=VS.85%29.aspx
                GenericMethod("tex3D", VectorType.Float4, GenericParam("s", SamplerType.Sampler3D), GenericParam("t", VectorType.Float3)),
                 
                // ret tex3D(s, t, ddx, ddy) http://msdn.microsoft.com/en-us/library/windows/desktop/ff471391%28v=VS.85%29.aspx
                GenericMethod("tex3D", VectorType.Float4, GenericParam("s", SamplerType.Sampler3D), GenericParam("t", VectorType.Float3), GenericParam("ddx", VectorType.Float3), GenericParam("ddy", VectorType.Float3)),

                // ret tex3Dbias(s, t) : http://msdn.microsoft.com/en-us/library/windows/desktop/bb509683%28v=VS.85%29.aspx
                GenericMethod("tex3Dbias", VectorType.Float4, GenericParam("s", SamplerType.Sampler3D), GenericParam("t", VectorType.Float4)),

                // ret tex3Dgrad(s, t, ddx, ddy) http://msdn.microsoft.com/en-us/library/windows/desktop/bb509684%28v=VS.85%29.aspx
                GenericMethod("tex3Dgrad", VectorType.Float4, GenericParam("s", SamplerType.Sampler3D), GenericParam("t", VectorType.Float3), GenericParam("ddx", VectorType.Float3), GenericParam("ddy", VectorType.Float3)),

                // ret tex3Dlod(s, t) http://msdn.microsoft.com/en-us/library/windows/desktop/bb509685%28v=VS.85%29.aspx
                GenericMethod("tex3Dlod", VectorType.Float4, GenericParam("s", SamplerType.Sampler3D), GenericParam("t", VectorType.Float4)),

                // ret tex3Dproj(s, t) http://msdn.microsoft.com/en-us/library/windows/desktop/bb509686%28v=VS.85%29.aspx
                GenericMethod("tex3Dproj", VectorType.Float4, GenericParam("s", SamplerType.Sampler3D), GenericParam("t", VectorType.Float4)),

                // -----------------------------------------
                // texCUBE functions
                // -----------------------------------------

                // ret texCUBE(s, t) : http://msdn.microsoft.com/en-us/library/windows/desktop/bb509687%28v=VS.85%29.aspx
                GenericMethod("texCUBE", VectorType.Float4, GenericParam("s", SamplerType.SamplerCube), GenericParam("t", VectorType.Float3)),
                 
                // ret texCUBE(s, t, ddx, ddy) http://msdn.microsoft.com/en-us/library/windows/desktop/ff471392%28v=VS.85%29.aspx
                GenericMethod("texCUBE", VectorType.Float4, GenericParam("s", SamplerType.SamplerCube), GenericParam("t", VectorType.Float3), GenericParam("ddx", VectorType.Float3), GenericParam("ddy", VectorType.Float3)),

                // ret texCUBEbias(s, t) : http://msdn.microsoft.com/en-us/library/windows/desktop/bb509688%28v=VS.85%29.aspx
                GenericMethod("texCUBEbias", VectorType.Float4, GenericParam("s", SamplerType.SamplerCube), GenericParam("t", VectorType.Float4)),

                // ret texCUBEgrad(s, t, ddx, ddy) http://msdn.microsoft.com/en-us/library/windows/desktop/bb509689%28v=VS.85%29.aspx
                GenericMethod("texCUBEgrad", VectorType.Float4, GenericParam("s", SamplerType.SamplerCube), GenericParam("t", VectorType.Float3), GenericParam("ddx", VectorType.Float3), GenericParam("ddy", VectorType.Float3)),

                // ret texCUBElod(s, t) http://msdn.microsoft.com/en-us/library/windows/desktop/bb509690%28v=VS.85%29.aspx
                GenericMethod("texCUBElod", VectorType.Float4, GenericParam("s", SamplerType.SamplerCube), GenericParam("t", VectorType.Float4)),

                // ret texCUBEproj(s, t) http://msdn.microsoft.com/en-us/library/windows/desktop/bb509691%28v=VS.85%29.aspx
                GenericMethod("texCUBEproj", VectorType.Float4, GenericParam("s", SamplerType.SamplerCube), GenericParam("t", VectorType.Float4)),

                //// tex2Dlod(s, t)
                //GenericMethod("tex2Dlod", VectorType.Float4, GenericContraint("SamplerState", type => type == StateType.SamplerState || type == StateType.SamplerStateOld), 
                //    GenericParam("s", "SamplerState"), GenericParam("t", VectorType.Float4)),
            };

        private static List<GenericParameterConstraint> GenericContraint(string genericT1, Func<TypeBase,bool> checkT1)
        {
            return new List<GenericParameterConstraint>() { new GenericParameterConstraint(genericT1, checkT1) };
        }

        private static List<GenericParameterConstraint> GenericContraint(string genericT1, Func<TypeBase,bool> checkT1, string genericT2, Func<TypeBase,bool> checkT2)
        {
            return new List<GenericParameterConstraint>() { new GenericParameterConstraint(genericT1, checkT1), new GenericParameterConstraint(genericT2, checkT2), };
        }

        private static Ast.Parameter GenericParam(string paramName, string genericTypeName)
        {
            return new Ast.Parameter() { Name = new Identifier(paramName), Type = new GenericParameterType(genericTypeName) };
        }

        private static Ast.Parameter GenericParam(string paramName, TypeBase type)
        {
            return new Ast.Parameter() { Name = new Identifier(paramName), Type = type };
        }

        private static Ast.Parameter GenericParam(string paramName, TypeBase type, Qualifier qualifier)
        {
            return new Ast.Parameter() { Name = new Identifier(paramName), Type = type, Qualifiers = qualifier };
        }

        private static MethodDeclaration GenericMethod(string methodName, TypeBase returnType, params Ast.Parameter[] parameters)
        {
            return GenericMethod(methodName, returnType, null, parameters);
        }

        private static MethodDeclaration GenericMethod(string methodName, TypeBase returnType, List<GenericParameterConstraint> constraints, params Ast.Parameter[] parameters)
        {
            var methodDeclaration = new MethodDeclaration() { Name = new Identifier(methodName), ReturnType = returnType };
            methodDeclaration.IsBuiltin = true;
            if (constraints != null)
                methodDeclaration.ParameterConstraints = constraints;
            methodDeclaration.Parameters.AddRange(parameters);
            return methodDeclaration;
        }


        static TypeBase[] FloatTargets = new[] { ScalarType.Float, ScalarType.Double };
        static TypeBase[] NumericTargets = new[] { ScalarType.Float, ScalarType.Double, ScalarType.Int, ScalarType.UInt };
        static TypeBase[] NumericAndBoolTargets = new[] { ScalarType.Float, ScalarType.Double, ScalarType.Int, ScalarType.UInt, ScalarType.Bool };

        class ParamDef
        {
            public string Name { get; set; }
            public Func<Function, Parameter[], TypeBase> ParamDecl { get; set; }
            public bool Out { get; set; }
        }
        class Function
        {
            public static ParamDef Param(int index)
            {
                return new ParamDef { Name = "ret", ParamDecl = (f, p) => p[index].GenerateType() };
            }

            public static ParamDef ParamVoid()
            {
                return new ParamDef { Name = "ret", ParamDecl = (f, p) => TypeBase.Void };
            }

            public static ParamDef Param(string name, int index, bool outParam = false)
            {
                return new ParamDef { Name = name, ParamDecl = (f, p) => p[index].GenerateType(), Out = outParam };
            }
            public static ParamDef Param(Func<Function, Parameter[], TypeBase> paramDecl)
            {
                return new ParamDef { Name = "ret", ParamDecl = paramDecl };
            }
            public static ParamDef Param(string name, Func<Function, Parameter[], TypeBase> paramDecl)
            {
                return new ParamDef { Name = name, ParamDecl = paramDecl };
            }
            public string Name { get; set; }
            public ParameterInfo[] Parameters { get; set; }
            public Func<Function, Parameter[], TypeBase[]> ParamList { get; set; }
            public Func<Function, Parameter[], TypeBase> Return { get; set; }
            public static Function Template1(string name, Target[] targets, TypeBase[] targetTypes, ParamDef ret, params ParamDef[] args)
            {
                var p = new ParameterInfo { Targets = targets, TargetTypes = targetTypes, SizeFlags = SizeFlags.None };
                return new Function { Name = name, Parameters = new[] { p }, ParamList = (f, p2) => args.Select(arg => arg.ParamDecl(f, p2)).ToArray(), Return = (f, p2) => ret.ParamDecl(f, p2) };
            }
            public static Function[] Functions = new[]
	        {
		        // Missing5: dst EvaluateAttribute
		        Template1("InterlockedAdd", new[] { Target.Scalar }, new[] {ScalarType.Int, ScalarType.UInt }, Param((f, p) => TypeBase.Void), Param("dest", 0), Param("value", 0), Param("original_value", 0, outParam: true)),
		        Template1("InterlockedAdd", new[] { Target.Scalar }, new[] {ScalarType.Int, ScalarType.UInt }, Param((f, p) => TypeBase.Void), Param("dest", 0), Param("value", 0)),
		        Template1("InterlockedAnd", new[] { Target.Scalar }, new[] {ScalarType.Int, ScalarType.UInt }, Param((f, p) => TypeBase.Void), Param("dest", 0), Param("value", 0), Param("original_value", 0, outParam: true)),
		        Template1("InterlockedAnd", new[] { Target.Scalar }, new[] {ScalarType.Int, ScalarType.UInt }, Param((f, p) => TypeBase.Void), Param("dest", 0), Param("value", 0)),
		        Template1("InterlockedCompareExchange", new[] { Target.Scalar }, new[] {ScalarType.Int, ScalarType.UInt }, Param((f, p) => TypeBase.Void), Param("dest", 0), Param("compare_value", 0), Param("value", 0), Param("original_value", 0, outParam: true)),
		        Template1("InterlockedCompareStore", new[] { Target.Scalar }, new[] {ScalarType.Int, ScalarType.UInt }, Param((f, p) => TypeBase.Void), Param("dest", 0), Param("compare_value", 0), Param("value", 0)),
		        Template1("InterlockedExchange", new[] { Target.Scalar }, new[] {ScalarType.Int, ScalarType.UInt }, Param((f, p) => TypeBase.Void), Param("dest", 0), Param("value", 0), Param("original_value", 0, outParam: true)),
		        Template1("InterlockedMax", new[] { Target.Scalar }, new[] {ScalarType.Int, ScalarType.UInt }, Param((f, p) => TypeBase.Void), Param("dest", 0), Param("value", 0), Param("original_value", 0, outParam: true)),
		        Template1("InterlockedMax", new[] { Target.Scalar }, new[] {ScalarType.Int, ScalarType.UInt }, Param((f, p) => TypeBase.Void), Param("dest", 0), Param("value", 0)),
		        Template1("InterlockedMin", new[] { Target.Scalar }, new[] {ScalarType.Int, ScalarType.UInt }, Param((f, p) => TypeBase.Void), Param("dest", 0), Param("value", 0), Param("original_value", 0, outParam: true)),
		        Template1("InterlockedMin", new[] { Target.Scalar }, new[] {ScalarType.Int, ScalarType.UInt }, Param((f, p) => TypeBase.Void), Param("dest", 0), Param("value", 0)),
		        Template1("InterlockedOr", new[] { Target.Scalar }, new[] {ScalarType.Int, ScalarType.UInt }, Param((f, p) => TypeBase.Void), Param("dest", 0), Param("value", 0), Param("original_value", 0, outParam: true)),
		        Template1("InterlockedOr", new[] { Target.Scalar }, new[] {ScalarType.Int, ScalarType.UInt }, Param((f, p) => TypeBase.Void), Param("dest", 0), Param("value", 0)),
		        Template1("InterlockedXor", new[] { Target.Scalar }, new[] {ScalarType.Int, ScalarType.UInt }, Param((f, p) => TypeBase.Void), Param("dest", 0), Param("value", 0), Param("original_value", 0, outParam: true)),
		        Template1("InterlockedXor", new[] { Target.Scalar }, new[] {ScalarType.Int, ScalarType.UInt }, Param((f, p) => TypeBase.Void), Param("dest", 0), Param("value", 0)),
		        Template1("abs", AllTargets, NumericTargets, Param(0), Param("x", 0)),
		        Template1("acos", AllTargets, FloatTargets, Param(0), Param("x", 0)),
		        Template1("all", AllTargets, NumericAndBoolTargets, Param((f, p) => ScalarType.Bool), Param("x", 0)),
		        //Template1("AllMemoryBarrier", new[] { Target.Scalar }, new TypeBase[] { ScalarType.Float }, ParamVoid()),
                //Template1("AllMemoryBarrierWithGroupSync", new[] { Target.Scalar }, new TypeBase[] { ScalarType.Float }, ParamVoid()),
		        Template1("any", AllTargets, NumericAndBoolTargets, Param((f, p) => ScalarType.Bool), Param("x", 0)),
                Template1("asdouble", new[] { Target.Scalar, Target.Vector2 }, new TypeBase[] { ScalarType.UInt }, Param((f,p) => p[0].ChangeTargetType(ScalarType.Double).GenerateType()), Param("l",0), Param("h",0)),
                Template1("asfloat", AllTargets, new TypeBase[] { ScalarType.Bool, ScalarType.Float, ScalarType.Int, ScalarType.UInt }, Param((f,p) => p[0].ChangeTargetType(ScalarType.Float).GenerateType()), Param("x", 0)),
		        Template1("asin", AllTargets, FloatTargets, Param(0), Param("x", 0)),
                Template1("asint", AllTargets, new TypeBase[] { ScalarType.Float, ScalarType.UInt }, Param((f,p) => p[0].ChangeTargetType(ScalarType.Int).GenerateType()), Param("x", 0)),
                Template1("asuint", AllTargets, new TypeBase[] { ScalarType.Float, ScalarType.Int }, Param((f,p) => p[0].ChangeTargetType(ScalarType.UInt).GenerateType()), Param("x", 0)),
                Template1("asuint", new[] { Target.Scalar }, new TypeBase[] { ScalarType.UInt }, ParamVoid(), Param("x", (f,p)=> p[0].ChangeTargetType(ScalarType.Double).GenerateType()), Param("y", 0, true), Param("z", 0, true)),
		        Template1("atan", AllTargets, FloatTargets, Param(0), Param("x", 0)),
		        Template1("atan2", AllTargets, FloatTargets, Param(0), Param("y", 0), Param("x", 0)),
		        Template1("ceil", AllTargets, FloatTargets, Param(0), Param("x", 0)),
		        Template1("clamp", AllTargets, NumericTargets, Param(0), Param("x", 0), Param("min", 0), Param("max", 0)),
		        Template1("clip", AllTargets, NumericTargets, ParamVoid(), Param("x", 0)),
		        Template1("cos", AllTargets, FloatTargets, Param(0), Param("x", 0)),
		        Template1("cosh", AllTargets, FloatTargets, Param(0), Param("x", 0)),
                Template1("countbits", new[] { Target.Scalar, Target.Vector }, new TypeBase[] { ScalarType.UInt }, Param(0), Param("x", 0)),
		        Template1("cross", new[] { Target.Vector3 }, FloatTargets, Param(0), Param("x", 0), Param("y", 0)),
                //Template1("D3DCOLORtoUBYTE4", new[] { Target.Vector4 }, new TypeBase[] { ScalarType.Float }, Param((f,p) => VectorType.Int4), Param("x", 0)),
		        Template1("ddx", AllTargets, FloatTargets, Param(0), Param("x", 0)),
                Template1("ddx_coarse", new[] { Target.Scalar, Target.Vector }, new TypeBase[] { ScalarType.Float }, Param(0), Param("x", 0)),
                Template1("ddx_fine", new[] { Target.Scalar, Target.Vector }, new TypeBase[] { ScalarType.Float }, Param(0), Param("x", 0)),
		        Template1("ddy", AllTargets, FloatTargets, Param(0), Param("x", 0)),
                Template1("ddy_coarse", new[] { Target.Scalar, Target.Vector }, new TypeBase[] { ScalarType.Float }, Param(0), Param("x", 0)),
                Template1("ddy_fine", new[] { Target.Scalar, Target.Vector }, new TypeBase[] { ScalarType.Float }, Param(0), Param("x", 0)),
		        Template1("degrees", AllTargets, FloatTargets, Param(0), Param("x", 0)),
		        Template1("determinant", new[] { Target.Matrix }, FloatTargets, Param((f, p) => p[0].ChangeTarget(Target.Scalar).GenerateType()), Param("x", 0)),
		        Template1("distance", new[] { Target.Vector }, FloatTargets, Param((f, p) => p[0].ChangeTarget(Target.Scalar).GenerateType()), Param("x", 0), Param("y", 0)),
		        Template1("dot", new[] { Target.Vector }, NumericTargets, Param((f, p) => p[0].ChangeTarget(Target.Scalar).GenerateType()), Param("x", 0), Param("y", 0)),
                Template1("EvaluateAttributeAtCentroid", AllTargets, NumericTargets, Param(0), Param("x", 0)),
                Template1("EvaluateAttributeAtSample", AllTargets, NumericTargets, Param(0), Param("x", 0), Param("s", (f,p) => ScalarType.UInt)),
                Template1("EvaluateAttributeSnapped", AllTargets, NumericTargets, Param(0), Param("x", 0), Param("s", (f,p) => VectorType.Int2)),
		        Template1("exp", AllTargets, FloatTargets, Param(0), Param("x", 0)),
		        Template1("exp2", AllTargets, FloatTargets, Param(0), Param("x", 0)),
                Template1("f16tof32", new[] { Target.Scalar, Target.Vector }, new TypeBase[] { ScalarType.UInt }, Param((f,p) => p[0].ChangeTargetType(ScalarType.Float).GenerateType()), Param("x",0)),
                Template1("f32tof16", new[] { Target.Scalar, Target.Vector }, new TypeBase[] { ScalarType.Float }, Param((f,p) => p[0].ChangeTargetType(ScalarType.UInt).GenerateType()), Param("x",0)),
		        Template1("faceforward", new[] { Target.Vector }, FloatTargets, Param(0), Param("n", 0), Param("i", 0), Param("ng", 0)),
                Template1("firstbithigh", new[] { Target.Scalar, Target.Vector }, new TypeBase[] { ScalarType.Int, ScalarType.UInt }, Param(0), Param("x", 0)),
                Template1("firstbitlow", new[] { Target.Scalar }, new TypeBase[] { ScalarType.Int }, Param(0), Param("x", 0)),
                Template1("firstbitlow", new[] { Target.Vector2, Target.Vector3, Target.Vector4 }, new TypeBase[] { ScalarType.UInt }, Param(0), Param("x", 0)),
		        Template1("floor", AllTargets, FloatTargets, Param(0), Param("x", 0)),
		        Template1("fmod", AllTargets, FloatTargets, Param(0), Param("x", 0), Param("y", 0)),
		        Template1("frac", AllTargets, FloatTargets, Param(0), Param("x", 0)),
		        Template1("frexp", AllTargets, FloatTargets, Param(0), Param("x", 0), Param("exp", 0)),
		        Template1("fwidth", AllTargets, FloatTargets, Param(0), Param("x", 0)),
		        Template1("isfinite", AllTargets, FloatTargets, Param((f, p) => p[0].ChangeTargetType(ScalarType.Bool).GenerateType()), Param("x", 0)),
		        Template1("isinf", AllTargets, FloatTargets, Param((f, p) => p[0].ChangeTargetType(ScalarType.Bool).GenerateType()), Param("x", 0)),
		        Template1("isnan", AllTargets, FloatTargets, Param((f, p) => p[0].ChangeTargetType(ScalarType.Bool).GenerateType()), Param("x", 0)),
		        Template1("ldexp", AllTargets, FloatTargets, Param(0), Param("x", 0), Param("exp", 0)),
		        Template1("length", new[] { Target.Vector }, FloatTargets, Param((f, p) => p[0].ChangeTarget(Target.Scalar).GenerateType()), Param("x", 0)),
		        Template1("lerp", AllTargets, FloatTargets, Param(0), Param("x", 0), Param("y", 0), Param("s", 0)),
		        Template1("lit", new[] { Target.Scalar }, FloatTargets, Param((f, p) => VectorType.Float4), Param("n_dot_l", 0), Param("n_dot_h", 0), Param("m", 0)),
		        Template1("log", AllTargets, FloatTargets, Param(0), Param("x", 0)),
		        Template1("log10", AllTargets, FloatTargets, Param(0), Param("x", 0)),
		        Template1("log2", AllTargets, FloatTargets, Param(0), Param("x", 0)),
                Template1("mad", new[] { Target.Scalar, Target.Vector }, NumericTargets, Param(0), Param("x", 0), Param("y", 0), Param("z", 0)),
		        Template1("max", AllTargets, NumericTargets, Param(0), Param("x", 0), Param("y", 0)),
		        Template1("min", AllTargets, NumericTargets, Param(0), Param("x", 0), Param("y", 0)),
		        Template1("modf", AllTargets, NumericTargets, Param(0), Param("x", 0), Param("ip", 0, outParam: true)),
		        Template1("mul", new[] { Target.Scalar }, FloatTargets, Param(0), Param("x", 0), Param("y", 0)), // Group 1
		        Template1("mul", new[] { Target.Vector }, FloatTargets, Param(0), Param("x", (f, p) => p[0].ChangeTarget(Target.Scalar).GenerateType()), Param("y", 0)), // Group 2
		        Template1("mul", new[] { Target.Matrix }, FloatTargets, Param(0), Param("x", (f, p) => p[0].ChangeTarget(Target.Scalar).GenerateType()), Param("y", 0)), // Group 3
		        Template1("mul", new[] { Target.Vector }, FloatTargets, Param(0), Param("x", 0), Param("y", (f, p) => p[0].ChangeTarget(Target.Scalar).GenerateType())), // Group 4
		        Template1("mul", new[] { Target.Matrix }, FloatTargets, Param(0), Param("x", 0), Param("y", (f, p) => p[0].ChangeTarget(Target.Scalar).GenerateType())), // Group 7
		        Template1("mul", new[] { Target.Vector }, FloatTargets, Param((f, p) => p[0].ChangeTarget(Target.Scalar).GenerateType()), Param("x", 0), Param("y", 0)), // Group 5
		        Template1("mul", new[] { Target.Matrix }, FloatTargets, Param((f, p) => p[0].ReduceFromMatrixColumn().GenerateType()), Param("x", (f, p) => p[0].ReduceFromMatrixRow().GenerateType()), Param("y", 0)), // Group 6
		        Template1("mul", new[] { Target.Matrix }, FloatTargets, Param((f, p) => p[0].ReduceFromMatrixRow().GenerateType()), Param("x", 0), Param("y", (f, p) => p[0].ReduceFromMatrixColumn().GenerateType())), // Group 8
		        Template1("mul", new[] { Target.Matrix }, FloatTargets, Param((f, p) => p[0].MakeMatrix(p[0].RowCount, 1).GenerateType()), Param("x", 0), Param("y", (f, p) => p[0].MakeMatrix(p[0].ColumnCount, 1).GenerateType())), // Group 9
		        Template1("mul", new[] { Target.Matrix }, FloatTargets, Param((f, p) => p[0].MakeMatrix(p[0].RowCount, 2).GenerateType()), Param("x", 0), Param("y", (f, p) => p[0].MakeMatrix(p[0].ColumnCount, 2).GenerateType())), // Group 9
		        Template1("mul", new[] { Target.Matrix }, FloatTargets, Param((f, p) => p[0].MakeMatrix(p[0].RowCount, 3).GenerateType()), Param("x", 0), Param("y", (f, p) => p[0].MakeMatrix(p[0].ColumnCount, 3).GenerateType())), // Group 9
		        Template1("mul", new[] { Target.Matrix }, FloatTargets, Param((f, p) => p[0].MakeMatrix(p[0].RowCount, 4).GenerateType()), Param("x", 0), Param("y", (f, p) => p[0].MakeMatrix(p[0].ColumnCount, 4).GenerateType())), // Group 9
		        Template1("normalize", AllTargets, FloatTargets, Param(0), Param("x", 0)),
		        Template1("pow", AllTargets, FloatTargets, Param(0), Param("x", 0), Param("y", 0)),
		        Template1("radians", AllTargets, FloatTargets, Param(0), Param("x", 0)),
                Template1("rcp", AllTargets, FloatTargets, Param(0), Param("x", 0)),
		        Template1("reflect", new[] { Target.Vector }, FloatTargets, Param(0), Param("i", 0), Param("n", 0)),
		        Template1("refract", new[] { Target.Vector }, FloatTargets, Param(0), Param("i", 0), Param("n", 0), Param("index", (f, p) => p[0].ChangeTarget(Target.Scalar).GenerateType())),
                Template1("reversebits", new[] { Target.Scalar, Target.Vector }, new TypeBase[] { ScalarType.UInt }, Param(0), Param("x", 0)),
		        Template1("round", AllTargets, FloatTargets, Param(0), Param("x", 0)),
		        Template1("rsqrt", AllTargets, FloatTargets, Param(0), Param("x", 0)),
		        Template1("saturate", AllTargets, FloatTargets, Param(0), Param("x", 0)),
		        Template1("sign", AllTargets, NumericTargets, Param((f, p) => p[0].ChangeTargetType(ScalarType.Int).GenerateType()), Param("x", 0)),
		        Template1("sin", AllTargets, FloatTargets, Param(0), Param("x", 0)),
		        Template1("sincos", AllTargets, FloatTargets, Param((f, p) => TypeBase.Void), Param("x", 0), Param("s", 0, outParam: true), Param("c", 0, outParam: true)),
		        Template1("sinh", AllTargets, FloatTargets, Param(0), Param("x", 0)),
		        Template1("smoothstep", AllTargets, FloatTargets, Param(0), Param("min", 0), Param("max", 0), Param("x", 0)),
		        Template1("sqrt", AllTargets, FloatTargets, Param(0), Param("x", 0)),
		        Template1("step", AllTargets, FloatTargets, Param(0), Param("y", 0), Param("x", 0)),
		        Template1("tan", AllTargets, FloatTargets, Param(0), Param("x", 0)),
		        Template1("tanh", AllTargets, FloatTargets, Param(0), Param("x", 0)),
		        Template1("transpose", new[] { Target.Matrix }, FloatTargets, Param((f, p) => p[0].Transpose().GenerateType()), Param("x", 0)),
		        Template1("trunc", AllTargets, FloatTargets, Param(0), Param("x", 0)),
	        };
        }
        static Target[] AllTargets = new[] { Target.Scalar, Target.Vector, Target.Matrix };
        enum Target
        {
            Scalar = 1,
            Vector = 2,
            SquareMatrix = 3,
            Matrix = 4,
            Vector2 = 5,
            Vector3 = 6,
            Vector4 = 7,
        }
        [Flags]
        enum SizeFlags
        {
            None = 0,
            AllMatrix = 1,
        }
        class ParameterInfo
        {
            public IList<Target> Targets;
            public IList<TypeBase> TargetTypes;
            public SizeFlags SizeFlags;
        }
        class Parameter
        {
            public Parameter ChangeTarget(Target target)
            {
                return new Parameter { Target = target, TargetType = TargetType, TargetSize = TargetSize };
            }
            public Parameter ChangeTargetType(TypeBase targetType)
            {
                return new Parameter { Target = Target, TargetType = targetType, TargetSize = TargetSize };
            }
            public Parameter MakeMatrix(int rowCount, int columnCount)
            {
                return new Parameter { Target = Target.Matrix, TargetType = TargetType, TargetSize = (columnCount - 1) * 4 + rowCount - 1 };
            }
            public Parameter ReduceFromMatrixRow()
            {
                return new Parameter { Target = Target.Vector, TargetType = TargetType, TargetSize = TargetSize % 4 };
            }
            public Parameter ReduceFromMatrixColumn()
            {
                return new Parameter { Target = Target.Vector, TargetType = TargetType, TargetSize = TargetSize / 4 };
            }
            public Parameter Transpose()
            {
                int row = TargetSize % 4;
                int column = TargetSize / 4;
                return new Parameter { Target = Target, TargetType = TargetType, TargetSize = row * 4 + column };
            }
            public int RowCount { get { return (TargetSize % 4) + 1; } }
            public int ColumnCount { get { return (TargetSize / 4) + 1; } }
            public Target Target { get; set; }
            public TypeBase TargetType { get; set; }
            public int TargetSize { get; set; }

            public TypeBase GenerateType()
            {
                if (Target == Target.Matrix)
                    return new MatrixType((ScalarType)TargetType, RowCount, ColumnCount);
                if (Target == Target.Vector)
                    return new VectorType((ScalarType)TargetType, RowCount);
                if (Target == Target.Vector2)
                    return new VectorType((ScalarType)TargetType, 2);
                if (Target == Target.Vector3)
                    return new VectorType((ScalarType)TargetType, 3);
                if (Target == Target.Vector4)
                    return new VectorType((ScalarType)TargetType, 4);
                return TargetType;
            }
        }

        static IEnumerable<Parameter> EnumerateParameters(ParameterInfo p)
        {
            for (int target = 0; target < p.Targets.Count(); ++target)
            {
                for (int targetType = 0; targetType < p.TargetTypes.Count(); ++targetType)
                {
                    switch (p.Targets[target])
                    {
                        case Target.Scalar:
                            yield return new Parameter { Target = p.Targets[target], TargetType = p.TargetTypes[targetType], TargetSize = 1 };
                            break;
                        case Target.Vector:
                        case Target.SquareMatrix:
                            for (int i = 0; i < 4; ++i)
                            {
                                yield return new Parameter { Target = p.Targets[target], TargetType = p.TargetTypes[targetType], TargetSize = i };
                            }
                            break;
                        case Target.Vector2:
                            yield return new Parameter { Target = p.Targets[target], TargetType = p.TargetTypes[targetType], TargetSize = 1 };
                            break;
                        case Target.Vector3:
                            yield return new Parameter { Target = p.Targets[target], TargetType = p.TargetTypes[targetType], TargetSize = 2 };
                            break;
                        case Target.Vector4:
                            yield return new Parameter { Target = p.Targets[target], TargetType = p.TargetTypes[targetType], TargetSize = 3 };
                            break;
                        case Target.Matrix:
                            for (int i = 0; i < 16; ++i)
                            {
                                yield return new Parameter { Target = p.Targets[target], TargetType = p.TargetTypes[targetType], TargetSize = i };
                            }
                            break;
                    }
                }
            }
        }

        #endregion
    }


    internal class GenericInstanceKey
    {
        public string GenericName;

        public List<Node> GenericParameters;

        public GenericInstanceKey(string genericName, List<Node> genericParams)
        {
            GenericName = genericName;
            GenericParameters = genericParams;
        }

        public override bool Equals(object obj)
        {
            var genInstKey = obj as GenericInstanceKey;
            if (genInstKey == null)
                return false;

            if (GenericParameters.Count != genInstKey.GenericParameters.Count)
                return false;

            bool res = true;
            for (int i = 0; i < GenericParameters.Count; ++i)
                res &= GenericParameters[i] == genInstKey.GenericParameters[i];
            
            return res;
        }

        public override int GetHashCode()
        {
            return (GenericName.GetHashCode() * 397);// ^ ;
        }
    }
}
