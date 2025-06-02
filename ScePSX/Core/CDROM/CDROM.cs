using System;
using System.Collections.Generic;
using System.Linq;

namespace ScePSX.CdRom
{
    [Serializable]
    public class Response
    {
        public byte[] values;
        public int interrupt;
        public long delay;
        public CDROM.CDROMState NextState;
        public bool FinishedProcessing;

        public Response(byte[] values, CDROM.Delays delay, CDROM.Flags INT, CDROM.CDROMState nextState)
        {
            this.NextState = nextState;
            this.values = values;
            this.delay = (long)delay;
            this.interrupt = (int)INT;
        }

        public Response(byte[] values, long delay, int INT, CDROM.CDROMState nextState)
        {
            this.NextState = nextState;
            this.values = values;
            this.delay = delay;
            this.interrupt = INT;
        }
    }

    [Serializable]
    public class ResponseBuffer
    {
        private byte[] Buffer;
        private int Length;
        private int Pointer;
        private int ActualLength;
        public bool HasUnreadData = false;

        public ResponseBuffer(int size)
        {
            this.Buffer = new byte[size];
            this.Length = size;
            this.Pointer = 0;
        }
        public byte ReadNext()
        {
            byte data = Buffer[Pointer];
            if ((Pointer + 1) >= ActualLength)
            {
                HasUnreadData = false;
            }
            Pointer = (Pointer + 1) % Length;
            return data;
        }
        public void WriteBuffer(ref byte[] data)
        {
            if (data.Length > this.Length)
            {
                Console.WriteLine("[CDROM] Buffer does not fit the data length");
            }
            Pointer = 0;
            ActualLength = data.Length;
            Clear();

            for (int i = 0; i < data.Length; i++)
            {
                Buffer[i] = data[i];
            }
            HasUnreadData = true;
        }
        private void Clear()
        {
            for (int i = 0; i < Buffer.Length; i++)
            {
                Buffer[i] = 0;
            }
        }
    }

    [Serializable]
    public unsafe class CDROM
    {
        const int OneSecond = 33868800;

        public enum Flags
        {
            INT1 = 1, //INT1 Received SECOND (or further) response to ReadS/ReadN (and Play+Report)
            INT2 = 2, //INT2 Received SECOND response (complete/done)
            INT3 = 3, //INT3 Received FIRST response (ack)
            INT4 = 4, //INT4 DataEnd(when Play/Forward reaches end of disk)
            INT5 = 5, //INT5 So many things, but mainly for errors
            INT6 = 6, //NA
            INT7 = 7  //NA
        }

        public enum Delays
        {
            Zero = 0,
            INT1_SingleSpeed = 0x006e1cd,
            INT1_DoubleSpeed = (int)(OneSecond * 0.65666),
            INT2_GetID = 0x0004a00,
            INT2_Pause_SingleSpeed = (int)(OneSecond * 0.070),
            INT2_Pause_DoubleSpeed = (int)(OneSecond * 0.035),
            INT2_PauseWhenPaused = 0x0001df2,
            INT2_Stop_SingleSpeed = 0x0d38aca,
            INT2_Stop_DoubleSpeed = 0x18a6076,
            INT2_StopWhenStopped = 0x0001d7b,
            INT2LongSeek = OneSecond * 2,
            INT2_Init = (int)(OneSecond * 0.120),
            INT3_Init = (int)(OneSecond * 0.002),
            INT3_General = 0x000c4e1,
            INT3_GetStatWhenStopped = 0x0005cf4,
            INT4 = 0, //? 
            INT5 = 0x0004a00
        }

        public enum Errors
        {
            SeekError = 0x04,
            DriveDoorOpen = 0x08,
            InvalidParameter = 0x10,
            InvalidNumberOfParameters = 0x20,
            InvalidCommand = 0x40,
            CannotRespondYet = 0x80
        }

        public enum CDROMState
        {
            Swaped,
            Idle,
            Seeking,
            ReadingData,
            PlayingCDDA,
            SwappingDisk
        }

        //Status Register
        int Index;                 //0-1
        int ADPBUSY = 0;           //2
        int PRMEMPT = 1;           //3
        int PRMWRDY = 1;           //4
        int RSLRRDY = 0;           //5
        int DRQSTS = 0;            //6
        int BUSYSTS = 0;           //7

        ResponseBuffer ResponseBuffer = new ResponseBuffer(16);
        Queue<Byte> ParameterBuffer = new Queue<Byte>();
        Queue<Response> Responses = new Queue<Response>();

        byte[] SeekParameters = new byte[3];

        byte IRQ_Enable; //0-7
        byte IRQ_Flag;  //0-7

        byte stat = 0x2;

        byte Mode;
        uint LastSize;
        bool AutoPause;
        bool CDDAReport;
        bool IsCDDA;

        int M, S, F;
        int CurrentPos;
        int SkipRate = 0;
        bool SetLoc;
        bool IsReportableSector => (F % 10) == 0;   //10,20,30..etc

        byte LeftCD_toLeft_SPU_Volume;
        byte LeftCD_toRight_SPU_Volume;
        byte RightCD_toRight_SPU_Volume;
        byte RightCD_toLeft_SPU_Volume;

        bool DoubleSpeed;

        CDROMState State;

        public byte CurrentCommand;

        int TransmissionDelay = 0;

        public bool LidOpen = false;

        public CDData DATA;
        public IRQController IRQCTL;

        long ReadRate = 0;
        uint SectorOffset = 0;

        bool SeekedP;
        bool SeekedL;
        int SwappingDelay = -1;
        bool IsError;
        int SpeedAdjust;

        public int CombineDelaySet = (int)(OneSecond * 0.002);

        public CDROM(IRQController irq, string path, string diskid = null)
        {
            this.IRQCTL = irq;
            DATA = new CDData(path, diskid);
        }

        public CDROM(IRQController irq, CDData cddata)
        {
            this.IRQCTL = irq;
            DATA = cddata;
        }

        public void SetSpeed(int Adjust)
        {
            Console.WriteLine($"[CDROM] Speed : {(Adjust == 0 ? "Adaptive" : Adjust + "x")}");
            SpeedAdjust = Adjust;
            //if (Adjust == 0 || Adjust == 1)
            //{
            //    SeekTimeMutil = 3;
            //    SpeedMutil = 75;
            //} else
            //{
            //    SeekTimeMutil = 3 * Adjust;
            //    SpeedMutil = 75 * Adjust;
            //}
        }

        public void Controller(byte command)
        {
            TransmissionDelay = 1000;
            IsError = CurrentCommand == 0x16 && command == 0x1 && Responses.Count > 0;
            if (IsError)
            {
                for (int i = 0; i < Responses.Count; i++)
                {
                    if (!Responses.ElementAt(i).FinishedProcessing && Responses.ElementAt(i).interrupt == (int)Flags.INT3)
                    {
                        Responses.ElementAt(i).NextState = CDROMState.Idle;
                    }
                }
            }
            Execute(command);
        }

        public void Execute(int command)
        {
            CurrentCommand = (byte)command;
            if (IsError)
            {
                Responses = new Queue<Response>(Responses.Where(x => x.interrupt == (int)Flags.INT3));
                stat = 0x6;
                Error(Errors.InvalidParameter, OneSecond * 5);
                Console.WriteLine($"[CDROM] Error at command {command:X2}");
                return;
            }
            //Console.WriteLine($"[CDROM] Command {command:X2}");
            switch (command)
            {
                case 0x00:
                    Sync();
                    break;
                case 0x01:
                    GetStat();
                    break;
                case 0x02:
                    Setloc();
                    break;
                case 0x03:
                    Play();
                    break;
                case 0x04:
                    Forward();
                    break;
                case 0x05:
                    Backward();
                    break;
                case 0x06:
                    ReadNS();
                    break;
                case 0x07:
                    MotorOn();
                    break;
                case 0x08:
                    Stop();
                    break;
                case 0x09:
                    Pause();
                    break;
                case 0x0A:
                    Init();
                    break;
                case 0x0B:
                    Mute();
                    break;
                case 0x0C:
                    UnMute();
                    break;
                case 0x0D:
                    SetFilter();
                    break;
                case 0x0E:
                    SetMode();
                    break;
                case 0x10:
                    GetLocL();
                    break;
                case 0x11:
                    GetLocP();
                    break;
                case 0x13:
                    GetTN();
                    break;
                case 0x14:
                    GetTD();
                    break;
                case 0x15:
                    SeekL();
                    break;
                case 0x16:
                    SeekP();
                    break;
                case 0x19:
                    Test();
                    break;
                case 0x1A:
                    GetID();
                    break;
                case 0x1B:
                    ReadNS();
                    break;
                case 0x1E:
                    ReadTOC();
                    break;
                default:
                    Console.WriteLine($"[CDROM] Invalid command {command:X2}");
                    break;
            }
            //Console.WriteLine($"[CDROM] Command {value:x2}");
            //Console.WriteLine($"        STAT {stat}");
            //Console.WriteLine($"        Repsone {BitConverter.ToString(Responses.ToArray()[0].values)}");
        }

        public void SwapDisk(CDData cddata)
        {
            //Console.WriteLine($"[CDROM] SwappingDisk Disk");
            SwappingDelay = (int)(OneSecond * 0.03);
            DATA = cddata;
            State = CDROMState.SwappingDisk;
            LidOpen = true;
            Responses.Clear();
            Response ack = new Response(new byte[] { 0x11, 0x80 }, Delays.INT3_General, Flags.INT5, State);
            Responses.Enqueue(ack);
        }

        private byte CDROM_Status()
        {
            PRMEMPT = ParameterBuffer.Count == 0 ? 1 : 0;
            PRMWRDY = ParameterBuffer.Count < 16 ? 1 : 0;
            RSLRRDY = ResponseBuffer.HasUnreadData ? 1 : 0;
            DRQSTS = (DATA.DataFifo.Count > 0 && DATA.BFRD == 1) ? 1 : 0;
            BUSYSTS = TransmissionDelay > 0 ? 1 : 0;
            byte status = (byte)((BUSYSTS << 7) | (DRQSTS << 6) | (RSLRRDY << 5) | (PRMWRDY << 4) | (PRMEMPT << 3) | (ADPBUSY << 2) | Index);
            return status;
        }

        public void write(uint address, byte value)
        {
            uint offset = address - 0x1F801800;
            switch (offset)
            {
                case 0:
                    Index = value & 0x3;
                    break;

                case 1:
                    switch (Index)
                    {
                        case 0:
                            Controller(value);
                            break;
                        case 3:
                            RightCD_toRight_SPU_Volume = value;
                            break;
                        default:
                            Console.WriteLine("[CDROM] Unknown Index (" + Index + ")" + " at CRROM command register");
                            break;
                    }
                    break;

                case 2:
                    switch (Index)
                    {
                        case 0:
                            ParameterBuffer.Enqueue(value);
                            break;
                        case 1:
                            IRQ_Enable = value;
                            break;
                        case 2:
                            LeftCD_toLeft_SPU_Volume = value;
                            break;
                        case 3:
                            RightCD_toLeft_SPU_Volume = value;
                            break;
                        default:
                            Console.WriteLine("[CDROM] Unknown Index (" + Index + ")" + " at CRROM IRQ enable register");
                            break;
                    }
                    break;

                case 3:
                    switch (Index)
                    {
                        case 0:
                            RequestRegister(value);
                            break;
                        case 1:
                            InterruptFlagRegister(value);
                            break;
                        case 2:
                            LeftCD_toRight_SPU_Volume = value;
                            break;
                        case 3:
                            ApplyVolume(value);
                            break;
                        default:
                            Console.WriteLine("[CDROM] Unknown Index (" + Index + ")" + " at CRROM IRQ flag register");
                            break;
                    }
                    break;

                default:
                    Console.WriteLine("[CDROM] Unhandled store at CRROM offset: " + offset + " index: " + Index);
                    break;
            }
        }

        public uint read(uint address)
        {
            uint offset = address - 0x1F801800;
            switch (offset)
            {
                case 0:
                    return CDROM_Status();
                case 1:
                    return ResponseBuffer.ReadNext();
                case 2:
                    return DATA.ReadByte();
                case 3:
                    switch (Index)
                    {
                        case 0:
                        case 2:
                            return (byte)(IRQ_Enable | 0xe0);
                        case 1:
                        case 3:
                            return (byte)(IRQ_Flag | 0xe0);
                        default:
                            Console.WriteLine("[CDROM] Unknown Index (" + Index + ")" + " at CRROM IRQ flag register");
                            return 0;
                    }
                default:
                    Console.WriteLine("[CDROM] Unhandled read at CRROM register: " + offset + " index: " + Index);
                    return 0;
            }
        }

        private void RequestRegister(byte value)
        {
            if (((value >> 7) & 1) == 1)
            {
                //Console.WriteLine("DATA REQUESTED");
                DATA.BFRD = 1;
                if (DATA.DataFifo.Count > 0)
                {
                    return;
                }
                DATA.MoveSectorToDataFifo();
            } else
            {
                //Console.WriteLine("FIFO CLEARED");
                DATA.BFRD = 0;
                DATA.DataFifo.Clear();
            }
        }

        private void InterruptFlagRegister(byte value)
        {
            IRQ_Flag &= (byte)~(value & 0x1F);
            if (((value >> 6) & 1) == 1)
            {
                ParameterBuffer.Clear();
            }

            if ((IRQ_Flag & 0x7) == 0 && Responses.Count > 0)
            {
                if (Responses.Peek().FinishedProcessing)
                {
                    Responses.Peek().delay = (int)Math.Ceiling(OneSecond * 0.00035); //350us
                }
            }
        }

        private void ApplyVolume(byte value)
        {
            bool isMute = (value & 1) != 0;
            bool applyVolume = ((value >> 5) & 1) != 0;
            if (isMute)
            {
                DATA.CurrentVolume.LtoL = 0;
                DATA.CurrentVolume.LtoR = 0;
                DATA.CurrentVolume.RtoL = 0;
                DATA.CurrentVolume.RtoR = 0;
            } else if (applyVolume)
            {
                DATA.CurrentVolume.LtoL = LeftCD_toLeft_SPU_Volume;
                DATA.CurrentVolume.LtoR = LeftCD_toRight_SPU_Volume;
                DATA.CurrentVolume.RtoL = RightCD_toLeft_SPU_Volume;
                DATA.CurrentVolume.RtoR = RightCD_toRight_SPU_Volume;
            }
        }

        private void Sync()
        {
            Error(Errors.InvalidCommand);
        }

        private void Test()
        {
            if (ParameterBuffer.Count == 0)
            {
                Error(Errors.InvalidNumberOfParameters);
                return;
            }
            byte parameter = ParameterBuffer.Dequeue();
            switch (parameter)
            {
                case 0x04:
                    ReadSCEx();
                    break;
                case 0x05:
                    GetSCExCounters();
                    break;
                case 0x20:
                    GetDateAndVersion();
                    break;
                case 0xFF:
                    Error(Errors.InvalidParameter);
                    break;
                default:
                    Console.WriteLine("[CDROM] Test command: unknown parameter: " + parameter.ToString("x"));
                    break;
            }
        }

        private void GetSCExCounters()
        {
            //"01h,01h" for Licensed
            Response ack = new Response(new byte[] { 0x0, 0x0 }, Delays.INT3_General, Flags.INT3, State);
            Responses.Enqueue(ack);
        }

        private void ReadSCEx()
        {
            //19h,04h --> INT3(stat) ;Read SCEx string (and force motor on)
            //stat |= 0x2;
            stat = 0x2;
            Response ack = new Response(new byte[] { stat }, Delays.INT3_General, Flags.INT3, State);
            Responses.Enqueue(ack);
        }

        private void MotorOn()
        {
            if ((stat & 1) != 0)
            {
                Console.WriteLine("[CDROM] MotorOn error!");
                Error(Errors.InvalidNumberOfParameters);
            } else
            {
                Console.WriteLine("[CDROM] MotorOn!");
                Response ack = new Response(new byte[] { stat }, Delays.INT3_General, Flags.INT3, State);
                stat |= 0x2;
                Response done = new Response(new byte[] { stat }, Delays.INT2_GetID, Flags.INT2, State);
                Responses.Enqueue(ack);
                Responses.Enqueue(done);
            }
        }
        private void ReadTOC()
        {
            //only in BIOS version vC1 and up.
            Response ack = new Response(new byte[] { stat }, Delays.INT3_General, Flags.INT3, State);
            Response done = new Response(new byte[] { stat }, Delays.INT2_GetID, Flags.INT2, State);
            Responses.Enqueue(ack);
            Responses.Enqueue(done);
        }

        private void Forward()
        {
            if (State != CDROMState.PlayingCDDA)
            {
                Error(Errors.CannotRespondYet);
                return;
            }
            SkipRate += 15;
            Response ack = new Response(new byte[] { stat }, Delays.INT3_General, Flags.INT3, State);
            Responses.Enqueue(ack);
        }

        private void Backward()
        {
            if (State != CDROMState.PlayingCDDA)
            {
                Error(Errors.CannotRespondYet);
                return;
            }
            SkipRate -= 15;
            Response ack = new Response(new byte[] { stat }, Delays.INT3_General, Flags.INT3, State);
            Responses.Enqueue(ack);
        }

        private void GetLocL()
        {
            //if (SeekedL || SeekedP || LidOpen)
            //{
            //    Error(Errors.CannotRespondYet);
            //    return;
            //}

            //INT3(amm,ass,asect,mode,file,channel,sm,ci)
            byte[] header = DATA.LastSectorHeader;      //MSF already in BCD?
            byte[] subHeader = DATA.LastSectorSubHeader;

            Response ack = new Response(new byte[] { header[0], header[1], header[2], header[3],
                subHeader[0], subHeader[1], subHeader[2], subHeader[3]}, Delays.INT3_General, Flags.INT3, State);
            Responses.Enqueue(ack);
        }

        private void GetLocP()
        {
            if (LidOpen)
            {
                Error(Errors.DriveDoorOpen);
                return;
            }

            //GetlocP - Command 11h - INT3(track,index,mm,ss,sect,amm,ass,asect) all BCD

            byte currentTrack = (byte)DATA.SelectedTrackNumber;

            bool pregap = CurrentPos < DATA.Disk.Tracks[currentTrack - 1].Start;

            byte index = DecToBcd((byte)(pregap ? 0x00 : 0x01));
            byte mm;
            byte ss;
            byte ff;
            byte amm;
            byte ass;
            byte aff;

            int cdm, cds, cdf;

            if (SeekedP)
            {   //Distance from the seeked position will be up to 50 frames around this location.
                (cdm, cds, cdf) = BytesToMSF(CurrentPos - (25 * 0x930) + (150 * 0x930));

            } else if (SeekedL)
            {   //Distance from the seeked position will be up to 10 frames around this location
                (cdm, cds, cdf) = BytesToMSF(CurrentPos - (10 * 0x930) + (150 * 0x930));

            } else
            {
                (cdm, cds, cdf) = (M, S, F);
            }
            int absoluteFrame = (cdm * 60 * 75) + (cds * 75) + cdf - 150;
            int trackFrame =
                (DATA.Disk.Tracks[DATA.SelectedTrackNumber - 1].M * 60 * 75) +
                (DATA.Disk.Tracks[DATA.SelectedTrackNumber - 1].S * 75) +
                (DATA.Disk.Tracks[DATA.SelectedTrackNumber - 1].F);

            int relativeM;
            int relativeS;
            int relativeF;

            (relativeM, relativeS, relativeF) = BytesToMSF((pregap ? trackFrame - absoluteFrame : absoluteFrame - trackFrame) * 0x930);

            currentTrack = DecToBcd(currentTrack);

            mm = DecToBcd((byte)relativeM);
            ss = DecToBcd((byte)relativeS);
            ff = DecToBcd((byte)relativeF);

            amm = DecToBcd((byte)cdm);
            ass = DecToBcd((byte)cds);
            aff = DecToBcd((byte)cdf);

            Response ack = new Response(new byte[] { currentTrack, index, mm, ss, ff, amm, ass, aff }, Delays.INT3_General, Flags.INT3, State);
            Responses.Enqueue(ack);
        }

        private void SetFilter()
        {
            DATA.Filter.fileNumber = ParameterBuffer.Dequeue();
            DATA.Filter.channelNumber = ParameterBuffer.Dequeue();
            Response ack = new Response(new byte[] { stat }, Delays.INT3_General, Flags.INT3, State);
            Responses.Enqueue(ack);
        }

        private void SeekP()
        {
            if (ParameterBuffer.Count > 0)
            {
                Error(Errors.InvalidNumberOfParameters);
                return;
            }
            stat |= 0x2;
            Response ack = new Response(new byte[] { stat }, Delays.INT3_General, Flags.INT3, CDROMState.Seeking);
            Responses.Enqueue(ack);

            int position = ((M * 60 * 75) + (S * 75) + F - 150) * 0x930;

            if (position > DATA.EndOfDisk)
            {
                stat = 0x6;
                Error(Errors.InvalidParameter, (long)(33868800 * (650 * 0.001)));
                return;
            }

            //may not be allowed to use SeekP in the data track
            int oldPosition = CurrentPos;
            CurrentPos = ((M * 60 * 75) + (S * 75) + F - 150) * 0x930;

            Response done = new Response(new byte[] { stat }, CalculateSeekTime((int)oldPosition, (int)CurrentPos), (int)Flags.INT2, CDROMState.Idle);
            Responses.Enqueue(done);
            SetLoc = false;
            DATA.SelectTrackAndRead(DATA.FindTrack(CurrentPos), CurrentPos);
            SeekedP = true;
        }

        //CD-DA Play
        private void Play()
        {
            int newIndex = ((M * 60 * 75) + (S * 75) + F) * 0x930;
            ReadRate = OneSecond / (DoubleSpeed ? 150 : 75);

            if (SetLoc)
            {
                ReadRate += (int)CalculateSeekTime((int)CurrentPos, (int)newIndex);
                SetLoc = false;
            }
            CurrentPos = newIndex;
            stat |= 0x2;
            SkipRate = 0;
            Response ack = new Response(new byte[] { stat }, Delays.INT3_General, Flags.INT3, CDROMState.PlayingCDDA);
            Responses.Enqueue(ack);

            if (ParameterBuffer.Count > 0 && ParameterBuffer.Peek() > 0)
            {
                int trackNumber = ParameterBuffer.Dequeue();
                if (trackNumber == 1)
                    trackNumber++; //Track 1 is always the data track, so we skip it
                if (DATA.SelectedTrackNumber != trackNumber)
                {
                    DATA.SelectTrackAndRead(trackNumber, CurrentPos);
                }

                M = DATA.Disk.Tracks[DATA.SelectedTrackNumber - 1].M;
                S = DATA.Disk.Tracks[DATA.SelectedTrackNumber - 1].S;
                F = DATA.Disk.Tracks[DATA.SelectedTrackNumber - 1].F;
                CurrentPos = ((M * 60 * 75) + (S * 75) + F) * 0x930;
            } else
            {
                int trackNumber = DATA.FindTrack(CurrentPos, 1);
                //Console.WriteLine($"[CDROM] CD-DA Pos: {CurrentIndex} FindTrack: {trackNumber}");
                if (DATA.SelectedTrackNumber != trackNumber)
                {
                    DATA.SelectTrackAndRead(trackNumber, CurrentPos);
                }
            }

            Console.WriteLine($"[CDROM] CD-DA at MSF {M}:{S}:{F} Track {DATA.SelectedTrackNumber}");
        }

        private void Stop()
        {
            Responses.Clear();
            stat = 0x2;
            Response ack = new Response(new byte[] { stat }, Delays.INT3_General, Flags.INT3, CDROMState.Idle);
            stat = 0x0;
            Response done = new Response(new byte[] { stat }, DoubleSpeed ? Delays.INT2_Stop_DoubleSpeed : Delays.INT2_Stop_SingleSpeed, Flags.INT2, CDROMState.Idle);
            Responses.Enqueue(ack);
            Responses.Enqueue(done);

            //may not need?
            if (DATA.Disk != null && DATA.Disk.IsValid)
            {
                M = DATA.Disk.Tracks[0].M;
                S = DATA.Disk.Tracks[0].S;
                F = DATA.Disk.Tracks[0].F;
                DATA.SelectTrackAndRead(1, CurrentPos);
            }
        }

        private void GetTD()
        {
            //GetTD - Command 14h,track --> INT3(stat,mm,ss) ;BCD
            if (ParameterBuffer.Count != 1)
            {
                Error(Errors.InvalidNumberOfParameters);
                return;
            }
            int BCD = ParameterBuffer.Dequeue();
            int N = BcdToDec((byte)BCD);
            int lastIndex = DATA.Disk.Tracks.Count - 1;
            int lastTrack = DATA.Disk.Tracks[lastIndex].TrackNumber;

            if (N > lastTrack || !IsValidBCD((byte)BCD))
            {
                Error(Errors.InvalidParameter);
                return;
            }
            Response ack;
            if (N == 0)
            {
                (int durationM, int durationS, int durationF) = BytesToMSF(DATA.Disk.Tracks[lastIndex].Length);

                int startM = DATA.Disk.Tracks[lastIndex].M;
                int startS = DATA.Disk.Tracks[lastIndex].S;

                int totalSeconds = (startM * 60 + startS) + (durationM * 60 + durationS);

                M = totalSeconds / 60;
                S = totalSeconds % 60;

                ack = new Response(new byte[] { stat, DecToBcd((byte)M), DecToBcd((byte)S) }, Delays.INT3_General, Flags.INT3, State);
                Responses.Enqueue(ack);
                Console.WriteLine($"[CDROM] GetTD 0 Total MSF: {M}:{S}:{0}");
            } else
            {
                byte M = (byte)DATA.Disk.Tracks[N - 1].M;
                byte S = (byte)(DATA.Disk.Tracks[N - 1].S);
                ack = new Response(new byte[] { stat, DecToBcd((byte)M), DecToBcd((byte)S) }, Delays.INT3_General, Flags.INT3, State);
                Responses.Enqueue(ack);
                Console.WriteLine($"[CDROM] GetTD: Track {N}, MSF: {M}:{S}:{0}");
            }
        }

        private void GetTN()
        {
            //GetTN - Command 13h --> INT3(stat,first,last) ;BCD
            if (ParameterBuffer.Count > 0)
            {
                Error(Errors.InvalidNumberOfParameters);
                return;
            }

            int lastIndex = DATA.Disk.Tracks.Count - 1;
            byte firstTrack = DecToBcd((byte)DATA.Disk.Tracks[0].TrackNumber);
            byte lastTrack = DecToBcd((byte)DATA.Disk.Tracks[lastIndex].TrackNumber);
            Response ack = new Response(new byte[] { stat, firstTrack, lastTrack }, Delays.INT3_General, Flags.INT3, State);
            Responses.Enqueue(ack);

            Console.WriteLine($"[CDROM] GetTN Tracks: {firstTrack} - {DATA.Disk.Tracks[lastIndex].TrackNumber}");
        }

        private void Mute()
        {
            Response ack = new Response(new byte[] { stat }, Delays.INT3_General, Flags.INT3, State);
            Responses.Enqueue(ack);
        }

        private void UnMute()
        {
            Response ack = new Response(new byte[] { stat }, Delays.INT3_General, Flags.INT3, State);
            Responses.Enqueue(ack);
        }

        private void Pause()
        {
            Responses.Clear();

            Response ack = new Response(new byte[] { stat }, Delays.INT3_General, Flags.INT3, CDROMState.Idle);
            stat = 0x2;
            Response done = new Response(new byte[] { stat }, DoubleSpeed ? Delays.INT2_Pause_DoubleSpeed : Delays.INT2_Pause_SingleSpeed, Flags.INT2, CDROMState.Idle);
            //Response done = new Response(new byte[] { stat }, Delays.INT3_General, Flags.INT2, CDROMState.Idle);
            Responses.Enqueue(ack);
            Responses.Enqueue(done);
        }

        private void Init()
        {

            if (ParameterBuffer.Count > 0)
            {
                ParameterBuffer.Clear();
                Error(Errors.InvalidNumberOfParameters);
                Console.WriteLine("[CDROM] Init error, too many parameters");
                return;
            }

            Mode = 0;
            stat = 0x2;
            Responses.Clear();
            DATA.DataFifo.Clear();
            //DataController.SectorBuffer.Clear();
            DATA.SelectTrackAndRead(1, 0);
            M = 0x00;
            S = 0x02;
            F = 0x00;
            SeekedL = SeekedP = false;
            //Responses.Enqueue(new Response(new byte[] { stat }, Delays.INT3_Init, Flags.INT3, CDROMState.Idle));
            //Responses.Enqueue(new Response(new byte[] { stat }, Delays.INT2_Init, Flags.INT2, CDROMState.Idle));
            Responses.Enqueue(new Response(new byte[] { stat }, Delays.Zero, Flags.INT3, CDROMState.Idle));
            Responses.Enqueue(new Response(new byte[] { stat }, Delays.Zero, Flags.INT2, CDROMState.Idle));
        }

        private void ReadNS()
        {
            if (DATA.Disk.IsAudioDisk)
            {
                if (!IsCDDA)
                {   //return error 0x40
                    Error(Errors.InvalidCommand);
                    return;
                }
            }

            int newIndex = ((M * 60 * 75) + (S * 75) + F - 150) * 0x930;
            ReadRate = OneSecond / (DoubleSpeed ? 150 : 75);

            if (SetLoc)
            {
                ReadRate += CalculateSeekTime(CurrentPos, newIndex);
                SetLoc = false;
            }

            CurrentPos = newIndex;

            //stat |= 0x2;
            stat = 0x2;
            stat |= 0x20;
            Response ack = new Response(new byte[] { stat }, Delays.Zero, Flags.INT3, CDROMState.ReadingData); //INT3_General
            Responses.Enqueue(ack);

            if (CurrentPos > DATA.Disk.Tracks[0].Length)
            {
                int last = DATA.Disk.Tracks.Count - 1;
                ack.NextState = CDROMState.Idle;
                stat = 0x6;
                Errors error;
                int delay;
                if (CurrentPos > DATA.EndOfDisk)
                {
                    error = Errors.InvalidParameter;
                    delay = (int)(OneSecond * 0.7);
                } else
                {
                    error = Errors.SeekError;
                    delay = (OneSecond * 4) + 300000;
                }
                Error(error, delay);
                return;
            }

            if (DATA.SelectedTrackNumber != 1)
            {
                DATA.SelectTrackAndRead(1, CurrentPos);
            }
        }

        private void SetMode()
        {
            if (ParameterBuffer.Count != 1)
            {
                Error(Errors.InvalidNumberOfParameters);
                return;
            }

            Mode = ParameterBuffer.Dequeue();

            if (((Mode >> 4) & 1) == 0)
            {
                if (((Mode >> 5) & 1) == 0)
                {
                    LastSize = 0x800;
                    SectorOffset = 24;
                } else
                {
                    LastSize = 0x924;
                    SectorOffset = 12;
                }
            }

            DATA.BytesToSkip = SectorOffset;
            DATA.SizeOfDataSegment = LastSize;
            DATA.Filter.IsEnabled = ((Mode >> 3) & 1) != 0;

            DoubleSpeed = ((Mode >> 7) & 1) != 0;
            AutoPause = ((Mode >> 1) & 1) != 0;  //For audio play only
            CDDAReport = ((Mode >> 2) & 1) != 0; //For audio play only
            IsCDDA = (Mode & 1) != 0;
            DATA.XA_ADPCM_En = ((Mode >> 6) & 1) != 0;    //(0=Off, 1=Send XA-ADPCM sectors to SPU Audio Input)

            Responses.Enqueue(new Response(new byte[] { stat }, (Delays)CombineDelaySet, Flags.INT3, State)); //INT3_General
        }

        private void SeekL()
        {
            if (ParameterBuffer.Count > 0)
            {
                Error(Errors.InvalidNumberOfParameters);
                return;
            }
            stat |= 0x2;
            Response ack = new Response(new byte[] { stat }, Delays.INT3_General, Flags.INT3, CDROMState.Seeking);
            Responses.Enqueue(ack);

            int position = ((M * 60 * 75) + (S * 75) + F - 150) * 0x930;

            if (position > DATA.EndOfDisk)
            {
                stat = 0x6;
                Error(Errors.InvalidParameter, 33868800);
                Console.WriteLine("[CDROM] SeekL error, out of disk!");
                return;
            }

            if (position > DATA.Disk.Tracks[0].Length)
            {
                stat = 0x6;
                Error(Errors.SeekError, (OneSecond * 4) + 300000);
                return;
            }

            int oldPosition = CurrentPos;
            CurrentPos = ((M * 60 * 75) + (S * 75) + F - 150) * 0x930;

            Response done = new Response(new byte[] { stat }, CalculateSeekTime(oldPosition, CurrentPos), (int)Flags.INT2, CDROMState.Idle);
            Responses.Enqueue(done);
            SetLoc = false;
            DATA.SelectTrackAndRead(DATA.FindTrack(CurrentPos), CurrentPos);
            SeekedL = true;
        }

        private void Setloc()
        {
            if (ParameterBuffer.Count != 3)
            {
                Error(Errors.InvalidNumberOfParameters);
                return;
            }

            SeekParameters[0] = ParameterBuffer.Dequeue();  //Minutes
            SeekParameters[1] = ParameterBuffer.Dequeue();  //Seconds 
            SeekParameters[2] = ParameterBuffer.Dequeue();  //Frames

            int MM = ((SeekParameters[0] & 0xF) * 1) + (((SeekParameters[0] >> 4) & 0xF) * 10);
            int SS = ((SeekParameters[1] & 0xF) * 1) + (((SeekParameters[1] >> 4) & 0xF) * 10);
            int FF = ((SeekParameters[2] & 0xF) * 1) + (((SeekParameters[2] >> 4) & 0xF) * 10);

            //int MM = BcdToDec(SeekParameters[0]);
            //int SS = BcdToDec(SeekParameters[1]);
            //int FF = BcdToDec(SeekParameters[2]);

            if (IsValidBCD(SeekParameters[0]) && IsValidBCD(SeekParameters[1]) && IsValidBCD(SeekParameters[2]) && IsValidSetloc(MM, SS, FF))
            {
                M = MM;
                S = SS;
                F = FF;
                Responses.Enqueue(new Response(new byte[] { stat }, Delays.INT3_General, Flags.INT3, State));
            } else
            {
                Error(Errors.InvalidParameter);
            }
            SetLoc = true;
            //Console.WriteLine($"[CDROM] Setloc {M}:{S}:{F}");
        }

        private void Error(Errors code)
        {
            Console.WriteLine($"[CDROM] Error {code} ");
            int open = LidOpen ? (1 << 4) : 0;
            CDROMState nextState = LidOpen ? CDROMState.SwappingDisk : CDROMState.Idle;
            stat = (byte)(0x3 | open);
            Responses.Enqueue(new Response(new byte[] { stat, (byte)code }, Delays.INT5, Flags.INT5, nextState));
            stat = (byte)(0x2 | open);
        }

        private void Error(Errors code, long Delay)
        {
            Console.WriteLine($"[CDROM] Error {code} ");
            int open = LidOpen ? (1 << 4) : 0;
            CDROMState nextState = LidOpen ? CDROMState.SwappingDisk : CDROMState.Idle;
            Responses.Enqueue(new Response(new byte[] { (byte)(stat | open), (byte)code }, Delay, (int)Flags.INT5, nextState));
            stat = (byte)(0x2 | open);
        }

        private void GetID()
        {
            if (ParameterBuffer.Count > 0)
            {
                Error(Errors.InvalidNumberOfParameters);
                return;
            }
            Response ack = new Response(new byte[] { stat }, Delays.INT3_General, Flags.INT3, State);
            Responses.Enqueue(ack);
            Response done;

            if (DATA.Disk != null && DATA.Disk.IsValid)
            {
                if (DATA.Disk.IsAudioDisk)
                {
                    done = new Response(new byte[] { 0x0A, 0x90, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, Delays.INT5, Flags.INT5, State);
                } else
                {
                    done = new Response(new byte[] { 0x02, 0x00, 0x20, 0x00, 0x53, 0x43, 0x45, 0x41 }, Delays.INT2_GetID, Flags.INT2, State);
                }
            } else
            {
                //No Disk -> INT3(stat) -> INT5(08h,40h, 00h,00h)
                done = new Response(new byte[] { 0x08, 0x40, 0x00, 0x00 }, Delays.INT5, Flags.INT5, State);
            }
            Responses.Enqueue(done);
        }

        private void GetStat()
        {
            if (!LidOpen)
            {
                stat = (byte)(stat & (~0x18));
                stat |= 0x2;
            }

            if (ParameterBuffer.Count > 0)
            {
                ParameterBuffer.Clear();
                Error(Errors.InvalidNumberOfParameters);
                stat = 0x2;
                return;
            }
            Response ack = new Response(new byte[] { stat }, Delays.INT3_General, Flags.INT3, State);
            Responses.Enqueue(ack);
        }

        private void GetDateAndVersion()
        {
            if (ParameterBuffer.Count > 0)
            {
                Error(Errors.InvalidNumberOfParameters);
                return;
            }
            Response ack = new Response(new byte[] { 0x94, 0x09, 0x19, 0xC0 }, Delays.INT3_General, Flags.INT3, State);
            Responses.Enqueue(ack);
        }

        private byte[] GetCDDAReport()
        {
            //Report --> INT1(stat,track,index,mm/amm,ss+80h/ass,sect/asect,peaklo,peakhi)
            bool isEven = ((F / 10) & 1) == 0;
            int track = DATA.SelectedTrackNumber;
            int mm;
            int ss;
            int ff;
            int index = 0x01; //TODO handle multi index tracks
            bool pregap = CurrentPos < DATA.Disk.Tracks[track - 1].Start;
            int or = 0;
            if (isEven)
            {
                mm = M;
                ss = S;
                ff = F;

            } else
            {
                int trackStart = DATA.Disk.Tracks[track - 1].Start;
                int difference = (int)(pregap ? trackStart - CurrentPos : CurrentPos - trackStart);
                (mm, ss, ff) = BytesToMSF(difference);
                or = 0x80;
            }
            return new byte[] { stat, DecToBcd((byte)track), DecToBcd((byte)index), DecToBcd((byte)mm), (byte)(DecToBcd((byte)ss) | or), DecToBcd((byte)ff), 0x00, 0xFF };
        }

        void IncrementIndex(byte offset)
        {
            F = (F + 1 + SkipRate);

            if (F >= 75)
            {
                S = S + (F / 75);
                F = F % 75;
            }

            if (S >= 60)
            {
                M = M + (S / 60);
                S = S % 60;
            }

            CurrentPos = ((M * 60 * 75) + (S * 75) + F - offset) * 0x930;
            SeekedP = SeekedL = false;

            if (SkipRate < 0 && CurrentPos < DATA.Disk.Tracks[0].Start)
            {
                SkipRate = 0;
            }

            if (SkipRate > 0 && CurrentPos > DATA.EndOfDisk)
            {
                SkipRate = 0;
                stat = 0;
                Responses.Enqueue(new Response(new byte[] { stat }, Delays.INT3_General, Flags.INT4, CDROMState.Idle));
                Console.WriteLine("[CDROM] IncrementIndex: end of disk!");
            }
        }

        private void Step(int cycles)
        {
            int count = Responses.Count;
            for (int i = 0; i < count; i++)
            {
                Responses.ElementAt(i).delay -= cycles;
                if (Responses.ElementAt(i).delay >= 0)
                {
                    break;
                } else
                {
                    //Responses.ElementAt(i).delay = (int)Math.Ceiling(33868800 * 0.0006);   //600us
                    Responses.ElementAt(i).FinishedProcessing = true;
                }
            }
        }

        public Span<uint> processDmaRead(int size)
        {
            //Console.WriteLine($"[CDROM] DMA Read Size: {size}");

            if (size <= 0 || size > 0x800)
            {
                Console.WriteLine($"[CDROM] Invalid DMA Read Size: {size}");
            }

            if (DATA.DataFifo.Count < size * 4)
            {
                Console.WriteLine("[CDROM] Not enough data in FIFO for DMA Read, requested: " + size * 4 + ", available: " + DATA.DataFifo.Count);
                return Span<uint>.Empty;
            }

            Span<uint> data = new uint[size];

            for (int i = 0; i < size; i++)
            {
                data[i] = DATA.ReadWord();
            }

            return data;
        }

        public bool tick(int cycles)
        {
            Step(cycles);

            if (Responses.Count > 0)
            {
                if ((IRQ_Flag & 0x7) == 0 && Responses.Peek().delay <= 0)
                {
                    Response current = Responses.Dequeue();
                    State = current.NextState;
                    IRQ_Flag |= (byte)current.interrupt;
                    ResponseBuffer.WriteBuffer(ref current.values);
                    if ((IRQ_Enable & IRQ_Flag) != 0)
                    {
                        IRQCTL.set(Interrupt.CDROM);
                    }
                }
            }

            if (TransmissionDelay > 0)
            {
                TransmissionDelay -= cycles;
            }

            switch (State)
            {
                case CDROMState.Idle:
                    stat = 0x2;
                    return false;

                case CDROMState.SwappingDisk:
                    if (SwappingDelay > 0)
                    {
                        SwappingDelay -= cycles;
                        stat |= (1 << 4);  //Lid open bit 0x18
                        LidOpen = true;
                    } else
                    {
                        Console.WriteLine($"[CDROM] Swaped Disk");
                        State = CDROMState.Idle;
                        LidOpen = false;
                    }
                    return false;

                case CDROMState.Seeking:
                    stat |= (1 << 6);
                    return false;

                case CDROMState.ReadingData:
                    if ((ReadRate -= cycles) > 0)
                    {
                        return false;
                    }

                    if (CurrentPos >= DATA.EndOfDisk)
                    {
                        Response pause = new Response(new byte[] { stat }, Delays.Zero, Flags.INT4, CDROMState.Idle);
                        Responses.Enqueue(pause);
                        Console.WriteLine("[CDROM] End of Disk!");
                        return false;
                    }

                    //Read at least one sector before setting the bit
                    stat |= (1 << 5);

                    //Console.WriteLine($"[CDROM] Data Read at {CurrentPos}");

                    bool sendToCPU = DATA.LoadNewSector(CurrentPos);
                    IncrementIndex(150);
                    if (sendToCPU)
                    {
                        Response sectorAck = new Response(new byte[] { stat }, Delays.Zero, Flags.INT1, CDROMState.ReadingData);
                        Responses.Enqueue(sectorAck);
                    }
                    ReadRate = OneSecond / (DoubleSpeed ? 150 : 75);

                    break;

                case CDROMState.PlayingCDDA:
                    if ((ReadRate -= cycles) > 0)
                    {
                        return false;
                    }

                    if (CurrentPos >= DATA.EndOfDisk || (CurrentPos >= DATA.EndOfTrack && AutoPause))
                    {
                        Response pause = new Response(new byte[] { stat }, Delays.Zero, Flags.INT4, CDROMState.Idle);
                        Responses.Enqueue(pause);
                        Console.WriteLine("[CDROM] CD-DA Paused Track: " + DATA.SelectedTrackNumber);
                        return false;
                    }

                    //Play at least one sector before setting the bit
                    stat |= (1 << 7);

                    //if (DATA.Disk.HasCue)
                    if (IsCDDA)
                    {
                        DATA.PlayCDDA(CurrentPos);
                    } else
                    {
                        Console.WriteLine("[CD-ROM] Not IsCDDA");
                    }

                    if (CDDAReport && IsReportableSector)
                    {
                        Response report = new Response(GetCDDAReport(), Delays.Zero, Flags.INT1, CDROMState.PlayingCDDA);
                        Responses.Enqueue(report);
                    }

                    IncrementIndex(0);
                    ReadRate = OneSecond / (DoubleSpeed ? 150 : 75);
                    break;
            }

            return false;
        }

        private (int, int, int) BytesToMSF(int totalSize)
        {
            int totalFrames = totalSize / 2352;
            int M = totalFrames / (60 * 75);
            int S = (totalFrames % (60 * 75)) / 75;
            int F = (totalFrames % (60 * 75)) % 75;
            return (M, S, F);
        }

        private byte DecToBcd(byte value)
        {
            return (byte)(value + 6 * (value / 10));
        }

        private int BcdToDec(byte value)
        {
            return value - 6 * (value >> 4);
        }

        private bool IsValidBCD(byte value)
        {
            return ((value & 0xF) < 0xA) && (((value >> 4) & 0xF) < 0xA);
        }

        private bool IsValidSetloc(int M, int S, int F)
        {
            return M <= 99 && S <= 59 && F <= 74;
        }

        private long CalculateSeekTime(int position, int destination)
        {
            long wait = (long)((long)OneSecond * 2 * 0.001 * (Math.Abs((position - destination)) / (75 * 0x930)));
            //Console.WriteLine("[CDROM] Difference of: " + (Math.Abs((position - destination)) / (75 * 0x930)) + " Seconds");
            //Console.WriteLine("[CDROM] Seek time: " + wait);
            return (long)Delays.INT3_General;
        }
    }
}
