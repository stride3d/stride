using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Stride.Core.CompilerServices.Diagnostics;

namespace Stride.Core.CompilerServices
{
    [Generator]
    public partial class SerializerGenerator : ISourceGenerator
    {
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
            try
            {
                var customContext = new GeneratorContext(context);
                var spec = GenerateSpec(customContext);

                var validator = new Validator(customContext);
                validator.Validate(spec);

                EmitCode(context, spec);
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    CompilerServicesUnhandledException,
                    Location.None,
                    ex.GetType().Name,
                    ex.ToString()));
            }
        }
    }

    // TODO: separate this into more classes such that they're more independent
}
