using System;
using System.Collections.Generic;

namespace ScePSX
{
    [Serializable]
    public class SerialIO : IDisposable
    {
        const uint CPU_CLOCK = 33868800;

        //Data (R/W) -> 1/4 word    (This is actually a FIFO)
        public byte TX_Data;
        public ushort BoudReload = 0;

        //Status (R) -> 4           This register comes after 4 bytes of Data address, although the Data length is only 1 byte
        [Serializable]
        struct StatusRegister
        {
            public byte TX_FIFO_Not_Full;    //0
            public byte RX_FIFO_Not_Empty;   //1
            public byte TX_Idle;             //2
            public byte RX_Parity_Error;     //3
            public byte RX_FIFO_Overrun;     //4     SIO1
            public byte RX_BadStopBit;       //5     SIO1
            public byte RX_InputLevel;       //6     SIO1
            public byte DSR_InputLevel;      //7
            public byte CTS_InputLevel;      //8
            public byte InterruptRequest;    //9
            //Unknown - Always 0             //10
            public int Baud;                 //11 - 31
        }
        StatusRegister Status;

        //Mode (R/W) -> 2
        [Serializable]
        struct ModeRegister
        {
            public byte BaudrateReloadFactor;//0 - 1
            public byte CharacterLength;     //2 - 3
            public byte ParityEnable;        //4
            public byte ParityType;          //5
            public byte StopBitLength;       //6 - 7     SIO1
            public byte ClockPolarity;       //8         SIO0
            //Not used                       //9 - 15
        }
        ModeRegister Mode;


        //Control (R/W) -> 2
        [Serializable]
        struct CtrlRegister
        {
            public byte TX_Enable;            //0
            public byte DTR_OutputLevel;      //1
            public byte RX_Enable;            //2
            public byte TX_OutputLevel;       //3     SIO1
            public byte Acknowledge;          //4     
            public byte RTS_OutputLevel;      //5     SIO1
            public byte Reset;                //6     
            //SIO1 Unknown                    //7
            public byte RX_InterruptMode;     //8 - 9
            public byte TX_InterruptEnable;   //10
            public byte RX_InterruptEnable;   //11
            public byte DSR_InterruptEnable;  //12
            public byte PortSelect;           //13    SIO0
            //Not used                        //14 - 15
        }

        [Serializable]
        public struct Command
        {
            public byte address;    //0x0 For data - 0xA for flow control 
            public byte value;
        }

        CtrlRegister Ctrl;
        Socket Socket;

        bool IsConnected = false;
        bool IsServer = false;

        public SerialIO()
        {
            Status.RX_InputLevel = 0;

            if (IsConnected)
            {
                //Setup TCP Connection
                AsyncCallback callback = new AsyncCallback(DataReceived);
                if (IsServer)
                {
                    Socket = new Server(callback);
                    Socket.AcceptClientConnection();
                } else
                {
                    Socket = new Client(callback);
                    Socket.ConnectToServer();
                }
                Status.TX_Idle = 1;
            } else
            {
                Console.WriteLine("[SIO1] Not connected");
            }
        }

        public uint read(uint address)
        {
            //Console.WriteLine("Read16: " + (address & 0xF).ToString("x"));

            switch (address & 0xF)
            {
                case 0x0:
                    return GetData();
                case 0x4:
                    return GetStatus();
                case 0x8:
                    return GetMode();
                case 0xA:
                    return GetCtrl();
                case 0xE:
                    return BoudReload;
                default:
                    Console.WriteLine("[SIO1] Attempting to read from: " + address.ToString("x"));
                    return 0;
            }
        }

        public void write(uint address, uint data)
        {
            //Console.WriteLine("Write16: " + (address & 0xF).ToString("x"));

            switch (address & 0xF)
            {
                case 0x0:
                    Transfare((byte)data);
                    break;
                case 0x8:
                    WriteMode((ushort)data);
                    break;
                case 0xA:
                    WriteCtrl((ushort)data);
                    break;
                case 0xE:
                    BoudReload = (ushort)data;
                    Reload();
                    break;
                case 0x4:
                    Console.WriteLine("[SIO1] Attempting to write to STAT");
                    break;
                default:
                    Console.WriteLine("[SIO1] Attempting to write to: " + address.ToString("x"));
                    break;
            }
        }

        private void Transfare(byte data)
        {
            if (!IsConnected || !Socket.IsConnected())
            {
                return;
            }

            TX_Data = data;

            if (Status.CTS_InputLevel == 1 && Ctrl.TX_Enable == 1)
            {
                byte address = 0x00;
                Socket.Send(new byte[] { address, data });
            }

            if (Ctrl.TX_InterruptEnable == 1)
            {
                if (Status.TX_FIFO_Not_Full == 1 || Status.TX_Idle == 1)
                {
                    Status.InterruptRequest = 1;
                    IRQ.Enqueue(CyclesPerInterrupt);
                }
            }

        }

        private uint GetStatus()
        {
            uint stat = 0;
            stat |= (uint)(Status.TX_FIFO_Not_Full & 1);
            stat |= ((uint)(Status.RX_FIFO_Not_Empty & 1)) << 1;
            stat |= ((uint)(Status.TX_Idle & 1)) << 2;
            stat |= ((uint)(Status.RX_Parity_Error & 1)) << 3;
            stat |= ((uint)(Status.RX_FIFO_Overrun & 1)) << 4;
            stat |= ((uint)(Status.RX_BadStopBit & 1)) << 5;
            stat |= ((uint)(Status.RX_InputLevel & 1)) << 6;
            stat |= ((uint)(Status.DSR_InputLevel & 1)) << 7;
            stat |= ((uint)(Status.CTS_InputLevel & 1)) << 8;
            stat |= ((uint)(Status.InterruptRequest & 1)) << 9;
            stat |= ((uint)(Status.Baud & 0xFFFF)) << 11;
            //Console.WriteLine("stat = " + stat.ToString("x"));
            return stat;
        }

        private ushort GetMode()
        {
            ushort mode = 0;
            mode |= (byte)(Mode.BaudrateReloadFactor & 3);
            mode |= (byte)((Mode.CharacterLength & 3) << 2);
            mode |= (byte)((Mode.ParityEnable & 1) << 4);
            mode |= (byte)((Mode.ParityType & 1) << 5);
            mode |= (byte)((Mode.StopBitLength & 3) << 6);
            mode |= (byte)((Mode.ClockPolarity & 1) << 8);
            return mode;
        }

        private void WriteMode(ushort value)
        {
            Mode.BaudrateReloadFactor = (byte)(value & 3);
            Mode.CharacterLength = (byte)((value >> 2) & 3);
            Mode.ParityEnable = (byte)((value >> 4) & 1);
            Mode.ParityType = (byte)((value >> 5) & 1);
            Mode.StopBitLength = (byte)((value >> 6) & 3);
            Mode.ClockPolarity = (byte)((value >> 8) & 1);
        }
        private ushort GetCtrl()
        {
            ushort control = 0;
            control |= (byte)(Ctrl.TX_Enable & 1);
            control |= (byte)((Ctrl.DTR_OutputLevel & 1) << 1);
            control |= (byte)((Ctrl.RX_Enable & 1) << 2);
            control |= (byte)((Ctrl.TX_OutputLevel & 1) << 3);
            //4 is write only
            control |= (byte)((Ctrl.RTS_OutputLevel & 1) << 5);
            //6 is write only
            //7 is unknown
            control |= (byte)((Ctrl.RX_InterruptMode & 3) << 8);
            control |= (byte)((Ctrl.TX_InterruptEnable & 1) << 10);
            control |= (byte)((Ctrl.RX_InterruptEnable & 1) << 11);
            control |= (byte)((Ctrl.DSR_InterruptEnable & 1) << 12);
            control |= (byte)((Ctrl.PortSelect & 1) << 13);
            return control;
        }

        private void WriteCtrl(ushort control)
        {
            Ctrl.TX_Enable = (byte)(control & 1);
            Ctrl.DTR_OutputLevel = (byte)((control >> 1) & 1);
            Ctrl.RX_Enable = (byte)((control >> 2) & 1);
            Ctrl.TX_OutputLevel = (byte)((control >> 3) & 1);
            Ctrl.Acknowledge = (byte)((control >> 4) & 1);       //Write only
            Ctrl.RTS_OutputLevel = (byte)((control >> 5) & 1);

            Ctrl.Reset = (byte)((control >> 6) & 1);             //Write only
            Ctrl.RX_InterruptMode = (byte)((control >> 8) & 3);
            Ctrl.TX_InterruptEnable = (byte)((control >> 10) & 1);
            Ctrl.RX_InterruptEnable = (byte)((control >> 11) & 1);
            Ctrl.DSR_InterruptEnable = (byte)((control >> 12) & 1);
            Ctrl.PortSelect = (byte)((control >> 13) & 1);

            /*Console.WriteLine("----------------------------------------");
            Console.WriteLine("Write Control:");
            Console.WriteLine("TX_Enable:" + Ctrl.TX_Enable);
            Console.WriteLine("DTR_OutputLevel:" + Ctrl.DTR_OutputLevel);
            Console.WriteLine("RX_Enable:" + Ctrl.RX_Enable);
            Console.WriteLine("TX_OutputLevel:" + Ctrl.TX_OutputLevel);
            Console.WriteLine("Acknowledge:" + Ctrl.Acknowledge);
            Console.WriteLine("RTS_OutputLevel:" + Ctrl.RTS_OutputLevel);
            Console.WriteLine("Reset:" + Ctrl.Reset);
            Console.WriteLine("RX_InterruptMode:" + Ctrl.RX_InterruptMode);
            Console.WriteLine("TX_InterruptEnable:" + Ctrl.TX_InterruptEnable);
            Console.WriteLine("RX_InterruptEnable:" + Ctrl.RX_InterruptEnable);
            Console.WriteLine("DSR_InterruptEnable:" + Ctrl.DSR_InterruptEnable);
            Console.WriteLine("PortSelect:" + Ctrl.PortSelect);*/

            if (Ctrl.Acknowledge == 1)
            {
                Ctrl.Acknowledge = 0;
                Status.InterruptRequest = 0;
                Status.RX_Parity_Error = 0;
                Status.RX_FIFO_Overrun = 0;
                Status.RX_BadStopBit = 0;
            }

            if (Ctrl.Reset == 1)
            {
                Ctrl.Reset = 0;
                Ctrl.Acknowledge = 0;
                Status.InterruptRequest = 0;
                Status.RX_Parity_Error = 0;
                Status.RX_FIFO_Overrun = 0;
                Status.RX_BadStopBit = 0;
                TX_Data = 0xFF;
                RXBuffer.Clear();
            }

            if (Ctrl.RX_Enable == 0)
            {
                RXBuffer.Clear();
            }

            if (IsConnected && Socket.IsConnected())
            {
                byte address = 0x0A;
                byte value = (byte)(GetCtrl() & 0xFF);            //DTR and RTS are in the first byte
                Socket.Send(new byte[] { address, value });     //Send it to the other side
            }
        }

        int delay = -1;
        int CyclesPerInterrupt = 500;
        public bool tick(int cycles)
        {
            /*if (delay > 0) {
                delay -= cycles;
                if (delay <= 0) {
                    Command cmd = Cmdbuffer.Dequeue();
                    HandleCommand(cmd);
                    if (Cmdbuffer.Count > 0) {
                        delay = CyclesPerInterrupt;
                    }
                }
            }*/
            bool ret = false;
            if (delay > 0)
            {
                delay -= cycles;
                if (delay <= 0)
                {
                    ret = true;
                    //Console.WriteLine("[SIO] IRQ");
                    if (IRQ.Count > 0)
                    {
                        delay = IRQ.Dequeue();
                    }
                }
            }

            Status.Baud -= cycles;
            if (Status.Baud <= 0)
            {
                Reload();
            }

            return ret;
        }

        Queue<int> IRQ = new Queue<int>();
        Queue<byte> RXBuffer = new Queue<byte>();
        Queue<Command> Cmdbuffer = new Queue<Command>();

        public void HandleCommand(Command cmd)
        {
            switch (cmd.address)
            {
                case 0x00:
                    RXBuffer.Enqueue(cmd.value);
                    Status.RX_FIFO_Not_Empty = Ctrl.RX_Enable;
                    //Console.WriteLine("RX_FIFO_Not_Empty/RX_InputLevel set to " + Status.RX_FIFO_Not_Empty);

                    /*if (Ctrl.DTR_OutputLevel == 1) {
                        InternalBuffer.Enqueue(cmd.value);
                        Status.RX_FIFO_Not_Empty = 1;
                        Ctrl.RX_Enable = 0;
                    } else {
                        if (Ctrl.RX_Enable == 1) {
                            InternalBuffer.Enqueue(cmd.value);
                            Status.RX_FIFO_Not_Empty = 1;
                        } else {
                            Status.RX_InputLevel = 0;
                            Status.RX_FIFO_Not_Empty = 0;
                            InternalBuffer.Clear();
                            return;
                        }
                    }*/

                    int n = 0;

                    switch (Ctrl.RX_InterruptMode)
                    {
                        case 0:
                            n = 1;
                            break;
                        case 1:
                            n = 2;
                            break;
                        case 2:
                            n = 4;
                            break;
                        case 3:
                            n = 8;
                            break;
                    }
                    if (RXBuffer.Count >= n)
                    {
                        if (Ctrl.RX_InterruptEnable == 1)
                        {
                            Status.InterruptRequest = 1;
                            IRQ.Enqueue(CyclesPerInterrupt);
                        }
                    }
                    break;

                case 0x0A:
                    Status.DSR_InputLevel = (byte)(((cmd.value) >> 1) & 1); //Remote DTR
                    Status.CTS_InputLevel = (byte)(((cmd.value) >> 5) & 1); //Remote RTS
                    Status.TX_FIFO_Not_Full = Status.CTS_InputLevel;
                    Status.TX_Idle = (byte)(Status.CTS_InputLevel & Ctrl.TX_Enable);
                    if (Ctrl.DSR_InterruptEnable == 1 && Status.DSR_InputLevel == 1)
                    {
                        Status.InterruptRequest = 1;
                        IRQ.Enqueue(CyclesPerInterrupt);
                    }
                    break;
                default:
                    Console.WriteLine("[SIO1] Unknown addr: " + cmd.address.ToString("x"));
                    break;
            }
        }

        public byte GetData()
        {
            if (!IsConnected || !Socket.IsConnected())
            {
                return 0xFF;
            }

            byte data = 0xFF;
            if (RXBuffer.Count > 0)
            {
                data = RXBuffer.Dequeue();
            }

            Status.RX_FIFO_Not_Empty = (byte)(RXBuffer.Count > 0 && Ctrl.RX_Enable == 1 ? 1 : 0);

            //Console.WriteLine("[SIO1] Data read: " + data.ToString("x"));
            return data;
        }

        public void Reload()
        {
            Status.Baud = BoudReload * Mode.BaudrateReloadFactor / 2;
            if (Status.Baud > 0)
            {
                CyclesPerInterrupt = (int)(CPU_CLOCK / (Status.Baud * 8));
            }
        }

        public void DataReceived(IAsyncResult result)
        {
            //Process Received data
            Socket.Stop(result);

            byte[] data = Socket.Receive();
            //Console.WriteLine("[SIO1] Received [" + data[0].ToString("x") + "]" + " = " + data[1].ToString("x"));
            Command command = new Command();
            command.address = data[0];
            command.value = data[1];
            HandleCommand(command);

            //Start reading again
            Socket.BeginReceiving();
        }

        public void Dispose()
        {
            if (Socket != null)
            {
                Socket.Terminate();
            }
        }
    }
}
