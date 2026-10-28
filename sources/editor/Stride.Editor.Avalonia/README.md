# Stride.Editor.Avalonia

This project is similar to `Stride.Editor` but with Avalonia-specific implementations: the preview service that runs the
preview game offscreen (`GameStudioPreviewService`), the viewport that shows its frames (`Engine/`: `OffscreenGameView`,
composited through the GPU interop of Avalonia's compositor by `GpuFramePresenter`, or read back by
`OffscreenFrameExporter`; input replayed by `AvaloniaInputBridge`), the default preview view (`StridePreviewView`, its control theme in `Themes/Generic.axaml`, which the application includes in its
resources) and the `ImageToBitmap` converter that shows the `Stride.Graphics.Image` thumbnails of `Stride.Editor`.

`IAvaloniaPreviewBuilder` is the Avalonia counterpart of `IWpfPreviewBuilder`: the toolkit-neutral `IPreviewBuilder` of
`Stride.Editor.Common` does not expose the hosting control.

## Notes

* It is Stride specific, but it can be used by other applications beside the GameStudio, as long as they use Avalonia.
* Like `Stride.Editor.Wpf`, this assembly is part of the editor host and declares no `StridePackageKind` or
  `StrideEditorToolkit`.
* `--viewport-readback` on the command line of the Game Studio turns the GPU interop off: the frames are read back
  through the CPU, for support when a driver misbehaves.
