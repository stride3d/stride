using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Stride.Core.CompilerServices
{
    [Generator]
    public partial class SerializerGenerator : ISourceGenerator
    {
        private static ITypeSymbol systemInt32Symbol;
        private static ITypeSymbol systemObjectSymbol;
        public void Initialize(GeneratorInitializationContext context)
        {
#if LAUNCH_DEBUGGER
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Launch();
            }
#endif
        }

        public void Execute(GeneratorExecutionContext context)
        {
            InitStaticSymbols(context);
            var spec = GenerateSpec(context);

            // TODO validate every type referenced by a member is serializable
            var validator = new Validator();
            validator.Validate(context, ref spec);

            EmitCode(context, spec);
        }

        private static void InitStaticSymbols(GeneratorExecutionContext context)
        {
            systemInt32Symbol ??= context.Compilation.GetSpecialType(SpecialType.System_Int32);
            systemObjectSymbol ??= context.Compilation.GetSpecialType(SpecialType.System_Object);
        }
    }
}
