using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.StrideDiagnostics.PropertyFinders;

namespace Stride.Core.StrideDiagnostics;

internal class Diagnoser
{
    private static List<Type> violationReporterTypes = typeof(Diagnoser).Assembly.GetTypes()
        .Where(type => typeof(IViolationReporter).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
        .ToList();
    internal void StartCreation(ClassInfo info)
    {
        DiagnoseDataMember(info);
    }
    private void DiagnoseDataMember(ClassInfo info)
    {
        // Instantiate each reporter and call ReportViolations
        foreach (var reporterType in violationReporterTypes)
        {
            var reporter = (IViolationReporter)Activator.CreateInstance(reporterType);
            var symbol = info.Symbol;
            reporter.ReportViolations(ref symbol, info);
        }
    }
}
