// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Shaders.Utility;

namespace Stride.Shaders.Parser.Utility
{
    static public class StrideMessageCode
    {
        // analysis warning: W0###
        public static readonly MessageCode WarningDeclarationCall                   = new MessageCode("W0201", "The method invocation [{0}] calls the method [{1}] which is only declared, and not defined in class [{2}]");
        public static readonly MessageCode WarningMissingStageKeyword               = new MessageCode("W0202", "The stage keyword is missing in The method declaration [{0}] in class [{1}]");
        public static readonly MessageCode WarningUseSemanticType                   = new MessageCode("W0203", "The generic [{0}] is not of Semantic type but was used as semantic. Change the type or change name of the generic if there is a conflict.");
                                                                              
        // analysis errors: E0###                                                   
        public static readonly MessageCode ErrorCyclicDependency                    = new MessageCode("E0201", "Cyclic mixin [{0}] dependency");
        public static readonly MessageCode ErrorFunctionRedefined                   = new MessageCode("E0202", "There is already a function with the same signature as [{0}] in class [{1}]");
        public static readonly MessageCode ErrorFunctionVariableNameConflict        = new MessageCode("E0203", "The function [{0}] has the same name as a variable in class [{1}]");
        public static readonly MessageCode ErrorVariableNameConflict                = new MessageCode("E0204", "The variable [{0}] has the same name as another variable in class [{1}]");
        public static readonly MessageCode ErrorVariableFunctionNameConflict        = new MessageCode("E0205", "The variable [{0}] has the same name as a method in class [{1}]");
        public static readonly MessageCode ErrorVreNoTypeInference                  = new MessageCode("E0206", "VariableReferenceExpression [{0}] has no type inference in class [{1}]");
        public static readonly MessageCode ErrorStageVariableTypeConflict           = new MessageCode("E0207", "Stage variable declaration [{0}] has not the same type as the actual definition [{1}] in class [{2}]");
        public static readonly MessageCode ErrorImpossibleBaseCall                  = new MessageCode("E0208", "Unable to find the base call [{0}] of class [{1}]");
        public static readonly MessageCode ErrorImpossibleVirtualCall               = new MessageCode("E0209", "Unable to find the virtual call [{0}] of class [{1}] in context [{2}]");
        public static readonly MessageCode ErrorExternStageVariableNotFound         = new MessageCode("E0210", "There is no matching instance for variable [{0}] in class [{1}]");
        public static readonly MessageCode ErrorExternStageFunctionNotFound         = new MessageCode("E0211", "Unable to find the virtual call [{0}] of extern class [{1}] in context [{2}]");
        public static readonly MessageCode ErrorMissingOverride                     = new MessageCode("E0212", "There is already a method with the same signature as [{0}] in class [{1}]. Missing override keyword?");
        public static readonly MessageCode ErrorOverrideDeclaration                 = new MessageCode("E0213", "There is no need for the override keyword when overriding the method declaration [{0}] in class [{1}]");
        public static readonly MessageCode ErrorNoMethodToOverride                  = new MessageCode("E0214", "There is no method [{0}] to override in class [{1}]");
        public static readonly MessageCode ErrorShaderClassTypeParameter            = new MessageCode("E0215", "The function [{0}] has a paramater [{1}] of shader class type which is not allowed in class [{2}]");
        public static readonly MessageCode ErrorShaderClassReturnType               = new MessageCode("E0216", "The function [{0}] is not allowed to return a class in class [{1}]");
        public static readonly MessageCode ErrorMissingAbstract                     = new MessageCode("E0217", "The method [{0}] is only declared, so it should have the abstract keyword in class [{1}]");
        public static readonly MessageCode ErrorUnnecessaryOverride                 = new MessageCode("E0218", "The method [{0}] is only declared, so it cannot have the override keyword in class [{1}]");
        public static readonly MessageCode ErrorUnnecessaryAbstract                 = new MessageCode("E0219", "The method [{0}] is defined, so it cannot have the abstract keyword in clas [{1}]");
        public static readonly MessageCode ErrorStageInitNotClassType               = new MessageCode("E0220", "The variable [{0}] is initialized at stage and should be of class type in class [{1}]");
        public static readonly MessageCode ErrorExternNotClassType                  = new MessageCode("E0221", "The extern variable [{0}] should be of class type in class [{1}]");
        public static readonly MessageCode ErrorMissingExtern                       = new MessageCode("E0222", "The variable [{0}] is of class type and should have the extern keyword in class [{1}]");
        public static readonly MessageCode ErrorVarNoInitialValue                   = new MessageCode("E0223", "The variable [{0}] should have an initial value to guess its type in class [{1}]");
        public static readonly MessageCode ErrorVarNoTypeFound                      = new MessageCode("E0224", "Unable to guess the type of the variable [{0}] in class [{1}]");
        public static readonly MessageCode ErrorTechniqueFound                      = new MessageCode("E0225", "Techniques like [{0}] are not allowed in the Stride shading language in class [{1}]");
        public static readonly MessageCode ErrorExternMemberNotFound                = new MessageCode("E0226", "There is no member [{0}] for the type [{1}] in class [{2}]");
        public static readonly MessageCode ErrorStreamNotFound                      = new MessageCode("E0227", "Unable to find stream variable [{0}] in class [{1}]");
        public static readonly MessageCode ErrorStreamUsage                         = new MessageCode("E0228", "the stream [{0}] was read first THEN written in class [{1}]");
        public static readonly MessageCode ErrorVariableNameAmbiguity               = new MessageCode("E0229", "The name [{0}] is ambiguous within variables in class [{2}]");
        public static readonly MessageCode ErrorMethodNameAmbiguity                 = new MessageCode("E0230", "The name [{0}] is ambiguous within methods in class [{1}]");
        public static readonly MessageCode ErrorMissingMethod                       = new MessageCode("E0231", "The method [{0}] in class [{1}] is not defined");
        public static readonly MessageCode ErrorCyclicMethod                        = new MessageCode("E0232", "Method [{0}] performs a cyclic call, which is not allowed in class [{1}]");
        public static readonly MessageCode ErrorDeclarationCall                     = new MessageCode("E0233", "The method invocation [{0}] calls the method [{1}] which is only declared, and not defined in class [{2}]");
        public static readonly MessageCode ErrorNoBaseMixin                         = new MessageCode("E0234", "base call [{0}] without any base class in class [{1}]");
        public static readonly MessageCode ErrorStageOutsideVariable                = new MessageCode("E0235", "Use of the stage keyword in [{0}] which is outside a variable in class [{1}]");
        public static readonly MessageCode ErrorMissingStreamsStruct                = new MessageCode("E0236", "The variable [{0}] is a stream/patchstream and should be called this way [streams/constants.{0}] in class [{1}]");
        public static readonly MessageCode ErrorMissingVariable                     = new MessageCode("E0237", "The variable [{0}] in class [{1}] is not defined");
        public static readonly MessageCode ErrorNoTypeInference                     = new MessageCode("E0238", "Unable to infer type for [{0}] in class [{1}]");
        public static readonly MessageCode ErrorShaderVariable                      = new MessageCode("E0239", "It is forbidden to create Shader variables like [{0}] in class [{1}]");
        public static readonly MessageCode ErrorInterfaceFound                      = new MessageCode("E0240", "Hlsl interfaces like [{0}] are not allowed in Stride. Use classes instead. In class [{1}]");
        public static readonly MessageCode ErrorMixinAsGeneric                      = new MessageCode("E0241", "A class like [{0}] cannot be used as a generic parameter for [{1}] in class [{2}]");
        public static readonly MessageCode ErrorInOutStream                         = new MessageCode("E0242", "The stream [{0}] is used as an inout parameter in method [{1}] in class [{2}]");
        public static readonly MessageCode ErrorIndexerNotLiteral                   = new MessageCode("E0243", "The IndexerExpression [{0}] of a composition have to be used with a literal index in class [{1}]");
        public static readonly MessageCode ErrorMultiDimArray                       = new MessageCode("E0244", "Multi-dimentional arrays [{0}] are not supported in foreach statement [{1}] in class [{2}]");
        public static readonly MessageCode ErrorExtraStageKeyword                   = new MessageCode("E0245", "The overriding method [{0}] have a stage keyword whereas its base doesn't [{1}], in class [{2}]");
        public static readonly MessageCode ErrorTypedefInMethod                     = new MessageCode("E0246", "The typedef [{0}] is defined a method ([{1}]) which is not allowed, in class [{2}]");
        public static readonly MessageCode ErrorNestedAssignment                    = new MessageCode("E0247", "Nested target assignment on the left like [{0}] are not supported, in shader [{1}]");
        public static readonly MessageCode ErrorMultidimensionalCompositionArray    = new MessageCode("E0248", "Multidimentional conposition arrays (type [{0}]) are not supported, in class [{1}]");
        public static readonly MessageCode ErrorOverrindingDeclaration              = new MessageCode("E0249", "Method [{0}] is an overriding declaration, this is not allowed");
        public static readonly MessageCode ErrorNullKeyword                         = new MessageCode("E0250", "Keyword null is used outside of variable initialization in [{0}] in class [{1}]");
        public static readonly MessageCode ErrorExtraStreamsPrefix                  = new MessageCode("E0251", "The variable [{0}] has the stream prefix but its declaration [{1}] is not a stream variable, in class [{2}]");
        public static readonly MessageCode ErrorNonStaticCallInStaticMethod         = new MessageCode("E0252", "The static method [{0}] performs a non-static call to [{1}], in class [{2}]");
        public static readonly MessageCode ErrorNonStaticReferenceInStaticMethod    = new MessageCode("E0253", "The static method [{0}] contains a reference to a non-static member [{1}], in class [{2}]");

        // module errors: E1###
        public static readonly MessageCode UnknownModuleError                       = new MessageCode("E1200", "Unknown module error");
        public static readonly MessageCode ErrorClassNotFound                       = new MessageCode("E1201", "The class [{0}] was not found from the include path");
        public static readonly MessageCode ErrorDependencyNotInModule               = new MessageCode("E1202", "The mixin [{0}] in [{1}] dependency is not in the module");
        public static readonly MessageCode ErrorClassSourceNotInstantiated          = new MessageCode("E1203", "The class source [{0}] contains generic parameters and is not instantiated");
        public static readonly MessageCode ErrorAmbiguousComposition                = new MessageCode("E1204", "The composition behind the variable [{0}] is ambiguous. Several matching variables were found.");
        
        // mix errors: E2###
        public static readonly MessageCode UnknownMixError                          = new MessageCode("E2200", "Unknown mix error");
        public static readonly MessageCode ErrorVariableNotFound                    = new MessageCode("E2201", "Variable [{0}] not found in class [{1}]");
        public static readonly MessageCode ErrorMissingStageVariable                = new MessageCode("E2202", "Missing stage variable [{0}] from class [{1}]");
        public static readonly MessageCode ErrorExternReferenceNotFound             = new MessageCode("E2203", "Extern reference [{0}] not found from class [{1}]");
        public static readonly MessageCode ErrorStageMixinNotFound                  = new MessageCode("E2204", "Stage mixin [{0}] not found from class [{1}] through stage initialized variable");
        public static readonly MessageCode ErrorStageMixinVariableNotFound          = new MessageCode("E2205", "Stage mixin [{0}] variable [{1}] not found from class [{1}]");
        public static readonly MessageCode ErrorStageMixinMethodNotFound            = new MessageCode("E2206", "Stage mixin [{0}] method [{1}] not found from class [{1}]");
        public static readonly MessageCode ErrorIncompleteTesselationShader         = new MessageCode("E2207", "Tessellation Shader is not compete, one stage is missing");
        public static readonly MessageCode ErrorSemanticCbufferConflict             = new MessageCode("E2208", "Variables [{0}] from [{1}] and [{2}] from [{3}] share the same semantic [{4}] but have distinct cbuffers ([{5}] and [{6}])");
        public static readonly MessageCode ErrorRecursiveCall                       = new MessageCode("E2209", "Method [{0}] performs a recursive call which is not supported in shader language");
        public static readonly MessageCode ErrorStreamUsageInitialization           = new MessageCode("E2210", "A stream usage was added but not correctly initialized");
        public static readonly MessageCode ErrorCrossStageMethodCall                = new MessageCode("E2211", "Method [{0}] that uses streams is called in both [{1}] and [{2}] shader stages");
        public static readonly MessageCode ErrorCallToAbstractMethod                = new MessageCode("E2212", "The method invocation [{0}] calls the abstract method [{1}]");
        public static readonly MessageCode ErrorCallNotFound                        = new MessageCode("E2213", "The method invocation [{0}] target could not be found");
        public static readonly MessageCode ErrorTopMixinNotFound                    = new MessageCode("E2214", "The top mixin of [{0}] could not be found");
        public static readonly MessageCode ErrorSemanticTypeConflict                = new MessageCode("E2215", "Variables [{0}] from [{1}] and [{2}] from [{3}] share the same semantic [{4}] but have distinct types ([{5}] and [{6}])");

        // linker errors: E3###
        public static readonly MessageCode SamplerFilterNotSupported                = new MessageCode("E3000", "The sampler filter [{0}] is not supported");
        public static readonly MessageCode SamplerAddressModeNotSupported           = new MessageCode("E3001", "The sampler address mode [{0}] is not supported. Expecting AddressV, AddressU, AddressW");
        public static readonly MessageCode SamplerBorderColorNotSupported           = new MessageCode("E3002", "The sampler color component [{0}] is not supported. Expecting a float");

        // loading errors
        public static readonly MessageCode ErrorUninstanciatedClass                 = new MessageCode("E3202", "The class [{0}] is not correctly instanciated with the current set of generics");
        public static readonly MessageCode SamplerFieldNotSupported                 = new MessageCode("E3003", "The sampler field [{0}] is not supported");
        public static readonly MessageCode LinkError                                = new MessageCode("E3004", "HLSL Link error: Could not find variable {0} in the shader");
        public static readonly MessageCode LinkArgumentsError                       = new MessageCode("E3005", "HLSL Link error: Invalid number of arguments. Expecting only name of the linked variable");
        public static readonly MessageCode VariableTypeNotSupported                 = new MessageCode("E3006", "The type of the variable [{0}] is not supported");
        public static readonly MessageCode StreamVariableWithoutPrefix              = new MessageCode("E3007", "Stream variable [{0}] is used without 'streams'. prefix");
        public static readonly MessageCode WrongGenericNumber                       = new MessageCode("E3008", "The class [{0}] could not be instanciated because the number of required generics does not match the number of passed generics.");
        public static readonly MessageCode SameNameGenerics                         = new MessageCode("E3009", "The generic [{0}] has the same name as [{1}]. Class [{2}] couldn't be instanciated.");
        public static readonly MessageCode FileNameNotMatchingClassName             = new MessageCode("E3010", "The shader file name [{0}] is not matching the shader class name [{1}]");
        public static readonly MessageCode ShaderMustContainSingleClassDeclaration  = new MessageCode("E3011", "The shader [{0}] must contain only a single shader class declaration");

        // compiler errors: E4###
        public static readonly MessageCode EntryPointNotFound                       = new MessageCode("E4000", "Entrypoint [{0}] was not found for stage [{1}] in Shader [{2}]");
    }
}
