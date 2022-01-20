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
            Keys key;
            if (SDLKeys.MapKeys.TryGetValue((KeyCode)e.Keysym.Sym, out key) && key != Keys.None)
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
            /// <summary>
            /// Map between SDL keys and Stride keys.
            /// </summary>
            internal static readonly Dictionary<KeyCode, Keys> MapKeys = NewMapKeys();

            /// <summary>
            /// Create a mapping between <see cref="KeyCode"/> and <see cref="Stride.Input.Keys"/>
            /// </summary>
            /// <remarks>Not all <see cref="Stride.Input.Keys"/> have a corresponding SDL entries. For the moment they are commented out in the code below.</remarks>
            /// <returns>A new map.</returns>
            private static Dictionary<KeyCode, Keys> NewMapKeys()
            {
                var map = new Dictionary<KeyCode, Keys>(200);
                map[KeyCode.KUnknown] = Keys.None;
                map[KeyCode.KCancel] = Keys.Cancel;
                map[KeyCode.KBackspace] = Keys.Back;
                map[KeyCode.KTab] = Keys.Tab;
                map[KeyCode.KKPTab] = Keys.Tab;
                //            map [KeyCode.KUnknown] = Keys.LineFeed;
                map[KeyCode.KClear] = Keys.Clear;
                map[KeyCode.KClearagain] = Keys.Clear;
                map[KeyCode.KKPClear] = Keys.Clear;
                map[KeyCode.KKPClearentry] = Keys.Clear;
                map[KeyCode.KKPEnter] = Keys.Enter;
                map[KeyCode.KReturn] = Keys.Return;
                map[KeyCode.KReturn2] = Keys.Return;
                map[KeyCode.KPause] = Keys.Pause;
                map[KeyCode.KCapslock] = Keys.Capital;
                //            map [KeyCode.KCapslock] = Keys.CapsLock;
                //            map [KeyCode.KUnknown] = Keys.HangulMode;
                //            map [KeyCode.KUnknown] = Keys.KanaMode;
                //            map [KeyCode.KUnknown] = Keys.JunjaMode;
                //            map [KeyCode.KUnknown] = Keys.FinalMode;
                //            map [KeyCode.KUnknown] = Keys.HanjaMode;
                //            map [KeyCode.KUnknown] = Keys.KanjiMode;
                map[KeyCode.KEscape] = Keys.Escape;
                //            map [KeyCode.KUnknown] = Keys.ImeConvert;
                //            map [KeyCode.KUnknown] = Keys.ImeNonConvert;
                //            map [KeyCode.KUnknown] = Keys.ImeAccept;
                //            map [KeyCode.KUnknown] = Keys.ImeModeChange;
                map[KeyCode.KSpace] = Keys.Space;
                map[KeyCode.KKPSpace] = Keys.Space;
                map[KeyCode.KPageup] = Keys.PageUp;
                map[KeyCode.KPrior] = Keys.Prior;
                //            map [KeyCode.KPagedown] = Keys.Next); // Next is the same as PageDo;
                map[KeyCode.KPagedown] = Keys.PageDown;
                map[KeyCode.KEnd] = Keys.End;
                map[KeyCode.KHome] = Keys.Home;
                map[KeyCode.KACHome] = Keys.Home;
                map[KeyCode.KLeft] = Keys.Left;
                map[KeyCode.KUp] = Keys.Up;
                map[KeyCode.KRight] = Keys.Right;
                map[KeyCode.KDown] = Keys.Down;
                map[KeyCode.KSelect] = Keys.Select;
                //            map [KeyCode.KUnknown] = Keys.Print;
                map[KeyCode.KExecute] = Keys.Execute;
                map[KeyCode.KPrintscreen] = Keys.PrintScreen;
                //            map [KeyCode.KPrintscreen] = Keys.Snapshot); // Snapshot is the same as PageDo;
                map[KeyCode.KInsert] = Keys.Insert;
                map[KeyCode.KDelete] = Keys.Delete;
                map[KeyCode.KHelp] = Keys.Help;
                map[KeyCode.K0] = Keys.D0;
                map[KeyCode.K1] = Keys.D1;
                map[KeyCode.K2] = Keys.D2;
                map[KeyCode.K3] = Keys.D3;
                map[KeyCode.K4] = Keys.D4;
                map[KeyCode.K5] = Keys.D5;
                map[KeyCode.K6] = Keys.D6;
                map[KeyCode.K7] = Keys.D7;
                map[KeyCode.K8] = Keys.D8;
                map[KeyCode.K9] = Keys.D9;
                map[KeyCode.KA] = Keys.A;
                map[KeyCode.KB] = Keys.B;
                map[KeyCode.KC] = Keys.C;
                map[KeyCode.KD] = Keys.D;
                map[KeyCode.KE] = Keys.E;
                map[KeyCode.KF] = Keys.F;
                map[KeyCode.KG] = Keys.G;
                map[KeyCode.KH] = Keys.H;
                map[KeyCode.KI] = Keys.I;
                map[KeyCode.KJ] = Keys.J;
                map[KeyCode.KK] = Keys.K;
                map[KeyCode.KL] = Keys.L;
                map[KeyCode.KM] = Keys.M;
                map[KeyCode.KN] = Keys.N;
                map[KeyCode.KO] = Keys.O;
                map[KeyCode.KP] = Keys.P;
                map[KeyCode.KQ] = Keys.Q;
                map[KeyCode.KR] = Keys.R;
                map[KeyCode.KS] = Keys.S;
                map[KeyCode.KT] = Keys.T;
                map[KeyCode.KU] = Keys.U;
                map[KeyCode.KV] = Keys.V;
                map[KeyCode.KW] = Keys.W;
                map[KeyCode.KX] = Keys.X;
                map[KeyCode.KY] = Keys.Y;
                map[KeyCode.KZ] = Keys.Z;
                map[KeyCode.KLgui] = Keys.LeftWin;
                map[KeyCode.KRgui] = Keys.RightWin;
                map[KeyCode.KApplication] = Keys.Apps; // TODO: Verify value
                map[KeyCode.KSleep] = Keys.Sleep;
                map[KeyCode.KKP0] = Keys.NumPad0;
                map[KeyCode.KKP1] = Keys.NumPad1;
                map[KeyCode.KKP2] = Keys.NumPad2;
                map[KeyCode.KKP3] = Keys.NumPad3;
                map[KeyCode.KKP4] = Keys.NumPad4;
                map[KeyCode.KKP5] = Keys.NumPad5;
                map[KeyCode.KKP6] = Keys.NumPad6;
                map[KeyCode.KKP7] = Keys.NumPad7;
                map[KeyCode.KKP8] = Keys.NumPad8;
                map[KeyCode.KKP9] = Keys.NumPad9;
                map[KeyCode.KKPMultiply] = Keys.Multiply;
                map[KeyCode.KPlus] = Keys.OemPlus;
                map[KeyCode.KKPPlus] = Keys.Add;
                map[KeyCode.KSeparator] = Keys.Separator;
                map[KeyCode.KMinus] = Keys.OemMinus;
                map[KeyCode.KKPMinus] = Keys.Subtract;
                map[KeyCode.KDecimalseparator] = Keys.Decimal;
                map[KeyCode.KKPDecimal] = Keys.NumPadDecimal;
                map[KeyCode.KKPDivide] = Keys.Divide;
                map[KeyCode.KF1] = Keys.F1;
                map[KeyCode.KF2] = Keys.F2;
                map[KeyCode.KF3] = Keys.F3;
                map[KeyCode.KF4] = Keys.F4;
                map[KeyCode.KF5] = Keys.F5;
                map[KeyCode.KF6] = Keys.F6;
                map[KeyCode.KF7] = Keys.F7;
                map[KeyCode.KF8] = Keys.F8;
                map[KeyCode.KF9] = Keys.F9;
                map[KeyCode.KF10] = Keys.F10;
                map[KeyCode.KF11] = Keys.F11;
                map[KeyCode.KF12] = Keys.F12;
                map[KeyCode.KF13] = Keys.F13;
                map[KeyCode.KF14] = Keys.F14;
                map[KeyCode.KF15] = Keys.F15;
                map[KeyCode.KF16] = Keys.F16;
                map[KeyCode.KF17] = Keys.F17;
                map[KeyCode.KF18] = Keys.F18;
                map[KeyCode.KF19] = Keys.F19;
                map[KeyCode.KF20] = Keys.F20;
                map[KeyCode.KF21] = Keys.F21;
                map[KeyCode.KF22] = Keys.F22;
                map[KeyCode.KF23] = Keys.F23;
                map[KeyCode.KF24] = Keys.F24;
                map[KeyCode.KNumlockclear] = Keys.NumLock;
                map[KeyCode.KScrolllock] = Keys.Scroll;
                map[KeyCode.KLshift] = Keys.LeftShift;
                map[KeyCode.KRshift] = Keys.RightShift;
                map[KeyCode.KLctrl] = Keys.LeftCtrl;
                map[KeyCode.KRctrl] = Keys.RightCtrl;
                map[KeyCode.KLalt] = Keys.LeftAlt;
                map[KeyCode.KRalt] = Keys.RightAlt;
                map[KeyCode.KACBack] = Keys.BrowserBack;
                map[KeyCode.KACForward] = Keys.BrowserForward;
                map[KeyCode.KACRefresh] = Keys.BrowserRefresh;
                map[KeyCode.KACStop] = Keys.BrowserStop;
                map[KeyCode.KACSearch] = Keys.BrowserSearch;
                map[KeyCode.KACBookmarks] = Keys.BrowserFavorites;
                map[KeyCode.KACHome] = Keys.BrowserHome;
                map[KeyCode.KAudiomute] = Keys.VolumeMute;
                map[KeyCode.KVolumedown] = Keys.VolumeDown;
                map[KeyCode.KVolumeup] = Keys.VolumeUp;
                map[KeyCode.KAudionext] = Keys.MediaNextTrack;
                map[KeyCode.KAudioprev] = Keys.MediaPreviousTrack;
                map[KeyCode.KAudiostop] = Keys.MediaStop;
                map[KeyCode.KAudioplay] = Keys.MediaPlayPause;
                map[KeyCode.KMail] = Keys.LaunchMail;
                map[KeyCode.KMediaselect] = Keys.SelectMedia;
                //            map [KeyCode.KUnknown] = Keys.LaunchApplication1;
                //            map [KeyCode.KUnknown] = Keys.LaunchApplication2;
                //            map [KeyCode.KUnknown] = Keys.Oem1;
                map[KeyCode.KSemicolon] = Keys.OemSemicolon;
                map[KeyCode.KComma] = Keys.OemComma;
                map[KeyCode.KPeriod] = Keys.OemPeriod;
                // Verified with http://kbdlayout.info/
                map[KeyCode.KKPPeriod] = Keys.NumPadDecimal;
                //            map [KeyCode.KUnknown] = Keys.Oem2;
                map[KeyCode.KSlash] = Keys.OemQuestion;
                //            map [KeyCode.KUnknown] = Keys.Oem3;
                map[KeyCode.KBackquote] = Keys.OemTilde;
                //            map [KeyCode.KUnknown] = Keys.Oem4;
                map[KeyCode.KLeftbracket] = Keys.OemOpenBrackets;
                //            map [KeyCode.KUnknown] = Keys.Oem5;
                //            map [KeyCode.KUnknown] = Keys.OemPipe;
                //            map [KeyCode.KUnknown] = Keys.Oem6;
                map[KeyCode.KRightbracket] = Keys.OemCloseBrackets;
                //            map [KeyCode.KUnknown] = Keys.Oem7;
                map[KeyCode.KQuote] = Keys.OemQuotes;
                //            map [KeyCode.KUnknown] = Keys.Oem8;
                //            map [KeyCode.KUnknown] = Keys.Oem102;
                map[KeyCode.KBackslash] = Keys.OemBackslash;
                //            map [KeyCode.KUnknown] = Keys.Attn;
                map[KeyCode.KCrsel] = Keys.CrSel;
                map[KeyCode.KExsel] = Keys.ExSel;
                //            map [KeyCode.KUnknown] = Keys.EraseEof;
                //            map [KeyCode.KUnknown] = Keys.Play;
                //            map [KeyCode.KUnknown] = Keys.Zoom;
                //            map [KeyCode.KUnknown] = Keys.NoName;
                //            map [KeyCode.KUnknown] = Keys.Pa1;
                map[KeyCode.KClear] = Keys.OemClear;
                return map;
            }
        }
    }
}
#endif
