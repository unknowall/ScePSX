using System;
using System.Collections.Generic;
using System.IO;
using static ScePSX.Controller;
using static SDL2.SDL;

namespace ScePSX.UI
{
    public class SDLHanlder
    {
        public CircularBuffer<byte>? SamplesBuffer;

        uint audiodeviceid;
        SDL_AudioCallback audioCallbackDelegate;

        Dictionary<SDL_GameControllerButton, InputAction>? ButtonMap;
        nint controller1, controller2;
        bool HasRumble1, HasRumble2;
        IniFile ini;
        int concount = 0;

        public SDLHanlder(IniFile ini)
        {
            this.ini = ini;

            SDL_Init(SDL_INIT_AUDIO | SDL_INIT_GAMECONTROLLER | SDL_INIT_HAPTIC);

            if (File.Exists("./ControllerDB.txt"))
            {
                Console.WriteLine("ScePSX Load ControllerMappings...");
                SDL_GameControllerAddMappingsFromFile("./ControllerDB.txt");
            }

            audioCallbackDelegate = AudioCallbackImpl;

            SDL_AudioSpec desired = new SDL_AudioSpec
            {
                channels = 2,
                format = AUDIO_S16LSB,
                freq = 44100,
                samples = 2048,
                callback = audioCallbackDelegate,
                userdata = IntPtr.Zero

            };
            SDL_AudioSpec obtained = new SDL_AudioSpec();

            SetAudioBuffer();

            audiodeviceid = SDL_OpenAudioDevice(null, 0, ref desired, out obtained, 0);

            if (audiodeviceid != 0) SDL_PauseAudioDevice(audiodeviceid, 0);

            InitControllerMap();
        }

        ~SDLHanlder()
        {
            if (audiodeviceid != 0) SDL_CloseAudioDevice(audiodeviceid);
            if (controller1 != 0)
            {
                SDL_GameControllerClose(controller1);
                SDL_JoystickClose(controller1);
            }
            SDL_Quit();
        }

        public void InitControllerMap()
        {
            ButtonMap = new()
            {
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A, InputAction.Circle },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B, InputAction.Cross },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X, InputAction.Triangle },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y, InputAction.Square },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK, InputAction.Select },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START, InputAction.Start },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER, InputAction.L1 },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER, InputAction.R1 },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP, InputAction.DPadUp },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN, InputAction.DPadDown },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT, InputAction.DPadLeft },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT, InputAction.DPadRight }
            };
        }

        public void SetAudioBuffer()
        {
            if (audiodeviceid != 0) SDL_PauseAudioDevice(audiodeviceid, 1);

            int bufms = ini.ReadInt("Audio", "Buffer");

            if (bufms < 50) bufms = 50;

            int alignedSize = ((bufms * 176 + 2048 - 1) / 2048) * 2048;

            SamplesBuffer = new CircularBuffer<byte>(alignedSize); // 300 ms = 52920

            if (audiodeviceid != 0) SDL_PauseAudioDevice(audiodeviceid, 0);
        }

        private unsafe void AudioCallbackImpl(IntPtr userdata, IntPtr stream, int len)
        {
            byte[] tempBuffer = new byte[len];

            int bytesRead = SamplesBuffer.Read(tempBuffer, 0, len);

            fixed (byte* ptr = tempBuffer)
            {
                Buffer.MemoryCopy(ptr, (void*)stream, len, bytesRead);
            }

            if (bytesRead < len)
            {
                new Span<byte>((void*)(stream + bytesRead), len - bytesRead).Fill(0);
            }
        }

        public void ControllerRumble(byte VibrationRight, byte VibrationLeft)
        {
            if (controller1 == 0) return;

            if (HasRumble1)
            {
                ushort VibrationRight1 = VibrationRight > 0 ? (ushort)0xFFFF : (ushort)0x0000;
                ushort VibrationLeft1 = (ushort)(VibrationLeft * 257);

                //if (VibrationRight1 != 0 || VibrationLeft1 != 0)
                //    Console.WriteLine($"Controller Rumble {VibrationRight1}, {VibrationLeft1}");

                if (SDL_GameControllerRumble(controller1, VibrationRight1, VibrationLeft1, 0) != 0)
                {
                    Console.WriteLine($"Controller 1 Rumble Error: {SDL_GetError()}");
                }
            }
        }

        public void CheckController()
        {
            concount = SDL_NumJoysticks();

            if (controller1 == 0 && concount >= 1)
            {
                if (SDL_IsGameController(0) == SDL_bool.SDL_TRUE)
                {
                    controller1 = SDL_GameControllerOpen(0);
                }
                else
                {
                    controller1 = SDL_JoystickOpen(0);
                }
                if (controller1 != 0)
                {
                    HasRumble1 = SDL_GameControllerHasRumble(controller1) == SDL_bool.SDL_TRUE;

                    OSD.Show($"{SDL_JoystickNameForIndex(0)} Connected");

                    Console.WriteLine($"Controller Device 1 : {SDL_JoystickNameForIndex(0)} Connected, Rumble: {HasRumble1}");
                    if (HasRumble1)
                        if (SDL_GameControllerRumble(controller1, 0, 0, 0) != 0)
                        {
                            Console.WriteLine($"Controller 1 Rumble Error: {SDL_GetError()}");
                        }
                    SDL_Event dummyEvent;
                    SDL_PollEvent(out dummyEvent);
                }
            }

            if (controller2 == 0 && concount >= 2)
            {
                if (SDL_IsGameController(1) == SDL_bool.SDL_TRUE)
                {
                    controller2 = SDL_GameControllerOpen(1);
                }
                else
                {
                    controller2 = SDL_JoystickOpen(1);
                }
                if (controller2 != 0)
                {
                    HasRumble2 = SDL_GameControllerHasRumble(controller2) == SDL_bool.SDL_TRUE;
                    Console.WriteLine($"Controller Device 2 : {SDL_JoystickNameForIndex(0)} Connected, Rumble: {HasRumble2}");
                    if (HasRumble2)
                        if (SDL_GameControllerRumble(controller2, 0, 0, 0) != 0)
                        {
                            Console.WriteLine($"Controller 2 Rumble Error: {SDL_GetError()}");
                        }
                }
                SDL_Event dummyEvent;
                SDL_PollEvent(out dummyEvent);
            }
        }

        public bool QueryControllerState(int conidx, PSXCore Core, bool isAnalog, bool KeyFirst)
        {
            nint controller;

            if (conidx == 1)
            {
                controller = controller1;
            }
            else if (conidx == 2)
            {
                controller = controller2;
            }
            else
            {
                return KeyFirst;
            }

            if (Core == null || controller == 0) return KeyFirst;

            conidx--;

            //Button
            bool isPadPressed = false;
            foreach (SDL_GameControllerButton button in Enum.GetValues(typeof(SDL_GameControllerButton)))
            {
                bool isPressed = SDL_GameControllerGetButton(controller, button) == 1;
                if (!isAnalog && isPressed)
                {
                    if (isPressed && (int)button >= 11 && (int)button <= 15)
                    {
                        isPadPressed = true;
                    }
                }
                if (isPressed && KeyFirst)
                    KeyFirst = false;
                if (!KeyFirst && ButtonMap.TryGetValue(button, out var gamepadInput))
                {
                    Core.Button(gamepadInput, isPressed, conidx);
                }
            }

            if (KeyFirst) return KeyFirst;

            //AnalogAxis
            float lx = 0.0f, ly = 0.0f, rx = 0.0f, ry = 0.0f;

            short leftX = SDL_GameControllerGetAxis(controller, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX);
            short leftY = SDL_GameControllerGetAxis(controller, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY);
            short rightX = SDL_GameControllerGetAxis(controller, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX);
            short rightY = SDL_GameControllerGetAxis(controller, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY);

            lx = NormalizeAxis(leftX);
            ly = NormalizeAxis(leftY);
            rx = NormalizeAxis(rightX);
            ry = NormalizeAxis(rightY);

            //TRIGGER
            short tl = SDL_GameControllerGetAxis(controller, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT);
            short tr = SDL_GameControllerGetAxis(controller, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT);

            Core.Button(InputAction.L2, tl > 16384 ? true : false, conidx);
            Core.Button(InputAction.R2, tr > 16384 ? true : false, conidx);

            Core.AnalogAxis(lx, ly, rx, ry, conidx);

            if (isPadPressed) return KeyFirst;

            //Hat
            int hatIndex = 0;
            int hatState = 0;
            IntPtr joystick = SDL_GameControllerGetJoystick(controller);
            if (joystick != IntPtr.Zero)
            {
                hatState = SDL_JoystickGetHat(joystick, hatIndex);

                Core.Button(InputAction.DPadUp, (hatState & SDL_HAT_UP) != 0, conidx);
                Core.Button(InputAction.DPadDown, (hatState & SDL_HAT_DOWN) != 0, conidx);
                Core.Button(InputAction.DPadLeft, (hatState & SDL_HAT_LEFT) != 0, conidx);
                Core.Button(InputAction.DPadRight, (hatState & SDL_HAT_RIGHT) != 0, conidx);
            }

            if (!isAnalog && hatState == 0)
            {
                // 将左摇杆的值转换为方向键状态
                Core.Button(InputAction.DPadUp, ly < -0.5f, conidx);    // 上
                Core.Button(InputAction.DPadDown, ly > 0.5f, conidx);   // 下
                Core.Button(InputAction.DPadLeft, lx < -0.5f, conidx);  // 左
                Core.Button(InputAction.DPadRight, lx > 0.5f, conidx);  // 右
            }

            return KeyFirst;
        }

        private float NormalizeAxis(short value)
        {
            float ret = Math.Clamp(value / 32767.0f, -1.0f, 1.0f);
            if (Math.Abs(ret) < 0.1f)
            {
                ret = 0.0f;
            }
            return ret;
        }
    }
}
