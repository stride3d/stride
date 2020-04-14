// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_UWP
using System;
using Windows.UI.Core;
using System.Collections.Generic;
using Windows.System;

namespace Stride.Input
{
    internal class KeyboardUWP : KeyboardDeviceBase, IDisposable
    {
        private static readonly Dictionary<VirtualKey, Keys> mapKeys;

        private readonly CoreWindow uiControl;

        static KeyboardUWP()
        {
            mapKeys = new Dictionary<VirtualKey, Keys>();
            // this dictionary was built from Desktop version (VirtualKey are compatible with WinForms keys)
            AddKeys(WinFormsKeys.None, Keys.None);
            AddKeys(WinFormsKeys.Cancel, Keys.Cancel);
            AddKeys(WinFormsKeys.Back, Keys.Back);
            AddKeys(WinFormsKeys.Tab, Keys.Tab);
            AddKeys(WinFormsKeys.LineFeed, Keys.LineFeed);
            AddKeys(WinFormsKeys.Clear, Keys.Clear);
            AddKeys(WinFormsKeys.Enter, Keys.Enter);
            AddKeys(WinFormsKeys.Return, Keys.Return);
            AddKeys(WinFormsKeys.Pause, Keys.Pause);
            AddKeys(WinFormsKeys.Capital, Keys.Capital);
            AddKeys(WinFormsKeys.CapsLock, Keys.CapsLock);
            AddKeys(WinFormsKeys.HangulMode, Keys.HangulMode);
            AddKeys(WinFormsKeys.KanaMode, Keys.KanaMode);
            AddKeys(WinFormsKeys.JunjaMode, Keys.JunjaMode);
            AddKeys(WinFormsKeys.FinalMode, Keys.FinalMode);
            AddKeys(WinFormsKeys.HanjaMode, Keys.HanjaMode);
            AddKeys(WinFormsKeys.KanjiMode, Keys.KanjiMode);
            AddKeys(WinFormsKeys.Escape, Keys.Escape);
            AddKeys(WinFormsKeys.IMEConvert, Keys.ImeConvert);
            AddKeys(WinFormsKeys.IMENonconvert, Keys.ImeNonConvert);
            AddKeys(WinFormsKeys.IMEAccept, Keys.ImeAccept);
            AddKeys(WinFormsKeys.IMEModeChange, Keys.ImeModeChange);
            AddKeys(WinFormsKeys.Space, Keys.Space);
            AddKeys(WinFormsKeys.PageUp, Keys.PageUp);
            AddKeys(WinFormsKeys.Prior, Keys.Prior);
            AddKeys(WinFormsKeys.Next, Keys.Next);
            AddKeys(WinFormsKeys.PageDown, Keys.PageDown);
            AddKeys(WinFormsKeys.End, Keys.End);
            AddKeys(WinFormsKeys.Home, Keys.Home);
            AddKeys(WinFormsKeys.Left, Keys.Left);
            AddKeys(WinFormsKeys.Up, Keys.Up);
            AddKeys(WinFormsKeys.Right, Keys.Right);
            AddKeys(WinFormsKeys.Down, Keys.Down);
            AddKeys(WinFormsKeys.Select, Keys.Select);
            AddKeys(WinFormsKeys.Print, Keys.Print);
            AddKeys(WinFormsKeys.Execute, Keys.Execute);
            AddKeys(WinFormsKeys.PrintScreen, Keys.PrintScreen);
            AddKeys(WinFormsKeys.Snapshot, Keys.Snapshot);
            AddKeys(WinFormsKeys.Insert, Keys.Insert);
            AddKeys(WinFormsKeys.Delete, Keys.Delete);
            AddKeys(WinFormsKeys.Help, Keys.Help);
            AddKeys(WinFormsKeys.D0, Keys.D0);
            AddKeys(WinFormsKeys.D1, Keys.D1);
            AddKeys(WinFormsKeys.D2, Keys.D2);
            AddKeys(WinFormsKeys.D3, Keys.D3);
            AddKeys(WinFormsKeys.D4, Keys.D4);
            AddKeys(WinFormsKeys.D5, Keys.D5);
            AddKeys(WinFormsKeys.D6, Keys.D6);
            AddKeys(WinFormsKeys.D7, Keys.D7);
            AddKeys(WinFormsKeys.D8, Keys.D8);
            AddKeys(WinFormsKeys.D9, Keys.D9);
            AddKeys(WinFormsKeys.A, Keys.A);
            AddKeys(WinFormsKeys.B, Keys.B);
            AddKeys(WinFormsKeys.C, Keys.C);
            AddKeys(WinFormsKeys.D, Keys.D);
            AddKeys(WinFormsKeys.E, Keys.E);
            AddKeys(WinFormsKeys.F, Keys.F);
            AddKeys(WinFormsKeys.G, Keys.G);
            AddKeys(WinFormsKeys.H, Keys.H);
            AddKeys(WinFormsKeys.I, Keys.I);
            AddKeys(WinFormsKeys.J, Keys.J);
            AddKeys(WinFormsKeys.K, Keys.K);
            AddKeys(WinFormsKeys.L, Keys.L);
            AddKeys(WinFormsKeys.M, Keys.M);
            AddKeys(WinFormsKeys.N, Keys.N);
            AddKeys(WinFormsKeys.O, Keys.O);
            AddKeys(WinFormsKeys.P, Keys.P);
            AddKeys(WinFormsKeys.Q, Keys.Q);
            AddKeys(WinFormsKeys.R, Keys.R);
            AddKeys(WinFormsKeys.S, Keys.S);
            AddKeys(WinFormsKeys.T, Keys.T);
            AddKeys(WinFormsKeys.U, Keys.U);
            AddKeys(WinFormsKeys.V, Keys.V);
            AddKeys(WinFormsKeys.W, Keys.W);
            AddKeys(WinFormsKeys.X, Keys.X);
            AddKeys(WinFormsKeys.Y, Keys.Y);
            AddKeys(WinFormsKeys.Z, Keys.Z);
            AddKeys(WinFormsKeys.LWin, Keys.LeftWin);
            AddKeys(WinFormsKeys.RWin, Keys.RightWin);
            AddKeys(WinFormsKeys.Apps, Keys.Apps);
            AddKeys(WinFormsKeys.Sleep, Keys.Sleep);
            AddKeys(WinFormsKeys.NumPad0, Keys.NumPad0);
            AddKeys(WinFormsKeys.NumPad1, Keys.NumPad1);
            AddKeys(WinFormsKeys.NumPad2, Keys.NumPad2);
            AddKeys(WinFormsKeys.NumPad3, Keys.NumPad3);
            AddKeys(WinFormsKeys.NumPad4, Keys.NumPad4);
            AddKeys(WinFormsKeys.NumPad5, Keys.NumPad5);
            AddKeys(WinFormsKeys.NumPad6, Keys.NumPad6);
            AddKeys(WinFormsKeys.NumPad7, Keys.NumPad7);
            AddKeys(WinFormsKeys.NumPad8, Keys.NumPad8);
            AddKeys(WinFormsKeys.NumPad9, Keys.NumPad9);
            AddKeys(WinFormsKeys.Multiply, Keys.Multiply);
            AddKeys(WinFormsKeys.Add, Keys.Add);
            AddKeys(WinFormsKeys.Separator, Keys.Separator);
            AddKeys(WinFormsKeys.Subtract, Keys.Subtract);
            AddKeys(WinFormsKeys.Decimal, Keys.Decimal);
            AddKeys(WinFormsKeys.Divide, Keys.Divide);
            AddKeys(WinFormsKeys.F1, Keys.F1);
            AddKeys(WinFormsKeys.F2, Keys.F2);
            AddKeys(WinFormsKeys.F3, Keys.F3);
            AddKeys(WinFormsKeys.F4, Keys.F4);
            AddKeys(WinFormsKeys.F5, Keys.F5);
            AddKeys(WinFormsKeys.F6, Keys.F6);
            AddKeys(WinFormsKeys.F7, Keys.F7);
            AddKeys(WinFormsKeys.F8, Keys.F8);
            AddKeys(WinFormsKeys.F9, Keys.F9);
            AddKeys(WinFormsKeys.F10, Keys.F10);
            AddKeys(WinFormsKeys.F11, Keys.F11);
            AddKeys(WinFormsKeys.F12, Keys.F12);
            AddKeys(WinFormsKeys.F13, Keys.F13);
            AddKeys(WinFormsKeys.F14, Keys.F14);
            AddKeys(WinFormsKeys.F15, Keys.F15);
            AddKeys(WinFormsKeys.F16, Keys.F16);
            AddKeys(WinFormsKeys.F17, Keys.F17);
            AddKeys(WinFormsKeys.F18, Keys.F18);
            AddKeys(WinFormsKeys.F19, Keys.F19);
            AddKeys(WinFormsKeys.F20, Keys.F20);
            AddKeys(WinFormsKeys.F21, Keys.F21);
            AddKeys(WinFormsKeys.F22, Keys.F22);
            AddKeys(WinFormsKeys.F23, Keys.F23);
            AddKeys(WinFormsKeys.F24, Keys.F24);
            AddKeys(WinFormsKeys.NumLock, Keys.NumLock);
            AddKeys(WinFormsKeys.Scroll, Keys.Scroll);
            AddKeys(WinFormsKeys.LShiftKey, Keys.LeftShift);
            AddKeys(WinFormsKeys.RShiftKey, Keys.RightShift);
            AddKeys(WinFormsKeys.LControlKey, Keys.LeftCtrl);
            AddKeys(WinFormsKeys.RControlKey, Keys.RightCtrl);
            AddKeys(WinFormsKeys.LMenu, Keys.LeftAlt);
            AddKeys(WinFormsKeys.RMenu, Keys.RightAlt);
            AddKeys(WinFormsKeys.BrowserBack, Keys.BrowserBack);
            AddKeys(WinFormsKeys.BrowserForward, Keys.BrowserForward);
            AddKeys(WinFormsKeys.BrowserRefresh, Keys.BrowserRefresh);
            AddKeys(WinFormsKeys.BrowserStop, Keys.BrowserStop);
            AddKeys(WinFormsKeys.BrowserSearch, Keys.BrowserSearch);
            AddKeys(WinFormsKeys.BrowserFavorites, Keys.BrowserFavorites);
            AddKeys(WinFormsKeys.BrowserHome, Keys.BrowserHome);
            AddKeys(WinFormsKeys.VolumeMute, Keys.VolumeMute);
            AddKeys(WinFormsKeys.VolumeDown, Keys.VolumeDown);
            AddKeys(WinFormsKeys.VolumeUp, Keys.VolumeUp);
            AddKeys(WinFormsKeys.MediaNextTrack, Keys.MediaNextTrack);
            AddKeys(WinFormsKeys.MediaPreviousTrack, Keys.MediaPreviousTrack);
            AddKeys(WinFormsKeys.MediaStop, Keys.MediaStop);
            AddKeys(WinFormsKeys.MediaPlayPause, Keys.MediaPlayPause);
            AddKeys(WinFormsKeys.LaunchMail, Keys.LaunchMail);
            AddKeys(WinFormsKeys.SelectMedia, Keys.SelectMedia);
            AddKeys(WinFormsKeys.LaunchApplication1, Keys.LaunchApplication1);
            AddKeys(WinFormsKeys.LaunchApplication2, Keys.LaunchApplication2);
            AddKeys(WinFormsKeys.Oem1, Keys.Oem1);
            AddKeys(WinFormsKeys.OemSemicolon, Keys.OemSemicolon);
            AddKeys(WinFormsKeys.Oemplus, Keys.OemPlus);
            AddKeys(WinFormsKeys.Oemcomma, Keys.OemComma);
            AddKeys(WinFormsKeys.OemMinus, Keys.OemMinus);
            AddKeys(WinFormsKeys.OemPeriod, Keys.OemPeriod);
            AddKeys(WinFormsKeys.Oem2, Keys.Oem2);
            AddKeys(WinFormsKeys.OemQuestion, Keys.OemQuestion);
            AddKeys(WinFormsKeys.Oem3, Keys.Oem3);
            AddKeys(WinFormsKeys.Oemtilde, Keys.OemTilde);
            AddKeys(WinFormsKeys.Oem4, Keys.Oem4);
            AddKeys(WinFormsKeys.OemOpenBrackets, Keys.OemOpenBrackets);
            AddKeys(WinFormsKeys.Oem5, Keys.Oem5);
            AddKeys(WinFormsKeys.OemPipe, Keys.OemPipe);
            AddKeys(WinFormsKeys.Oem6, Keys.Oem6);
            AddKeys(WinFormsKeys.OemCloseBrackets, Keys.OemCloseBrackets);
            AddKeys(WinFormsKeys.Oem7, Keys.Oem7);
            AddKeys(WinFormsKeys.OemQuotes, Keys.OemQuotes);
            AddKeys(WinFormsKeys.Oem8, Keys.Oem8);
            AddKeys(WinFormsKeys.Oem102, Keys.Oem102);
            AddKeys(WinFormsKeys.OemBackslash, Keys.OemBackslash);
            AddKeys(WinFormsKeys.Attn, Keys.Attn);
            AddKeys(WinFormsKeys.Crsel, Keys.CrSel);
            AddKeys(WinFormsKeys.Exsel, Keys.ExSel);
            AddKeys(WinFormsKeys.EraseEof, Keys.EraseEof);
            AddKeys(WinFormsKeys.Play, Keys.Play);
            AddKeys(WinFormsKeys.Zoom, Keys.Zoom);
            AddKeys(WinFormsKeys.NoName, Keys.NoName);
            AddKeys(WinFormsKeys.Pa1, Keys.Pa1);
            AddKeys(WinFormsKeys.OemClear, Keys.OemClear);
        }

        public KeyboardUWP(InputSourceUWP source, CoreWindow uiControl)
        {
            Source = source;
            this.uiControl = uiControl;
            uiControl.KeyDown += CoreWindowOnKeyDown;
            uiControl.KeyUp += CoreWindowOnKeyUp;
        }

        public void Dispose()
        {
            uiControl.KeyDown -= CoreWindowOnKeyDown;
            uiControl.KeyUp -= CoreWindowOnKeyUp;
        }

        public override IInputSource Source { get; }

        public override string Name { get; } = "UWP Keyboard";

        public override Guid Id { get; } = new Guid("2296CBDF-46EA-4D68-B8E5-5900C25C774D");

        private static void AddKeys(WinFormsKeys fromKey, Keys toKey)
        {
            if (!mapKeys.ContainsKey((VirtualKey)fromKey))
            {
                mapKeys.Add((VirtualKey)fromKey, toKey);
            }
        }

        private void HandleKey(bool isDown, VirtualKey virtualKey, CorePhysicalKeyStatus keyStatus)
        {
            // Remap certain keys
            switch (virtualKey)
            {
                case VirtualKey.Shift:
                    // Only way to differentiate left and right shift is through the scan code
                    virtualKey = keyStatus.ScanCode == 54 ? VirtualKey.RightShift : VirtualKey.LeftShift;
                    break;
                case VirtualKey.Control:
                    virtualKey = keyStatus.IsExtendedKey ? VirtualKey.RightControl : VirtualKey.LeftControl;
                    break;
                case VirtualKey.Menu:
                    virtualKey = keyStatus.IsExtendedKey ? VirtualKey.RightMenu : VirtualKey.LeftMenu;
                    break;
            }

            // Let Alt + F4 go through
            if (virtualKey == VirtualKey.F4 && this.IsKeyDown(Keys.LeftAlt))
                return;

            // Map key to stride key
            Keys strideKey;
            if (!mapKeys.TryGetValue(virtualKey, out strideKey))
                return;

            if (isDown)
            {
                HandleKeyDown(strideKey);
            }
            else
            {
                HandleKeyUp(strideKey);
            }
        }
        
        private void CoreWindowOnKeyDown(CoreWindow sender, KeyEventArgs args)
        {
            HandleKey(true, args.VirtualKey, args.KeyStatus);
        }

        private void CoreWindowOnKeyUp(CoreWindow sender, KeyEventArgs args)
        {
            HandleKey(false, args.VirtualKey, args.KeyStatus);
        }
    }
}
#endif
