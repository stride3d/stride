using System;

namespace Stride.Input;

public class GamePadLayout8BitDo : GamePadLayout
{
    private static readonly ushort _vendorId = 0x2DC8;

    public GamePadLayout8BitDo()
    {
        AddButtonToButton(7, GamePadButton.Start);
        AddButtonToButton(6, GamePadButton.Back);
        AddButtonToButton(8, GamePadButton.LeftThumb);
        AddButtonToButton(9, GamePadButton.RightThumb);
        AddButtonToButton(4, GamePadButton.LeftShoulder);
        AddButtonToButton(5, GamePadButton.RightShoulder);
        AddButtonToButton(0, GamePadButton.A);
        AddButtonToButton(1, GamePadButton.B);
        AddButtonToButton(2, GamePadButton.X);
        AddButtonToButton(3, GamePadButton.Y);
        AddAxisToAxis(0, GamePadAxis.LeftThumbX);
        AddAxisToAxis(1, GamePadAxis.LeftThumbY, true);
        AddAxisToAxis(3, GamePadAxis.RightThumbX);
        AddAxisToAxis(4, GamePadAxis.RightThumbY, true);
        AddAxisToAxis(2, GamePadAxis.LeftTrigger, remap: true);
        AddAxisToAxis(5, GamePadAxis.RightTrigger, remap: true);
    }

    public override bool MatchDevice(IInputSource source, IGameControllerDevice device)
    {
        byte[] guidBytes = device.ProductId.ToByteArray();

        ushort vendorId = BitConverter.ToUInt16(guidBytes, 4);

        return vendorId == _vendorId;
    }
}
