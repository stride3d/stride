// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Core.Mathematics;
using Stride.Input;

namespace Stride.Engine.Mcp.Tools
{
    [McpServerToolType]
    public sealed class SimulateInputTool
    {
        [McpServerTool(Name = "simulate_input"), Description("Injects simulated input into the running game. Supports keyboard, mouse, and gamepad actions. Press actions (key_press, mouse_click, gamepad_button_press) perform down + wait one frame + up.")]
        public static async Task<string> SimulateInput(
            GameBridge bridge,
            [Description("Action to perform: key_down, key_up, key_press, mouse_move, mouse_down, mouse_up, mouse_click, gamepad_button_down, gamepad_button_up, gamepad_button_press, gamepad_axis")] string action,
            [Description("Key name for keyboard actions (e.g. Space, W, Left, Return)")] string key = null,
            [Description("Mouse button for mouse actions: Left, Right, Middle")] string button = null,
            [Description("Normalized position for mouse_move as 'x,y' (0-1 range)")] string position = null,
            [Description("Gamepad button name (e.g. A, B, X, Y, LeftShoulder, Start, PadUp)")] string gamepadButton = null,
            [Description("Gamepad axis name (e.g. LeftThumbX, LeftThumbY, RightTrigger)")] string gamepadAxis = null,
            [Description("Axis value from -1.0 to 1.0 for thumbsticks, 0.0 to 1.0 for triggers")] float axisValue = 0f,
            CancellationToken cancellationToken = default)
        {
            try
            {
                switch (action?.ToLowerInvariant())
                {
                    case "key_down":
                        return await HandleKeyDown(bridge, key, cancellationToken);
                    case "key_up":
                        return await HandleKeyUp(bridge, key, cancellationToken);
                    case "key_press":
                        return await HandleKeyPress(bridge, key, cancellationToken);
                    case "mouse_move":
                        return await HandleMouseMove(bridge, position, cancellationToken);
                    case "mouse_down":
                        return await HandleMouseDown(bridge, button, cancellationToken);
                    case "mouse_up":
                        return await HandleMouseUp(bridge, button, cancellationToken);
                    case "mouse_click":
                        return await HandleMouseClick(bridge, button, cancellationToken);
                    case "gamepad_button_down":
                        return await HandleGamepadButtonDown(bridge, gamepadButton, cancellationToken);
                    case "gamepad_button_up":
                        return await HandleGamepadButtonUp(bridge, gamepadButton, cancellationToken);
                    case "gamepad_button_press":
                        return await HandleGamepadButtonPress(bridge, gamepadButton, cancellationToken);
                    case "gamepad_axis":
                        return await HandleGamepadAxis(bridge, gamepadAxis, axisValue, cancellationToken);
                    default:
                        return JsonSerializer.Serialize(new { error = $"Unknown action: {action}" });
                }
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }

        private static async Task<string> HandleKeyDown(GameBridge bridge, string key, CancellationToken ct)
        {
            if (!TryParseKey(key, out var parsedKey))
                return JsonSerializer.Serialize(new { error = $"Invalid key: {key}" });

            await bridge.RunOnGameThread(_ => bridge.Keyboard.SimulateDown(parsedKey), ct);
            return JsonSerializer.Serialize(new { success = true, action = "key_down", key });
        }

        private static async Task<string> HandleKeyUp(GameBridge bridge, string key, CancellationToken ct)
        {
            if (!TryParseKey(key, out var parsedKey))
                return JsonSerializer.Serialize(new { error = $"Invalid key: {key}" });

            await bridge.RunOnGameThread(_ => bridge.Keyboard.SimulateUp(parsedKey), ct);
            return JsonSerializer.Serialize(new { success = true, action = "key_up", key });
        }

        private static async Task<string> HandleKeyPress(GameBridge bridge, string key, CancellationToken ct)
        {
            if (!TryParseKey(key, out var parsedKey))
                return JsonSerializer.Serialize(new { error = $"Invalid key: {key}" });

            // Down on one frame
            await bridge.RunOnGameThread(_ => bridge.Keyboard.SimulateDown(parsedKey), ct);
            // Up on next frame
            await bridge.RunOnGameThread(_ => bridge.Keyboard.SimulateUp(parsedKey), ct);
            return JsonSerializer.Serialize(new { success = true, action = "key_press", key });
        }

        private static async Task<string> HandleMouseMove(GameBridge bridge, string position, CancellationToken ct)
        {
            if (!TryParsePosition(position, out var pos))
                return JsonSerializer.Serialize(new { error = $"Invalid position format: {position}. Expected 'x,y' with values 0-1." });

            await bridge.RunOnGameThread(_ => bridge.Mouse.SetPosition(pos), ct);
            return JsonSerializer.Serialize(new { success = true, action = "mouse_move", x = pos.X, y = pos.Y });
        }

        private static async Task<string> HandleMouseDown(GameBridge bridge, string button, CancellationToken ct)
        {
            if (!TryParseMouseButton(button, out var parsedButton))
                return JsonSerializer.Serialize(new { error = $"Invalid mouse button: {button}" });

            await bridge.RunOnGameThread(_ => bridge.Mouse.SimulateMouseDown(parsedButton), ct);
            return JsonSerializer.Serialize(new { success = true, action = "mouse_down", button });
        }

        private static async Task<string> HandleMouseUp(GameBridge bridge, string button, CancellationToken ct)
        {
            if (!TryParseMouseButton(button, out var parsedButton))
                return JsonSerializer.Serialize(new { error = $"Invalid mouse button: {button}" });

            await bridge.RunOnGameThread(_ => bridge.Mouse.SimulateMouseUp(parsedButton), ct);
            return JsonSerializer.Serialize(new { success = true, action = "mouse_up", button });
        }

        private static async Task<string> HandleMouseClick(GameBridge bridge, string button, CancellationToken ct)
        {
            if (!TryParseMouseButton(button, out var parsedButton))
                return JsonSerializer.Serialize(new { error = $"Invalid mouse button: {button}" });

            // Down on one frame
            await bridge.RunOnGameThread(_ => bridge.Mouse.SimulateMouseDown(parsedButton), ct);
            // Up on next frame
            await bridge.RunOnGameThread(_ => bridge.Mouse.SimulateMouseUp(parsedButton), ct);
            return JsonSerializer.Serialize(new { success = true, action = "mouse_click", button });
        }

        private static async Task<string> HandleGamepadButtonDown(GameBridge bridge, string gamepadButton, CancellationToken ct)
        {
            if (!TryParseGamePadButton(gamepadButton, out var parsedButton))
                return JsonSerializer.Serialize(new { error = $"Invalid gamepad button: {gamepadButton}" });

            await bridge.RunOnGameThread(_ => bridge.GamePad.SetButton(parsedButton, true), ct);
            return JsonSerializer.Serialize(new { success = true, action = "gamepad_button_down", gamepadButton });
        }

        private static async Task<string> HandleGamepadButtonUp(GameBridge bridge, string gamepadButton, CancellationToken ct)
        {
            if (!TryParseGamePadButton(gamepadButton, out var parsedButton))
                return JsonSerializer.Serialize(new { error = $"Invalid gamepad button: {gamepadButton}" });

            await bridge.RunOnGameThread(_ => bridge.GamePad.SetButton(parsedButton, false), ct);
            return JsonSerializer.Serialize(new { success = true, action = "gamepad_button_up", gamepadButton });
        }

        private static async Task<string> HandleGamepadButtonPress(GameBridge bridge, string gamepadButton, CancellationToken ct)
        {
            if (!TryParseGamePadButton(gamepadButton, out var parsedButton))
                return JsonSerializer.Serialize(new { error = $"Invalid gamepad button: {gamepadButton}" });

            // Down on one frame
            await bridge.RunOnGameThread(_ => bridge.GamePad.SetButton(parsedButton, true), ct);
            // Up on next frame
            await bridge.RunOnGameThread(_ => bridge.GamePad.SetButton(parsedButton, false), ct);
            return JsonSerializer.Serialize(new { success = true, action = "gamepad_button_press", gamepadButton });
        }

        private static async Task<string> HandleGamepadAxis(GameBridge bridge, string gamepadAxis, float axisValue, CancellationToken ct)
        {
            if (!TryParseGamePadAxis(gamepadAxis, out var parsedAxis))
                return JsonSerializer.Serialize(new { error = $"Invalid gamepad axis: {gamepadAxis}" });

            await bridge.RunOnGameThread(_ => bridge.GamePad.SetAxis(parsedAxis, axisValue), ct);
            return JsonSerializer.Serialize(new { success = true, action = "gamepad_axis", gamepadAxis, axisValue });
        }

        private static bool TryParseKey(string key, out Keys result)
        {
            result = default;
            if (string.IsNullOrEmpty(key))
                return false;
            return Enum.TryParse(key, ignoreCase: true, out result);
        }

        private static bool TryParseMouseButton(string button, out MouseButton result)
        {
            result = default;
            if (string.IsNullOrEmpty(button))
                return false;
            return Enum.TryParse(button, ignoreCase: true, out result);
        }

        private static bool TryParseGamePadButton(string button, out GamePadButton result)
        {
            result = default;
            if (string.IsNullOrEmpty(button))
                return false;
            return Enum.TryParse(button, ignoreCase: true, out result);
        }

        private static bool TryParseGamePadAxis(string axis, out GamePadAxis result)
        {
            result = default;
            if (string.IsNullOrEmpty(axis))
                return false;
            return Enum.TryParse(axis, ignoreCase: true, out result);
        }

        private static bool TryParsePosition(string position, out Vector2 result)
        {
            result = default;
            if (string.IsNullOrEmpty(position))
                return false;

            var parts = position.Split(',');
            if (parts.Length != 2)
                return false;

            if (!float.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x))
                return false;
            if (!float.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var y))
                return false;

            result = new Vector2(x, y);
            return true;
        }
    }
}
