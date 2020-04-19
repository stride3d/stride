// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;

using Stride.Core.Shaders.Ast.Stride;
using Stride.Shaders.Parser.Utility;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;
using Stride.Core.Shaders.Utility;

namespace Stride.Shaders.Parser.Mixins
{
    internal class MixinVirtualTable : ShaderVirtualTable
    {
        #region Public properties

        /// <summary>
        /// List of all declared methods
        /// </summary>
        public HashSet<MethodDeclarationShaderCouple> Methods { get; private set; }

        /// <summary>
        /// List of all declared Variables
        /// </summary>
        public HashSet<VariableShaderCouple> Variables { get; private set; }

        /// <summary>
        /// List of all the structure definitions
        /// </summary>
        public List<StructType> StructureTypes { get; private set; } // list instead of hashset because order can be important

        /// <summary>
        /// List of all the Typedefs
        /// </summary>
        public List<Typedef> Typedefs { get; private set; } // list instead of hashset because order can be important

        #endregion

        #region Constructor

        public MixinVirtualTable()
        {
            Methods = new HashSet<MethodDeclarationShaderCouple>();
            Variables = new HashSet<VariableShaderCouple>();
            StructureTypes = new List<StructType>();
            Typedefs = new List<Typedef>();
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Merge with a local virtual table =  need to check override keywords
        /// </summary>
        /// <param name="virtualTable">the virtual table to add</param>
        /// <param name="mixinName">the name of the mixin</param>
        /// <param name="log">the error logger</param>
        public void MergeWithLocalVirtualTable(MixinVirtualTable virtualTable, string mixinName, LoggerResult log)
        {
            foreach (var method in virtualTable.Methods)
            {
                var methodDecl = Methods.LastOrDefault(x => x.Method.IsSameSignature(method.Method));
                if (methodDecl != null)
                {
                    var isBaseMethod = method.Shader.BaseClasses.Any(x => x.Name.Text == methodDecl.Shader.Name.Text);

                    if (isBaseMethod)
                    {
                        if (methodDecl.Method is MethodDefinition)
                        {
                            if (!method.Method.Qualifiers.Contains(StrideStorageQualifier.Override))
                            {
                                log.Error(StrideMessageCode.ErrorMissingOverride, method.Method.Span, method.Method, mixinName);
                                continue;
                            }
                        }
                        else if (method.Method.Qualifiers.Contains(StrideStorageQualifier.Override))
                        {
                            log.Error(StrideMessageCode.ErrorOverrideDeclaration, method.Method.Span, method.Method, mixinName);
                            continue;
                        }
                    }

                    Methods.Remove(methodDecl);
                }
                else
                {
                    if (method.Method.Qualifiers.Contains(StrideStorageQualifier.Override))
                    {
                        log.Error(StrideMessageCode.ErrorNoMethodToOverride, method.Method.Span, method.Method, mixinName);
                        continue;
                    }
                }

                Methods.Add(method);
                
                // TODO: handle declarations vs definitions
            }

            Variables.UnionWith(virtualTable.Variables.Where(x => !Variables.Contains(x)));
            StructureTypes.AddRange(virtualTable.StructureTypes.Where(x => !StructureTypes.Contains(x)));
            Typedefs.AddRange(virtualTable.Typedefs.Where(x => !Typedefs.Contains(x)));
        }

        /// <summary>
        /// Check the name conflict between the two virtual tables
        /// </summary>
        public bool CheckNameConflict(MixinVirtualTable virtualTable, LoggerResult log)
        {
            var conflict = false;

            // Note: we allow conflicts for static variables
            foreach (var variable in virtualTable.Variables.Where(variable => !variable.Variable.Qualifiers.Contains(Stride.Core.Shaders.Ast.Hlsl.StorageQualifier.Static) && Variables.Any(x => x.Variable.Name.Text == variable.Variable.Name.Text)))
            {
                log.Error(StrideMessageCode.ErrorVariableNameConflict, variable.Variable.Span, variable.Variable, "");
                conflict = true;
            }

            return conflict;
        }

        #endregion
    }
}
