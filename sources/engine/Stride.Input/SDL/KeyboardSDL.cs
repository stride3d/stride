// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_UI_SDL
using System;
using System.Collections.Generic;
using System.Text;
using Silk.NET.SDL;
using Stride.Graphics.SDL;
using Window = Stride.Graphics.SDL.Window;

namespace Stride.Input
{
    internal class KeyboardSDL : KeyboardDeviceBase, ITextInputDevice, IDisposable
    {
        private static Sdl SDL = Window.SDL;

        private readonly Window window;
        private readonly List<TextInputEvent> textEvents = new List<TextInputEvent>();

        public KeyboardSDL(InputSourceSDL source, Window window)
        {
            Source = source;
            this.window = window;
            this.window.KeyDownActions += OnKeyEvent;
            this.window.KeyUpActions += OnKeyEvent;
            this.window.TextInputActions += OnTextInputActions;
            this.window.TextEditingActions += OnTextEditingActions;

            Id = InputDeviceUtils.DeviceNameToGuid(window.SdlHandle.ToString() + Name);
        }

        public void Dispose()
        {
            window.KeyDownActions -= OnKeyEvent;
            window.KeyUpActions -= OnKeyEvent;
        }

        public override string Name => "SDL Keyboard";

        public override Guid Id { get; }

        public override IInputSource Source { get; }

        public override void Update(List<InputEvent> inputEvents)
        {
            base.Update(inputEvents);

            inputEvents.AddRange(textEvents);
            textEvents.Clear();
        }

        public void EnabledTextInput()
        {
            SDL.StartTextInput();
        }

        public void DisableTextInput()
        {
            SDL.StopTextInput();
        }

        private void OnKeyEvent(KeyboardEvent e)
        {
            // Try to map to a stride key
            Keys key = SDLKeys.MapKey((KeyCode)e.Keysym.Sym, e.Keysym.Scancode);
            if (key != Keys.None)
            {
                if ((EventType)e.Type == EventType.Keydown)
                    HandleKeyDown(key);
                else
                    HandleKeyUp(key);
            }
        }

        private unsafe void OnTextEditingActions(TextEditingEvent e)
        {
            var textInputEvent = InputEventPool<TextInputEvent>.GetOrCreate(this);
            textInputEvent.Text = SDLBufferToString(e.Text);
            textInputEvent.Type = TextInputEventType.Composition;
            textInputEvent.CompositionStart = e.Start;
            textInputEvent.CompositionLength = e.Length;
            textEvents.Add(textInputEvent);
        }

        private unsafe void OnTextInputActions(Silk.NET.SDL.TextInputEvent e)
        {
            var textInputEvent = InputEventPool<TextInputEvent>.GetOrCreate(this);
            textInputEvent.Text = SDLBufferToString(e.Text);
            textInputEvent.Type = TextInputEventType.Input;
            textEvents.Add(textInputEvent);
        }

        private unsafe string SDLBufferToString(byte* text, int size = 32)
        {
            byte[] sourceBytes = new byte[size];
            int length = 0;

            for (int i = 0; i < size; i++)
            {
                if (text[i] == 0)
                    break;

                sourceBytes[i] = text[i];
                length++;
            }

            return Encoding.UTF8.GetString(sourceBytes, 0, length);
        }

        /// <summary>
        /// Mapping between <see cref="KeyCode"/> and <see cref="Stride.Input.Keys"/> needed for
        /// translating SDL key events into Stride ones.
        /// </summary>
        private static class SDLKeys
        {
            internal static Keys MapKey(KeyCode input, Scancode scancode)
            {
                // Resources: http://kbdlayout.info/kbdusx/overview+virtualkeys
                //            https://wiki.libsdl.org/SDL_Keycode
                //            http://kbdedit.com/manual/low_level_vk_list.html
                switch(input)
                {
                    case KeyCode.KUnknown: return Keys.None;
                    case KeyCode.KCancel: return Keys.Cancel;
                    case KeyCode.KKPBackspace: case KeyCode.KBackspace: return Keys.Back;
                    case KeyCode.KTab: case KeyCode.KKPTab: return Keys.Tab;
                    //            KeyCode.KUnknown: return Keys.LineFeed;
                    case KeyCode.KClear: case KeyCode.KClearagain: case KeyCode.KKPClear: case KeyCode.KKPClearentry: return Keys.Clear;
                    case KeyCode.KReturn: case KeyCode.KReturn2: return Keys.Return;
                    case KeyCode.KPause: return Keys.Pause;
                    //            KeyCode.KCapslock: return Keys.Capital; // Capital is the same as CapsLock
                    case KeyCode.KCapslock: return Keys.CapsLock;
                    //            KeyCode.KUnknown: return Keys.HangulMode;
                    //            KeyCode.KUnknown: return Keys.KanaMode;
                    //            KeyCode.KUnknown: return Keys.JunjaMode;
                    //            KeyCode.KUnknown: return Keys.FinalMode;
                    //            KeyCode.KUnknown: return Keys.HanjaMode;
                    //            KeyCode.KUnknown: return Keys.KanjiMode;
                    case KeyCode.KEscape: return Keys.Escape;
                    //            KeyCode.KUnknown: return Keys.ImeConvert;
                    //            KeyCode.KUnknown: return Keys.ImeNonConvert;
                    //            KeyCode.KUnknown: return Keys.ImeAccept;
                    //            KeyCode.KUnknown: return Keys.ImeModeChange;
                    case KeyCode.KSpace: case KeyCode.KKPSpace: return Keys.Space;
                    case KeyCode.KPageup: return Keys.PageUp;
                    case KeyCode.KPrior: return Keys.Prior;
                    //            KeyCode.KPagedown: return Keys.Next; // Next is the same as PageDown
                    case KeyCode.KPagedown: return Keys.PageDown;
                    case KeyCode.KEnd: return Keys.End;
                    case KeyCode.KHome: return Keys.Home;
                    case KeyCode.KLeft: return Keys.Left;
                    case KeyCode.KUp: return Keys.Up;
                    case KeyCode.KRight: return Keys.Right;
                    case KeyCode.KDown: return Keys.Down;
                    case KeyCode.KSelect: return Keys.Select;
                    //            KeyCode.KUnknown: return Keys.Print;
                    case KeyCode.KExecute: return Keys.Execute;
                    case KeyCode.KPrintscreen: return Keys.PrintScreen;
                    //            KeyCode.KPrintscreen: return Keys.Snapshot; // Snapshot is the same as PrintScreen
                    case KeyCode.KInsert: return Keys.Insert;
                    case KeyCode.KDelete: return Keys.Delete;
                    case KeyCode.KHelp: return Keys.Help;
                    case KeyCode.K0: return Keys.D0;
                    case KeyCode.K1: return Keys.D1;
                    case KeyCode.K2: return Keys.D2;
                    case KeyCode.K3: return Keys.D3;
                    case KeyCode.K4: return Keys.D4;
                    case KeyCode.K5: return Keys.D5;
                    case KeyCode.K6: return Keys.D6;
                    case KeyCode.K7: return Keys.D7;
                    case KeyCode.K8: return Keys.D8;
                    case KeyCode.K9: return Keys.D9;
                    case KeyCode.KA: return Keys.A;
                    case KeyCode.KB: return Keys.B;
                    case KeyCode.KC: return Keys.C;
                    case KeyCode.KD: return Keys.D;
                    case KeyCode.KE: return Keys.E;
                    case KeyCode.KF: return Keys.F;
                    case KeyCode.KG: return Keys.G;
                    case KeyCode.KH: return Keys.H;
                    case KeyCode.KI: return Keys.I;
                    case KeyCode.KJ: return Keys.J;
                    case KeyCode.KK: return Keys.K;
                    case KeyCode.KL: return Keys.L;
                    case KeyCode.KM: return Keys.M;
                    case KeyCode.KN: return Keys.N;
                    case KeyCode.KO: return Keys.O;
                    case KeyCode.KP: return Keys.P;
                    case KeyCode.KQ: return Keys.Q;
                    case KeyCode.KR: return Keys.R;
                    case KeyCode.KS: return Keys.S;
                    case KeyCode.KT: return Keys.T;
                    case KeyCode.KU: return Keys.U;
                    case KeyCode.KV: return Keys.V;
                    case KeyCode.KW: return Keys.W;
                    case KeyCode.KX: return Keys.X;
                    case KeyCode.KY: return Keys.Y;
                    case KeyCode.KZ: return Keys.Z;
                    case KeyCode.KLgui: return Keys.LeftWin;
                    case KeyCode.KRgui: return Keys.RightWin;
                    case KeyCode.KApplication: return Keys.Apps;
                    case KeyCode.KSleep: return Keys.Sleep;
                    case KeyCode.KKP0: return Keys.NumPad0;
                    case KeyCode.KKP1: return Keys.NumPad1;
                    case KeyCode.KKP2: return Keys.NumPad2;
                    case KeyCode.KKP3: return Keys.NumPad3;
                    case KeyCode.KKP4: return Keys.NumPad4;
                    case KeyCode.KKP5: return Keys.NumPad5;
                    case KeyCode.KKP6: return Keys.NumPad6;
                    case KeyCode.KKP7: return Keys.NumPad7;
                    case KeyCode.KKP8: return Keys.NumPad8;
                    case KeyCode.KKP9: return Keys.NumPad9;
                    case KeyCode.KKPMultiply: return Keys.Multiply;
                    case KeyCode.KPlus/*KPlus is not a physical key*/: case KeyCode.KKPPlus: return Keys.Add;
                    case KeyCode.KSeparator: return Keys.Separator;
                    case KeyCode.KKPMinus: return Keys.Subtract;
                    case KeyCode.KKPComma: case KeyCode.KKPPeriod: case KeyCode.KKPDecimal: return Keys.Decimal;
                    case KeyCode.KThousandsseparator: case KeyCode.KDecimalseparator: return Keys.Decimal; // See ISO/IEC 9995-4
                    case KeyCode.KKPDivide: return Keys.Divide;
                    case KeyCode.KF1: return Keys.F1;
                    case KeyCode.KF2: return Keys.F2;
                    case KeyCode.KF3: return Keys.F3;
                    case KeyCode.KF4: return Keys.F4;
                    case KeyCode.KF5: return Keys.F5;
                    case KeyCode.KF6: return Keys.F6;
                    case KeyCode.KF7: return Keys.F7;
                    case KeyCode.KF8: return Keys.F8;
                    case KeyCode.KF9: return Keys.F9;
                    case KeyCode.KF10: return Keys.F10;
                    case KeyCode.KF11: return Keys.F11;
                    case KeyCode.KF12: return Keys.F12;
                    case KeyCode.KF13: return Keys.F13;
                    case KeyCode.KF14: return Keys.F14;
                    case KeyCode.KF15: return Keys.F15;
                    case KeyCode.KF16: return Keys.F16;
                    case KeyCode.KF17: return Keys.F17;
                    case KeyCode.KF18: return Keys.F18;
                    case KeyCode.KF19: return Keys.F19;
                    case KeyCode.KF20: return Keys.F20;
                    case KeyCode.KF21: return Keys.F21;
                    case KeyCode.KF22: return Keys.F22;
                    case KeyCode.KF23: return Keys.F23;
                    case KeyCode.KF24: return Keys.F24;
                    case KeyCode.KNumlockclear: return Keys.NumLock;
                    case KeyCode.KScrolllock: return Keys.Scroll;
                    case KeyCode.KLshift: return Keys.LeftShift;
                    case KeyCode.KRshift: return Keys.RightShift;
                    case KeyCode.KLctrl: return Keys.LeftCtrl;
                    case KeyCode.KRctrl: return Keys.RightCtrl;
                    case KeyCode.KLalt: return Keys.LeftAlt;
                    case KeyCode.KRalt: return Keys.RightAlt;
                    case KeyCode.KACBack: return Keys.BrowserBack;
                    case KeyCode.KACForward: return Keys.BrowserForward;
                    case KeyCode.KACRefresh: return Keys.BrowserRefresh;
                    case KeyCode.KACStop: return Keys.BrowserStop;
                    case KeyCode.KACSearch: return Keys.BrowserSearch;
                    case KeyCode.KACBookmarks: return Keys.BrowserFavorites;
                    case KeyCode.KACHome: return Keys.BrowserHome;
                    case KeyCode.KAudiomute: return Keys.VolumeMute;
                    case KeyCode.KVolumedown: return Keys.VolumeDown;
                    case KeyCode.KVolumeup: return Keys.VolumeUp;
                    case KeyCode.KAudionext: return Keys.MediaNextTrack;
                    case KeyCode.KAudioprev: return Keys.MediaPreviousTrack;
                    case KeyCode.KAudiostop: return Keys.MediaStop;
                    case KeyCode.KAudioplay: return Keys.MediaPlayPause;
                    case KeyCode.KMail: return Keys.LaunchMail;
                    case KeyCode.KMediaselect: return Keys.SelectMedia;
                    case KeyCode.KApp1: return Keys.LaunchApplication1;
                    case KeyCode.KApp2: return Keys.LaunchApplication2;
                    //            KeyCode.KSemicolon: return Keys.Oem1; // Same as OemSemicolon
                    case KeyCode.KSemicolon: return Keys.OemSemicolon;
                    case KeyCode.KEquals: return Keys.OemPlus;
                    case KeyCode.KComma: return Keys.OemComma;
                    case KeyCode.KMinus: return Keys.OemMinus;
                    case KeyCode.KPeriod: return Keys.OemPeriod;
                    //            KeyCode.KUnknown: return Keys.Oem2; // Same as OemQuestion
                    case KeyCode.KSlash: return Keys.OemQuestion;
                    //            KeyCode.KUnknown: return Keys.Oem3; // Same as OemTilde
                    case KeyCode.KBackquote: return Keys.OemTilde;
                    //            KeyCode.KUnknown: return Keys.Oem4; // Same as OemOpenBrackets
                    case KeyCode.KLeftbracket: return Keys.OemOpenBrackets;
                    //            KeyCode.KUnknown: return Keys.Oem5; // Same as OemPipe
                    case KeyCode.KBackslash when scancode != Scancode.ScancodeNonusbackslash: return Keys.OemPipe; // SDL maps both Oem5 and Oem102 to the same KeyCode; we have to select based on scancode
                    //            KeyCode.KUnknown: return Keys.Oem6; // Same as OemCloseBrackets
                    case KeyCode.KRightbracket: return Keys.OemCloseBrackets;
                    //            KeyCode.KUnknown: return Keys.Oem7; // same as OemQuotes
                    case KeyCode.KQuote: return Keys.OemQuotes;
                    // SDL maps OEM8 to Backquote which is already OEMTilde; this key is often used to open in game consoles and such; I think we should keep it as is
                    // to avoid UK players being unable to access that feature
                    // http://kbdlayout.info/kbdsmsfi
                    // KeyCode.KBackquote: return Keys.Oem8;
                    //            KeyCode.KUnknown: return Keys.Oem102; // same as OemBackslash
                    case KeyCode.KBackslash when scancode == Scancode.ScancodeNonusbackslash: return Keys.OemBackslash;
                    //            KeyCode.KUnknown: return Keys.Attn;
                    case KeyCode.KCrsel: return Keys.CrSel;
                    case KeyCode.KExsel: return Keys.ExSel;
                    //            KeyCode.KUnknown: return Keys.EraseEof;
                    //            KeyCode.KUnknown: return Keys.Play;
                    //            KeyCode.KUnknown: return Keys.Zoom;
                    //            KeyCode.KUnknown: return Keys.NoName;
                    //            KeyCode.KUnknown: return Keys.Pa1;
                    //            KeyCode.KUnknown: return Keys.OemClear;
                    case KeyCode.KKPEnter: return Keys.NumPadEnter;
                    default: return Keys.None;
                }
            }
        }
    }
}
#endif
