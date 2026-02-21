using System.Collections.Generic;

namespace ScePSX
{
    public class Controller
    {
        public byte RightJoyX;
        public byte RightJoyY;
        public byte LeftJoyX;
        public byte LeftJoyY;

        public bool IsAnalog = false;

        protected Queue<byte> DataFifo = new Queue<byte>();

        public bool ack;

        public int transferCounter = 0;
        public byte VibrationRight = 0x00;
        public byte VibrationLeft = 0x00;

        public IRumbleHandler RumbleHandler = null;

        private enum Mode
        {
            Idle,
            Connected,
            Transfering,
        }
        Mode mode = Mode.Idle;

        public enum InputAction
        {
            DPadUp,
            DPadDown,
            DPadLeft,
            DPadRight,
            Triangle,
            Circle,
            Cross,
            Square,
            Select,
            Start,
            L1,
            L2,
            L3,
            R1,
            R2,
            R3
        }

        private Dictionary<InputAction, byte> InputActions = new()
        {
            { InputAction.DPadUp, 1 },
            { InputAction.DPadDown, 1 },
            { InputAction.DPadLeft, 1 },
            { InputAction.DPadRight, 1 },
            { InputAction.Triangle, 1 },
            { InputAction.Circle, 1 },
            { InputAction.Cross, 1 },
            { InputAction.Square, 1 },
            { InputAction.Select, 1 },
            { InputAction.Start, 1 },
            { InputAction.L1, 1 },
            { InputAction.L2, 1 },
            { InputAction.L3, 1 },
            { InputAction.R1, 1 },
            { InputAction.R2, 1 },
            { InputAction.R3, 1 }
        };

        public byte process(byte b)
        {
            switch (mode)
            {
                case Mode.Idle:
                    switch (b)
                    {
                        case 0x01:
                            //Console.WriteLine("[Controller] Idle Process 0x01");
                            mode = Mode.Connected;
                            ack = true;
                            return 0xFF;
                        default:
                            //Console.WriteLine($"[Controller] Idle Process Warning: {b:x2}");
                            DataFifo.Clear();
                            ack = false;
                            return 0xFF;
                    }

                case Mode.Connected:
                    switch (b)
                    {
                        case 0x42:
                            //Console.WriteLine("[Controller] Connected Init Transfer Process 0x42");
                            mode = Mode.Transfering;
                            GenRepsone();
                            transferCounter = 0;
                            ack = true;
                            return DataFifo.Dequeue();
                        default:
                            //Console.WriteLine("[Controller] Connected Transfer Process unknow command {b:x2} RESET TO IDLE");
                            mode = Mode.Idle;
                            DataFifo.Clear();
                            ack = false;
                            return 0xFF;
                    }

                case Mode.Transfering:
                    byte data = DataFifo.Dequeue();
                    if (IsAnalog)
                    {
                        if (transferCounter == 2)
                        {
                            VibrationRight = b;
                        }
                        if (transferCounter == 3)
                        {
                            VibrationLeft = b;
                            RumbleHandler?.ControllerRumble(VibrationRight, VibrationLeft);
                            //Console.WriteLine($"[Controller] Rumble {VibrationRight},{VibrationLeft}");
                        }
                    }
                    transferCounter++;
                    ack = DataFifo.Count > 0;
                    if (!ack)
                    {
                        //Console.WriteLine("[Controller] Changing to idle");
                        mode = Mode.Idle;
                    }
                    //Console.WriteLine($"[Controller] Transfer Process value:{b:x2} response: {data:x2} queueCount: {transferDataFifo.Count} ack: {ack}");
                    return data;
                default:
                    //Console.WriteLine("[Controller] This should be unreachable");
                    return 0xFF;
            }
        }

        public void resetToIdle()
        {
            mode = Mode.Idle;
            transferCounter = 0;
            VibrationRight = 0;
            VibrationLeft = 0;
        }

        private void GenRepsone()
        {
            //0x5A73 = DualAnalogController, 0x5A41 = DigitalController
            if (IsAnalog)
            {
                DataFifo.Enqueue(0x73);
            } else
            {
                VibrationRight = 0;
                VibrationLeft = 0;
                DataFifo.Enqueue(0x41);
            }
            DataFifo.Enqueue(0x5A);

            var b00 = InputActions[InputAction.Select];
            var b01 = (byte)1; //InputActions[InputAction.L3];
            var b02 = (byte)1; //InputActions[InputAction.R3];
            var b03 = InputActions[InputAction.Start];
            var b04 = InputActions[InputAction.DPadUp];
            var b05 = InputActions[InputAction.DPadRight];
            var b06 = InputActions[InputAction.DPadDown];
            var b07 = InputActions[InputAction.DPadLeft];

            var b08 = InputActions[InputAction.L2];
            var b09 = InputActions[InputAction.R2];
            var b10 = InputActions[InputAction.L1];
            var b11 = InputActions[InputAction.R1];
            var b12 = InputActions[InputAction.Triangle];
            var b13 = InputActions[InputAction.Circle];
            var b14 = InputActions[InputAction.Cross];
            var b15 = InputActions[InputAction.Square];

            var Button1 = (byte)0;

            Button1 |= b07;
            Button1 <<= 1;
            Button1 |= b06;
            Button1 <<= 1;
            Button1 |= b05;
            Button1 <<= 1;
            Button1 |= b04;
            Button1 <<= 1;
            Button1 |= b03;
            Button1 <<= 1;
            Button1 |= b02;
            Button1 <<= 1;
            Button1 |= b01;
            Button1 <<= 1;
            Button1 |= b00;

            var Button2 = (byte)0;

            Button2 |= b15;
            Button2 <<= 1;
            Button2 |= b14;
            Button2 <<= 1;
            Button2 |= b13;
            Button2 <<= 1;
            Button2 |= b12;
            Button2 <<= 1;
            Button2 |= b11;
            Button2 <<= 1;
            Button2 |= b10;
            Button2 <<= 1;
            Button2 |= b09;
            Button2 <<= 1;
            Button2 |= b08;

            DataFifo.Enqueue(Button1);
            DataFifo.Enqueue(Button2);

            if (IsAnalog)
            {
                DataFifo.Enqueue(RightJoyX);
                DataFifo.Enqueue(RightJoyY);
                DataFifo.Enqueue(LeftJoyX);
                DataFifo.Enqueue(LeftJoyY);
            }
        }

        public void Button(InputAction inputCode, bool Down = false)
        {
            InputActions[inputCode] = (byte)((Down) ? 0 : 1); //pressed : released
        }

        public void AnalogAxis(float lx, float ly, float rx, float ry)
        {
            //IsAnalog = true;

            //Convert [-1 , 1] to [0 , 0xFF]
            RightJoyX = (byte)(((rx + 1.0f) / 2.0f) * 0xFF);
            LeftJoyX = (byte)(((lx + 1.0f) / 2.0f) * 0xFF);
            RightJoyY = (byte)(((ry + 1.0f) / 2.0f) * 0xFF);
            LeftJoyY = (byte)(((ly + 1.0f) / 2.0f) * 0xFF);
        }

    }

}
