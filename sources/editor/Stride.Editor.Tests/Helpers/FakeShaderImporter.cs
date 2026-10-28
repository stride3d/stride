// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.BuildEngine;
using Stride.Editor.Build;

namespace Stride.Editor.Tests.Helpers;

/// <summary>
/// An importer whose import is a single step that does nothing, in place of the Stride shader importer.
/// </summary>
public sealed class FakeShaderImporter : IShaderImporter
{
    /// <summary>
    /// The sessions the importer was asked to import the shaders of.
    /// </summary>
    public List<SessionViewModel> Sessions { get; } = [];

    public ImportStep Step { get; } = new();

    public ListBuildStep? CreateSystemShaderBuildSteps(SessionViewModel session)
    {
        Sessions.Add(session);
        var steps = new ListBuildStep();
        steps.Add(Step);
        return steps;
    }

    public sealed class ImportStep : BuildStep
    {
        public override string Title => "Import";

        public bool Executed { get; private set; }

        public override Task<ResultStatus> Execute(IExecuteContext executeContext, BuilderContext builderContext)
        {
            Executed = true;
            return Task.FromResult(ResultStatus.Successful);
        }

        public override string ToString() => Title;
    }
}
