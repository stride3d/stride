using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Stride.Editor.CrashReport
{
    public static class CrashReporter
    {
        ///Todo: We could send report as issue to Github repo
        public static Task Report(CrashReportData data)
        {
            return Task.CompletedTask;
        }
    }
}