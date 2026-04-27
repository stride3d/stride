using Stride.Core.IO;
using Stride.Launcher.Services;

namespace Stride.Launcher.Tests.Helpers;

internal sealed class InMemoryLauncherSettings : ILauncherSettingsService
{
    private readonly List<string> completedTasks = [];

    public bool CloseLauncherAutomatically { get; set; }
    public string ActiveVersion { get; set; } = "";
    public string PreferredFramework { get; set; } = "net10.0";
    public int CurrentTab { get; set; }
    public IReadOnlyCollection<UDirectory> DeveloperVersions { get; init; } = [];

    public int SaveCallCount { get; private set; }

    public bool IsTaskCompleted(string taskName) => completedTasks.Contains(taskName);

    public void MarkTaskCompleted(string taskName)
    {
        if (!completedTasks.Contains(taskName))
        {
            completedTasks.Add(taskName);
            SaveCallCount++;
        }
    }

    public void Save() => SaveCallCount++;
}
