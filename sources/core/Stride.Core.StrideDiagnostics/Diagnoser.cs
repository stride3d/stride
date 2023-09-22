using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Stride.Core.StrideDiagnostics.PropertyFinders;

namespace Stride.Core.StrideDiagnostics;

internal class Diagnoser
{
    private static List<Type> violationReporterTypes = typeof(Diagnoser).Assembly.GetTypes()
        .Where(type => typeof(IViolationReporter).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
        .ToList();
    internal void StartCreation(ClassInfo info)
    {
        var allProperties = info.Symbol.GetMembers().OfType<IPropertySymbol>().Cast<ISymbol>();
        // Instantiate each reporter and call ReportViolations
        foreach (var reporterType in violationReporterTypes)
        {
            var reporter = (IViolationReporter)Activator.CreateInstance(reporterType);
            reporter.ClassInfo = info;
            foreach (var property in allProperties)
            {
                reporter.ReportViolation(property, info);
            }
        }
    }
}
