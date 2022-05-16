// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Input
{
    /// <summary>
    /// Enumeration for keys.
    /// </summary>
    public enum Keys
    {
        /// <summary>
        /// Invalid key.
        /// </summary>
        None = 0,

        /// <summary>
        /// The 'cancel' key.
        /// </summary>
        Cancel = 1,

        /// <summary>
        /// The 'back'/'backspace' key.
        /// </summary>
        Back = 2,

        /// <summary>
        /// The 'back'/'backspace' key.
        /// </summary>
        BackSpace = Back,

        /// <summary>
        /// The 'tab' key.
        /// </summary>
        Tab = 3,

        /// <summary>
        /// The 'linefeed' key.
        /// </summary>
        LineFeed = 4,

        /// <summary>
        /// The 'clear' key.
        /// </summary>
        Clear = 5,

        /// <summary>
        /// The 'enter'/'return' key.
        /// </summary>
        Enter = 6,

        /// <summary>
        /// The 'enter'/'return' key.
        /// </summary>
        Return = Enter,

        /// <summary>
        /// The 'pause' key.
        /// </summary>
        Pause = 7,

        /// <summary>
        /// The 'capslock'/'capital' key.
        /// </summary>
        Capital = CapsLock,

        /// <summary>
        /// The 'capslock'/'capital' key.
        /// </summary>
        CapsLock = 8,

        /// <summary>
        /// The 'hangulmode' key.
        /// </summary>
        HangulMode = 9,

        /// <summary>
        /// The 'kanamode' key.
        /// </summary>
        KanaMode = 9,

        /// <summary>
        /// The 'junjamode' key.
        /// </summary>
        JunjaMode = 10,

        /// <summary>
        /// The 'finalmode' key.
        /// </summary>
        FinalMode = 11,

        /// <summary>
        /// The 'hanjamode' key.
        /// </summary>
        HanjaMode = 12,

        /// <summary>
        /// The 'kanjimode' key.
        /// </summary>
        KanjiMode = 12,

        /// <summary>
        /// The 'escape' key.
        /// </summary>
        Escape = 13,

        /// <summary>
        /// The 'imeconvert' key.
        /// </summary>
        ImeConvert = 14,

        /// <summary>
        /// The 'imenonconvert' key.
        /// </summary>
        ImeNonConvert = 15,

        /// <summary>
        /// The 'imeaccept' key.
        /// </summary>
        ImeAccept = 16,

        /// <summary>
        /// The 'imemodechange' key.
        /// </summary>
        ImeModeChange = 17,

        /// <summary>
        /// The 'space' key.
        /// </summary>
        Space = 18,

        /// <summary>
        /// The 'pageup'/'prior' key.
        /// </summary>
        PageUp = 19,

        /// <summary>
        /// The 'pageup'/'prior' key.
        /// </summary>
        Prior = PageUp,

        /// <summary>
        /// The 'pagedown'/'next' key.
        /// </summary>
        Next = PageDown,

        /// <summary>
        /// The 'pagedown'/'next' key.
        /// </summary>
        PageDown = 20,

        /// <summary>
        /// The 'end' key.
        /// </summary>
        End = 21,

        /// <summary>
        /// The 'home' key.
        /// </summary>
        Home = 22,

        /// <summary>
        /// The 'left' key.
        /// </summary>
        Left = 23,

        /// <summary>
        /// The 'up' key.
        /// </summary>
        Up = 24,

        /// <summary>
        /// The 'right' key.
        /// </summary>
        Right = 25,

        /// <summary>
        /// The 'down' key.
        /// </summary>
        Down = 26,

        /// <summary>
        /// The 'select' key.
        /// </summary>
        Select = 27,

        /// <summary>
        /// The 'print' key, not to be confused with <see cref="PrintScreen"/>.
        /// </summary>
        Print = 28,

        /// <summary>
        /// The 'execute' key.
        /// </summary>
        Execute = 29,

        /// <summary>
        /// The 'snapshot'/'printscreen' key.
        /// </summary>
        PrintScreen = 30,

        /// <summary>
        /// The 'snapshot'/'printscreen' key
        /// </summary>
        Snapshot = PrintScreen,

        /// <summary>
        /// The 'insert' key.
        /// </summary>
        Insert = 31,

        /// <summary>
        /// The 'delete' key.
        /// </summary>
        Delete = 32,

        /// <summary>
        /// The 'help' key.
        /// </summary>
        Help = 33,

        /// <summary>
        /// The number row '0' key.
        /// </summary>
        D0 = 34,

        /// <summary>
        /// The number row '1' key.
        /// </summary>
        D1 = 35,

        /// <summary>
        /// The number row '2' key.
        /// </summary>
        D2 = 36,

        /// <summary>
        /// The number row '3' key.
        /// </summary>
        D3 = 37,

        /// <summary>
        /// The number row '4' key.
        /// </summary>
        D4 = 38,

        /// <summary>
        /// The number row '5' key.
        /// </summary>
        D5 = 39,

        /// <summary>
        /// The number row '6' key.
        /// </summary>
        D6 = 40,

        /// <summary>
        /// The number row '7' key.
        /// </summary>
        D7 = 41,

        /// <summary>
        /// The number row '8' key.
        /// </summary>
        D8 = 42,

        /// <summary>
        /// The number row '9' key.
        /// </summary>
        D9 = 43,

        /// <summary>
        /// The 'a' key.
        /// </summary>
        A = 44,

        /// <summary>
        /// The 'b' key.
        /// </summary>
        B = 45,

        /// <summary>
        /// The 'c' key.
        /// </summary>
        C = 46,

        /// <summary>
        /// The 'd' key.
        /// </summary>
        D = 47,

        /// <summary>
        /// The 'e' key.
        /// </summary>
        E = 48,

        /// <summary>
        /// The 'f' key.
        /// </summary>
        F = 49,

        /// <summary>
        /// The 'g' key.
        /// </summary>
        G = 50,

        /// <summary>
        /// The 'h' key.
        /// </summary>
        H = 51,

        /// <summary>
        /// The 'i' key.
        /// </summary>
        I = 52,

        /// <summary>
        /// The 'j' key.
        /// </summary>
        J = 53,

        /// <summary>
        /// The 'k' key.
        /// </summary>
        K = 54,

        /// <summary>
        /// The 'l' key.
        /// </summary>
        L = 55,

        /// <summary>
        /// The 'm' key.
        /// </summary>
        M = 56,

        /// <summary>
        /// The 'n' key.
        /// </summary>
        N = 57,

        /// <summary>
        /// The 'o' key.
        /// </summary>
        O = 58,

        /// <summary>
        /// The 'p' key.
        /// </summary>
        P = 59,

        /// <summary>
        /// The 'q' key.
        /// </summary>
        Q = 60,

        /// <summary>
        /// The 'r' key.
        /// </summary>
        R = 61,

        /// <summary>
        /// The 's' key.
        /// </summary>
        S = 62,

        /// <summary>
        /// The 't' key.
        /// </summary>
        T = 63,

        /// <summary>
        /// The 'u' key.
        /// </summary>
        U = 64,

        /// <summary>
        /// The 'v' key.
        /// </summary>
        V = 65,

        /// <summary>
        /// The 'w' key.
        /// </summary>
        W = 66,

        /// <summary>
        /// The 'x' key.
        /// </summary>
        X = 67,

        /// <summary>
        /// The 'y' key.
        /// </summary>
        Y = 68,

        /// <summary>
        /// The 'z' key.
        /// </summary>
        Z = 69,

        /// <summary>
        /// The left 'windows'/'command'/'meta' key.
        /// </summary>
        LeftWin = 70,

        /// <summary>
        /// The right 'windows'/'command'/'meta' key.
        /// </summary>
        RightWin = 71,

        /// <summary>
        /// The 'apps'/'Menu'/'FN2' key, between the windows and right ctrl key on a 104 keys keyboard.
        /// </summary>
        Apps = 72,

        /// <summary>
        /// The 'sleep' key.
        /// </summary>
        Sleep = 73,

        /// <summary>
        /// The numeric keypad '0' key.
        /// </summary>
        NumPad0 = 74,

        /// <summary>
        /// The numeric keypad '1' key.
        /// </summary>
        NumPad1 = 75,

        /// <summary>
        /// The numeric keypad '2' key.
        /// </summary>
        NumPad2 = 76,

        /// <summary>
        /// The numeric keypad '3' key.
        /// </summary>
        NumPad3 = 77,

        /// <summary>
        /// The numeric keypad '4' key.
        /// </summary>
        NumPad4 = 78,

        /// <summary>
        /// The numeric keypad '5' key.
        /// </summary>
        NumPad5 = 79,

        /// <summary>
        /// The numeric keypad '6' key.
        /// </summary>
        NumPad6 = 80,

        /// <summary>
        /// The numeric keypad '7' key.
        /// </summary>
        NumPad7 = 81,

        /// <summary>
        /// The numeric keypad '8' key.
        /// </summary>
        NumPad8 = 82,

        /// <summary>
        /// The numeric keypad '9' key.
        /// </summary>
        NumPad9 = 83,

        /// <summary>
        /// The numeric keypad 'multiply' key.
        /// </summary>
        Multiply = 84,

        /// <summary>
        /// The numeric keypad 'add' key.
        /// </summary>
        Add = 85,

        /// <summary>
        /// The 'separator' key.
        /// </summary>
        Separator = 86,

        /// <summary>
        /// The numeric keypad 'subtract' key.
        /// </summary>
        Subtract = 87,

        /// <summary>
        /// The numeric keypad 'decimal' and 'decimal separator' key.
        /// </summary>
        Decimal = 88,

        /// <summary>
        /// The numeric keypad 'divide' key.
        /// </summary>
        Divide = 89,

        /// <summary>
        /// The function key 'f1'.
        /// </summary>
        F1 = 90,

        /// <summary>
        /// The function key 'f2'.
        /// </summary>
        F2 = 91,

        /// <summary>
        /// The function key 'f3'.
        /// </summary>
        F3 = 92,

        /// <summary>
        /// The function key 'f4'.
        /// </summary>
        F4 = 93,

        /// <summary>
        /// The function key 'f5'.
        /// </summary>
        F5 = 94,

        /// <summary>
        /// The function key 'f6'.
        /// </summary>
        F6 = 95,

        /// <summary>
        /// The function key 'f7'.
        /// </summary>
        F7 = 96,

        /// <summary>
        /// The function key 'f8'.
        /// </summary>
        F8 = 97,

        /// <summary>
        /// The function key 'f9'.
        /// </summary>
        F9 = 98,

        /// <summary>
        /// The function key 'f10'.
        /// </summary>
        F10 = 99,

        /// <summary>
        /// The function key 'f11'.
        /// </summary>
        F11 = 100,

        /// <summary>
        /// The function key 'f12'.
        /// </summary>
        F12 = 101,

        /// <summary>
        /// The function key 'f13'.
        /// </summary>
        F13 = 102,

        /// <summary>
        /// The function key 'f14'.
        /// </summary>
        F14 = 103,

        /// <summary>
        /// The function key 'f15'.
        /// </summary>
        F15 = 104,

        /// <summary>
        /// The function key 'f16'.
        /// </summary>
        F16 = 105,

        /// <summary>
        /// The function key 'f17'.
        /// </summary>
        F17 = 106,

        /// <summary>
        /// The function key 'f18'.
        /// </summary>
        F18 = 107,

        /// <summary>
        /// The function key 'f19'.
        /// </summary>
        F19 = 108,

        /// <summary>
        /// The function key 'f20'.
        /// </summary>
        F20 = 109,

        /// <summary>
        /// The function key 'f21'.
        /// </summary>
        F21 = 110,

        /// <summary>
        /// The function key 'f22'.
        /// </summary>
        F22 = 111,

        /// <summary>
        /// The function key 'f23'.
        /// </summary>
        F23 = 112,

        /// <summary>
        /// The function key 'f24'.
        /// </summary>
        F24 = 113,

        /// <summary>
        /// The 'numlock' key.
        /// </summary>
        NumLock = 114,

        /// <summary>
        /// The 'scroll'/'scroll lock' key.
        /// </summary>
        Scroll = 115,

        /// <summary>
        /// The left 'shift' key.
        /// </summary>
        LeftShift = 116,

        /// <summary>
        /// The right 'shift' key.
        /// </summary>
        RightShift = 117,

        /// <summary>
        /// The left 'ctrl' key.
        /// </summary>
        LeftCtrl = 118,

        /// <summary>
        /// The right 'ctrl' key.
        /// </summary>
        RightCtrl = 119,

        /// <summary>
        /// The left 'alt' key.
        /// </summary>
        LeftAlt = 120,

        /// <summary>
        /// The right 'alt' key.
        /// </summary>
        RightAlt = 121,

        /// <summary>
        /// The 'browserback' key.
        /// </summary>
        BrowserBack = 122,

        /// <summary>
        /// The 'browserforward' key.
        /// </summary>
        BrowserForward = 123,

        /// <summary>
        /// The 'browserrefresh' key.
        /// </summary>
        BrowserRefresh = 124,

        /// <summary>
        /// The 'browserstop' key.
        /// </summary>
        BrowserStop = 125,

        /// <summary>
        /// The 'browsersearch' key.
        /// </summary>
        BrowserSearch = 126,

        /// <summary>
        /// The 'browserfavorites' key.
        /// </summary>
        BrowserFavorites = 127,

        /// <summary>
        /// The 'browserhome' key.
        /// </summary>
        BrowserHome = 128,

        /// <summary>
        /// The 'volumemute' key.
        /// </summary>
        VolumeMute = 129,

        /// <summary>
        /// The 'volumedown' key.
        /// </summary>
        VolumeDown = 130,

        /// <summary>
        /// The 'volumeup' key.
        /// </summary>
        VolumeUp = 131,

        /// <summary>
        /// The 'medianexttrack' key.
        /// </summary>
        MediaNextTrack = 132,

        /// <summary>
        /// The 'mediaprevioustrack' key.
        /// </summary>
        MediaPreviousTrack = 133,

        /// <summary>
        /// The 'mediastop' key.
        /// </summary>
        MediaStop = 134,

        /// <summary>
        /// The 'mediaplaypause' key.
        /// </summary>
        MediaPlayPause = 135,

        /// <summary>
        /// The 'launchmail' key.
        /// </summary>
        LaunchMail = 136,

        /// <summary>
        /// The 'selectmedia' key.
        /// </summary>
        SelectMedia = 137,

        /// <summary>
        /// The 'launchapplication1' key.
        /// </summary>
        LaunchApplication1 = 138,

        /// <summary>
        /// The 'launchapplication2' key.
        /// </summary>
        LaunchApplication2 = 139,

        /// <summary>
        /// The 'oem1'/'semicolon' key, to the right of the 'L' key on a standard US layout.
        /// </summary>
        Oem1 = OemSemicolon,

        /// <summary>
        /// The 'oem1'/'semicolon' key, to the right of the 'L' key on a standard US layout.
        /// </summary>
        OemSemicolon = 140,

        /// <summary>
        /// The 'oemplus'/'Equals' key, to the left of the backspace key on a standard US layout.
        /// </summary>
        OemPlus = 141,

        /// <summary>
        /// The 'oemcomma' key, to the right of the 'M' key on a standard US layout.
        /// </summary>
        OemComma = 142,

        /// <summary>
        /// The 'oemminus' key, to the right of the number row '0' key on a standard US layout.
        /// </summary>
        OemMinus = 143,

        /// <summary>
        /// The 'oemperiod' key, right between 'M' and the right shift key on a standard US layout.
        /// </summary>
        OemPeriod = 144,

        /// <summary>
        /// The 'oemquestion'/'oem2' key, next to 'right shift' on a standard US layout.
        /// </summary>
        Oem2 = OemQuestion,

        /// <summary>
        /// The 'oemquestion'/'oem2' key, next to 'right shift' on a standard US layout.
        /// </summary>
        OemQuestion = 145,

        /// <summary>
        /// The 'oemtilde'/'oem3' key, above 'tab' on a standard US layout.
        /// </summary>
        Oem3 = OemTilde,

        /// <summary>
        /// The 'oemtilde'/'oem3' key, above 'tab' on a standard US layout.
        /// </summary>
        OemTilde = 146,

        /// <summary>
        /// The 'OemOpenBrackets'/'oem4' key, to the right of 'P' on a standard US layout.
        /// </summary>
        Oem4 = OemOpenBrackets,

        /// <summary>
        /// The 'OemOpenBrackets'/'oem4' key, to the right of 'P' on a standard US layout.
        /// </summary>
        OemOpenBrackets = 149,

        /// <summary>
        /// The 'OemPipe'/'oem5' key, either at the lower left of the 'return' key for ISO or between 'return' and 'backspace' for ANSI on a standard US layout.
        /// </summary>
        Oem5 = OemPipe,

        /// <summary>
        /// The 'OemPipe'/'oem5' key, either at the lower left of the 'return' key for ISO or between 'return' and 'backspace' for ANSI on a standard US layout.
        /// </summary>
        OemPipe = 150,

        /// <summary>
        /// The 'OemCloseBrackets'/'oem6' key, second key to the right of 'P' on a standard US layout.
        /// </summary>
        Oem6 = OemCloseBrackets,

        /// <summary>
        /// The 'OemCloseBrackets'/'oem6' key, second key to the right of 'P' on a standard US layout.
        /// </summary>
        OemCloseBrackets = 151,

        /// <summary>
        /// The 'OemQuotes'/'oem7' key, second key to the right of 'L' on a standard US layout.
        /// </summary>
        Oem7 = OemQuotes,

        /// <summary>
        /// The 'OemQuotes'/'oem7' key, second key to the right of 'L' on a standard US layout.
        /// </summary>
        OemQuotes = 152,

        /// <summary>
        /// The 'oem8' key, on a UK layout the left most key on the number row, same position as <see cref="OemTilde"/> on US layout.
        /// </summary>
        Oem8 = 153,

        /// <summary>
        /// The 'OemBackSlash'/'oem102' key, on US ISO keyboards this key sits between left shift and 'Z'.
        /// </summary>
        Oem102 = OemBackslash,

        /// <summary>
        /// The 'OemBackSlash'/'oem102' key, on US ISO keyboards this key sits between left shift and 'Z'.
        /// </summary>
        OemBackslash = 154,

        /// <summary>
        /// The 'attn' key.
        /// </summary>
        Attn = 163,

        /// <summary>
        /// The 'crsel' key.
        /// </summary>
        CrSel = 164,

        /// <summary>
        /// The 'exsel' key.
        /// </summary>
        ExSel = 165,

        /// <summary>
        /// The 'eraseeof' key.
        /// </summary>
        EraseEof = 166,

        /// <summary>
        /// The 'play' key.
        /// </summary>
        Play = 167,

        /// <summary>
        /// The 'zoom' key.
        /// </summary>
        Zoom = 168,

        /// <summary>
        /// A windows-reserved key.
        /// </summary>
        NoName = 169,

        /// <summary>
        /// The 'PA1' or 'Program Action'/'Program Attention' 1 key on a 3270 IBM keyboard.
        /// </summary>
        Pa1 = 170,

        /// <summary>
        /// The 'oemclear' key.
        /// </summary>
        OemClear = 171,

        /// <summary>
        /// The numeric keypad 'enter' key in SDL and RawInput, Winforms forwards it to <see cref="Return"/> instead.
        /// </summary>
        NumPadEnter = 180,

        /// <summary>
        /// The numeric keypad 'decimal' key.
        /// </summary>
        [System.Obsolete($"Use {nameof(Decimal)} instead")] NumPadDecimal = 181,
    }
}