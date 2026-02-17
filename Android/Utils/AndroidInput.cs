using System;
using System.Collections.Generic;
using Android.App;
using Android.Hardware.Input;
using Android.OS;
using Android.Views;
using static ScePSX.Controller;

#pragma warning disable CS8618
#pragma warning disable CS8600
#pragma warning disable CS8602

namespace ScePSX;

public class AndroidInputHandler : Java.Lang.Object, View.IOnGenericMotionListener, View.IOnKeyListener
{
    private Dictionary<InputAction, bool> buttonStates = new Dictionary<InputAction, bool>();

    // 摇杆轴状态 (归一化 -1.0f 到 1.0f)
    private float leftX, leftY, rightX, rightY;
    private float triggerLeft, triggerRight;

    private const float DEADZONE = 0.1f;

    public bool isAnalog;

    private Dictionary<Keycode, InputAction> keycodeMap = new Dictionary<Keycode, InputAction>
    {
        // 标准手柄按钮映射
        { Keycode.ButtonA, InputAction.Cross },      // A键
        { Keycode.ButtonB, InputAction.Circle },     // B键
        { Keycode.ButtonX, InputAction.Square },     // X键
        { Keycode.ButtonY, InputAction.Triangle },   // Y键
        { Keycode.ButtonL1, InputAction.L1 },
        { Keycode.ButtonR1, InputAction.R1 },
        { Keycode.ButtonL2, InputAction.L2 },
        { Keycode.ButtonR2, InputAction.R2 },
        { Keycode.ButtonSelect, InputAction.Select },
        { Keycode.ButtonStart, InputAction.Start },
        { Keycode.DpadUp, InputAction.DPadUp },
        { Keycode.DpadDown, InputAction.DPadDown },
        { Keycode.DpadLeft, InputAction.DPadLeft },
        { Keycode.DpadRight, InputAction.DPadRight },
        { Keycode.ButtonThumbl, InputAction.L3 },    // 左摇杆按下
        { Keycode.ButtonThumbr, InputAction.R3 },    // 右摇杆按下
    };

    public event Action<InputAction, bool> OnButtonChanged;
    public event Action<float, float, float, float> OnAnalogAxisChanged; // lx, ly, rx, ry
    public event Action<InputAction, bool> OnTriggerChanged; // L2/R2 扳机键

    public AndroidInputHandler(Activity activity)
    {
        foreach (InputAction action in Enum.GetValues(typeof(InputAction)))
        {
            buttonStates[action] = false;
        }
        var rootView = activity.Window.DecorView;
        rootView.SetOnGenericMotionListener(this);
        rootView.SetOnKeyListener(this);

        IsGamepadConnected();
    }

    public void ControllerRumble(byte VibrationRight, byte VibrationLeft)
    {
        try
        {
            var vibrator = (Vibrator?)Android.App.Application.Context.GetSystemService(Android.Content.Context.VibratorService);
            if (vibrator != null && vibrator.HasVibrator)
            {
                // 将 0-255 转换为 0-0xFFFF
                ushort right = (ushort)(VibrationRight * 257);
                ushort left = (ushort)(VibrationLeft * 257);

                long[] pattern = { 0, left, right };
                vibrator.Vibrate(VibrationEffect.CreateWaveform(pattern, -1));
            }
        } catch (Exception ex)
        {
            Console.WriteLine($"Vibration failed: {ex.Message}");
        }
    }

    private bool IsGamepadKey(Keycode keyCode)
    {
        return keycodeMap.ContainsKey(keyCode) ||
               keyCode == Keycode.ButtonA ||
               keyCode == Keycode.ButtonB ||
               keyCode == Keycode.ButtonX ||
               keyCode == Keycode.ButtonY ||
               keyCode == Keycode.ButtonL1 ||
               keyCode == Keycode.ButtonR1 ||
               keyCode == Keycode.ButtonL2 ||
               keyCode == Keycode.ButtonR2 ||
               keyCode == Keycode.ButtonSelect ||
               keyCode == Keycode.ButtonStart ||
               keyCode == Keycode.ButtonThumbl ||
               keyCode == Keycode.ButtonThumbr ||
               (keyCode >= Keycode.DpadUp && keyCode <= Keycode.DpadRight);
    }

    public bool OnKey(View? v, Keycode keyCode, KeyEvent? e)
    {
        if (!e.Source.HasFlag(InputSourceType.Gamepad))
            return false;

        bool isPressed = e.Action == KeyEventActions.Down;

        //Console.WriteLine($"手柄按键：{keyCode}，状态：{(isPressed ? "按下" : "抬起")}");

        if (keycodeMap.TryGetValue(keyCode, out InputAction action))
        {
            UpdateButtonState(action, isPressed);
            return true;
        }

        return false;
    }

    public bool OnGenericMotion(View? v, MotionEvent? e)
    {
        //Console.WriteLine($"OnGenericMotion: ActionButton {e.ActionButton} ButtonState {e.ButtonState}");

        if (e.Action != MotionEventActions.Move)
            return false;

        var source = e.Source;

        if ((source & InputSourceType.Gamepad) == InputSourceType.Gamepad ||
            (source & InputSourceType.Joystick) == InputSourceType.Joystick)
        {
            ProcessJoystickInput(e);
            return true;
        }

        return false;
    }

    private void ProcessJoystickInput(MotionEvent e)
    {
        float newLeftX = GetAxisValue(e, Axis.X);
        float newLeftY = GetAxisValue(e, Axis.Y);
        float newRightX = GetAxisValue(e, Axis.Z);
        float newRightY = GetAxisValue(e, Axis.Rz);

        float newTriggerLeft = GetAxisValue(e, Axis.Ltrigger);
        float newTriggerRight = GetAxisValue(e, Axis.Rtrigger);

        if (Math.Abs(newTriggerLeft) < 0.01f && Math.Abs(newTriggerRight) < 0.01f)
        {
            newTriggerLeft = GetAxisValue(e, Axis.Brake);      // 某些手柄使用 Brake
            newTriggerRight = GetAxisValue(e, Axis.Gas);       // 某些手柄使用 Gas
        }

        newLeftX = NormalizeAxis(newLeftX);
        newLeftY = NormalizeAxis(newLeftY);
        newRightX = NormalizeAxis(newRightX);
        newRightY = NormalizeAxis(newRightY);

        newTriggerLeft = NormalizeTrigger(newTriggerLeft);
        newTriggerRight = NormalizeTrigger(newTriggerRight);

        bool analogChanged = false;

        if (Math.Abs(newLeftX - leftX) > 0.01f)
        {
            leftX = newLeftX;
            analogChanged = true;
        }
        if (Math.Abs(newLeftY - leftY) > 0.01f)
        {
            leftY = newLeftY;
            analogChanged = true;
        }
        if (Math.Abs(newRightX - rightX) > 0.01f)
        {
            rightX = newRightX;
            analogChanged = true;
        }
        if (Math.Abs(newRightY - rightY) > 0.01f)
        {
            rightY = newRightY;
            analogChanged = true;
        }

        if (analogChanged)
        {
            OnAnalogAxisChanged?.Invoke(leftX, leftY, rightX, rightY);
        }

        UpdateTriggerState(InputAction.L2, newTriggerLeft);
        UpdateTriggerState(InputAction.R2, newTriggerRight);

        // 处理方向键（HAT开关）
        float hatX = GetAxisValue(e, Axis.HatX);
        float hatY = GetAxisValue(e, Axis.HatY);
        ProcessDPadFromHat(hatX, hatY);

        if (hatX == 0 && hatY == 0 && !isAnalog)
        {
            ProcessDPadFromLeftStick(leftX, leftY);
        }
    }

    private float GetAxisValue(MotionEvent e, Axis axis)
    {
        try
        {
            if (e.Device != null)
            {
                var device = e.Device;
                if (device.MotionRanges != null)
                {
                    foreach (var range in device.MotionRanges)
                    {
                        if (range.Axis == axis)
                        {
                            return e.GetAxisValue(axis);
                        }
                    }
                }
            }
            if (e.HistorySize > 0)
            {
                return e.GetHistoricalAxisValue(axis, 0);
            }
            return e.GetAxisValue(axis);
        } catch
        {
            return 0;
        }
    }

    private float NormalizeAxis(float value)
    {
        if (Math.Abs(value) < DEADZONE)
        {
            return 0.0f;
        }

        if (value > 0)
        {
            return (value - DEADZONE) / (1.0f - DEADZONE);
        } else
        {
            return (value + DEADZONE) / (1.0f - DEADZONE);
        }
    }

    private float NormalizeTrigger(float value)
    {
        if (value < DEADZONE)
        {
            return 0.0f;
        }
        return Math.Min(value, 1.0f);
    }

    private void UpdateButtonState(InputAction action, bool isPressed)
    {
        if (buttonStates.TryGetValue(action, out bool currentState) && currentState != isPressed)
        {
            buttonStates[action] = isPressed;
            OnButtonChanged?.Invoke(action, isPressed);
        }
    }

    private void UpdateTriggerState(InputAction action, float value)
    {
        bool isPressed = value > 0.5f;
        UpdateButtonState(action, isPressed);
    }

    private void ProcessDPadFromHat(float hatX, float hatY)
    {
        const float threshold = 0.5f;

        UpdateButtonState(InputAction.DPadUp, hatY < -threshold);
        UpdateButtonState(InputAction.DPadDown, hatY > threshold);
        UpdateButtonState(InputAction.DPadLeft, hatX < -threshold);
        UpdateButtonState(InputAction.DPadRight, hatX > threshold);
    }

    private void ProcessDPadFromLeftStick(float lx, float ly)
    {
        UpdateButtonState(InputAction.DPadUp, ly < -0.5f);
        UpdateButtonState(InputAction.DPadDown, ly > 0.5f);
        UpdateButtonState(InputAction.DPadLeft, lx < -0.5f);
        UpdateButtonState(InputAction.DPadRight, lx > 0.5f);
    }

    public bool GetButtonState(InputAction action)
    {
        return buttonStates.TryGetValue(action, out bool state) && state;
    }

    public void GetAnalogState(out float lx, out float ly, out float rx, out float ry)
    {
        lx = leftX;
        ly = leftY;
        rx = rightX;
        ry = rightY;
    }

    public static bool IsGamepadConnected()
    {
        var manager = (InputManager)Android.App.Application.Context.GetSystemService(Android.Content.Context.InputService);
        int[] deviceIds = manager.GetInputDeviceIds();

        foreach (int deviceId in deviceIds)
        {
            var device = manager.GetInputDevice(deviceId);
            if (device != null &&
                (device.Sources.HasFlag(InputSourceType.Gamepad) ||
                 device.Sources.HasFlag(InputSourceType.Joystick)))
            {
                Console.WriteLine($"检测到手柄：{device.Name} (ID: {deviceId})");
                return true;
            }
        }
        return false;
    }

    public static string[] GetConnectedGamepadInfo()
    {
        var result = new List<string>();
        var manager = (InputManager)Android.App.Application.Context.GetSystemService(Android.Content.Context.InputService);
        int[] deviceIds = manager.GetInputDeviceIds();

        foreach (int deviceId in deviceIds)
        {
            var device = manager.GetInputDevice(deviceId);
            if (device != null &&
                (device.Sources.HasFlag(InputSourceType.Gamepad) ||
                 device.Sources.HasFlag(InputSourceType.Joystick)))
            {
                string info = $"{device.Name} (ID: {deviceId})";
                result.Add(info);
            }
        }
        return result.ToArray();
    }
}
