using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;

namespace Stride.Launcher.Tests.Helpers;

internal sealed class FakeDialogService : IDialogService
{
    private int nextMultiButtonResult;

    public bool WasCalled { get; private set; }

    public void SetNextResult(int result) => nextMultiButtonResult = result;

    public Task<int> MessageBoxAsync(string message, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
    {
        WasCalled = true;
        return Task.FromResult(nextMultiButtonResult);
    }

    public Task<MessageBoxResult> MessageBoxAsync(string message, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
        => Task.FromResult(MessageBoxResult.OK);

    public Task<CheckedMessageBoxResult> CheckedMessageBoxAsync(string message, bool? isChecked, string checkboxMessage, MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage image = MessageBoxImage.None)
        => throw new NotImplementedException();

    public Task<CheckedMessageBoxResult> CheckedMessageBoxAsync(string message, bool? isChecked, string checkboxMessage, IReadOnlyCollection<DialogButtonInfo> buttons, MessageBoxImage image = MessageBoxImage.None)
        => throw new NotImplementedException();

    public Task<UFile?> OpenFilePickerAsync(UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<UFile>> OpenMultipleFilesPickerAsync(UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null)
        => throw new NotImplementedException();

    public Task<UDirectory?> OpenFolderPickerAsync(UDirectory? initialPath = null)
        => throw new NotImplementedException();

    public Task<UFile?> SaveFilePickerAsync(UDirectory? initialPath = null, IReadOnlyList<FilePickerFilter>? filters = null, string? defaultExtension = null, string? defaultFileName = null)
        => throw new NotImplementedException();

    public void Exit(int exitCode = 0) => throw new NotImplementedException();

    public bool HasMainWindow => false;
}
