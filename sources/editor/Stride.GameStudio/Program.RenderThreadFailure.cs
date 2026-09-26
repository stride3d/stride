// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Translation;
using Stride.Editor;
using Stride.GameStudio.ViewModels;

namespace Stride.GameStudio;

public static partial class Program
{
    // UCEERR_RENDERTHREADFAILURE: the WPF render thread failed for good, usually as its GPU device was lost (TDR)
    private const int RenderThreadFailure = unchecked((int)0x88980406);

    /// <summary>
    /// Restarts the studio if the WPF render thread failed; any other exception is left to the crash report.
    /// </summary>
    /// <returns><see langword="true"/> if the exception was a render thread failure (the process then exits).</returns>
    private static bool TryRestartAfterRenderThreadFailure(Exception exception)
    {
        if (exception is not COMException { HResult: RenderThreadFailure } || terminating)
            return false;

        RestartAfterRenderThreadFailure(exception);
        return true;
    }

    /// <summary>
    /// The WPF render thread is gone, so no WPF window can show anymore (dialogs, the save prompt). Not a Stride crash
    /// (the GPU or its driver): the session is saved after a native message box, and the studio restarts on it.
    /// </summary>
    private static void RestartAfterRenderThreadFailure(Exception exception)
    {
        terminating = true;
        try
        {
            GlobalLogger.GetLogger("GameStudio").Error("The WPF render thread failed; Game Studio restarts.", exception);

            var session = GameStudioViewModel.GameStudio?.Session;
            var sessionPath = session?.SessionFilePath?.ToOSPath();

            // The UI thread doesn't pump while the boxes are up (see ShowNativeMessageBox)
            try { DisableProcessWindowsGhosting(); } catch { /* cosmetic */ }

            // Native boxes (user32), not WPF. TODO: SDL for Linux/macOS with the Avalonia port
            // WPF does not say why its render thread failed, but a loss one of our devices saw is almost surely the cause
            var loss = GraphicsDeviceLoss.FirstLoss;
            var message = loss is not null
                ? string.Format(Tr._p("Message", "Game Studio can no longer draw its windows (the graphics device was lost: {0}) and needs to restart."), loss.Status)
                : Tr._p("Message", "Game Studio can no longer draw its windows (the graphics device was probably lost) and needs to restart.");
            if (session != null && session.HasUnsavedAssets())
            {
                if (ShowNativeMessageBox(message + "\n\n" + Tr._p("Message", "Do you want to save your changes first?"), MB_YESNO | MB_ICONWARNING) == IDYES)
                {
                    var result = session.SaveSessionWithoutUI();
                    if (result.HasErrors)
                        ShowNativeMessageBox(string.Format(Tr._p("Message", "The session could not be saved:\n\n{0}"), result.ToText()), MB_ICONERROR);
                }
            }
            else
            {
                ShowNativeMessageBox(message, MB_ICONWARNING);
            }

            Restart(sessionPath);
        }
        catch (Exception e)
        {
            e.Ignore();
        }
        Environment.Exit(0);
    }

    private const uint MB_YESNO = 0x4;
    private const uint MB_ICONERROR = 0x10;
    private const uint MB_ICONWARNING = 0x30;
    private const uint MB_SETFOREGROUND = 0x10000;
    private const uint MB_TOPMOST = 0x40000;
    private const int IDYES = 6;

    /// <summary>
    /// Shows a message box on its own thread while the UI thread waits without pumping. A box on the UI thread would
    /// run the queued WPF work, such as the device loss dialog: that modal window never paints, and disables the box.
    /// </summary>
    private static int ShowNativeMessageBox(string text, uint type)
    {
        var result = 0;
        var thread = new Thread(() => result = MessageBoxW(IntPtr.Zero, text, "Stride", type | MB_SETFOREGROUND | MB_TOPMOST));
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        return result;
    }
}
