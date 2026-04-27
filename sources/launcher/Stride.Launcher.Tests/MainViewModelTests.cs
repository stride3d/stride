using Stride.Launcher.Tests.Helpers;
using Xunit;

namespace Stride.Launcher.Tests;

public sealed class MainViewModelTests
{
    [Fact]
    public void HasDoneTask_ReturnsFalse_WhenTaskNotRecorded()
    {
        var (vm, _, _) = TestViewModelFactory.CreateMainViewModel();
        Assert.False(vm.HasDoneTask("SomeTask"));
    }

    [Fact]
    public void HasDoneTask_ReturnsTrue_AfterSaveTaskAsDone()
    {
        var (vm, _, _) = TestViewModelFactory.CreateMainViewModel();
        vm.SaveTaskAsDone("SomeTask");
        Assert.True(vm.HasDoneTask("SomeTask"));
    }

    [Fact]
    public void SaveTaskAsDone_IsIdempotent()
    {
        var (vm, settings, _) = TestViewModelFactory.CreateMainViewModel();
        vm.SaveTaskAsDone("SomeTask");
        vm.SaveTaskAsDone("SomeTask");
        Assert.Equal(1, settings.SaveCallCount);
    }

    [Fact]
    public void CurrentTab_Setter_PersistsValueAndSaves()
    {
        var (vm, settings, _) = TestViewModelFactory.CreateMainViewModel();
        var savesBefore = settings.SaveCallCount;

        vm.CurrentTab = 1;

        Assert.Equal(1, settings.CurrentTab);
        Assert.Equal(savesBefore + 1, settings.SaveCallCount);
    }

    [Fact]
    public void CurrentTab_Setter_DoesNotSave_WhenValueUnchanged()
    {
        var (vm, settings, _) = TestViewModelFactory.CreateMainViewModel();
        vm.CurrentTab = 0; // default is already 0; SetValue returns false → no save
        var savesBefore = settings.SaveCallCount;

        vm.CurrentTab = 0;

        Assert.Equal(savesBefore, settings.SaveCallCount);
    }

    [Fact]
    public async Task TryCloseAsync_ReturnsTrue_AndPersists_WhenNoVersionIsProcessing()
    {
        var (vm, settings, dialog) = TestViewModelFactory.CreateMainViewModel();
        var savesBefore = settings.SaveCallCount;

        var result = await vm.TryCloseAsync();

        Assert.True(result);
        Assert.False(dialog.WasCalled);
        Assert.Equal(savesBefore + 1, settings.SaveCallCount);
    }
}
