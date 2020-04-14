// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko.Input
{
    /// <summary>
    /// Describes a virtual button (a key from a keyboard, a mouse button, an axis of a joystick...etc.).
    /// </summary>
    public partial class VirtualButton
    {
        /// <summary>
        /// Keyboard virtual button.
        /// </summary>
        public class Keyboard : VirtualButton
        {
            /// <summary>
            /// The 'none' key.
            /// </summary>
            public static readonly VirtualButton None = new Keyboard("none", (int)Keys.None);

            /// <summary>
            /// The 'cancel' key.
            /// </summary>
            public static readonly VirtualButton Cancel = new Keyboard("cancel", (int)Keys.Cancel);

            /// <summary>
            /// The 'back' key.
            /// </summary>
            public static readonly VirtualButton Back = new Keyboard("back", (int)Keys.Back);

            /// <summary>
            /// The 'tab' key.
            /// </summary>
            public static readonly VirtualButton Tab = new Keyboard("tab", (int)Keys.Tab);

            /// <summary>
            /// The 'linefeed' key.
            /// </summary>
            public static readonly VirtualButton LineFeed = new Keyboard("linefeed", (int)Keys.LineFeed);

            /// <summary>
            /// The 'clear' key.
            /// </summary>
            public static readonly VirtualButton Clear = new Keyboard("clear", (int)Keys.Clear);

            /// <summary>
            /// The 'enter' key.
            /// </summary>
            public static readonly VirtualButton Enter = new Keyboard("enter", (int)Keys.Enter);

            /// <summary>
            /// The 'return' key.
            /// </summary>
            public static readonly VirtualButton Return = new Keyboard("return", (int)Keys.Return);

            /// <summary>
            /// The 'pause' key.
            /// </summary>
            public static readonly VirtualButton Pause = new Keyboard("pause", (int)Keys.Pause);

            /// <summary>
            /// The 'capital' key.
            /// </summary>
            public static readonly VirtualButton Capital = new Keyboard("capital", (int)Keys.Capital);

            /// <summary>
            /// The 'capslock' key.
            /// </summary>
            public static readonly VirtualButton CapsLock = new Keyboard("capslock", (int)Keys.CapsLock);

            /// <summary>
            /// The 'hangulmode' key.
            /// </summary>
            public static readonly VirtualButton HangulMode = new Keyboard("hangulmode", (int)Keys.HangulMode);

            /// <summary>
            /// The 'kanamode' key.
            /// </summary>
            public static readonly VirtualButton KanaMode = new Keyboard("kanamode", (int)Keys.KanaMode);

            /// <summary>
            /// The 'junjamode' key.
            /// </summary>
            public static readonly VirtualButton JunjaMode = new Keyboard("junjamode", (int)Keys.JunjaMode);

            /// <summary>
            /// The 'finalmode' key.
            /// </summary>
            public static readonly VirtualButton FinalMode = new Keyboard("finalmode", (int)Keys.FinalMode);

            /// <summary>
            /// The 'hanjamode' key.
            /// </summary>
            public static readonly VirtualButton HanjaMode = new Keyboard("hanjamode", (int)Keys.HanjaMode);

            /// <summary>
            /// The 'kanjimode' key.
            /// </summary>
            public static readonly VirtualButton KanjiMode = new Keyboard("kanjimode", (int)Keys.KanjiMode);

            /// <summary>
            /// The 'escape' key.
            /// </summary>
            public static readonly VirtualButton Escape = new Keyboard("escape", (int)Keys.Escape);

            /// <summary>
            /// The 'imeconvert' key.
            /// </summary>
            public static readonly VirtualButton ImeConvert = new Keyboard("imeconvert", (int)Keys.ImeConvert);

            /// <summary>
            /// The 'imenonconvert' key.
            /// </summary>
            public static readonly VirtualButton ImeNonConvert = new Keyboard("imenonconvert", (int)Keys.ImeNonConvert);

            /// <summary>
            /// The 'imeaccept' key.
            /// </summary>
            public static readonly VirtualButton ImeAccept = new Keyboard("imeaccept", (int)Keys.ImeAccept);

            /// <summary>
            /// The 'imemodechange' key.
            /// </summary>
            public static readonly VirtualButton ImeModeChange = new Keyboard("imemodechange", (int)Keys.ImeModeChange);

            /// <summary>
            /// The 'space' key.
            /// </summary>
            public static readonly VirtualButton Space = new Keyboard("space", (int)Keys.Space);

            /// <summary>
            /// The 'pageup' key.
            /// </summary>
            public static readonly VirtualButton PageUp = new Keyboard("pageup", (int)Keys.PageUp);

            /// <summary>
            /// The 'prior' key.
            /// </summary>
            public static readonly VirtualButton Prior = new Keyboard("prior", (int)Keys.Prior);

            /// <summary>
            /// The 'next' key.
            /// </summary>
            public static readonly VirtualButton Next = new Keyboard("next", (int)Keys.Next);

            /// <summary>
            /// The 'pagedown' key.
            /// </summary>
            public static readonly VirtualButton PageDown = new Keyboard("pagedown", (int)Keys.PageDown);

            /// <summary>
            /// The 'end' key.
            /// </summary>
            public static readonly VirtualButton End = new Keyboard("end", (int)Keys.End);

            /// <summary>
            /// The 'home' key.
            /// </summary>
            public static readonly VirtualButton Home = new Keyboard("home", (int)Keys.Home);

            /// <summary>
            /// The 'left' key.
            /// </summary>
            public static readonly VirtualButton Left = new Keyboard("left", (int)Keys.Left);

            /// <summary>
            /// The 'up' key.
            /// </summary>
            public static readonly VirtualButton Up = new Keyboard("up", (int)Keys.Up);

            /// <summary>
            /// The 'right' key.
            /// </summary>
            public static readonly VirtualButton Right = new Keyboard("right", (int)Keys.Right);

            /// <summary>
            /// The 'down' key.
            /// </summary>
            public static readonly VirtualButton Down = new Keyboard("down", (int)Keys.Down);

            /// <summary>
            /// The 'select' key.
            /// </summary>
            public static readonly VirtualButton Select = new Keyboard("select", (int)Keys.Select);

            /// <summary>
            /// The 'print' key.
            /// </summary>
            public static readonly VirtualButton Print = new Keyboard("print", (int)Keys.Print);

            /// <summary>
            /// The 'execute' key.
            /// </summary>
            public static readonly VirtualButton Execute = new Keyboard("execute", (int)Keys.Execute);

            /// <summary>
            /// The 'printscreen' key.
            /// </summary>
            public static readonly VirtualButton PrintScreen = new Keyboard("printscreen", (int)Keys.PrintScreen);

            /// <summary>
            /// The 'snapshot' key.
            /// </summary>
            public static readonly VirtualButton Snapshot = new Keyboard("snapshot", (int)Keys.Snapshot);

            /// <summary>
            /// The 'insert' key.
            /// </summary>
            public static readonly VirtualButton Insert = new Keyboard("insert", (int)Keys.Insert);

            /// <summary>
            /// The 'delete' key.
            /// </summary>
            public static readonly VirtualButton Delete = new Keyboard("delete", (int)Keys.Delete);

            /// <summary>
            /// The 'help' key.
            /// </summary>
            public static readonly VirtualButton Help = new Keyboard("help", (int)Keys.Help);

            /// <summary>
            /// The 'd0' key.
            /// </summary>
            public static readonly VirtualButton D0 = new Keyboard("d0", (int)Keys.D0);

            /// <summary>
            /// The 'd1' key.
            /// </summary>
            public static readonly VirtualButton D1 = new Keyboard("d1", (int)Keys.D1);

            /// <summary>
            /// The 'd2' key.
            /// </summary>
            public static readonly VirtualButton D2 = new Keyboard("d2", (int)Keys.D2);

            /// <summary>
            /// The 'd3' key.
            /// </summary>
            public static readonly VirtualButton D3 = new Keyboard("d3", (int)Keys.D3);

            /// <summary>
            /// The 'd4' key.
            /// </summary>
            public static readonly VirtualButton D4 = new Keyboard("d4", (int)Keys.D4);

            /// <summary>
            /// The 'd5' key.
            /// </summary>
            public static readonly VirtualButton D5 = new Keyboard("d5", (int)Keys.D5);

            /// <summary>
            /// The 'd6' key.
            /// </summary>
            public static readonly VirtualButton D6 = new Keyboard("d6", (int)Keys.D6);

            /// <summary>
            /// The 'd7' key.
            /// </summary>
            public static readonly VirtualButton D7 = new Keyboard("d7", (int)Keys.D7);

            /// <summary>
            /// The 'd8' key.
            /// </summary>
            public static readonly VirtualButton D8 = new Keyboard("d8", (int)Keys.D8);

            /// <summary>
            /// The 'd9' key.
            /// </summary>
            public static readonly VirtualButton D9 = new Keyboard("d9", (int)Keys.D9);

            /// <summary>
            /// The 'a' key.
            /// </summary>
            public static readonly VirtualButton A = new Keyboard("a", (int)Keys.A);

            /// <summary>
            /// The 'b' key.
            /// </summary>
            public static readonly VirtualButton B = new Keyboard("b", (int)Keys.B);

            /// <summary>
            /// The 'c' key.
            /// </summary>
            public static readonly VirtualButton C = new Keyboard("c", (int)Keys.C);

            /// <summary>
            /// The 'd' key.
            /// </summary>
            public static readonly VirtualButton D = new Keyboard("d", (int)Keys.D);

            /// <summary>
            /// The 'e' key.
            /// </summary>
            public static readonly VirtualButton E = new Keyboard("e", (int)Keys.E);

            /// <summary>
            /// The 'f' key.
            /// </summary>
            public static readonly VirtualButton F = new Keyboard("f", (int)Keys.F);

            /// <summary>
            /// The 'g' key.
            /// </summary>
            public static readonly VirtualButton G = new Keyboard("g", (int)Keys.G);

            /// <summary>
            /// The 'h' key.
            /// </summary>
            public static readonly VirtualButton H = new Keyboard("h", (int)Keys.H);

            /// <summary>
            /// The 'i' key.
            /// </summary>
            public static readonly VirtualButton I = new Keyboard("i", (int)Keys.I);

            /// <summary>
            /// The 'j' key.
            /// </summary>
            public static readonly VirtualButton J = new Keyboard("j", (int)Keys.J);

            /// <summary>
            /// The 'k' key.
            /// </summary>
            public static readonly VirtualButton K = new Keyboard("k", (int)Keys.K);

            /// <summary>
            /// The 'l' key.
            /// </summary>
            public static readonly VirtualButton L = new Keyboard("l", (int)Keys.L);

            /// <summary>
            /// The 'm' key.
            /// </summary>
            public static readonly VirtualButton M = new Keyboard("m", (int)Keys.M);

            /// <summary>
            /// The 'n' key.
            /// </summary>
            public static readonly VirtualButton N = new Keyboard("n", (int)Keys.N);

            /// <summary>
            /// The 'o' key.
            /// </summary>
            public static readonly VirtualButton O = new Keyboard("o", (int)Keys.O);

            /// <summary>
            /// The 'p' key.
            /// </summary>
            public static readonly VirtualButton P = new Keyboard("p", (int)Keys.P);

            /// <summary>
            /// The 'q' key.
            /// </summary>
            public static readonly VirtualButton Q = new Keyboard("q", (int)Keys.Q);

            /// <summary>
            /// The 'r' key.
            /// </summary>
            public static readonly VirtualButton R = new Keyboard("r", (int)Keys.R);

            /// <summary>
            /// The 's' key.
            /// </summary>
            public static readonly VirtualButton S = new Keyboard("s", (int)Keys.S);

            /// <summary>
            /// The 't' key.
            /// </summary>
            public static readonly VirtualButton T = new Keyboard("t", (int)Keys.T);

            /// <summary>
            /// The 'u' key.
            /// </summary>
            public static readonly VirtualButton U = new Keyboard("u", (int)Keys.U);

            /// <summary>
            /// The 'v' key.
            /// </summary>
            public static readonly VirtualButton V = new Keyboard("v", (int)Keys.V);

            /// <summary>
            /// The 'w' key.
            /// </summary>
            public static readonly VirtualButton W = new Keyboard("w", (int)Keys.W);

            /// <summary>
            /// The 'x' key.
            /// </summary>
            public static readonly VirtualButton X = new Keyboard("x", (int)Keys.X);

            /// <summary>
            /// The 'y' key.
            /// </summary>
            public static readonly VirtualButton Y = new Keyboard("y", (int)Keys.Y);

            /// <summary>
            /// The 'z' key.
            /// </summary>
            public static readonly VirtualButton Z = new Keyboard("z", (int)Keys.Z);

            /// <summary>
            /// The 'leftwin' key.
            /// </summary>
            public static readonly VirtualButton LeftWin = new Keyboard("leftwin", (int)Keys.LeftWin);

            /// <summary>
            /// The 'rightwin' key.
            /// </summary>
            public static readonly VirtualButton RightWin = new Keyboard("rightwin", (int)Keys.RightWin);

            /// <summary>
            /// The 'apps' key.
            /// </summary>
            public static readonly VirtualButton Apps = new Keyboard("apps", (int)Keys.Apps);

            /// <summary>
            /// The 'sleep' key.
            /// </summary>
            public static readonly VirtualButton Sleep = new Keyboard("sleep", (int)Keys.Sleep);

            /// <summary>
            /// The 'numpad0' key.
            /// </summary>
            public static readonly VirtualButton NumPad0 = new Keyboard("numpad0", (int)Keys.NumPad0);

            /// <summary>
            /// The 'numpad1' key.
            /// </summary>
            public static readonly VirtualButton NumPad1 = new Keyboard("numpad1", (int)Keys.NumPad1);

            /// <summary>
            /// The 'numpad2' key.
            /// </summary>
            public static readonly VirtualButton NumPad2 = new Keyboard("numpad2", (int)Keys.NumPad2);

            /// <summary>
            /// The 'numpad3' key.
            /// </summary>
            public static readonly VirtualButton NumPad3 = new Keyboard("numpad3", (int)Keys.NumPad3);

            /// <summary>
            /// The 'numpad4' key.
            /// </summary>
            public static readonly VirtualButton NumPad4 = new Keyboard("numpad4", (int)Keys.NumPad4);

            /// <summary>
            /// The 'numpad5' key.
            /// </summary>
            public static readonly VirtualButton NumPad5 = new Keyboard("numpad5", (int)Keys.NumPad5);

            /// <summary>
            /// The 'numpad6' key.
            /// </summary>
            public static readonly VirtualButton NumPad6 = new Keyboard("numpad6", (int)Keys.NumPad6);

            /// <summary>
            /// The 'numpad7' key.
            /// </summary>
            public static readonly VirtualButton NumPad7 = new Keyboard("numpad7", (int)Keys.NumPad7);

            /// <summary>
            /// The 'numpad8' key.
            /// </summary>
            public static readonly VirtualButton NumPad8 = new Keyboard("numpad8", (int)Keys.NumPad8);

            /// <summary>
            /// The 'numpad9' key.
            /// </summary>
            public static readonly VirtualButton NumPad9 = new Keyboard("numpad9", (int)Keys.NumPad9);

            /// <summary>
            /// The 'multiply' key.
            /// </summary>
            public static readonly VirtualButton Multiply = new Keyboard("multiply", (int)Keys.Multiply);

            /// <summary>
            /// The 'add' key.
            /// </summary>
            public static readonly VirtualButton Add = new Keyboard("add", (int)Keys.Add);

            /// <summary>
            /// The 'separator' key.
            /// </summary>
            public static readonly VirtualButton Separator = new Keyboard("separator", (int)Keys.Separator);

            /// <summary>
            /// The 'subtract' key.
            /// </summary>
            public static readonly VirtualButton Subtract = new Keyboard("subtract", (int)Keys.Subtract);

            /// <summary>
            /// The 'decimal' key.
            /// </summary>
            public static readonly VirtualButton Decimal = new Keyboard("decimal", (int)Keys.Decimal);

            /// <summary>
            /// The 'divide' key.
            /// </summary>
            public static readonly VirtualButton Divide = new Keyboard("divide", (int)Keys.Divide);

            /// <summary>
            /// The 'f1' key.
            /// </summary>
            public static readonly VirtualButton F1 = new Keyboard("f1", (int)Keys.F1);

            /// <summary>
            /// The 'f2' key.
            /// </summary>
            public static readonly VirtualButton F2 = new Keyboard("f2", (int)Keys.F2);

            /// <summary>
            /// The 'f3' key.
            /// </summary>
            public static readonly VirtualButton F3 = new Keyboard("f3", (int)Keys.F3);

            /// <summary>
            /// The 'f4' key.
            /// </summary>
            public static readonly VirtualButton F4 = new Keyboard("f4", (int)Keys.F4);

            /// <summary>
            /// The 'f5' key.
            /// </summary>
            public static readonly VirtualButton F5 = new Keyboard("f5", (int)Keys.F5);

            /// <summary>
            /// The 'f6' key.
            /// </summary>
            public static readonly VirtualButton F6 = new Keyboard("f6", (int)Keys.F6);

            /// <summary>
            /// The 'f7' key.
            /// </summary>
            public static readonly VirtualButton F7 = new Keyboard("f7", (int)Keys.F7);

            /// <summary>
            /// The 'f8' key.
            /// </summary>
            public static readonly VirtualButton F8 = new Keyboard("f8", (int)Keys.F8);

            /// <summary>
            /// The 'f9' key.
            /// </summary>
            public static readonly VirtualButton F9 = new Keyboard("f9", (int)Keys.F9);

            /// <summary>
            /// The 'f10' key.
            /// </summary>
            public static readonly VirtualButton F10 = new Keyboard("f10", (int)Keys.F10);

            /// <summary>
            /// The 'f11' key.
            /// </summary>
            public static readonly VirtualButton F11 = new Keyboard("f11", (int)Keys.F11);

            /// <summary>
            /// The 'f12' key.
            /// </summary>
            public static readonly VirtualButton F12 = new Keyboard("f12", (int)Keys.F12);

            /// <summary>
            /// The 'f13' key.
            /// </summary>
            public static readonly VirtualButton F13 = new Keyboard("f13", (int)Keys.F13);

            /// <summary>
            /// The 'f14' key.
            /// </summary>
            public static readonly VirtualButton F14 = new Keyboard("f14", (int)Keys.F14);

            /// <summary>
            /// The 'f15' key.
            /// </summary>
            public static readonly VirtualButton F15 = new Keyboard("f15", (int)Keys.F15);

            /// <summary>
            /// The 'f16' key.
            /// </summary>
            public static readonly VirtualButton F16 = new Keyboard("f16", (int)Keys.F16);

            /// <summary>
            /// The 'f17' key.
            /// </summary>
            public static readonly VirtualButton F17 = new Keyboard("f17", (int)Keys.F17);

            /// <summary>
            /// The 'f18' key.
            /// </summary>
            public static readonly VirtualButton F18 = new Keyboard("f18", (int)Keys.F18);

            /// <summary>
            /// The 'f19' key.
            /// </summary>
            public static readonly VirtualButton F19 = new Keyboard("f19", (int)Keys.F19);

            /// <summary>
            /// The 'f20' key.
            /// </summary>
            public static readonly VirtualButton F20 = new Keyboard("f20", (int)Keys.F20);

            /// <summary>
            /// The 'f21' key.
            /// </summary>
            public static readonly VirtualButton F21 = new Keyboard("f21", (int)Keys.F21);

            /// <summary>
            /// The 'f22' key.
            /// </summary>
            public static readonly VirtualButton F22 = new Keyboard("f22", (int)Keys.F22);

            /// <summary>
            /// The 'f23' key.
            /// </summary>
            public static readonly VirtualButton F23 = new Keyboard("f23", (int)Keys.F23);

            /// <summary>
            /// The 'f24' key.
            /// </summary>
            public static readonly VirtualButton F24 = new Keyboard("f24", (int)Keys.F24);

            /// <summary>
            /// The 'numlock' key.
            /// </summary>
            public static readonly VirtualButton NumLock = new Keyboard("numlock", (int)Keys.NumLock);

            /// <summary>
            /// The 'scroll' key.
            /// </summary>
            public static readonly VirtualButton Scroll = new Keyboard("scroll", (int)Keys.Scroll);

            /// <summary>
            /// The 'leftshift' key.
            /// </summary>
            public static readonly VirtualButton LeftShift = new Keyboard("leftshift", (int)Keys.LeftShift);

            /// <summary>
            /// The 'rightshift' key.
            /// </summary>
            public static readonly VirtualButton RightShift = new Keyboard("rightshift", (int)Keys.RightShift);

            /// <summary>
            /// The 'leftctrl' key.
            /// </summary>
            public static readonly VirtualButton LeftCtrl = new Keyboard("leftctrl", (int)Keys.LeftCtrl);

            /// <summary>
            /// The 'rightctrl' key.
            /// </summary>
            public static readonly VirtualButton RightCtrl = new Keyboard("rightctrl", (int)Keys.RightCtrl);

            /// <summary>
            /// The 'leftalt' key.
            /// </summary>
            public static readonly VirtualButton LeftAlt = new Keyboard("leftalt", (int)Keys.LeftAlt);

            /// <summary>
            /// The 'rightalt' key.
            /// </summary>
            public static readonly VirtualButton RightAlt = new Keyboard("rightalt", (int)Keys.RightAlt);

            /// <summary>
            /// The 'browserback' key.
            /// </summary>
            public static readonly VirtualButton BrowserBack = new Keyboard("browserback", (int)Keys.BrowserBack);

            /// <summary>
            /// The 'browserforward' key.
            /// </summary>
            public static readonly VirtualButton BrowserForward = new Keyboard("browserforward", (int)Keys.BrowserForward);

            /// <summary>
            /// The 'browserrefresh' key.
            /// </summary>
            public static readonly VirtualButton BrowserRefresh = new Keyboard("browserrefresh", (int)Keys.BrowserRefresh);

            /// <summary>
            /// The 'browserstop' key.
            /// </summary>
            public static readonly VirtualButton BrowserStop = new Keyboard("browserstop", (int)Keys.BrowserStop);

            /// <summary>
            /// The 'browsersearch' key.
            /// </summary>
            public static readonly VirtualButton BrowserSearch = new Keyboard("browsersearch", (int)Keys.BrowserSearch);

            /// <summary>
            /// The 'browserfavorites' key.
            /// </summary>
            public static readonly VirtualButton BrowserFavorites = new Keyboard("browserfavorites", (int)Keys.BrowserFavorites);

            /// <summary>
            /// The 'browserhome' key.
            /// </summary>
            public static readonly VirtualButton BrowserHome = new Keyboard("browserhome", (int)Keys.BrowserHome);

            /// <summary>
            /// The 'volumemute' key.
            /// </summary>
            public static readonly VirtualButton VolumeMute = new Keyboard("volumemute", (int)Keys.VolumeMute);

            /// <summary>
            /// The 'volumedown' key.
            /// </summary>
            public static readonly VirtualButton VolumeDown = new Keyboard("volumedown", (int)Keys.VolumeDown);

            /// <summary>
            /// The 'volumeup' key.
            /// </summary>
            public static readonly VirtualButton VolumeUp = new Keyboard("volumeup", (int)Keys.VolumeUp);

            /// <summary>
            /// The 'medianexttrack' key.
            /// </summary>
            public static readonly VirtualButton MediaNextTrack = new Keyboard("medianexttrack", (int)Keys.MediaNextTrack);

            /// <summary>
            /// The 'mediaprevioustrack' key.
            /// </summary>
            public static readonly VirtualButton MediaPreviousTrack = new Keyboard("mediaprevioustrack", (int)Keys.MediaPreviousTrack);

            /// <summary>
            /// The 'mediastop' key.
            /// </summary>
            public static readonly VirtualButton MediaStop = new Keyboard("mediastop", (int)Keys.MediaStop);

            /// <summary>
            /// The 'mediaplaypause' key.
            /// </summary>
            public static readonly VirtualButton MediaPlayPause = new Keyboard("mediaplaypause", (int)Keys.MediaPlayPause);

            /// <summary>
            /// The 'launchmail' key.
            /// </summary>
            public static readonly VirtualButton LaunchMail = new Keyboard("launchmail", (int)Keys.LaunchMail);

            /// <summary>
            /// The 'selectmedia' key.
            /// </summary>
            public static readonly VirtualButton SelectMedia = new Keyboard("selectmedia", (int)Keys.SelectMedia);

            /// <summary>
            /// The 'launchapplication1' key.
            /// </summary>
            public static readonly VirtualButton LaunchApplication1 = new Keyboard("launchapplication1", (int)Keys.LaunchApplication1);

            /// <summary>
            /// The 'launchapplication2' key.
            /// </summary>
            public static readonly VirtualButton LaunchApplication2 = new Keyboard("launchapplication2", (int)Keys.LaunchApplication2);

            /// <summary>
            /// The 'oem1' key.
            /// </summary>
            public static readonly VirtualButton Oem1 = new Keyboard("oem1", (int)Keys.Oem1);

            /// <summary>
            /// The 'oemsemicolon' key.
            /// </summary>
            public static readonly VirtualButton OemSemicolon = new Keyboard("oemsemicolon", (int)Keys.OemSemicolon);

            /// <summary>
            /// The 'oemplus' key.
            /// </summary>
            public static readonly VirtualButton OemPlus = new Keyboard("oemplus", (int)Keys.OemPlus);

            /// <summary>
            /// The 'oemcomma' key.
            /// </summary>
            public static readonly VirtualButton OemComma = new Keyboard("oemcomma", (int)Keys.OemComma);

            /// <summary>
            /// The 'oemminus' key.
            /// </summary>
            public static readonly VirtualButton OemMinus = new Keyboard("oemminus", (int)Keys.OemMinus);

            /// <summary>
            /// The 'oemperiod' key.
            /// </summary>
            public static readonly VirtualButton OemPeriod = new Keyboard("oemperiod", (int)Keys.OemPeriod);

            /// <summary>
            /// The 'oem2' key.
            /// </summary>
            public static readonly VirtualButton Oem2 = new Keyboard("oem2", (int)Keys.Oem2);

            /// <summary>
            /// The 'oemquestion' key.
            /// </summary>
            public static readonly VirtualButton OemQuestion = new Keyboard("oemquestion", (int)Keys.OemQuestion);

            /// <summary>
            /// The 'oem3' key.
            /// </summary>
            public static readonly VirtualButton Oem3 = new Keyboard("oem3", (int)Keys.Oem3);

            /// <summary>
            /// The 'oemtilde' key.
            /// </summary>
            public static readonly VirtualButton OemTilde = new Keyboard("oemtilde", (int)Keys.OemTilde);

            /// <summary>
            /// The 'oem4' key.
            /// </summary>
            public static readonly VirtualButton Oem4 = new Keyboard("oem4", (int)Keys.Oem4);

            /// <summary>
            /// The 'oemopenbrackets' key.
            /// </summary>
            public static readonly VirtualButton OemOpenBrackets = new Keyboard("oemopenbrackets", (int)Keys.OemOpenBrackets);

            /// <summary>
            /// The 'oem5' key.
            /// </summary>
            public static readonly VirtualButton Oem5 = new Keyboard("oem5", (int)Keys.Oem5);

            /// <summary>
            /// The 'oempipe' key.
            /// </summary>
            public static readonly VirtualButton OemPipe = new Keyboard("oempipe", (int)Keys.OemPipe);

            /// <summary>
            /// The 'oem6' key.
            /// </summary>
            public static readonly VirtualButton Oem6 = new Keyboard("oem6", (int)Keys.Oem6);

            /// <summary>
            /// The 'oemclosebrackets' key.
            /// </summary>
            public static readonly VirtualButton OemCloseBrackets = new Keyboard("oemclosebrackets", (int)Keys.OemCloseBrackets);

            /// <summary>
            /// The 'oem7' key.
            /// </summary>
            public static readonly VirtualButton Oem7 = new Keyboard("oem7", (int)Keys.Oem7);

            /// <summary>
            /// The 'oemquotes' key.
            /// </summary>
            public static readonly VirtualButton OemQuotes = new Keyboard("oemquotes", (int)Keys.OemQuotes);

            /// <summary>
            /// The 'oem8' key.
            /// </summary>
            public static readonly VirtualButton Oem8 = new Keyboard("oem8", (int)Keys.Oem8);

            /// <summary>
            /// The 'oem102' key.
            /// </summary>
            public static readonly VirtualButton Oem102 = new Keyboard("oem102", (int)Keys.Oem102);

            /// <summary>
            /// The 'oembackslash' key.
            /// </summary>
            public static readonly VirtualButton OemBackslash = new Keyboard("oembackslash", (int)Keys.OemBackslash);

            /// <summary>
            /// The 'attn' key.
            /// </summary>
            public static readonly VirtualButton Attn = new Keyboard("attn", (int)Keys.Attn);

            /// <summary>
            /// The 'crsel' key.
            /// </summary>
            public static readonly VirtualButton CrSel = new Keyboard("crsel", (int)Keys.CrSel);

            /// <summary>
            /// The 'exsel' key.
            /// </summary>
            public static readonly VirtualButton ExSel = new Keyboard("exsel", (int)Keys.ExSel);

            /// <summary>
            /// The 'eraseeof' key.
            /// </summary>
            public static readonly VirtualButton EraseEof = new Keyboard("eraseeof", (int)Keys.EraseEof);

            /// <summary>
            /// The 'play' key.
            /// </summary>
            public static readonly VirtualButton Play = new Keyboard("play", (int)Keys.Play);

            /// <summary>
            /// The 'zoom' key.
            /// </summary>
            public static readonly VirtualButton Zoom = new Keyboard("zoom", (int)Keys.Zoom);

            /// <summary>
            /// The 'noname' key.
            /// </summary>
            public static readonly VirtualButton NoName = new Keyboard("noname", (int)Keys.NoName);

            /// <summary>
            /// The 'pa1' key.
            /// </summary>
            public static readonly VirtualButton Pa1 = new Keyboard("pa1", (int)Keys.Pa1);

            /// <summary>
            /// The 'oemclear' key.
            /// </summary>
            public static readonly VirtualButton OemClear = new Keyboard("oemclear", (int)Keys.OemClear);

            protected Keyboard(string name, int id, bool isPositiveAndNegative = false)
                : base(name, VirtualButtonType.Keyboard, id, isPositiveAndNegative)
            {
            }

            public override float GetValue(InputManager manager)
            {
                return IsDown(manager) ? 1.0f : 0.0f;
            }

            public override bool IsDown(InputManager manager)
            {
                return manager.IsKeyDown((Keys)Index);
            }

            public override bool IsPressed(InputManager manager)
            {
                return manager.IsKeyPressed((Keys)Index);
            }

            public override bool IsReleased(InputManager manager)
            {
                return manager.IsKeyReleased((Keys)Index);
            }
        }
    }
}
