using Microsoft.CodeAnalysis;
using StrideDiagnostics.PropertyFinders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StrideDiagnostics;

internal class Diagnoser
{
    internal void StartCreation(ClassInfo info)
    {
        DiagnoseDataMember(info);
    }
    private void DiagnoseDataMember(ClassInfo info)
    {
        IEnumerable<Type> reporterTypes = typeof(Diagnoser).Assembly.GetTypes()
            .Where(type => typeof(IViolationReporter).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);

        // Instantiate each reporter and call ReportViolations
        foreach (Type reporterType in reporterTypes)
        {
            IViolationReporter reporter = (IViolationReporter)Activator.CreateInstance(reporterType);
            INamedTypeSymbol symbol = info.Symbol;
            reporter.ReportViolations(ref symbol, info);
        }
    }
}