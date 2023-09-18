using Microsoft.CodeAnalysis;
using Stride.Core.StrideDiagnostics.PropertyFinders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Stride.Core.StrideDiagnostics;

internal class Diagnoser
{
    internal void StartCreation(ClassInfo info)
    {
        DiagnoseDataMember(info);
    }
    private void DiagnoseDataMember(ClassInfo info)
    {
        var reporterTypes = typeof(Diagnoser).Assembly.GetTypes()
            .Where(type => typeof(IViolationReporter).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);

        // Instantiate each reporter and call ReportViolations
        foreach (var reporterType in reporterTypes)
        {
            var reporter = (IViolationReporter)Activator.CreateInstance(reporterType);
            var symbol = info.Symbol;
            reporter.ReportViolations(ref symbol, info);
        }
    }
}