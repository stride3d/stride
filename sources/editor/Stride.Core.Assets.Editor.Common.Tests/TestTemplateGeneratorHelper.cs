// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Common.Tests.Helpers;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Assets.Editor.ViewModel.Progress;
using Stride.Core.Assets.Templates;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.Common.Tests;

public class TestTemplateGeneratorHelper
{
    private readonly FakeSession session = new();
    private readonly PackageTemplateGeneratorParameters parameters = new() { Logger = new LoggerResult() };

    [Fact]
    public async Task CancelledPreparationLogsAndShowsNoProgress()
    {
        var generator = new FakeGenerator(prepare: () => false);

        var result = await TemplateGeneratorHelper.RunTemplateGeneratorSafe(generator, parameters, CreateWorkProgress());

        Assert.False(result);
        Assert.False(generator.HasRun);
        Assert.Contains(parameters.Logger.Messages, x => x.Type == LogMessageType.Info && x.Text == "Operation cancelled.");
        Assert.Empty(session.ProgressWindows.Shown);
    }

    [Fact]
    public async Task FailedPreparationShowsTheProgressWindowAtOnce()
    {
        var generator = new FakeGenerator(prepare: () => throw new InvalidOperationException("Preparation failed"));
        var workProgress = CreateWorkProgress();

        var result = await TemplateGeneratorHelper.RunTemplateGeneratorSafe(generator, parameters, workProgress);

        Assert.False(result);
        Assert.False(generator.HasRun);
        Assert.True(parameters.Logger.HasErrors);
        Assert.Equal([(workProgress, 0)], session.ProgressWindows.Shown);
    }

    [Fact]
    public async Task SuccessfulRunShowsTheProgressWindowAfterADelay()
    {
        var generator = new FakeGenerator(prepare: () => true, run: () => true);
        var workProgress = CreateWorkProgress();

        var result = await TemplateGeneratorHelper.RunTemplateGeneratorSafe(generator, parameters, workProgress);

        Assert.True(result);
        Assert.True(generator.HasRun);
        Assert.Equal([(workProgress, 500)], session.ProgressWindows.Shown);
    }

    [Fact]
    public async Task FailingRunIsLogged()
    {
        var generator = new FakeGenerator(prepare: () => true, run: () => throw new InvalidOperationException("Run failed"));

        var result = await TemplateGeneratorHelper.RunTemplateGeneratorSafe(generator, parameters, CreateWorkProgress());

        Assert.False(result);
        Assert.True(parameters.Logger.HasErrors);
    }

    private WorkProgressViewModel CreateWorkProgress() => new(session.ServiceProvider, parameters.Logger);

    private sealed class FakeGenerator(Func<bool> prepare, Func<bool>? run = null) : ITemplateGenerator<PackageTemplateGeneratorParameters>
    {
        public bool HasRun { get; private set; }

        public bool IsSupportingTemplate(TemplateDescription templateDescription) => true;

        public Task<bool> PrepareForRun(PackageTemplateGeneratorParameters parameters) => Task.FromResult(prepare());

        public bool Run(PackageTemplateGeneratorParameters parameters)
        {
            HasRun = true;
            return run!();
        }
    }
}
