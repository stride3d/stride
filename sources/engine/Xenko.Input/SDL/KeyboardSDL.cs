// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if XENKO_UI_SDL
using System;
using System.Collections.Generic;
using System.Text;
using SDL2;
using Xenko.Graphics.SDL;

namespace Xenko.Input
{
    internal class KeyboardSDL : KeyboardDeviceBase, ITextInputDevice, IDisposable
    {
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
        }
        
        public void Dispose()
        {
            window.KeyDownActions -= OnKeyEvent;
            window.KeyUpActions -= OnKeyEvent;
        }

        public override string Name => "SDL Keyboard";

        public override Guid Id => new Guid("a25469ad-804e-4713-82da-347c6b187323");

        public override IInputSource Source { get; }

        public override void Update(List<InputEvent> inputEvents)
        {
            base.Update(inputEvents);

            inputEvents.AddRange(textEvents);
            textEvents.Clear();
        }

        public void EnabledTextInput()
        {
            SDL.SDL_StartTextInput();
        }

        public void DisableTextInput()
        {
            SDL.SDL_StopTextInput();
        }

        private void OnKeyEvent(SDL.SDL_KeyboardEvent e)
        {
            // Try to map to a xenko key
            Keys key;
            if (SDLKeys.MapKeys.TryGetValue(e.keysym.sym, out key) && key != Keys.None)
            {
                if (e.type == SDL.SDL_EventType.SDL_KEYDOWN)
                    HandleKeyDown(key);
                else
                    HandleKeyUp(key);
            }
        }

        private unsafe void OnTextEditingActions(SDL.SDL_TextEditingEvent e)
        {
            var textInputEvent = InputEventPool<TextInputEvent>.GetOrCreate(this);
            textInputEvent.Text = SDLBufferToString(e.text);
            textInputEvent.Type = TextInputEventType.Composition;
            textInputEvent.CompositionStart = e.start;
            textInputEvent.CompositionLength = e.length;
            textEvents.Add(textInputEvent);
        }

        private unsafe void OnTextInputActions(SDL.SDL_TextInputEvent e)
        {
            var textInputEvent = InputEventPool<TextInputEvent>.GetOrCreate(this);
            textInputEvent.Text = SDLBufferToString(e.text);
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
        /// Mapping between <see cref="SDL.SDL_Keycode"/> and <see cref="Xenko.Input.Keys"/> needed for
        /// translating SDL key events into Xenko ones.
        /// </summary>
        private static class SDLKeys
        {
            /// <summary>
            /// Map between SDL keys and Xenko keys.
            /// </summary>
            internal static readonly Dictionary<SDL.SDL_Keycode, Keys> MapKeys = NewMapKeys();

            /// <summary>
            /// Create a mapping between <see cref="SDL.SDL_Keycode"/> and <see cref="Xenko.Input.Keys"/>
            /// </summary>
            /// <remarks>Not all <see cref="Xenko.Input.Keys"/> have a corresponding SDL entries. For the moment they are commented out in the code below.</remarks>
            /// <returns>A new map.</returns>
            private static Dictionary<SDL.SDL_Keycode, Keys> NewMapKeys()
            {
                var map = new Dictionary<SDL.SDL_Keycode, Keys>(200);
                map[SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.None;
                map[SDL.SDL_Keycode.SDLK_CANCEL] = Keys.Cancel;
                map[SDL.SDL_Keycode.SDLK_BACKSPACE] = Keys.Back;
                map[SDL.SDL_Keycode.SDLK_TAB] = Keys.Tab;
                map[SDL.SDL_Keycode.SDLK_KP_TAB] = Keys.Tab;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.LineFeed;
                map[SDL.SDL_Keycode.SDLK_CLEAR] = Keys.Clear;
                map[SDL.SDL_Keycode.SDLK_CLEARAGAIN] = Keys.Clear;
                map[SDL.SDL_Keycode.SDLK_KP_CLEAR] = Keys.Clear;
                map[SDL.SDL_Keycode.SDLK_KP_CLEARENTRY] = Keys.Clear;
                map[SDL.SDL_Keycode.SDLK_KP_ENTER] = Keys.Enter;
                map[SDL.SDL_Keycode.SDLK_RETURN] = Keys.Return;
                map[SDL.SDL_Keycode.SDLK_RETURN2] = Keys.Return;
                map[SDL.SDL_Keycode.SDLK_PAUSE] = Keys.Pause;
                map[SDL.SDL_Keycode.SDLK_CAPSLOCK] = Keys.Capital;
                //            map [SDL.SDL_Keycode.SDLK_CAPSLOCK] = Keys.CapsLock;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.HangulMode;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.KanaMode;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.JunjaMode;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.FinalMode;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.HanjaMode;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.KanjiMode;
                map[SDL.SDL_Keycode.SDLK_ESCAPE] = Keys.Escape;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.ImeConvert;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.ImeNonConvert;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.ImeAccept;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.ImeModeChange;
                map[SDL.SDL_Keycode.SDLK_SPACE] = Keys.Space;
                map[SDL.SDL_Keycode.SDLK_KP_SPACE] = Keys.Space;
                map[SDL.SDL_Keycode.SDLK_PAGEUP] = Keys.PageUp;
                map[SDL.SDL_Keycode.SDLK_PRIOR] = Keys.Prior;
                //            map [SDL.SDL_Keycode.SDLK_PAGEDOWN] = Keys.Next); // Next is the same as PageDo;
                map[SDL.SDL_Keycode.SDLK_PAGEDOWN] = Keys.PageDown;
                map[SDL.SDL_Keycode.SDLK_END] = Keys.End;
                map[SDL.SDL_Keycode.SDLK_HOME] = Keys.Home;
                map[SDL.SDL_Keycode.SDLK_AC_HOME] = Keys.Home;
                map[SDL.SDL_Keycode.SDLK_LEFT] = Keys.Left;
                map[SDL.SDL_Keycode.SDLK_UP] = Keys.Up;
                map[SDL.SDL_Keycode.SDLK_RIGHT] = Keys.Right;
                map[SDL.SDL_Keycode.SDLK_DOWN] = Keys.Down;
                map[SDL.SDL_Keycode.SDLK_SELECT] = Keys.Select;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.Print;
                map[SDL.SDL_Keycode.SDLK_EXECUTE] = Keys.Execute;
                map[SDL.SDL_Keycode.SDLK_PRINTSCREEN] = Keys.PrintScreen;
                //            map [SDL.SDL_Keycode.SDLK_PRINTSCREEN] = Keys.Snapshot); // Snapshot is the same as PageDo;
                map[SDL.SDL_Keycode.SDLK_INSERT] = Keys.Insert;
                map[SDL.SDL_Keycode.SDLK_DELETE] = Keys.Delete;
                map[SDL.SDL_Keycode.SDLK_HELP] = Keys.Help;
                map[SDL.SDL_Keycode.SDLK_0] = Keys.D0;
                map[SDL.SDL_Keycode.SDLK_1] = Keys.D1;
                map[SDL.SDL_Keycode.SDLK_2] = Keys.D2;
                map[SDL.SDL_Keycode.SDLK_3] = Keys.D3;
                map[SDL.SDL_Keycode.SDLK_4] = Keys.D4;
                map[SDL.SDL_Keycode.SDLK_5] = Keys.D5;
                map[SDL.SDL_Keycode.SDLK_6] = Keys.D6;
                map[SDL.SDL_Keycode.SDLK_7] = Keys.D7;
                map[SDL.SDL_Keycode.SDLK_8] = Keys.D8;
                map[SDL.SDL_Keycode.SDLK_9] = Keys.D9;
                map[SDL.SDL_Keycode.SDLK_a] = Keys.A;
                map[SDL.SDL_Keycode.SDLK_b] = Keys.B;
                map[SDL.SDL_Keycode.SDLK_c] = Keys.C;
                map[SDL.SDL_Keycode.SDLK_d] = Keys.D;
                map[SDL.SDL_Keycode.SDLK_e] = Keys.E;
                map[SDL.SDL_Keycode.SDLK_f] = Keys.F;
                map[SDL.SDL_Keycode.SDLK_g] = Keys.G;
                map[SDL.SDL_Keycode.SDLK_h] = Keys.H;
                map[SDL.SDL_Keycode.SDLK_i] = Keys.I;
                map[SDL.SDL_Keycode.SDLK_j] = Keys.J;
                map[SDL.SDL_Keycode.SDLK_k] = Keys.K;
                map[SDL.SDL_Keycode.SDLK_l] = Keys.L;
                map[SDL.SDL_Keycode.SDLK_m] = Keys.M;
                map[SDL.SDL_Keycode.SDLK_n] = Keys.N;
                map[SDL.SDL_Keycode.SDLK_o] = Keys.O;
                map[SDL.SDL_Keycode.SDLK_p] = Keys.P;
                map[SDL.SDL_Keycode.SDLK_q] = Keys.Q;
                map[SDL.SDL_Keycode.SDLK_r] = Keys.R;
                map[SDL.SDL_Keycode.SDLK_s] = Keys.S;
                map[SDL.SDL_Keycode.SDLK_t] = Keys.T;
                map[SDL.SDL_Keycode.SDLK_u] = Keys.U;
                map[SDL.SDL_Keycode.SDLK_v] = Keys.V;
                map[SDL.SDL_Keycode.SDLK_w] = Keys.W;
                map[SDL.SDL_Keycode.SDLK_x] = Keys.X;
                map[SDL.SDL_Keycode.SDLK_y] = Keys.Y;
                map[SDL.SDL_Keycode.SDLK_z] = Keys.Z;
                map[SDL.SDL_Keycode.SDLK_LGUI] = Keys.LeftWin;
                map[SDL.SDL_Keycode.SDLK_RGUI] = Keys.RightWin;
                map[SDL.SDL_Keycode.SDLK_APPLICATION] = Keys.Apps; // TODO: Verify value
                map[SDL.SDL_Keycode.SDLK_SLEEP] = Keys.Sleep;
                map[SDL.SDL_Keycode.SDLK_KP_0] = Keys.NumPad0;
                map[SDL.SDL_Keycode.SDLK_KP_1] = Keys.NumPad1;
                map[SDL.SDL_Keycode.SDLK_KP_2] = Keys.NumPad2;
                map[SDL.SDL_Keycode.SDLK_KP_3] = Keys.NumPad3;
                map[SDL.SDL_Keycode.SDLK_KP_4] = Keys.NumPad4;
                map[SDL.SDL_Keycode.SDLK_KP_5] = Keys.NumPad5;
                map[SDL.SDL_Keycode.SDLK_KP_6] = Keys.NumPad6;
                map[SDL.SDL_Keycode.SDLK_KP_7] = Keys.NumPad7;
                map[SDL.SDL_Keycode.SDLK_KP_8] = Keys.NumPad8;
                map[SDL.SDL_Keycode.SDLK_KP_9] = Keys.NumPad9;
                map[SDL.SDL_Keycode.SDLK_KP_MULTIPLY] = Keys.Multiply;
                map[SDL.SDL_Keycode.SDLK_PLUS] = Keys.OemPlus;
                map[SDL.SDL_Keycode.SDLK_KP_PLUS] = Keys.Add;
                map[SDL.SDL_Keycode.SDLK_SEPARATOR] = Keys.Separator;
                map[SDL.SDL_Keycode.SDLK_MINUS] = Keys.OemMinus;
                map[SDL.SDL_Keycode.SDLK_KP_MINUS] = Keys.Subtract;
                map[SDL.SDL_Keycode.SDLK_DECIMALSEPARATOR] = Keys.Decimal;
                map[SDL.SDL_Keycode.SDLK_KP_DECIMAL] = Keys.Decimal;
                map[SDL.SDL_Keycode.SDLK_KP_DIVIDE] = Keys.Divide;
                map[SDL.SDL_Keycode.SDLK_F1] = Keys.F1;
                map[SDL.SDL_Keycode.SDLK_F2] = Keys.F2;
                map[SDL.SDL_Keycode.SDLK_F3] = Keys.F3;
                map[SDL.SDL_Keycode.SDLK_F4] = Keys.F4;
                map[SDL.SDL_Keycode.SDLK_F5] = Keys.F5;
                map[SDL.SDL_Keycode.SDLK_F6] = Keys.F6;
                map[SDL.SDL_Keycode.SDLK_F7] = Keys.F7;
                map[SDL.SDL_Keycode.SDLK_F8] = Keys.F8;
                map[SDL.SDL_Keycode.SDLK_F9] = Keys.F9;
                map[SDL.SDL_Keycode.SDLK_F10] = Keys.F10;
                map[SDL.SDL_Keycode.SDLK_F11] = Keys.F11;
                map[SDL.SDL_Keycode.SDLK_F12] = Keys.F12;
                map[SDL.SDL_Keycode.SDLK_F13] = Keys.F13;
                map[SDL.SDL_Keycode.SDLK_F14] = Keys.F14;
                map[SDL.SDL_Keycode.SDLK_F15] = Keys.F15;
                map[SDL.SDL_Keycode.SDLK_F16] = Keys.F16;
                map[SDL.SDL_Keycode.SDLK_F17] = Keys.F17;
                map[SDL.SDL_Keycode.SDLK_F18] = Keys.F18;
                map[SDL.SDL_Keycode.SDLK_F19] = Keys.F19;
                map[SDL.SDL_Keycode.SDLK_F20] = Keys.F20;
                map[SDL.SDL_Keycode.SDLK_F21] = Keys.F21;
                map[SDL.SDL_Keycode.SDLK_F22] = Keys.F22;
                map[SDL.SDL_Keycode.SDLK_F23] = Keys.F23;
                map[SDL.SDL_Keycode.SDLK_F24] = Keys.F24;
                map[SDL.SDL_Keycode.SDLK_NUMLOCKCLEAR] = Keys.NumLock;
                map[SDL.SDL_Keycode.SDLK_SCROLLLOCK] = Keys.Scroll;
                map[SDL.SDL_Keycode.SDLK_LSHIFT] = Keys.LeftShift;
                map[SDL.SDL_Keycode.SDLK_RSHIFT] = Keys.RightShift;
                map[SDL.SDL_Keycode.SDLK_LCTRL] = Keys.LeftCtrl;
                map[SDL.SDL_Keycode.SDLK_RCTRL] = Keys.RightCtrl;
                map[SDL.SDL_Keycode.SDLK_LALT] = Keys.LeftAlt;
                map[SDL.SDL_Keycode.SDLK_RALT] = Keys.RightAlt;
                map[SDL.SDL_Keycode.SDLK_AC_BACK] = Keys.BrowserBack;
                map[SDL.SDL_Keycode.SDLK_AC_FORWARD] = Keys.BrowserForward;
                map[SDL.SDL_Keycode.SDLK_AC_REFRESH] = Keys.BrowserRefresh;
                map[SDL.SDL_Keycode.SDLK_AC_STOP] = Keys.BrowserStop;
                map[SDL.SDL_Keycode.SDLK_AC_SEARCH] = Keys.BrowserSearch;
                map[SDL.SDL_Keycode.SDLK_AC_BOOKMARKS] = Keys.BrowserFavorites;
                map[SDL.SDL_Keycode.SDLK_AC_HOME] = Keys.BrowserHome;
                map[SDL.SDL_Keycode.SDLK_AUDIOMUTE] = Keys.VolumeMute;
                map[SDL.SDL_Keycode.SDLK_VOLUMEDOWN] = Keys.VolumeDown;
                map[SDL.SDL_Keycode.SDLK_VOLUMEUP] = Keys.VolumeUp;
                map[SDL.SDL_Keycode.SDLK_AUDIONEXT] = Keys.MediaNextTrack;
                map[SDL.SDL_Keycode.SDLK_AUDIOPREV] = Keys.MediaPreviousTrack;
                map[SDL.SDL_Keycode.SDLK_AUDIOSTOP] = Keys.MediaStop;
                map[SDL.SDL_Keycode.SDLK_AUDIOPLAY] = Keys.MediaPlayPause;
                map[SDL.SDL_Keycode.SDLK_MAIL] = Keys.LaunchMail;
                map[SDL.SDL_Keycode.SDLK_MEDIASELECT] = Keys.SelectMedia;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.LaunchApplication1;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.LaunchApplication2;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.Oem1;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.OemSemicolon;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.OemComma;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.OemPeriod;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.Oem2;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.OemQuestion;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.Oem3;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.OemTilde;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.Oem4;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.OemOpenBrackets;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.Oem5;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.OemPipe;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.Oem6;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.OemCloseBrackets;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.Oem7;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.OemQuotes;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.Oem8;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.Oem102;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.OemBackslash;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.Attn;
                map[SDL.SDL_Keycode.SDLK_CRSEL] = Keys.CrSel;
                map[SDL.SDL_Keycode.SDLK_EXSEL] = Keys.ExSel;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.EraseEof;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.Play;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.Zoom;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.NoName;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.Pa1;
                //            map [SDL.SDL_Keycode.SDLK_UNKNOWN] = Keys.OemClear;
                return map;
            }
        }
    }
}
#endif
