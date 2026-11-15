// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.CompilerServices.Analyzers;
using Xunit;

namespace Stride.Core.CompilerServices.Tests.AnalyzerTests;

public class STRDIAG018_Test
{
    // A Dispatcher with the shape the analyzer matches, and a caller whose call is filled in by each test
    private const string Source = """
        namespace Stride.Core.Threading
        {{
            public static class Dispatcher
            {{
                public static void For<TState>(int fromInclusive, int toExclusive, TState state, System.Action<TState, int> action) {{ }}
                [System.Obsolete]
                public static void For(int fromInclusive, int toExclusive, System.Action<int> action) {{ }}
            }}
        }}
        namespace Game
        {{
            public class Caller
            {{
                private int value;
                private static void Process(Caller self, int index) {{ }}
                private void ProcessInstance(Caller self, int index) {{ }}

                public void Run(System.Action<Caller, int> callback)
                {{
                    #pragma warning disable CS0612
                    {0};
                    #pragma warning restore CS0612
                }}
            }}
        }}
        """;

    [Theory]
    [InlineData("Stride.Core.Threading.Dispatcher.For(0, 10, this, static (self, i) => self.value++)")]
    [InlineData("Stride.Core.Threading.Dispatcher.For(0, 10, this, Process)")]
    [InlineData("Stride.Core.Threading.Dispatcher.For(0, 10, this, callback)")]
    [InlineData("Stride.Core.Threading.Dispatcher.For(0, 10, i => value++)")]
    public async Task No_Warning(string call)
    {
        await TestHelper.ExpectNoDiagnosticsAsync(string.Format(Source, call));
    }

    [Theory]
    [InlineData("Stride.Core.Threading.Dispatcher.For(0, 10, this, (self, i) => value++)")]
    [InlineData("Stride.Core.Threading.Dispatcher.For(0, 10, this, (self, i) => self.value++)")]
    [InlineData("Stride.Core.Threading.Dispatcher.For(0, 10, this, ProcessInstance)")]
    public async Task Warning(string call)
    {
        await TestHelper.ExpectDiagnosticAsync(string.Format(Source, call), STRDIAG018NonStaticDispatcherCallback.DiagnosticId);
    }
}
