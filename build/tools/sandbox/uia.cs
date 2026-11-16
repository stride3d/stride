// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#:property TargetFramework=net10.0-windows
#:property UseWPF=true
#:property UseWindowsForms=true
#:property PublishAot=false

// UI driver for the Windows Sandbox kit (see README.md): lists windows, reads and acts on UI Automation elements, sends
// mouse and keyboard input, and takes pictures of the screen.
//
// A window is "hwnd:0x...", or a part of its title. An element is a part of its Name or AutomationId ("=x" for an exact
// match), optionally filtered by control type (Button, ListItem, TabItem, ...). Exit code 2 means "not found".

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;
using System.Windows.Forms;

static class Program
{
    const string Usage = """
        uia windows                                  visible top-level windows: hwnd, process, rect, title
        uia tree <window> [depth=4]                  UI Automation tree of a window
        uia find <window> <element> [type]           elements that match
        uia invoke <window> <element> [type] [n]     invoke, select, toggle or expand the n-th match (else click it)
        uia clickel <window> <element> [type] [left|right|double] [n]
        uia setvalue <window> <element> <type> <value>
        uia text <window> <element> [type]           text (TextPattern, ValuePattern or Name) of each match
        uia click <x> <y> [left|right|double]        screen pixels
        uia drag <x1> <y1> <x2> <y2> [ctrl] [right]  drag in steps, holding Ctrl or with the right button when asked
        uia keys <text>                              types text
        uia key <chord>                              ctrl+s, ctrl+shift+d, f5, esc, del, enter, ...
        uia activate <window>
        uia shot <file.png> [window]                 the whole screen, or one window
        uia shotrect <file.png> <x> <y> <w> <h>
        uia clip                                     clipboard text
        """;

    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc f, IntPtr l);
    delegate bool EnumProc(IntPtr h, IntPtr l);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr h, int c);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] static extern void mouse_event(uint f, int x, int y, uint d, IntPtr e);
    [DllImport("user32.dll")] static extern void keybd_event(byte v, byte s, uint f, IntPtr e);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern short VkKeyScan(char c);
    [StructLayout(LayoutKind.Sequential)] struct RECT { public int L, T, R, B; }

    const uint LeftDown = 2, LeftUp = 4, RightDown = 8, RightUp = 16, KeyUp = 2;
    const byte VkShift = 0x10, VkControl = 0x11;

    static List<IntPtr> TopWindows()
    {
        var all = new List<IntPtr>();
        EnumWindows((h, l) => { if (IsWindowVisible(h)) all.Add(h); return true; }, IntPtr.Zero);
        return all;
    }

    static string Title(IntPtr h)
    {
        var s = new StringBuilder(512);
        GetWindowText(h, s, s.Capacity);
        return s.ToString();
    }

    static string ProcessName(IntPtr h)
    {
        GetWindowThreadProcessId(h, out var pid);
        try { return Process.GetProcessById((int)pid).ProcessName; } catch (ArgumentException) { return "?"; }
    }

    static IntPtr FindWindow(string spec)
    {
        if (spec.StartsWith("hwnd:")) return new IntPtr(Convert.ToInt64(spec[5..], 16));
        foreach (var h in TopWindows())
            if (Title(h).Contains(spec, StringComparison.OrdinalIgnoreCase)) return h;
        throw new NotFoundException("no window: " + spec);
    }

    static bool Matches(AutomationElement e, string what, string? type)
    {
        var c = e.Current;
        if (type != null && !c.ControlType.ProgrammaticName.EndsWith("." + type, StringComparison.OrdinalIgnoreCase)) return false;
        if (what == "*") return true;
        if (what.StartsWith('=')) return c.Name == what[1..] || c.AutomationId == what[1..];
        return (c.Name ?? "").Contains(what, StringComparison.OrdinalIgnoreCase) || (c.AutomationId ?? "").Contains(what, StringComparison.OrdinalIgnoreCase);
    }

    static List<AutomationElement> Search(string window, string what, string? type)
    {
        var found = new List<AutomationElement>();
        foreach (AutomationElement e in AutomationElement.FromHandle(FindWindow(window)).FindAll(TreeScope.Descendants, Condition.TrueCondition))
        {
            try { if (Matches(e, what, type)) found.Add(e); } catch (ElementNotAvailableException) { }
        }
        return found;
    }

    static AutomationElement SearchOne(string window, string what, string? type, int index)
    {
        var found = Search(window, what, type);
        if (found.Count <= index) throw new NotFoundException($"not found: {what} {type} (#{index}, {found.Count} matches)");
        return found[index];
    }

    static string Describe(AutomationElement e)
    {
        var c = e.Current;
        var r = c.BoundingRectangle;
        var name = (c.Name ?? "").Replace('\r', ' ').Replace('\n', ' ');
        if (name.Length > 160) name = name[..160] + "...";
        var rect = r.IsEmpty ? "0,0,0,0" : $"{(int)r.X},{(int)r.Y},{(int)r.Width},{(int)r.Height}";
        return $"{c.ControlType.ProgrammaticName.Replace("ControlType.", "")} name=\"{name}\" id=\"{c.AutomationId}\" rect={rect}{(c.IsEnabled ? "" : " disabled")}";
    }

    static void Tree(AutomationElement e, int depth, int max)
    {
        Console.WriteLine(new string(' ', depth * 2) + Describe(e));
        if (depth >= max) return;
        for (var c = TreeWalker.ControlViewWalker.GetFirstChild(e); c != null; c = TreeWalker.ControlViewWalker.GetNextSibling(c))
            Tree(c, depth + 1, max);
    }

    static void Click(int x, int y, string how)
    {
        if (how is not ("left" or "right" or "double")) throw new ArgumentException("click: left, right or double, not " + how);
        SetCursorPos(x, y);
        Thread.Sleep(80);
        var (down, up) = how == "right" ? (RightDown, RightUp) : (LeftDown, LeftUp);
        mouse_event(down, 0, 0, 0, IntPtr.Zero); Thread.Sleep(30); mouse_event(up, 0, 0, 0, IntPtr.Zero);
        if (how == "double") { Thread.Sleep(60); mouse_event(LeftDown, 0, 0, 0, IntPtr.Zero); Thread.Sleep(30); mouse_event(LeftUp, 0, 0, 0, IntPtr.Zero); }
    }

    static void ClickElement(AutomationElement e, string how)
    {
        var r = e.Current.BoundingRectangle;
        Click((int)(r.X + r.Width / 2), (int)(r.Y + r.Height / 2), how);
    }

    // Moves in steps so that drag thresholds and hover effects see the motion; Ctrl goes down after the button,
    // as the scene gizmos read it when the drag starts.
    static void Drag(int x1, int y1, int x2, int y2, bool ctrl, bool right, int steps = 25)
    {
        var (down, up) = right ? (RightDown, RightUp) : (LeftDown, LeftUp);
        SetCursorPos(x1, y1); Thread.Sleep(150);
        mouse_event(down, 0, 0, 0, IntPtr.Zero); Thread.Sleep(150);
        if (ctrl) { keybd_event(VkControl, 0, 0, IntPtr.Zero); Thread.Sleep(150); }
        for (var i = 1; i <= steps; i++) { SetCursorPos(x1 + (x2 - x1) * i / steps, y1 + (y2 - y1) * i / steps); Thread.Sleep(40); }
        Thread.Sleep(200);
        mouse_event(up, 0, 0, 0, IntPtr.Zero); Thread.Sleep(100);
        if (ctrl) keybd_event(VkControl, 0, KeyUp, IntPtr.Zero);
    }

    static byte VirtualKey(string key)
    {
        switch (key.ToLowerInvariant())
        {
            case "ctrl" or "control": return VkControl;
            case "shift": return VkShift;
            case "alt": return 0x12;
            case "win": return 0x5B;
            case "esc" or "escape": return 0x1B;
            case "enter" or "return": return 0x0D;
            case "tab": return 0x09;
            case "space": return 0x20;
            case "back" or "backspace": return 0x08;
            case "del" or "delete": return 0x2E;
            case "ins" or "insert": return 0x2D;
            case "up": return 0x26;
            case "down": return 0x28;
            case "left": return 0x25;
            case "right": return 0x27;
            case "home": return 0x24;
            case "end": return 0x23;
            case "pageup": return 0x21;
            case "pagedown": return 0x22;
            case "apps" or "menu": return 0x5D;
        }
        if (key.Length > 1 && (key[0] == 'f' || key[0] == 'F') && int.TryParse(key[1..], out var f) && f is >= 1 and <= 24)
            return (byte)(0x70 + f - 1);
        if (key.Length != 1) throw new ArgumentException("unknown key: " + key);
        return (byte)(VkKeyScan(key[0]) & 0xff);
    }

    static void Chord(string chord)
    {
        var keys = chord.Split('+').Select(VirtualKey).ToArray();
        foreach (var k in keys) { keybd_event(k, 0, 0, IntPtr.Zero); Thread.Sleep(20); }
        foreach (var k in keys.Reverse()) { keybd_event(k, 0, KeyUp, IntPtr.Zero); Thread.Sleep(20); }
    }

    static void TypeText(string text)
    {
        foreach (var ch in text)
        {
            var scan = VkKeyScan(ch);
            var shift = (scan & 0x100) != 0;
            var vk = (byte)(scan & 0xff);
            if (shift) keybd_event(VkShift, 0, 0, IntPtr.Zero);
            keybd_event(vk, 0, 0, IntPtr.Zero); keybd_event(vk, 0, KeyUp, IntPtr.Zero);
            if (shift) keybd_event(VkShift, 0, KeyUp, IntPtr.Zero);
            Thread.Sleep(15);
        }
    }

    static void Shot(string path, Rectangle r)
    {
        using var b = new Bitmap(r.Width, r.Height);
        using (var g = Graphics.FromImage(b))
            g.CopyFromScreen(r.Location, Point.Empty, r.Size);
        b.Save(path, ImageFormat.Png);
        Console.WriteLine($"saved {path} {r.Width}x{r.Height}");
    }

    static Rectangle WindowRect(string window)
    {
        GetWindowRect(FindWindow(window), out var r);
        return new Rectangle(r.L, r.T, r.R - r.L, r.B - r.T);
    }

    sealed class NotFoundException(string message) : Exception(message);

    [STAThread]
    static int Main(string[] a)
    {
        SetProcessDPIAware();
        string? Arg(int i) => a.Length > i ? a[i] : null;
        int Index(int i) => a.Length > i ? int.Parse(a[i]) : 0;
        try
        {
            switch (Arg(0))
            {
                case "windows":
                    foreach (var h in TopWindows())
                    {
                        var t = Title(h);
                        if (t.Length == 0) continue;
                        GetWindowRect(h, out var r);
                        Console.WriteLine($"hwnd:0x{h.ToInt64():x} {ProcessName(h)} [{r.L},{r.T},{r.R - r.L},{r.B - r.T}] \"{t}\"");
                    }
                    break;
                case "tree":
                    Tree(AutomationElement.FromHandle(FindWindow(a[1])), 0, a.Length > 2 ? int.Parse(a[2]) : 4);
                    break;
                case "find":
                    foreach (var e in Search(a[1], a[2], Arg(3))) Console.WriteLine(Describe(e));
                    break;
                case "invoke":
                {
                    var e = SearchOne(a[1], a[2], Arg(3), Index(4));
                    if (e.TryGetCurrentPattern(InvokePattern.Pattern, out var p)) ((InvokePattern)p).Invoke();
                    else if (e.TryGetCurrentPattern(SelectionItemPattern.Pattern, out p)) ((SelectionItemPattern)p).Select();
                    else if (e.TryGetCurrentPattern(TogglePattern.Pattern, out p)) ((TogglePattern)p).Toggle();
                    else if (e.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out p)) ((ExpandCollapsePattern)p).Expand();
                    else ClickElement(e, "left");
                    Console.WriteLine("invoked " + Describe(e));
                    break;
                }
                case "clickel":
                {
                    var e = SearchOne(a[1], a[2], Arg(3), Index(5));
                    ClickElement(e, Arg(4) ?? "left");
                    Console.WriteLine("clicked " + Describe(e));
                    break;
                }
                case "setvalue":
                {
                    var e = SearchOne(a[1], a[2], a[3], 0);
                    ((ValuePattern)e.GetCurrentPattern(ValuePattern.Pattern)).SetValue(a[4]);
                    Console.WriteLine("set " + Describe(e));
                    break;
                }
                case "text":
                    foreach (var e in Search(a[1], a[2], Arg(3)))
                    {
                        if (e.TryGetCurrentPattern(TextPattern.Pattern, out var p)) Console.WriteLine(((TextPattern)p).DocumentRange.GetText(-1));
                        else if (e.TryGetCurrentPattern(ValuePattern.Pattern, out p)) Console.WriteLine(((ValuePattern)p).Current.Value);
                        else Console.WriteLine(e.Current.Name);
                    }
                    break;
                case "click":
                    Click(int.Parse(a[1]), int.Parse(a[2]), Arg(3) ?? "left");
                    break;
                case "drag":
                {
                    var flags = a.Skip(5).Select(f => f.ToLowerInvariant()).ToList();
                    Drag(int.Parse(a[1]), int.Parse(a[2]), int.Parse(a[3]), int.Parse(a[4]), flags.Contains("ctrl"), flags.Contains("right"));
                    break;
                }
                case "keys":
                    TypeText(a[1]);
                    break;
                case "key":
                    Chord(a[1]);
                    break;
                case "activate":
                {
                    var h = FindWindow(a[1]);
                    ShowWindow(h, 9 /* SW_RESTORE */);
                    SetForegroundWindow(h);
                    break;
                }
                case "shot":
                    Shot(a[1], a.Length > 2 ? WindowRect(a[2]) : SystemInformation.VirtualScreen);
                    break;
                case "shotrect":
                    Shot(a[1], new Rectangle(int.Parse(a[2]), int.Parse(a[3]), int.Parse(a[4]), int.Parse(a[5])));
                    break;
                case "clip":
                    Console.WriteLine(Clipboard.GetText());
                    break;
                default:
                    Console.WriteLine(Usage);
                    return 1;
            }
            return 0;
        }
        catch (NotFoundException x)
        {
            Console.WriteLine(x.Message);
            return 2;
        }
        catch (Exception x) when (x is ArgumentException or FormatException or IndexOutOfRangeException)
        {
            Console.WriteLine("error: " + x.Message + Environment.NewLine + Usage);
            return 1;
        }
        catch (Exception x)
        {
            Console.WriteLine("error: " + x.Message);
            return 3;
        }
    }
}
