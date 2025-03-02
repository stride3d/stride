// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Core.Mathematics;
using Point = Avalonia.Point;

namespace Stride.Core.Presentation.Avalonia.Controls;

/// <summary>
/// A <see cref="Control"/> that can host a game engine window. This control is faster than <see cref="HwndHost"/> but might behave
/// a bit less nicely on certain cases (such as resize, etc.).
/// </summary>
public class GameEngineHost : Control, IDisposable /*, IWin32Window, IKeyboardInputSink*/
{
    // FIXME xplat-editor
    //private readonly List<HwndSource> contextMenuSources = new List<HwndSource>();
    private bool updateRequested;
    private int mouseMoveCount;
    private Point contextMenuPosition;
    private Vector dpiScale;
    private Int4 lastBoundingBox;
    private bool attached;
    private bool isDisposed;

    static GameEngineHost()
    {
        FocusableProperty.OverrideMetadata(typeof(GameEngineHost), new StyledPropertyMetadata<bool>(true));
        IsVisibleProperty.Changed.AddClassHandler<GameEngineHost>(OnIsVisibleChanged);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameEngineHost"/> class.
    /// </summary>
    /// <param name="childHandle">The hwnd of the child (hosted) window.</param>
    public GameEngineHost(IntPtr childHandle)
    {
        Handle = childHandle;
        MinWidth = 32;
        MinHeight = 32;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        LayoutUpdated += OnLayoutUpdated;
    }

    public IntPtr Handle { get; }
    
    // FIXME xplat-editor
    //IKeyboardInputSite IKeyboardInputSink.KeyboardInputSite { get; set; }

    public void Dispose()
    {
        if (isDisposed)
            return;

        Loaded -= OnLoaded;
        Unloaded -= OnUnloaded;
        LayoutUpdated -= OnLayoutUpdated;
        // TODO: This seems to be blocking when exiting the Game Studio, but doesn't seem to be necessary
        //NativeHelper.SetParent(Handle, IntPtr.Zero);        
        // FIXME xplat-editor
        //NativeHelper.DestroyWindow(Handle);
        isDisposed = true;
    }
    
    // FIXME xplat-editor
    ///// <inheritdoc />
    //protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    //{
    //    dpiScale = newDpi;
    //    UpdateWindowPosition();
    //}

    private void OnLoaded(object? sender, RoutedEventArgs routedEventArgs)
    {
        Attach();
        UpdateWindowPosition();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        Detach();
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        // Remark: this callback is invoked a lot. It is critical to do minimum work if no update is needed.
        UpdateWindowPosition();
    }

    private static void OnIsVisibleChanged(GameEngineHost @this, AvaloniaPropertyChangedEventArgs e)
    {
        var newValue = (bool)e.NewValue;
        if (newValue)
        {
            @this.Attach();
            @this.UpdateWindowPosition();
        }
        else
        {
            @this.Detach();
        }
    }

    private void Attach()
    {
        if (attached)
            return;

        //var hwndSource = GetHwndSource();
        //if (hwndSource == null)
        //    return;

        //var hwndParent = hwndSource.Handle;
        //if (hwndParent == IntPtr.Zero)
        //    return;

        //// Get current DPI
        //dpiScale = VisualTreeHelper.GetDpi(this);

        //var style = NativeHelper.GetWindowLong(Handle, NativeHelper.GWL_STYLE);
        //// Removes Caption bar and the sizing border
        //// Must be a child window to be hosted
        //style |= NativeHelper.WS_CHILD;

        //NativeHelper.SetWindowLong(Handle, NativeHelper.GWL_STYLE, style);
        //NativeHelper.ShowWindow(Handle, NativeHelper.SW_HIDE);

        //// Update the parent to be the parent of the host
        //NativeHelper.SetParent(Handle, hwndParent);

        //// Register keyboard sink to make shortcuts work
        //((IKeyboardInputSink)this).KeyboardInputSite = ((IKeyboardInputSink)hwndSource).RegisterKeyboardInputSink(this);
        attached = true;
    }

    private void Detach()
    {
        if (!attached)
            return;

        //// Hide window, clear parent
        //NativeHelper.ShowWindow(Handle, NativeHelper.SW_HIDE);
        //NativeHelper.SetParent(Handle, IntPtr.Zero);

        //// Unregister keyboard sink
        //var site = ((IKeyboardInputSink)this).KeyboardInputSite;
        //((IKeyboardInputSink)this).KeyboardInputSite = null;
        //site?.Unregister();

        // Make sure we will actually attach next time Attach() is called
        lastBoundingBox = Int4.Zero;
        attached = false;
    }

    private void UpdateWindowPosition()
    {
        if (updateRequested || !attached)
            return;

        updateRequested = true;

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            updateRequested = false;
            Visual? root = null;
            var shouldShow = true;
            var parent = this.GetVisualParent();
            while (parent != null)
            {
                root = parent;

                if (parent is Control parentElement)
                {
                    if (!parentElement.IsLoaded || !parentElement.IsVisible)
                        shouldShow = false;
                }

                parent = root.GetVisualParent();
            }

            if (root == null)
                return;
            
            // FIXME xplat-editor
            //// Find proper position for the game
            //var positionTransform = TransformToAncestor(root);
            //var areaPosition = positionTransform.Transform(new Point(0, 0));
            //var boundingBox = new Int4((int)(areaPosition.X * dpiScale.DpiScaleX), (int)(areaPosition.Y * dpiScale.DpiScaleY), (int)(ActualWidth * dpiScale.DpiScaleX), (int)(ActualHeight * dpiScale.DpiScaleY));
            //if (boundingBox != lastBoundingBox)
            //{
            //    lastBoundingBox = boundingBox;
            //    // Move the window asynchronously, without activating it, without touching the Z order
            //    // TODO: do we want SWP_NOCOPYBITS?
            //    const int flags = NativeHelper.SWP_ASYNCWINDOWPOS | NativeHelper.SWP_NOACTIVATE | NativeHelper.SWP_NOZORDER;
            //    NativeHelper.SetWindowPos(Handle, NativeHelper.HWND_TOP, boundingBox.X, boundingBox.Y, boundingBox.Z, boundingBox.W, flags);
            //}

            if (attached)
            {
                // FIXME xplat-editor
                //NativeHelper.ShowWindow(Handle, shouldShow ? NativeHelper.SW_SHOWNOACTIVATE : NativeHelper.SW_HIDE);
            }
        }, DispatcherPriority.Input); // This code must be dispatched after the DispatcherPriority.Loaded to properly work since it's checking the IsLoaded flag!
    }

    /// <summary>
    /// Forwards a message that comes from the hosted window to the WPF window. This method can be used for example to forward keyboard events.
    /// </summary>
    /// <param name="hwnd">The hwnd of the hosted window.</param>
    /// <param name="msg">The message identifier.</param>
    /// <param name="wParam">The word parameter of the message.</param>
    /// <param name="lParam">The long parameter of the message.</param>
    public void ForwardMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
    {
        DispatcherOperation task;
        // FIXME xplat-editor
        //switch (msg)
        //{
        //    case NativeHelper.WM_RBUTTONDOWN:
        //        mouseMoveCount = 0;
        //        task = Dispatcher.InvokeAsync(() =>
        //        {
        //            RaiseMouseButtonEvent(Mouse.PreviewMouseDownEvent, MouseButton.Right);
        //            RaiseMouseButtonEvent(Mouse.MouseDownEvent, MouseButton.Right);
        //        });
        //        task.Wait(TimeSpan.FromSeconds(1.0f));
        //        break;
        //    case NativeHelper.WM_RBUTTONUP:
        //        task = Dispatcher.InvokeAsync(() =>
        //        {
        //            RaiseMouseButtonEvent(Mouse.PreviewMouseUpEvent, MouseButton.Right);
        //            RaiseMouseButtonEvent(Mouse.MouseUpEvent, MouseButton.Right);
        //        });
        //        task.Wait(TimeSpan.FromSeconds(1.0f));
        //        break;
        //    case NativeHelper.WM_LBUTTONDOWN:
        //        task = Dispatcher.InvokeAsync(() =>
        //        {
        //            RaiseMouseButtonEvent(Mouse.PreviewMouseDownEvent, MouseButton.Left);
        //            RaiseMouseButtonEvent(Mouse.MouseDownEvent, MouseButton.Left);
        //        });
        //        task.Wait(TimeSpan.FromSeconds(1.0f));
        //        break;
        //    case NativeHelper.WM_LBUTTONUP:
        //        task = Dispatcher.InvokeAsync(() =>
        //        {
        //            RaiseMouseButtonEvent(Mouse.PreviewMouseUpEvent, MouseButton.Left);
        //            RaiseMouseButtonEvent(Mouse.MouseUpEvent, MouseButton.Left);
        //        });
        //        task.Wait(TimeSpan.FromSeconds(1.0f));
        //        break;
        //    case NativeHelper.WM_MOUSEMOVE:
        //        ++mouseMoveCount;
        //        break;
        //    case NativeHelper.WM_CONTEXTMENU:
        //        // TODO: Tracking drag offset would be better, but might be difficult since we replace the mouse to its initial position each time it is moved.
        //        if (mouseMoveCount < 3)
        //        {
        //            Dispatcher.InvokeAsync(() =>
        //            {
        //                Visual? dependencyObject = this;
        //                while (dependencyObject != null)
        //                {
        //                    var element = dependencyObject as Control;
        //                    if (element?.ContextMenu != null)
        //                    {
        //                        element.Focus();
        //                        // Data context will not be properly set if the popup is open this way, so let's set it ourselves
        //                        element.ContextMenu.SetCurrentValue(DataContextProperty, element.DataContext);
        //                        element.ContextMenu.IsOpen = true;
        //                        var source = (HwndSource)PresentationSource.FromVisual(element.ContextMenu);
        //                        if (source != null)
        //                        {
        //                            source.AddHook(ContextMenuWndProc);
        //                            contextMenuPosition = Mouse.GetPosition(this);
        //                            lock (contextMenuSources)
        //                            {
        //                                contextMenuSources.Add(source);
        //                            }
        //                        }
        //                        break;
        //                    }
        //                    dependencyObject = dependencyObject.GetVisualParent();
        //                }
        //            });
        //        }
        //        break;
        //    default:
        //        var parent = NativeHelper.GetParent(hwnd);
        //        NativeHelper.PostMessage(parent, msg, wParam, lParam);
        //        break;
        //}
    }

    //private void RaiseMouseButtonEvent(RoutedEvent routedEvent, MouseButton button)
    //{
    //    RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, button)
    //    {
    //        RoutedEvent = routedEvent,
    //        Source = this,
    //    });
    //}

    //private IntPtr ContextMenuWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    //{
    //    switch (msg)
    //    {
    //        case NativeHelper.WM_LBUTTONDOWN:
    //        case NativeHelper.WM_RBUTTONDOWN:
    //            // We need to change from the context menu coordinates to the HwndHost coordinates and re-encode lParam
    //            var position = new Point(-(short)(lParam.ToInt64() & 0xFFFF), -((lParam.ToInt64() & 0xFFFF0000) >> 16));
    //            var offset = contextMenuPosition - position;
    //            lParam = new IntPtr((short)offset.X + ((short)offset.Y << 16));
    //            var threadId = NativeHelper.GetWindowThreadProcessId(Handle, IntPtr.Zero);
    //            NativeHelper.PostThreadMessage(threadId, msg, wParam, lParam);
    //            break;
    //        case NativeHelper.WM_DESTROY:
    //            lock (contextMenuSources)
    //            {
    //                var source = contextMenuSources.First(x => x.Handle == hwnd);
    //                source.RemoveHook(ContextMenuWndProc);
    //            }
    //            break;
    //    }
    //    return IntPtr.Zero;
    //}
    
    //[CanBeNull]
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //private HwndSource GetHwndSource()
    //{
    //    return (HwndSource)PresentationSource.FromVisual(this);
    //}
    
    // FIXME xplat-editor
    //IKeyboardInputSite IKeyboardInputSink.RegisterKeyboardInputSink(IKeyboardInputSink sink)
    //{
    //    throw new NotSupportedException();
    //}

    //bool IKeyboardInputSink.TranslateAccelerator(ref MSG msg, ModifierKeys modifiers)
    //{
    //    return false;
    //}

    //bool IKeyboardInputSink.TabInto(TraversalRequest request)
    //{
    //    return false;
    //}

    //bool IKeyboardInputSink.OnMnemonic(ref MSG msg, ModifierKeys modifiers)
    //{
    //    return false;
    //}

    //bool IKeyboardInputSink.TranslateChar(ref MSG msg, ModifierKeys modifiers)
    //{
    //    return false;
    //}

    //bool IKeyboardInputSink.HasFocusWithin()
    //{
    //    var focus = NativeHelper.GetFocus();
    //    return Handle != IntPtr.Zero && (focus == Handle || NativeHelper.IsChild(Handle, focus));
    //}
}
