// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_ANDROID

using System;
using Android.Views;
using Stride.Games.Android;
using Keycode = Android.Views.Keycode;

namespace Stride.Input
{
    internal class KeyboardAndroid : KeyboardDeviceBase, IDisposable
    {
        private readonly AndroidStrideGameView gameView;

        public KeyboardAndroid(InputSourceAndroid source, AndroidStrideGameView gameView)
        {
            Source = source;
            this.gameView = gameView;
            var listener = new Listener(this);
            gameView.SetOnKeyListener(listener);
        }

        public override string Name => "Android Keyboard";

        public override Guid Id => new Guid("98468e4a-2895-4f87-b750-5ffe2dd943ae");

        public override IInputSource Source { get; }

        public void Dispose()
        {
            gameView.SetOnKeyListener(null);
        }

        protected class Listener : Java.Lang.Object, View.IOnKeyListener
        {
            private readonly KeyboardAndroid keyboard;

            public Listener(KeyboardAndroid keyboard)
            {
                this.keyboard = keyboard;
            }

            public bool OnKey(View v, Keycode keyCode, Android.Views.KeyEvent e)
            {
                var strideKey = ConvertKeyFromAndroid(keyCode);

                if (e.Action == KeyEventActions.Down)
                {
                    keyboard.HandleKeyDown(strideKey);
                }
                else
                {
                    keyboard.HandleKeyUp(strideKey);
                }

                return true;
            }

            private Keys ConvertKeyFromAndroid(Keycode key)
            {
                switch (key)
                {
                    case Keycode.Num0:
                        return Keys.D0;
                    case Keycode.Num1:
                        return Keys.D1;
                    case Keycode.Num2:
                        return Keys.D2;
                    case Keycode.Num3:
                        return Keys.D3;
                    case Keycode.Num4:
                        return Keys.D4;
                    case Keycode.Num5:
                        return Keys.D5;
                    case Keycode.Num6:
                        return Keys.D6;
                    case Keycode.Num7:
                        return Keys.D7;
                    case Keycode.Num8:
                        return Keys.D8;
                    case Keycode.Num9:
                        return Keys.D9;
                    case Keycode.A:
                        return Keys.A;
                    case Keycode.B:
                        return Keys.B;
                    case Keycode.C:
                        return Keys.C;
                    case Keycode.D:
                        return Keys.D;
                    case Keycode.E:
                        return Keys.E;
                    case Keycode.F:
                        return Keys.F;
                    case Keycode.G:
                        return Keys.G;
                    case Keycode.H:
                        return Keys.H;
                    case Keycode.I:
                        return Keys.I;
                    case Keycode.J:
                        return Keys.J;
                    case Keycode.K:
                        return Keys.K;
                    case Keycode.L:
                        return Keys.L;
                    case Keycode.M:
                        return Keys.M;
                    case Keycode.N:
                        return Keys.N;
                    case Keycode.O:
                        return Keys.O;
                    case Keycode.P:
                        return Keys.P;
                    case Keycode.Q:
                        return Keys.Q;
                    case Keycode.R:
                        return Keys.R;
                    case Keycode.S:
                        return Keys.S;
                    case Keycode.T:
                        return Keys.T;
                    case Keycode.U:
                        return Keys.U;
                    case Keycode.V:
                        return Keys.V;
                    case Keycode.W:
                        return Keys.W;
                    case Keycode.X:
                        return Keys.X;
                    case Keycode.Y:
                        return Keys.Y;
                    case Keycode.Z:
                        return Keys.Z;
                    case Keycode.AltLeft:
                        return Keys.LeftAlt;
                    case Keycode.AltRight:
                        return Keys.RightAlt;
                    case Keycode.ShiftLeft:
                        return Keys.LeftShift;
                    case Keycode.ShiftRight:
                        return Keys.RightShift;
                    case Keycode.Enter:
                        return Keys.Enter;
                    case Keycode.Back:
                        return Keys.Back;
                    case Keycode.Tab:
                        return Keys.Tab;
                    case Keycode.Del:
                        return Keys.Delete;
                    case Keycode.PageUp:
                        return Keys.PageUp;
                    case Keycode.PageDown:
                        return Keys.PageDown;
                    case Keycode.DpadUp:
                        return Keys.Up;
                    case Keycode.DpadDown:
                        return Keys.Down;
                    case Keycode.DpadLeft:
                        return Keys.Right;
                    case Keycode.DpadRight:
                        return Keys.Right;
                    case Keycode.CapsLock:
                        return Keys.CapsLock;
                    case Keycode.Backslash:
                        return Keys.OemBackslash;
                    case Keycode.Clear:
                        return Keys.Clear;
                    case Keycode.Comma:
                        return Keys.OemComma;
                    case Keycode.CtrlLeft:
                        return Keys.LeftCtrl;
                    case Keycode.CtrlRight:
                        return Keys.RightCtrl;
                    case Keycode.Escape:
                        return Keys.Escape;
                    case Keycode.F1:
                        return Keys.F1;
                    case Keycode.F2:
                        return Keys.F2;
                    case Keycode.F3:
                        return Keys.F3;
                    case Keycode.F4:
                        return Keys.F4;
                    case Keycode.F5:
                        return Keys.F5;
                    case Keycode.F6:
                        return Keys.F6;
                    case Keycode.F7:
                        return Keys.F7;
                    case Keycode.F8:
                        return Keys.F8;
                    case Keycode.F9:
                        return Keys.F9;
                    case Keycode.F10:
                        return Keys.F10;
                    case Keycode.F11:
                        return Keys.F11;
                    case Keycode.F12:
                        return Keys.F12;
                    case Keycode.Home:
                        return Keys.Home;
                    case Keycode.Insert:
                        return Keys.Insert;
                    case Keycode.Kana:
                        return Keys.KanaMode;
                    case Keycode.Minus:
                        return Keys.OemMinus;
                    case Keycode.Mute:
                        return Keys.VolumeMute;
                    case Keycode.NumLock:
                        return Keys.NumLock;
                    case Keycode.Numpad0:
                        return Keys.NumPad0;
                    case Keycode.Numpad1:
                        return Keys.NumPad1;
                    case Keycode.Numpad2:
                        return Keys.NumPad2;
                    case Keycode.Numpad3:
                        return Keys.NumPad3;
                    case Keycode.Numpad4:
                        return Keys.NumPad4;
                    case Keycode.Numpad5:
                        return Keys.NumPad5;
                    case Keycode.Numpad6:
                        return Keys.NumPad6;
                    case Keycode.Numpad7:
                        return Keys.NumPad7;
                    case Keycode.Numpad8:
                        return Keys.NumPad8;
                    case Keycode.Numpad9:
                        return Keys.NumPad9;
                    case Keycode.NumpadAdd:
                        return Keys.Add;
                    case Keycode.NumpadComma:
                        return Keys.OemComma;
                    case Keycode.NumpadDivide:
                        return Keys.Divide;
                    case Keycode.NumpadDot:
                        return Keys.NumPadDecimal;
                    case Keycode.NumpadEnter:
                        return Keys.NumPadEnter;
                    case Keycode.NumpadMultiply:
                        return Keys.Multiply;
                    case Keycode.NumpadSubtract:
                        return Keys.Subtract;
                    case Keycode.Period:
                        return Keys.OemPeriod;
                    case Keycode.Plus:
                        return Keys.OemPlus;
                    case Keycode.LeftBracket:
                        return Keys.OemOpenBrackets;
                    case Keycode.RightBracket:
                        return Keys.OemCloseBrackets;
                    case Keycode.Semicolon:
                        return Keys.OemSemicolon;
                    case Keycode.Sleep:
                        return Keys.Sleep;
                    case Keycode.Space:
                        return Keys.Space;
                    case Keycode.Star:
                        return Keys.Multiply;
                    case Keycode.VolumeDown:
                        return Keys.VolumeDown;
                    case Keycode.VolumeMute:
                        return Keys.VolumeMute;
                    case Keycode.VolumeUp:
                        return Keys.VolumeUp;
                    case Keycode.K11:
                    case Keycode.K12:
                    case Keycode.ThreeDMode:
                    case Keycode.Apostrophe:
                    case Keycode.AppSwitch:
                    case Keycode.Assist:
                    case Keycode.At:
                    case Keycode.AvrInput:
                    case Keycode.AvrPower:
                    case Keycode.Bookmark:
                    case Keycode.Break:
                    case Keycode.BrightnessDown:
                    case Keycode.BrightnessUp:
                    case Keycode.Button1:
                    case Keycode.Button2:
                    case Keycode.Button3:
                    case Keycode.Button4:
                    case Keycode.Button5:
                    case Keycode.Button6:
                    case Keycode.Button7:
                    case Keycode.Button8:
                    case Keycode.Button9:
                    case Keycode.Button10:
                    case Keycode.Button11:
                    case Keycode.Button12:
                    case Keycode.Button13:
                    case Keycode.Button14:
                    case Keycode.Button15:
                    case Keycode.Button16:
                    case Keycode.ButtonA:
                    case Keycode.ButtonB:
                    case Keycode.ButtonC:
                    case Keycode.ButtonL1:
                    case Keycode.ButtonL2:
                    case Keycode.ButtonMode:
                    case Keycode.ButtonR1:
                    case Keycode.ButtonR2:
                    case Keycode.ButtonSelect:
                    case Keycode.ButtonStart:
                    case Keycode.ButtonThumbl:
                    case Keycode.ButtonThumbr:
                    case Keycode.ButtonX:
                    case Keycode.ButtonY:
                    case Keycode.ButtonZ:
                    case Keycode.Calculator:
                    case Keycode.Calendar:
                    case Keycode.Call:
                    case Keycode.Camera:
                    case Keycode.Captions:
                    case Keycode.ChannelDown:
                    case Keycode.ChannelUp:
                    case Keycode.Contacts:
                    case Keycode.DpadCenter:
                    case Keycode.Dvr:
                    case Keycode.Eisu:
                    case Keycode.Endcall:
                    case Keycode.Envelope:
                    case Keycode.Equals:
                    case Keycode.Explorer:
                    case Keycode.Focus:
                    case Keycode.Forward:
                    case Keycode.ForwardDel:
                    case Keycode.Function:
                    case Keycode.Grave:
                    case Keycode.Guide:
                    case Keycode.Headsethook:
                    case Keycode.Help:
                    case Keycode.Henkan:
                    case Keycode.Info:
                    case Keycode.KatakanaHiragana:
                    case Keycode.LanguageSwitch:
                    case Keycode.LastChannel:
                    case Keycode.MannerMode:
                    case Keycode.MediaAudioTrack:
                    case Keycode.MediaClose:
                    case Keycode.MediaEject:
                    case Keycode.MediaFastForward:
                    case Keycode.MediaNext:
                    case Keycode.MediaPause:
                    case Keycode.MediaPlay:
                    case Keycode.MediaPlayPause:
                    case Keycode.MediaPrevious:
                    case Keycode.MediaRecord:
                    case Keycode.MediaRewind:
                    case Keycode.MediaStop:
                    case Keycode.MediaTopMenu:
                    case Keycode.Menu:
                    case Keycode.MetaLeft:
                    case Keycode.MetaRight:
                    case Keycode.MoveEnd:
                    case Keycode.MoveHome:
                    case Keycode.Muhenkan:
                    case Keycode.Music:
                    case Keycode.Notification:
                    case Keycode.Num:
                    case Keycode.NumpadEquals:
                    case Keycode.NumpadLeftParen:
                    case Keycode.NumpadRightParen:
                    case Keycode.Pairing:
                    case Keycode.Pictsymbols:
                    case Keycode.Pound:
                    case Keycode.Power:
                    case Keycode.ProgBlue:
                    case Keycode.ProgGreen:
                    case Keycode.ProgRed:
                    case Keycode.ProgYellow:
                    case Keycode.Ro:
                    case Keycode.ScrollLock:
                    case Keycode.Search:
                    case Keycode.Settings:
                    case Keycode.Slash:
                    case Keycode.SoftLeft:
                    case Keycode.SoftRight:
                    case Keycode.StbInput:
                    case Keycode.StbPower:
                    case Keycode.SwitchCharset:
                    case Keycode.Sym:
                    case Keycode.Sysrq:
                    case Keycode.Tv:
                    case Keycode.TvAntennaCable:
                    case Keycode.TvAudioDescription:
                    case Keycode.TvAudioDescriptionMixDown:
                    case Keycode.TvAudioDescriptionMixUp:
                    case Keycode.TvContentsMenu:
                    case Keycode.TvDataService:
                    case Keycode.TvInput:
                    case Keycode.TvInputComponent1:
                    case Keycode.TvInputComponent2:
                    case Keycode.TvInputComposite1:
                    case Keycode.TvInputComposite2:
                    case Keycode.TvInputHdmi1:
                    case Keycode.TvInputHdmi2:
                    case Keycode.TvInputHdmi3:
                    case Keycode.TvInputHdmi4:
                    case Keycode.TvInputVga1:
                    case Keycode.TvMediaContextMenu:
                    case Keycode.TvNetwork:
                    case Keycode.TvNumberEntry:
                    case Keycode.TvPower:
                    case Keycode.TvRadioService:
                    case Keycode.TvSatellite:
                    case Keycode.TvSatelliteBs:
                    case Keycode.TvSatelliteCs:
                    case Keycode.TvSatelliteService:
                    case Keycode.TvTeletext:
                    case Keycode.TvTerrestrialAnalog:
                    case Keycode.TvTerrestrialDigital:
                    case Keycode.TvTimerProgramming:
                    case Keycode.TvZoomMode:
                    case Keycode.Unknown:
                    case Keycode.VoiceAssist:
                    case Keycode.Wakeup:
                    case Keycode.Window:
                    case Keycode.Yen:
                    case Keycode.ZenkakuHankaku:
                    case Keycode.ZoomIn:
                    case Keycode.ZoomOut:
                    default:
                        return (Keys)(-1);
                }
            }
        }
    }
}

#endif
