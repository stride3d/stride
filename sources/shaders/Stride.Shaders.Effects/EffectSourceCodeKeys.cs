// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Rendering;

namespace Stride.Shaders
{
    /// <summary>
    /// Keys used for sourcecode generation.
    /// </summary>
    public static class EffectSourceCodeKeys
    {
        /// <summary>
        /// When compiling a sdsl, this will generate a source code file
        /// </summary>
        public static readonly ObjectParameterKey<bool> Enable = (ObjectParameterKey<bool>)ParameterKeys.Merge(ParameterKeys.NewObject<bool>(), typeof(EffectSourceCodeKeys), "EffectSourceCode.Enable");

        /// <summary>
        /// The class modifier declaration (Default: "public partial")
        /// </summary>
        public static readonly ObjectParameterKey<string> ClassDeclaration = (ObjectParameterKey<string>)ParameterKeys.Merge(ParameterKeys.NewObject("public partial"), typeof(EffectSourceCodeKeys), "EffectSourceCode.ClassDeclaration");

        /// <summary>
        /// The namespace used for the declaration.
        /// </summary>
        public static readonly ObjectParameterKey<string> Namespace = (ObjectParameterKey<string>)ParameterKeys.Merge(ParameterKeys.NewObject<string>(), typeof(EffectSourceCodeKeys), "EffectSourceCode.Namespace");

        /// <summary>
        /// The classname used for the (Default: name of the effect).
        /// </summary>
        public static readonly ObjectParameterKey<string> ClassName = (ObjectParameterKey<string>)ParameterKeys.Merge(ParameterKeys.NewObject<string>(), typeof(EffectSourceCodeKeys), "EffectSourceCode.ClassName");

        /// <summary>
        /// The field declaration (default: "private")
        /// </summary>
        public static readonly ObjectParameterKey<string> FieldDeclaration = (ObjectParameterKey<string>)ParameterKeys.Merge(ParameterKeys.NewObject("private"), typeof(EffectSourceCodeKeys), "EffectSourceCode.FieldDeclaration");

        /// <summary>
        /// The field name (default: "binaryBytecode")
        /// </summary>
        public static readonly ObjectParameterKey<string> FieldName = (ObjectParameterKey<string>)ParameterKeys.Merge(ParameterKeys.NewObject("binaryBytecode"), typeof(EffectSourceCodeKeys), "EffectSourceCode.FieldName");

        [ModuleInitializer]
        internal static void __Initialize__()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(EffectSourceCodeKeys).TypeHandle);
        }
    }
}
