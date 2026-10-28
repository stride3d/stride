// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Templates;
using Stride.Assets.Templates;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Templates;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Assets.Editor.Tests.Templates;

/// <summary>
/// The game template the project selection window runs, through the agnostic generator: the files it writes in a temporary
/// folder, without loading the game in a session.
/// </summary>
public sealed class NewGameGenerationTests
{
    [SkippableFact]
    public void TheNewGameTemplateWritesTheGameProjects()
    {
        // The dotnet new templates of the installed Stride.Templates.Games package, as the editor registers them at startup
        DotNetNewTemplateBridge.RegisterProjectTemplates();
        var template = TemplateManager.FindTemplates(TemplateScope.Session)
            .OfType<TemplateDotNetNewDescription>()
            .FirstOrDefault(x => x.TemplateShortName == "stride-game");
        Skip.If(template is null, "The Stride.Templates.Games package is not installed on this machine: the new game template is not available.");

        var platform = MultiChoiceTemplateParameterViewModel.CurrentHostPlatformChoice();
        var platformName = MultiChoiceTemplateParameterViewModel.ToPlatform(platform)?.ToString();
        var output = new UDirectory(Path.Combine(Path.GetTempPath(), "Stride.Assets.Editor.Tests", Guid.NewGuid().ToString("N")));
        var logger = new LoggerResult();
        var session = new PackageSession { SolutionPath = UPath.Combine<UFile>(output, "UnitTestGame.slnx") };
        var parameters = new SessionTemplateGeneratorParameters
        {
            Session = session,
            Unattended = true,
            Description = template,
            Name = "UnitTestGame",
            Namespace = "UnitTestGame",
            OutputDirectory = output,
            Logger = logger,
        };
        // What the parameters dialog passes: the resolved host platform (the "host" default of the template)
        DotNetNewTemplateGenerator.SetParameters(parameters, new Dictionary<string, string> { ["platforms"] = platform });
        var generator = new DotNetNewTemplateGenerator();
        try
        {
            Assert.True(generator.PrepareForRun(parameters).Result, logger.ToText());
            Assert.True(generator.GenerateFilesOnly(parameters), logger.ToText());
            Assert.False(logger.HasErrors, logger.ToText());

            var gameDirectory = Path.Combine(output, "UnitTestGame.Game");
            Assert.True(File.Exists(Path.Combine(output, "UnitTestGame.slnx")), "The solution was not written.");
            Assert.True(File.Exists(Path.Combine(gameDirectory, "UnitTestGame.Game.csproj")), "The game project was not written.");
            Assert.True(File.Exists(Path.Combine(gameDirectory, "UnitTestGame.Game.sdpkg")), "The package was not written.");
            Assert.True(File.Exists(Path.Combine(gameDirectory, "BasicCameraController.cs")), "The camera script was not written.");
            Assert.True(File.Exists(Path.Combine(gameDirectory, "Assets", "MainScene.sdscene")), "The main scene was not written.");
            Assert.True(File.Exists(Path.Combine(gameDirectory, "Assets", "GameSettings.sdgamesettings")), "The game settings were not written.");
            Assert.Contains("Name: UnitTestGame", File.ReadAllText(Path.Combine(gameDirectory, "UnitTestGame.Game.sdpkg")));

            // The executable project of the host platform (Windows and Linux are named after the platform)
            if (platformName is "Windows" or "Linux")
            {
                var platformProject = Path.Combine(output, $"UnitTestGame.{platformName}", $"UnitTestGame.{platformName}.csproj");
                Assert.True(File.Exists(platformProject), $"The {platformName} project was not written.");
                var solution = File.ReadAllText(Path.Combine(output, "UnitTestGame.slnx"));
                Assert.Contains("UnitTestGame.Game/UnitTestGame.Game.csproj", solution);
                Assert.Contains($"UnitTestGame.{platformName}/UnitTestGame.{platformName}.csproj", solution);
            }
        }
        finally
        {
            try
            {
                Directory.Delete(output, true);
            }
            catch (IOException)
            {
                // Best effort
            }
        }
    }
}
