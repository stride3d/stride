// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if XENKO_PLATFORM_WINDOWS_DESKTOP && (XENKO_UI_WINFORMS || XENKO_UI_WPF)
using System.Collections.Generic;
using System.Runtime.InteropServices;
using WinFormsKeys = System.Windows.Forms.Keys;

namespace Xenko.Input
{
    /// <summary>
    /// Mapping between <see cref="WinFormsKeys"/> and <see cref="Xenko.Input.Keys"/> needed for
    /// translating Winform key events into Xenko ones.
    /// </summary>
    internal static class WinKeys
    {
        [DllImport("user32.dll")]
        internal static extern int MapVirtualKey(int uCode, uint uMapType);

        /// <summary>
        /// Map between Winform keys and Xenko keys.
        /// </summary>
        internal static readonly Dictionary<WinFormsKeys, Keys> MapKeys = NewMapKeys();

#pragma warning disable SA1310 // Field names should not contain underscore
        public const uint MAPVK_VK_TO_VSC = 0x00;
        public const uint MAPVK_VSC_TO_VK = 0x01;
        public const uint MAPVK_VK_TO_CHAR = 0x02;
        public const uint MAPVK_VSC_TO_VK_EX = 0x03;
        public const uint MAPVK_VK_TO_VSC_EX = 0x04;
#pragma warning restore SA1310 // Field names should not contain underscore

        /// <summary>
        /// Create a mapping between <see cref="WinFormsKeys"/> and <see cref="Xenko.Input.Keys"/>
        /// </summary>
        /// <returns>A new map.</returns>
        private static Dictionary<WinFormsKeys, Keys> NewMapKeys()
        {
            var map = new Dictionary<WinFormsKeys, Keys>(200);
            map[WinFormsKeys.None] = Keys.None;
            map[WinFormsKeys.Cancel] = Keys.Cancel;
            map[WinFormsKeys.Back] = Keys.Back;
            map[WinFormsKeys.Tab] = Keys.Tab;
            map[WinFormsKeys.LineFeed] = Keys.LineFeed;
            map[WinFormsKeys.Clear] = Keys.Clear;
            map[WinFormsKeys.Enter] = Keys.Enter;
            map[WinFormsKeys.Return] = Keys.Return;
            map[WinFormsKeys.Pause] = Keys.Pause;
            map[WinFormsKeys.Capital] = Keys.Capital;
            map[WinFormsKeys.CapsLock] = Keys.CapsLock;
            map[WinFormsKeys.HangulMode] = Keys.HangulMode;
            map[WinFormsKeys.KanaMode] = Keys.KanaMode;
            map[WinFormsKeys.JunjaMode] = Keys.JunjaMode;
            map[WinFormsKeys.FinalMode] = Keys.FinalMode;
            map[WinFormsKeys.HanjaMode] = Keys.HanjaMode;
            map[WinFormsKeys.KanjiMode] = Keys.KanjiMode;
            map[WinFormsKeys.Escape] = Keys.Escape;
            map[WinFormsKeys.IMEConvert] = Keys.ImeConvert;
            map[WinFormsKeys.IMENonconvert] = Keys.ImeNonConvert;
            map[WinFormsKeys.IMEAccept] = Keys.ImeAccept;
            map[WinFormsKeys.IMEModeChange] = Keys.ImeModeChange;
            map[WinFormsKeys.Space] = Keys.Space;
            map[WinFormsKeys.PageUp] = Keys.PageUp;
            map[WinFormsKeys.Prior] = Keys.Prior;
            map[WinFormsKeys.Next] = Keys.Next;
            map[WinFormsKeys.PageDown] = Keys.PageDown;
            map[WinFormsKeys.End] = Keys.End;
            map[WinFormsKeys.Home] = Keys.Home;
            map[WinFormsKeys.Left] = Keys.Left;
            map[WinFormsKeys.Up] = Keys.Up;
            map[WinFormsKeys.Right] = Keys.Right;
            map[WinFormsKeys.Down] = Keys.Down;
            map[WinFormsKeys.Select] = Keys.Select;
            map[WinFormsKeys.Print] = Keys.Print;
            map[WinFormsKeys.Execute] = Keys.Execute;
            map[WinFormsKeys.PrintScreen] = Keys.PrintScreen;
            map[WinFormsKeys.Snapshot] = Keys.Snapshot;
            map[WinFormsKeys.Insert] = Keys.Insert;
            map[WinFormsKeys.Delete] = Keys.Delete;
            map[WinFormsKeys.Help] = Keys.Help;
            map[WinFormsKeys.D0] = Keys.D0;
            map[WinFormsKeys.D1] = Keys.D1;
            map[WinFormsKeys.D2] = Keys.D2;
            map[WinFormsKeys.D3] = Keys.D3;
            map[WinFormsKeys.D4] = Keys.D4;
            map[WinFormsKeys.D5] = Keys.D5;
            map[WinFormsKeys.D6] = Keys.D6;
            map[WinFormsKeys.D7] = Keys.D7;
            map[WinFormsKeys.D8] = Keys.D8;
            map[WinFormsKeys.D9] = Keys.D9;
            map[WinFormsKeys.A] = Keys.A;
            map[WinFormsKeys.B] = Keys.B;
            map[WinFormsKeys.C] = Keys.C;
            map[WinFormsKeys.D] = Keys.D;
            map[WinFormsKeys.E] = Keys.E;
            map[WinFormsKeys.F] = Keys.F;
            map[WinFormsKeys.G] = Keys.G;
            map[WinFormsKeys.H] = Keys.H;
            map[WinFormsKeys.I] = Keys.I;
            map[WinFormsKeys.J] = Keys.J;
            map[WinFormsKeys.K] = Keys.K;
            map[WinFormsKeys.L] = Keys.L;
            map[WinFormsKeys.M] = Keys.M;
            map[WinFormsKeys.N] = Keys.N;
            map[WinFormsKeys.O] = Keys.O;
            map[WinFormsKeys.P] = Keys.P;
            map[WinFormsKeys.Q] = Keys.Q;
            map[WinFormsKeys.R] = Keys.R;
            map[WinFormsKeys.S] = Keys.S;
            map[WinFormsKeys.T] = Keys.T;
            map[WinFormsKeys.U] = Keys.U;
            map[WinFormsKeys.V] = Keys.V;
            map[WinFormsKeys.W] = Keys.W;
            map[WinFormsKeys.X] = Keys.X;
            map[WinFormsKeys.Y] = Keys.Y;
            map[WinFormsKeys.Z] = Keys.Z;
            map[WinFormsKeys.LWin] = Keys.LeftWin;
            map[WinFormsKeys.RWin] = Keys.RightWin;
            map[WinFormsKeys.Apps] = Keys.Apps;
            map[WinFormsKeys.Sleep] = Keys.Sleep;
            map[WinFormsKeys.NumPad0] = Keys.NumPad0;
            map[WinFormsKeys.NumPad1] = Keys.NumPad1;
            map[WinFormsKeys.NumPad2] = Keys.NumPad2;
            map[WinFormsKeys.NumPad3] = Keys.NumPad3;
            map[WinFormsKeys.NumPad4] = Keys.NumPad4;
            map[WinFormsKeys.NumPad5] = Keys.NumPad5;
            map[WinFormsKeys.NumPad6] = Keys.NumPad6;
            map[WinFormsKeys.NumPad7] = Keys.NumPad7;
            map[WinFormsKeys.NumPad8] = Keys.NumPad8;
            map[WinFormsKeys.NumPad9] = Keys.NumPad9;
            map[WinFormsKeys.Multiply] = Keys.Multiply;
            map[WinFormsKeys.Add] = Keys.Add;
            map[WinFormsKeys.Separator] = Keys.Separator;
            map[WinFormsKeys.Subtract] = Keys.Subtract;
            map[WinFormsKeys.Decimal] = Keys.Decimal;
            map[WinFormsKeys.Divide] = Keys.Divide;
            map[WinFormsKeys.F1] = Keys.F1;
            map[WinFormsKeys.F2] = Keys.F2;
            map[WinFormsKeys.F3] = Keys.F3;
            map[WinFormsKeys.F4] = Keys.F4;
            map[WinFormsKeys.F5] = Keys.F5;
            map[WinFormsKeys.F6] = Keys.F6;
            map[WinFormsKeys.F7] = Keys.F7;
            map[WinFormsKeys.F8] = Keys.F8;
            map[WinFormsKeys.F9] = Keys.F9;
            map[WinFormsKeys.F10] = Keys.F10;
            map[WinFormsKeys.F11] = Keys.F11;
            map[WinFormsKeys.F12] = Keys.F12;
            map[WinFormsKeys.F13] = Keys.F13;
            map[WinFormsKeys.F14] = Keys.F14;
            map[WinFormsKeys.F15] = Keys.F15;
            map[WinFormsKeys.F16] = Keys.F16;
            map[WinFormsKeys.F17] = Keys.F17;
            map[WinFormsKeys.F18] = Keys.F18;
            map[WinFormsKeys.F19] = Keys.F19;
            map[WinFormsKeys.F20] = Keys.F20;
            map[WinFormsKeys.F21] = Keys.F21;
            map[WinFormsKeys.F22] = Keys.F22;
            map[WinFormsKeys.F23] = Keys.F23;
            map[WinFormsKeys.F24] = Keys.F24;
            map[WinFormsKeys.NumLock] = Keys.NumLock;
            map[WinFormsKeys.Scroll] = Keys.Scroll;
            map[WinFormsKeys.LShiftKey] = Keys.LeftShift;
            map[WinFormsKeys.RShiftKey] = Keys.RightShift;
            map[WinFormsKeys.LControlKey] = Keys.LeftCtrl;
            map[WinFormsKeys.RControlKey] = Keys.RightCtrl;
            map[WinFormsKeys.LMenu] = Keys.LeftAlt;
            map[WinFormsKeys.RMenu] = Keys.RightAlt;
            map[WinFormsKeys.BrowserBack] = Keys.BrowserBack;
            map[WinFormsKeys.BrowserForward] = Keys.BrowserForward;
            map[WinFormsKeys.BrowserRefresh] = Keys.BrowserRefresh;
            map[WinFormsKeys.BrowserStop] = Keys.BrowserStop;
            map[WinFormsKeys.BrowserSearch] = Keys.BrowserSearch;
            map[WinFormsKeys.BrowserFavorites] = Keys.BrowserFavorites;
            map[WinFormsKeys.BrowserHome] = Keys.BrowserHome;
            map[WinFormsKeys.VolumeMute] = Keys.VolumeMute;
            map[WinFormsKeys.VolumeDown] = Keys.VolumeDown;
            map[WinFormsKeys.VolumeUp] = Keys.VolumeUp;
            map[WinFormsKeys.MediaNextTrack] = Keys.MediaNextTrack;
            map[WinFormsKeys.MediaPreviousTrack] = Keys.MediaPreviousTrack;
            map[WinFormsKeys.MediaStop] = Keys.MediaStop;
            map[WinFormsKeys.MediaPlayPause] = Keys.MediaPlayPause;
            map[WinFormsKeys.LaunchMail] = Keys.LaunchMail;
            map[WinFormsKeys.SelectMedia] = Keys.SelectMedia;
            map[WinFormsKeys.LaunchApplication1] = Keys.LaunchApplication1;
            map[WinFormsKeys.LaunchApplication2] = Keys.LaunchApplication2;
            map[WinFormsKeys.Oem1] = Keys.Oem1;
            map[WinFormsKeys.OemSemicolon] = Keys.OemSemicolon;
            map[WinFormsKeys.Oemplus] = Keys.OemPlus;
            map[WinFormsKeys.Oemcomma] = Keys.OemComma;
            map[WinFormsKeys.OemMinus] = Keys.OemMinus;
            map[WinFormsKeys.OemPeriod] = Keys.OemPeriod;
            map[WinFormsKeys.Oem2] = Keys.Oem2;
            map[WinFormsKeys.OemQuestion] = Keys.OemQuestion;
            map[WinFormsKeys.Oem3] = Keys.Oem3;
            map[WinFormsKeys.Oemtilde] = Keys.OemTilde;
            map[WinFormsKeys.Oem4] = Keys.Oem4;
            map[WinFormsKeys.OemOpenBrackets] = Keys.OemOpenBrackets;
            map[WinFormsKeys.Oem5] = Keys.Oem5;
            map[WinFormsKeys.OemPipe] = Keys.OemPipe;
            map[WinFormsKeys.Oem6] = Keys.Oem6;
            map[WinFormsKeys.OemCloseBrackets] = Keys.OemCloseBrackets;
            map[WinFormsKeys.Oem7] = Keys.Oem7;
            map[WinFormsKeys.OemQuotes] = Keys.OemQuotes;
            map[WinFormsKeys.Oem8] = Keys.Oem8;
            map[WinFormsKeys.Oem102] = Keys.Oem102;
            map[WinFormsKeys.OemBackslash] = Keys.OemBackslash;
            map[WinFormsKeys.Attn] = Keys.Attn;
            map[WinFormsKeys.Crsel] = Keys.CrSel;
            map[WinFormsKeys.Exsel] = Keys.ExSel;
            map[WinFormsKeys.EraseEof] = Keys.EraseEof;
            map[WinFormsKeys.Play] = Keys.Play;
            map[WinFormsKeys.Zoom] = Keys.Zoom;
            map[WinFormsKeys.NoName] = Keys.NoName;
            map[WinFormsKeys.Pa1] = Keys.Pa1;
            map[WinFormsKeys.OemClear] = Keys.OemClear;
            return map;
        }
    }
}

#endif
