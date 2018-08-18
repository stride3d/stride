// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable SA1402 // File may only contain a single class
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Xenko.Core.Annotations;

namespace Xenko.Core.Diagnostics
{
    public class PerformanceReport
    {
        public struct PerformanceReportInfo
        {
            public string Text { get; set; }
            public long Milliseconds { get; set; }
            public long Ticks { get; set; }
        }

        public IEnumerable<PerformanceReportInfo> Measures { get; private set; }

        private readonly List<PerformanceReportInfo> measures = new List<PerformanceReportInfo>();

        private readonly Stopwatch stopwatch = new Stopwatch();
        private string currentMeasureText;

        public PerformanceReport()
        {
            Measures = new ReadOnlyCollection<PerformanceReportInfo>(measures);
        }

        [Conditional("DEBUG")]
        public void BeginMeasure(string text)
        {
            if (currentMeasureText != null)
                EndMeasure();

            currentMeasureText = text;
            stopwatch.Reset();
            stopwatch.Start();
        }

        [Conditional("DEBUG")]
        public void EndMeasure()
        {
            stopwatch.Stop();

            var ticks = stopwatch.ElapsedTicks;
            var ms = stopwatch.ElapsedMilliseconds;

            measures.Add(new PerformanceReportInfo { Text = currentMeasureText, Milliseconds = ms, Ticks = ticks });
            currentMeasureText = null;
        }

        public void Reset()
        {
            measures.Clear();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            var totalTicks = measures.Sum(info => info.Ticks);

            foreach (var info in measures)
            {
                sb.AppendLine($"{info.Text}: {info.Milliseconds} ms, {info.Ticks} ticks ({(double)info.Ticks * 100.0 / (double)totalTicks:F2}%)");
            }

            return sb.ToString();
        }
    }

    public class PerformanceCheckBlock : IDisposable
    {
        private readonly PerformanceReport report;

        public PerformanceCheckBlock([NotNull] string text, [NotNull] PerformanceReport report)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Invalid 'text' argument");
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            this.report = report;
            this.report.BeginMeasure(text);
        }

        public void Dispose()
        {
            report.EndMeasure();
        }
    }
}
