using System;
using System.Runtime.InteropServices;
using MessagePack;
using ScePSX.CdRom;

namespace ScePSX
{
    public class SPU
    {
        //[IgnoreMember]
        //public unsafe byte* RAM = (byte*)Marshal.AllocHGlobal(512 * 1024);
        byte[] RAM = new byte[512 * 1024];

        ushort SPUCNT;
        bool reverbEnabled;
        bool SPUEnable;
        bool IRQ9Enable;
        bool CDAudioEnable;
        bool CDReverbEnable;
        bool SPUMuted;

        //STAT:
        byte SPU_Mode;                          //0-5
        byte IRQ_Flag;                          //6
        public byte DMA_Read_Write_Request;     //7 seems to be same as SPUCNT.Bit5
        public byte DMA_Write_Request;          //8            
        public byte DMA_Read_Request;           //9            
        byte Data_transfer_busy = 0;                //10 (1 = busy)           
        byte Writing_Capture_Buffers = 0;           //11 Writing to First/Second half of Capture Buffers (0=First, 1=Second)

        short mainVolumeLeft;
        short mainVolumeRight;
        short vLOUT;
        short vROUT;

        uint KOFF;
        uint KON;

        uint PMON;

        uint NON;           //Noise mode enable
        uint EON;           //Echo 

        uint CDInputVolume;
        uint external_Audio_Input_Volume;

        ushort transfer_Control;
        uint transfer_address;
        uint currentAddress;
        uint reverbCurrentAddress;
        SPUVoice[] voices = new SPUVoice[24];
        uint SPU_IRQ_Address;

        private int clk_counter = 0;
        public const uint CYCLES_PER_SAMPLE = 0x300; //0x300
        byte[] outputBuffer = new byte[2048];
        int outputBufferPtr = 0;
        int sumLeft;
        int sumRight;
        private int reverbCounter = 0;
        uint captureOffset = 0;

        //Reverb registers
        ushort mBASE;   //(divided by 8)
        ushort dAPF1;   //Type: disp
        ushort dAPF2;   //Type: disp
        short vIIR;     //Type: volume
        short vCOMB1;   //Type: volume
        short vCOMB2;   //Type: volume
        short vCOMB3;   //Type: volume
        short vCOMB4;   //Type: volume
        short vWALL;    //Type: volume
        short vAPF1;    //Type: volume
        short vAPF2;    //Type: volume
        ushort mLSAME;  //Type: src/dst
        ushort mRSAME;  //Type: src/dst
        ushort mLCOMB1; //Type: src
        ushort mRCOMB1; //Type: src
        ushort mLCOMB2; //Type: src
        ushort mRCOMB2; //Type: src
        ushort dLSAME;  //Type: src
        ushort dRSAME;  //Type: src
        ushort mLDIFF;  //Type: src/dst
        ushort mRDIFF;  //Type: src/dst
        ushort mLCOMB3; //Type: src
        ushort mRCOMB3; //Type: src
        ushort mLCOMB4; //Type: src
        ushort mRCOMB4; //Type: src
        ushort dLDIFF;  //Type: src
        ushort dRDIFF;  //Type: src
        ushort mLAPF1;  //Type: src/dst
        ushort mRAPF1;  //Type: src/dst
        ushort mLAPF2;  //Type: src/dst
        ushort mRAPF2;  //Type: src/dst
        short vLIN;     //Type: volume
        short vRIN;     //Type: volume

        [IgnoreMember]
        public ICoreHandler host;

        [IgnoreMember]
        public CDData CDDataControl;
        [IgnoreMember]
        public IRQController IrqController;

        public SPU()
        {
        }

        public SPU(ICoreHandler host, CDData CDControl, IRQController IrqController)
        {
            this.host = host;
            this.IrqController = IrqController;
            this.CDDataControl = CDControl;

            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = new SPUVoice(1);
            }
        }

        public void write(uint address, ushort value)
        {
            uint offset = address - 0x1F801C00;
            switch (offset)
            {
                //Voice 0...23 
                case uint when ((offset + 0x1F801C00) >= 0x1F801C00 && (offset + 0x1F801C00) <= 0x1F801D7F):

                    //index = offset/16 - 0xC0
                    uint index = (((offset + 0x1F801C00) & 0xFF0) >> 4) - 0xC0;

                    switch ((offset + 0x1F801C00) & 0xf)
                    {
                        case 0x0:
                            voices[index].volumeLeft = (short)value;
                            break;
                        case 0x2:
                            voices[index].volumeRight = (short)value;
                            break;
                        case 0x4:
                            voices[index].ADPCM_Pitch = value;
                            break;
                        case 0x6:
                            voices[index].ADPCM = value;
                            break;
                        case 0x8:
                            voices[index].setADSR_LOW(value);
                            break;
                        case 0xA:
                            voices[index].setADSR_HI(value);
                            break;
                        case 0xC:
                            voices[index].adsr.adsrVolume = value;
                            break;
                        case 0xE:
                            voices[index].ADPCM_RepeatAdress = value;
                            break;
                        default:
                            Console.WriteLine("[SPU] Unknown voice register: " + (offset & 0xf).ToString("x"));
                            break;
                    }
                    break;

                case 0x1aa:
                    setCtrl(value);
                    break;
                case 0x180:
                    mainVolumeLeft = (short)value;
                    break;
                case 0x182:
                    mainVolumeRight = (short)value;
                    break;
                case 0x184:
                    vLOUT = (short)value;
                    break;
                case 0x186:
                    vROUT = (short)value;
                    break;
                case 0x188:
                    KON = (KON & 0xFFFF0000) | value;
                    break;
                case 0x18a:
                    KON = (KON & 0x0000FFFF) | ((uint)(value << 16));
                    break;
                case 0x18c:
                    KOFF = (KOFF & 0xFFFF0000) | value;
                    break;
                case 0x18e:
                    KOFF = (KOFF & 0x0000FFFF) | ((uint)(value << 16));
                    break;
                case 0x190:
                    PMON = (PMON & 0xFFFF0000) | value;
                    break;
                case 0x192:
                    PMON = (PMON & 0x0000FFFF) | ((uint)(value << 16));
                    break;
                case 0x194:
                    NON = (NON & 0xFFFF0000) | value;
                    break;
                case 0x196:
                    NON = (NON & 0x0000FFFF) | ((uint)(value << 16));
                    break;
                case 0x198:
                    EON = (EON & 0xFFFF0000) | value;
                    break;
                case 0x19a:
                    EON = (EON & 0x0000FFFF) | ((uint)(value << 16));
                    break;
                case 0x1b0:
                    CDInputVolume = (CDInputVolume & 0xFFFF0000) | value;
                    break;
                case 0x1b2:
                    CDInputVolume = (CDInputVolume & 0x0000FFFF) | ((uint)(value << 16));
                    break;
                case 0x1b4:
                    external_Audio_Input_Volume = (external_Audio_Input_Volume & 0xFFFF0000) | value;
                    break;
                case 0x1b6:
                    external_Audio_Input_Volume = (external_Audio_Input_Volume & 0x0000FFFF) | ((uint)(value << 16));
                    break;
                case 0x1ac:
                    transfer_Control = value;
                    break;
                case 0x1a4:
                    SPU_IRQ_Address = ((uint)value) << 3;
                    break;
                case 0x1a6:
                    transfer_address = value;
                    currentAddress = ((uint)value) << 3; //multiplied by 8
                    break;

                case 0x1a8:
                    if ((transfer_Control >> 1 & 7) != 2)
                    {
                        Console.WriteLine($"[SPU] transfer_Control Err {(transfer_Control >> 1 & 7)}");
                    }
                    currentAddress &= 0x7FFFF;
                    if ((currentAddress <= SPU_IRQ_Address) && ((currentAddress + 1) >= SPU_IRQ_Address))
                    {
                        SPU_IRQ();
                    }
                    RAM[currentAddress++] = (byte)(value & 0xFF);
                    RAM[currentAddress++] = (byte)((value >> 8) & 0xFF);

                    break;

                //Read only?
                case 0x19C:
                    for (int i = 0; i < 16; i++)
                    {
                        voices[i].ENDX = (uint)((value >> i) & 1);
                    }
                    break;

                case 0x19E:
                    for (int i = 16; i < voices.Length; i++)
                    {
                        voices[i].ENDX = (uint)((value >> (i - 16)) & 1);
                    }
                    break;

                //Reverb area
                case 0x1a2:
                    mBASE = value;
                    reverbCurrentAddress = (uint)value << 3;
                    break;

                case 0x1c0:
                    dAPF1 = value;
                    break;
                case 0x1c2:
                    dAPF2 = value;
                    break;
                case 0x1c4:
                    vIIR = (short)value;
                    break;
                case 0x1c6:
                    vCOMB1 = (short)value;
                    break;
                case 0x1c8:
                    vCOMB2 = (short)value;
                    break;
                case 0x1cA:
                    vCOMB3 = (short)value;
                    break;
                case 0x1cC:
                    vCOMB4 = (short)value;
                    break;
                case 0x1cE:
                    vWALL = (short)value;
                    break;
                case 0x1D0:
                    vAPF1 = (short)value;
                    break;
                case 0x1D2:
                    vAPF2 = (short)value;
                    break;
                case 0x1D4:
                    mLSAME = value;
                    break;
                case 0x1D6:
                    mRSAME = value;
                    break;
                case 0x1D8:
                    mLCOMB1 = value;
                    break;
                case 0x1DA:
                    mRCOMB1 = value;
                    break;
                case 0x1DC:
                    mLCOMB2 = value;
                    break;
                case 0x1DE:
                    mRCOMB2 = value;
                    break;
                case 0x1E0:
                    dLSAME = value;
                    break;
                case 0x1E2:
                    dRSAME = value;
                    break;
                case 0x1E4:
                    mLDIFF = value;
                    break;
                case 0x1E6:
                    mRDIFF = value;
                    break;
                case 0x1E8:
                    mLCOMB3 = value;
                    break;
                case 0x1EA:
                    mRCOMB3 = value;
                    break;
                case 0x1EC:
                    mLCOMB4 = value;
                    break;
                case 0x1EE:
                    mRCOMB4 = value;
                    break;
                case 0x1F0:
                    dLDIFF = value;
                    break;
                case 0x1F2:
                    dRDIFF = value;
                    break;
                case 0x1F4:
                    mLAPF1 = value;
                    break;
                case 0x1F6:
                    mRAPF1 = value;
                    break;
                case 0x1F8:
                    mLAPF2 = value;
                    break;
                case 0x1FA:
                    mRAPF2 = value;
                    break;
                case 0x1FC:
                    vLIN = (short)value;
                    break;
                case 0x1FE:
                    vRIN = (short)value;
                    break;

                default:
                    Console.WriteLine("[SPU] Offset: " + offset.ToString("x") + "\nFull address: 0x" + (offset + 0x1f801c00).ToString("x"));
                    break;
            }
        }

        public ushort read(uint address)
        {
            uint offset = address - 0x1F801C00;
            ushort endx = 0;

            switch (offset)
            {
                case 0x1aa:
                    return SPUCNT;
                case 0x1ae:
                    return readStat();
                case 0x180:
                    return (ushort)mainVolumeLeft;
                case 0x182:
                    return (ushort)mainVolumeRight;
                case 0x184:
                    return (ushort)vLOUT;
                case 0x186:
                    return (ushort)vROUT;
                case 0x188:
                    return (ushort)KON;
                case 0x18a:
                    return (ushort)(KON >> 16);
                case 0x18c:
                    return (ushort)KOFF;
                case 0x18e:
                    return (ushort)(KOFF >> 16);
                case 0x1ac:
                    return transfer_Control;

                //Voice 0...23 
                case uint when ((offset + 0x1F801C00) >= 0x1F801C00 && (offset + 0x1F801C00) <= 0x1F801D7F):

                    //offset/16 - 0xC0
                    uint index = (((offset + 0x1F801C00) & 0xFF0) >> 4) - 0xC0;

                    switch ((offset + 0x1F801C00) & 0xf)
                    {

                        case 0x0:
                            return (ushort)voices[index].volumeLeft;
                        case 0x2:
                            return (ushort)voices[index].volumeRight;
                        case 0x4:
                            return voices[index].ADPCM_Pitch;
                        case 0x6:
                            return voices[index].ADPCM;
                        case 0x8:
                            return voices[index].adsr.adsrLOW;
                        case 0xA:
                            return voices[index].adsr.adsrHI;
                        case 0xC:
                            return voices[index].adsr.adsrVolume;
                        case 0xE:
                            return voices[index].ADPCM_RepeatAdress;

                        default:
                            Console.WriteLine("[SPU] Unknown voice register: " + (offset & 0xf).ToString("x"));
                            return 0;

                    }

                // 1F801E00h..1F801E5Fh - Voice 0..23 Internal Registers
                case uint when ((offset + 0x1F801C00) >= 0x1F801E00 && (offset + 0x1F801C00) <= 0x1F801E5F):
                    return 0;

                //1F801D98h - Voice 0..23 Reverb mode aka Echo On (EON) (R/W)
                case 0x190:
                    return (ushort)PMON;
                case 0x192:
                    return (ushort)(PMON >> 16);
                case 0x194:
                    return (ushort)NON;
                case 0x196:
                    return (ushort)(NON >> 16);
                case 0x198:
                    return (ushort)EON;
                case 0x19a:
                    return (ushort)(EON >> 16);
                //Read only?
                case 0x19C:
                    for (int i = 0; i < 16; i++)
                    {
                        endx |= (ushort)(voices[i].ENDX << i);
                    }
                    return endx;
                case 0x19E:
                    for (int i = 16; i < voices.Length; i++)
                    {
                        endx |= (ushort)(voices[i].ENDX << i);
                    }
                    return endx;

                case 0x1b0:
                    return (ushort)CDInputVolume;
                case 0x1b2:
                    return (ushort)(CDInputVolume >> 16);
                case 0x1b8:
                    return (ushort)mainVolumeLeft;
                case 0x1ba:
                    return (ushort)mainVolumeRight;
                case 0x1a4:
                    return (ushort)(SPU_IRQ_Address >> 3);
                case 0x1a6:
                    return (ushort)transfer_address;
                case 0x1b4:
                    return (ushort)external_Audio_Input_Volume;
                case 0x1b6:
                    return (ushort)(external_Audio_Input_Volume >> 16);

                //Reverb area
                case 0x1a2:
                    return mBASE;
                case 0x1c0:
                    return dAPF1;
                case 0x1c2:
                    return dAPF2;
                case 0x1c4:
                    return (ushort)vIIR;
                case 0x1c6:
                    return (ushort)vCOMB1;
                case 0x1c8:
                    return (ushort)vCOMB2;
                case 0x1cA:
                    return (ushort)vCOMB3;
                case 0x1cC:
                    return (ushort)vCOMB4;
                case 0x1cE:
                    return (ushort)vWALL;
                case 0x1D0:
                    return (ushort)vAPF1;
                case 0x1D2:
                    return (ushort)vAPF2;
                case 0x1D4:
                    return mLSAME;
                case 0x1D6:
                    return mRSAME;
                case 0x1D8:
                    return mLCOMB1;
                case 0x1DA:
                    return mRCOMB1;
                case 0x1DC:
                    return mLCOMB2;
                case 0x1DE:
                    return mRCOMB2;
                case 0x1E0:
                    return dLSAME;
                case 0x1E2:
                    return dRSAME;
                case 0x1E4:
                    return mLDIFF;
                case 0x1E6:
                    return mRDIFF;
                case 0x1E8:
                    return mLCOMB3;
                case 0x1EA:
                    return mRCOMB3;
                case 0x1EC:
                    return mLCOMB4;
                case 0x1EE:
                    return mRCOMB4;
                case 0x1F0:
                    return dLDIFF;
                case 0x1F2:
                    return dRDIFF;
                case 0x1F4:
                    return mLAPF1;
                case 0x1F6:
                    return mRAPF1;
                case 0x1F8:
                    return mLAPF2;
                case 0x1FA:
                    return mRAPF2;
                case 0x1FC:
                    return (ushort)vLIN;
                case 0x1FE:
                    return (ushort)vRIN;

                default:
                    Console.WriteLine("[SPU] Offset: " + offset.ToString("x") + "\nFull address: 0x" + (offset + 0x1f801c00).ToString("x"));
                    return 0;
            }
        }

        private ushort readStat()
        {
            uint status = 0;

            status |= SPU_Mode;
            status |= ((uint)IRQ_Flag) << 6;
            status |= ((uint)DMA_Read_Write_Request) << 7;
            status |= ((uint)DMA_Write_Request) << 8;
            status |= ((uint)DMA_Read_Request) << 9;
            status |= ((uint)Data_transfer_busy) << 10;
            status |= ((uint)Writing_Capture_Buffers) << 11;

            //12-15 are uknown/unused (seems to be usually zero)

            return (ushort)status;
        }

        public void setCtrl(ushort value)
        {
            SPUCNT = value;
            CDAudioEnable = (value & 1) == 1;
            CDReverbEnable = ((value >> 2) & 1) == 1;
            DMA_Read_Write_Request = (byte)((value >> 5) & 0x1);
            SPU_Mode = (byte)(value & 0x3F);
            IRQ9Enable = ((SPUCNT >> 6) & 1) == 1;
            reverbEnabled = ((value >> 7) & 1) == 1;     //Only affects Reverb bufffer write, SPU can still read from reverb area

            //8-9   Noise Frequency Step    (0..03h = Step "4,5,6,7")
            //10-13 Noise Frequency Shift   (0..0Fh = Low .. High Frequency)

            SPUMuted = ((value >> 14) & 1) == 0;
            SPUEnable = ((SPUCNT >> 15) & 1) == 1;

            //Console.WriteLine($"[SPU] 0x1F801DAA SPUEnable={SPUEnable} SPUMuted={SPUMuted}");

            if (!SPUEnable)
            {
                for (int i = 0; i < voices.Length; i++)
                {
                    voices[i].adsr.setPhase(SPUADSR.Phase.Off);
                }
            }

            if (!IRQ9Enable)
            {
                IRQ_Flag = 0;
            }
            uint dmaMode = ((uint)((value >> 4) & 0x3));
            switch (dmaMode)
            {
                case 0: //Stop
                case 1: //Manual
                    DMA_Read_Request = 0;
                    DMA_Write_Request = 0;
                    break;

                case 2:
                    DMA_Write_Request = 1;
                    break;
                case 3:
                    DMA_Read_Request = 1;
                    break;
            }

        }

        public bool tick(int cycles)
        {
            clk_counter += cycles;
            if (clk_counter < CYCLES_PER_SAMPLE || !SPUEnable)
                return false;
            clk_counter = 0;
            reverbCounter = (reverbCounter + 1) & 1;    //For half the frequency

            uint edgeKeyOn = KON;
            uint edgeKeyOff = KOFF;
            KON = 0;
            KOFF = 0;

            sumLeft = 0;
            sumRight = 0;
            int reverbLeft = 0;
            int reverbRight = 0;
            int reverbLeft_Input = 0;
            int reverbRight_Input = 0;
            bool voiceHitAddress = false;

            for (int i = 0; i < voices.Length; i++)
            {

                if ((edgeKeyOn & (1 << i)) != 0)
                {
                    voices[i].keyOn();
                }

                if ((edgeKeyOff & (1 << i)) != 0)
                {
                    voices[i].keyOff();
                }

                if (voices[i].adsr.phase == SPUADSR.Phase.Off)
                {
                    //voices[i].lastSample = 0;
                    continue;
                }

                short sample = 0;

                if ((NON & (1 << i)) == 0)
                {

                    if (!voices[i].isLoaded)
                    {
                        voices[i].loadSamples(ref RAM, SPU_IRQ_Address);
                        voices[i].decodeADPCM();
                        voiceHitAddress = voiceHitAddress || voices[i].hit_IRQ_Address;
                        voices[i].hit_IRQ_Address = false;
                    }

                    sample = voices[i].interpolate();
                    modulatePitch(i);
                    voices[i].checkSamplesIndex();
                } else
                {
                    //Console.WriteLine("[SPU] Noise generator !");
                    voices[i].adsr.adsrVolume = 0;
                    voices[i].lastSample = 0;
                }

                sample = (short)((sample * voices[i].adsr.adsrVolume) >> 15);
                voices[i].adsr.ADSREnvelope();
                voices[i].lastSample = sample;

                sumLeft += (sample * voices[i].getVolumeLeft()) >> 15;
                sumRight += (sample * voices[i].getVolumeRight()) >> 15;

                if (((EON >> i) & 1) == 1)
                {   //Adding samples from any channel with active reverb
                    reverbLeft_Input += (sample * voices[i].getVolumeLeft()) >> 15;
                    reverbRight_Input += (sample * voices[i].getVolumeRight()) >> 15;
                }

            }

            //CD Samples are consumed even if CD audio is disabled, they will also end up in the capture buffer 
            int cdSamples = CDDataControl.CDAudioSamples.Count;
            short CDAudioLeft = 0;
            short CDAudioRight = 0;
            short CDAudioLeft_BeforeVol = 0;
            short CDAudioRight_BeforeVol = 0;
            if (cdSamples > 0)
            {
                short CDLeftVolume = (short)CDInputVolume;
                short CDRightVolume = (short)(CDInputVolume >> 16);
                CDAudioLeft_BeforeVol = CDDataControl.CDAudioSamples.Dequeue();
                CDAudioRight_BeforeVol = CDDataControl.CDAudioSamples.Dequeue();
                CDAudioLeft += (short)((CDAudioLeft_BeforeVol * CDLeftVolume) >> 15);
                CDAudioRight += (short)((CDAudioRight_BeforeVol * CDRightVolume) >> 15);
            }
            captureBuffers(0x000 + captureOffset, CDAudioLeft_BeforeVol);
            captureBuffers(0x400 + captureOffset, CDAudioRight_BeforeVol);

            sumLeft += CDAudioEnable ? CDAudioLeft : 0;
            sumRight += CDAudioEnable ? CDAudioRight : 0;
            reverbLeft_Input += (CDAudioEnable && CDReverbEnable) ? CDAudioLeft : 0;
            reverbRight_Input += (CDAudioEnable && CDReverbEnable) ? CDAudioRight : 0;

            captureBuffers(0x800 + captureOffset, voices[1].lastSample);
            captureBuffers(0xC00 + captureOffset, voices[3].lastSample);
            captureOffset += 2;

            if (captureOffset > 0x3FF)
            {
                captureOffset = 0;
            }

            if (reverbCounter == 1)
            {
                (reverbLeft, reverbRight) = processReverb(reverbLeft_Input, reverbRight_Input);
            }

            sumLeft += reverbLeft;
            sumRight += reverbRight;

            sumLeft = (Math.Clamp(sumLeft, -0x8000, 0x7FFE) * mainVolumeLeft) >> 15;
            sumRight = (Math.Clamp(sumRight, -0x8000, 0x7FFE) * mainVolumeRight) >> 15;

            sumLeft = SPUMuted ? 0 : sumLeft;
            sumRight = SPUMuted ? 0 : sumRight;

            outputBuffer[outputBufferPtr++] = (byte)sumLeft;
            outputBuffer[outputBufferPtr++] = (byte)(sumLeft >> 8);
            outputBuffer[outputBufferPtr++] = (byte)sumRight;
            outputBuffer[outputBufferPtr++] = (byte)(sumRight >> 8);

            if (outputBufferPtr >= 2048)
            {
                host.SamplesReady(outputBuffer);
                outputBufferPtr -= 2048;
            }

            if (voiceHitAddress)
            {
                if (IRQ9Enable)
                {
                    IRQ_Flag = 1;
                    return true;
                }
            }

            return false;
        }

        private void captureBuffers(uint address, short value)
        {
            Span<byte> Memory = new Span<byte>(RAM, (int)address, 2);
            MemoryMarshal.Write<short>(Memory, in value);
            if ((SPU_IRQ_Address == address) && (((transfer_Control >> 2) & 0x3) != 0))
            {
                SPU_IRQ();
            }
        }

        private (int, int) processReverb(int leftInput, int rightInput)
        {
            int Lin = (vLIN * leftInput) >> 15;
            int Rin = (vRIN * rightInput) >> 15;

            short leftSideReflection = (short)Math.Clamp((((Lin +
                ((reverbMemoryRead((uint)dLSAME << 3) * vWALL) >> 15) -
                reverbMemoryRead(((uint)mLSAME << 3) - 2)) * vIIR) >> 15) +
                reverbMemoryRead(((uint)mLSAME << 3) - 2), -0x8000, +0x7FFF);

            short rightSideReflection = (short)Math.Clamp((((Rin +
                ((reverbMemoryRead((uint)dRSAME << 3) * vWALL) >> 15) -
                reverbMemoryRead(((uint)mRSAME << 3) - 2)) * vIIR) >> 15) +
                reverbMemoryRead(((uint)mRSAME << 3) - 2), -0x8000, +0x7FFF);

            reverbMemoryWrite(leftSideReflection, (uint)mLSAME << 3);
            reverbMemoryWrite(rightSideReflection, (uint)mRSAME << 3);

            short leftSideReflection_d = (short)Math.Clamp((((Lin +
                  ((reverbMemoryRead((uint)dRDIFF << 3) * vWALL) >> 15) -
                  reverbMemoryRead(((uint)mLDIFF << 3) - 2)) * vIIR) >> 15) +
                  reverbMemoryRead(((uint)mLDIFF << 3) - 2), -0x8000, +0x7FFF);


            short rightSideReflection_d = (short)Math.Clamp((((Rin +
                  ((reverbMemoryRead((uint)dLDIFF << 3) * vWALL) >> 15) -
                  reverbMemoryRead(((uint)mRDIFF << 3) - 2)) * vIIR) >> 15) +
                  reverbMemoryRead(((uint)mRDIFF << 3) - 2), -0x8000, +0x7FFF);


            reverbMemoryWrite(leftSideReflection_d, (uint)mLDIFF << 3);
            reverbMemoryWrite(rightSideReflection_d, (uint)mRDIFF << 3);

            short Lout = (short)Math.Clamp((
                (vCOMB1 * reverbMemoryRead((uint)mLCOMB1 << 3)) >> 15) +
                ((vCOMB2 * reverbMemoryRead((uint)mLCOMB2 << 3)) >> 15) +
                ((vCOMB3 * reverbMemoryRead((uint)mLCOMB3 << 3)) >> 15) +
                ((vCOMB4 * reverbMemoryRead((uint)mLCOMB4 << 3)) >> 15), -0x8000, +0x7FFF);


            short Rout = (short)Math.Clamp((
                (vCOMB1 * reverbMemoryRead((uint)mRCOMB1 << 3)) >> 15) +
                ((vCOMB2 * reverbMemoryRead((uint)mRCOMB2 << 3)) >> 15) +
                ((vCOMB3 * reverbMemoryRead((uint)mRCOMB3 << 3)) >> 15) +
                ((vCOMB4 * reverbMemoryRead((uint)mRCOMB4 << 3)) >> 15), -0x8000, +0x7FFF);


            Lout = (short)Math.Clamp(Lout - Math.Clamp(((vAPF1 * reverbMemoryRead(((uint)mLAPF1 << 3) - ((uint)dAPF1 << 3))) >> 15), -0x8000, +0x7FFF), -0x8000, +0x7FFF);
            Rout = (short)Math.Clamp(Rout - Math.Clamp(((vAPF1 * reverbMemoryRead(((uint)mRAPF1 << 3) - ((uint)dAPF1 << 3))) >> 15), -0x8000, +0x7FFF), -0x8000, +0x7FFF);

            reverbMemoryWrite(Lout, (uint)mLAPF1 << 3);
            reverbMemoryWrite(Rout, (uint)mRAPF1 << 3);

            Lout = (short)Math.Clamp(Math.Clamp(((Lout * vAPF1) >> 15), -0x8000, +0x7FFF) + reverbMemoryRead(((uint)mLAPF1 << 3) - ((uint)dAPF1 << 3)), -0x8000, +0x7FFF);
            Rout = (short)Math.Clamp(Math.Clamp(((Rout * vAPF1) >> 15), -0x8000, +0x7FFF) + reverbMemoryRead(((uint)mRAPF1 << 3) - ((uint)dAPF1 << 3)), -0x8000, +0x7FFF);

            Lout = (short)Math.Clamp(Lout - Math.Clamp(((vAPF2 * reverbMemoryRead(((uint)mLAPF2 << 3) - ((uint)dAPF2 << 3))) >> 15), -0x8000, +0x7FFF), -0x8000, +0x7FFF);
            Rout = (short)Math.Clamp(Rout - Math.Clamp(((vAPF2 * reverbMemoryRead(((uint)mRAPF2 << 3) - ((uint)dAPF2 << 3))) >> 15), -0x8000, +0x7FFF), -0x8000, +0x7FFF);

            reverbMemoryWrite(Lout, (uint)mLAPF2 << 3);
            reverbMemoryWrite(Rout, (uint)mRAPF2 << 3);

            Lout = (short)Math.Clamp(Math.Clamp(((Lout * vAPF2) >> 15), -0x8000, +0x7FFF) + reverbMemoryRead(((uint)mLAPF2 << 3) - ((uint)dAPF2 << 3)), -0x8000, +0x7FFF);
            Rout = (short)Math.Clamp(Math.Clamp(((Rout * vAPF2) >> 15), -0x8000, +0x7FFF) + reverbMemoryRead(((uint)mRAPF2 << 3) - ((uint)dAPF2 << 3)), -0x8000, +0x7FFF);

            int leftOutput = Math.Clamp((Lout * vLOUT) >> 15, -0x8000, +0x7FFF);
            int rightOutput = Math.Clamp((Rout * vROUT) >> 15, -0x8000, +0x7FFF);

            reverbCurrentAddress = Math.Max((uint)mBASE << 3, (reverbCurrentAddress + 2) & 0x7FFFE);

            return (leftOutput, rightOutput);

        }
        public void SPU_IRQ()
        {
            if (IRQ9Enable)
            {
                IRQ_Flag = 1;
                IrqController.set(Interrupt.SPU);
            }
        }

        private void reverbMemoryWrite(short value, uint address)
        {
            if (!reverbEnabled)
            {
                return;
            }

            address += reverbCurrentAddress;
            uint start = (uint)mBASE << 3;
            uint end = 0x7FFFF;
            uint final = (start + ((address - start) % (end - start))) & 0x7FFFE;
            if ((final == SPU_IRQ_Address) || ((final + 1) == SPU_IRQ_Address))
            {
                SPU_IRQ();
            }
            RAM[final] = (byte)value;
            RAM[final + 1] = (byte)(value >> 8);
        }

        private short reverbMemoryRead(uint address)
        {
            address += reverbCurrentAddress;
            uint start = (uint)mBASE << 3;
            uint end = 0x7FFFF;
            uint final = (start + ((address - start) % (end - start))) & 0x7FFFE;
            if ((final == SPU_IRQ_Address) || ((final + 1) == SPU_IRQ_Address))
            {
                SPU_IRQ();
            }
            return (short)(((uint)RAM[final + 1] << 8) | RAM[final]);
        }

        private void modulatePitch(int i)
        {
            int step = voices[i].ADPCM_Pitch;

            if (((PMON & (1 << i)) != 0) && i > 0)
            {
                int factor = voices[i - 1].lastSample;
                factor += 0x8000;
                step = step * factor;
                step = step >> 15;
                step = step & 0x0000FFFF;
            }
            if (step > 0x3FFF)
            {
                step = 0x4000;
            }
            voices[i].pitchCounter = (voices[i].pitchCounter + (ushort)step);
        }

        public unsafe Span<uint> processDmaRead(int size)
        {
            currentAddress &= 0x7FFFF;

            if ((currentAddress <= SPU_IRQ_Address) && ((currentAddress + 3) >= SPU_IRQ_Address))
            {
                SPU_IRQ();
            }

            Span<uint> dma = new uint[size / 4];

            for (int i = 0; i < size / 4; i++)
            {
                uint b0 = RAM[currentAddress++];
                uint b1 = RAM[currentAddress++];
                uint b2 = RAM[currentAddress++];
                uint b3 = RAM[currentAddress++];
                dma[i] = b0 | (b1 << 8) | (b2 << 16) | (b3 << 24);
            }

            return dma;
        }

        public unsafe void processDmaWrite(Span<uint> dma)
        {
            currentAddress &= 0x7FFFF;

            if ((currentAddress <= SPU_IRQ_Address) && ((currentAddress + 3) >= SPU_IRQ_Address))
            {
                SPU_IRQ();
            }

            for (int i = 0; i < dma.Length; i++)
            {
                RAM[currentAddress++ & 0x7FFFF] = (byte)(dma[i] & 0xFF);
                RAM[currentAddress++ & 0x7FFFF] = (byte)((dma[i] >> 8) & 0xFF);
                RAM[currentAddress++ & 0x7FFFF] = (byte)((dma[i] >> 16) & 0xFF);
                RAM[currentAddress++ & 0x7FFFF] = (byte)((dma[i] >> 24) & 0xFF);
            }
        }
    }

    public class SPUVoice
    {
        public short volumeLeft;
        public short volumeRight;

        public ushort ADPCM_Pitch;
        public SPUADSR adsr = new SPUADSR();
        public ushort ADPCM;
        public ushort ADPCM_RepeatAdress;
        public ushort current_address;
        public uint pitchCounter;

        public short old;
        public short older;
        public short lastSample;
        public uint ENDX = 1;

        public bool isLoaded = false;
        public bool hit_IRQ_Address = false;

        short[] decodedSamples = new short[31]; //28 samples + 3 
        byte[] samples = new byte[16];

        private int[] pos_xa_adpcm_table = { 0, +60, +115, +98, +122 };
        private int[] neg_xa_adpcm_table = { 0, 0, -52, -55, -60 };

        private short[] gaussTable = new short[] {
                -0x001, -0x001, -0x001, -0x001, -0x001, -0x001, -0x001, -0x001,
                -0x001, -0x001, -0x001, -0x001, -0x001, -0x001, -0x001, -0x001,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0001,
                0x0001, 0x0001, 0x0001, 0x0002, 0x0002, 0x0002, 0x0003, 0x0003,
                0x0003, 0x0004, 0x0004, 0x0005, 0x0005, 0x0006, 0x0007, 0x0007,
                0x0008, 0x0009, 0x0009, 0x000A, 0x000B, 0x000C, 0x000D, 0x000E,
                0x000F, 0x0010, 0x0011, 0x0012, 0x0013, 0x0015, 0x0016, 0x0018,
                0x0019, 0x001B, 0x001C, 0x001E, 0x0020, 0x0021, 0x0023, 0x0025,
                0x0027, 0x0029, 0x002C, 0x002E, 0x0030, 0x0033, 0x0035, 0x0038,
                0x003A, 0x003D, 0x0040, 0x0043, 0x0046, 0x0049, 0x004D, 0x0050,
                0x0054, 0x0057, 0x005B, 0x005F, 0x0063, 0x0067, 0x006B, 0x006F,
                0x0074, 0x0078, 0x007D, 0x0082, 0x0087, 0x008C, 0x0091, 0x0096,
                0x009C, 0x00A1, 0x00A7, 0x00AD, 0x00B3, 0x00BA, 0x00C0, 0x00C7,
                0x00CD, 0x00D4, 0x00DB, 0x00E3, 0x00EA, 0x00F2, 0x00FA, 0x0101,
                0x010A, 0x0112, 0x011B, 0x0123, 0x012C, 0x0135, 0x013F, 0x0148,
                0x0152, 0x015C, 0x0166, 0x0171, 0x017B, 0x0186, 0x0191, 0x019C,
                0x01A8, 0x01B4, 0x01C0, 0x01CC, 0x01D9, 0x01E5, 0x01F2, 0x0200,
                0x020D, 0x021B, 0x0229, 0x0237, 0x0246, 0x0255, 0x0264, 0x0273,
                0x0283, 0x0293, 0x02A3, 0x02B4, 0x02C4, 0x02D6, 0x02E7, 0x02F9,
                0x030B, 0x031D, 0x0330, 0x0343, 0x0356, 0x036A, 0x037E, 0x0392,
                0x03A7, 0x03BC, 0x03D1, 0x03E7, 0x03FC, 0x0413, 0x042A, 0x0441,
                0x0458, 0x0470, 0x0488, 0x04A0, 0x04B9, 0x04D2, 0x04EC, 0x0506,
                0x0520, 0x053B, 0x0556, 0x0572, 0x058E, 0x05AA, 0x05C7, 0x05E4,
                0x0601, 0x061F, 0x063E, 0x065C, 0x067C, 0x069B, 0x06BB, 0x06DC,
                0x06FD, 0x071E, 0x0740, 0x0762, 0x0784, 0x07A7, 0x07CB, 0x07EF,
                0x0813, 0x0838, 0x085D, 0x0883, 0x08A9, 0x08D0, 0x08F7, 0x091E,
                0x0946, 0x096F, 0x0998, 0x09C1, 0x09EB, 0x0A16, 0x0A40, 0x0A6C,
                0x0A98, 0x0AC4, 0x0AF1, 0x0B1E, 0x0B4C, 0x0B7A, 0x0BA9, 0x0BD8,
                0x0C07, 0x0C38, 0x0C68, 0x0C99, 0x0CCB, 0x0CFD, 0x0D30, 0x0D63,
                0x0D97, 0x0DCB, 0x0E00, 0x0E35, 0x0E6B, 0x0EA1, 0x0ED7, 0x0F0F,
                0x0F46, 0x0F7F, 0x0FB7, 0x0FF1, 0x102A, 0x1065, 0x109F, 0x10DB,
                0x1116, 0x1153, 0x118F, 0x11CD, 0x120B, 0x1249, 0x1288, 0x12C7,
                0x1307, 0x1347, 0x1388, 0x13C9, 0x140B, 0x144D, 0x1490, 0x14D4,
                0x1517, 0x155C, 0x15A0, 0x15E6, 0x162C, 0x1672, 0x16B9, 0x1700,
                0x1747, 0x1790, 0x17D8, 0x1821, 0x186B, 0x18B5, 0x1900, 0x194B,
                0x1996, 0x19E2, 0x1A2E, 0x1A7B, 0x1AC8, 0x1B16, 0x1B64, 0x1BB3,
                0x1C02, 0x1C51, 0x1CA1, 0x1CF1, 0x1D42, 0x1D93, 0x1DE5, 0x1E37,
                0x1E89, 0x1EDC, 0x1F2F, 0x1F82, 0x1FD6, 0x202A, 0x207F, 0x20D4,
                0x2129, 0x217F, 0x21D5, 0x222C, 0x2282, 0x22DA, 0x2331, 0x2389,
                0x23E1, 0x2439, 0x2492, 0x24EB, 0x2545, 0x259E, 0x25F8, 0x2653,
                0x26AD, 0x2708, 0x2763, 0x27BE, 0x281A, 0x2876, 0x28D2, 0x292E,
                0x298B, 0x29E7, 0x2A44, 0x2AA1, 0x2AFF, 0x2B5C, 0x2BBA, 0x2C18,
                0x2C76, 0x2CD4, 0x2D33, 0x2D91, 0x2DF0, 0x2E4F, 0x2EAE, 0x2F0D,
                0x2F6C, 0x2FCC, 0x302B, 0x308B, 0x30EA, 0x314A, 0x31AA, 0x3209,
                0x3269, 0x32C9, 0x3329, 0x3389, 0x33E9, 0x3449, 0x34A9, 0x3509,
                0x3569, 0x35C9, 0x3629, 0x3689, 0x36E8, 0x3748, 0x37A8, 0x3807,
                0x3867, 0x38C6, 0x3926, 0x3985, 0x39E4, 0x3A43, 0x3AA2, 0x3B00,
                0x3B5F, 0x3BBD, 0x3C1B, 0x3C79, 0x3CD7, 0x3D35, 0x3D92, 0x3DEF,
                0x3E4C, 0x3EA9, 0x3F05, 0x3F62, 0x3FBD, 0x4019, 0x4074, 0x40D0,
                0x412A, 0x4185, 0x41DF, 0x4239, 0x4292, 0x42EB, 0x4344, 0x439C,
                0x43F4, 0x444C, 0x44A3, 0x44FA, 0x4550, 0x45A6, 0x45FC, 0x4651,
                0x46A6, 0x46FA, 0x474E, 0x47A1, 0x47F4, 0x4846, 0x4898, 0x48E9,
                0x493A, 0x498A, 0x49D9, 0x4A29, 0x4A77, 0x4AC5, 0x4B13, 0x4B5F,
                0x4BAC, 0x4BF7, 0x4C42, 0x4C8D, 0x4CD7, 0x4D20, 0x4D68, 0x4DB0,
                0x4DF7, 0x4E3E, 0x4E84, 0x4EC9, 0x4F0E, 0x4F52, 0x4F95, 0x4FD7,
                0x5019, 0x505A, 0x509A, 0x50DA, 0x5118, 0x5156, 0x5194, 0x51D0,
                0x520C, 0x5247, 0x5281, 0x52BA, 0x52F3, 0x532A, 0x5361, 0x5397,
                0x53CC, 0x5401, 0x5434, 0x5467, 0x5499, 0x54CA, 0x54FA, 0x5529,
                0x5558, 0x5585, 0x55B2, 0x55DE, 0x5609, 0x5632, 0x565B, 0x5684,
                0x56AB, 0x56D1, 0x56F6, 0x571B, 0x573E, 0x5761, 0x5782, 0x57A3,
                0x57C3, 0x57E2, 0x57FF, 0x581C, 0x5838, 0x5853, 0x586D, 0x5886,
                0x589E, 0x58B5, 0x58CB, 0x58E0, 0x58F4, 0x5907, 0x5919, 0x592A,
                0x593A, 0x5949, 0x5958, 0x5965, 0x5971, 0x597C, 0x5986, 0x598F,
                0x5997, 0x599E, 0x59A4, 0x59A9, 0x59AD, 0x59B0, 0x59B2, 0x59B3,
        };

        public SPUVoice()
        {
        }

        public SPUVoice(int dummy)
        {
            adsr.adsrLOW = 0;
            adsr.adsrHI = 0;
            adsr.adsrVolume = 0;
            adsr.setPhase(SPUADSR.Phase.Off);
            volumeLeft = 0;
            volumeRight = 0;
            lastSample = 0;
            ADPCM = 0;
            ADPCM_Pitch = 0;
        }

        public void setADSR_LOW(ushort v)
        {
            adsr.adsrLOW = v;
        }

        public void setADSR_HI(ushort v)
        {
            adsr.adsrHI = v;
        }

        public void loadSamples(ref byte[] SPU_RAM, uint IRQ_Address)
        {
            hit_IRQ_Address = (IRQ_Address >= current_address << 3) && (IRQ_Address <= ((current_address << 3) + samples.Length - 1));

            for (int i = 0; i < samples.Length; i++)
            {
                int index = (current_address << 3) + i;
                samples[i] = SPU_RAM[index];
            }

            isLoaded = true;

            //Handle Loop Start/End/Repeat flags
            uint flags = samples[1];

            //Loop Start bit
            if (((flags >> 2) & 1) != 0)
            {
                ADPCM_RepeatAdress = current_address;
            }
        }

        public void decodeADPCM()
        {
            //save the last 3 samples from the last decoded block
            decodedSamples[2] = decodedSamples[decodedSamples.Length - 1];
            decodedSamples[1] = decodedSamples[decodedSamples.Length - 2];
            decodedSamples[0] = decodedSamples[decodedSamples.Length - 3];

            int headerShift = samples[0] & 0xF;
            if (headerShift > 12)
            {
                headerShift = 9;
            }

            int shift = 12 - headerShift;
            //3 bits, unlike XA-ADPCM where filter is 2 bits
            int filter = (samples[0] & 0x70) >> 4;
            if (filter > 4)
            {
                filter = 4;
            }

            int f0 = pos_xa_adpcm_table[filter];
            int f1 = neg_xa_adpcm_table[filter];
            int t;
            int s;
            int position = 2; //skip shift and flags
            int nibble = 1;

            for (int i = 0; i < 28; i++)
            {
                //sample number inside the byte (either 0 or 1)
                nibble = (nibble + 1) & 0x1;

                t = signed4bits((byte)(samples[position] >> (nibble << 2) & 0x0F));
                s = (t << shift) + (old * f0 + older * f1 + 32) / 64;

                short decoded = (short)Math.Clamp(s, -0x8000, 0x7FFF);
                //Skip 3 (last 3 of previous block)
                decodedSamples[3 + i] = decoded;
                older = old;
                old = decoded;

                position += nibble;
            }

        }

        uint sampleIndex;

        public short interpolate()
        {
            int interpolated;
            uint interpolationIndex = getInterpolationIndex();
            sampleIndex = getCurrentSampleIndex();

            interpolated = gaussTable[0x0FF - interpolationIndex] * decodedSamples[sampleIndex + 0];
            interpolated += gaussTable[0x1FF - interpolationIndex] * decodedSamples[sampleIndex + 1];
            interpolated += gaussTable[0x100 + interpolationIndex] * decodedSamples[sampleIndex + 2];
            interpolated += gaussTable[0x000 + interpolationIndex] * decodedSamples[sampleIndex + 3];
            interpolated = interpolated >> 15;

            return (short)interpolated;
        }

        public void checkSamplesIndex()
        {

            sampleIndex = getCurrentSampleIndex();

            if (sampleIndex >= 28)
            {
                changeCurrentSampleIndex(-28);

                current_address += 2;
                isLoaded = false;

                uint flags = samples[1];

                //Loop End bit
                if ((flags & 0x1) != 0)
                {
                    ENDX = 1;

                    //Loop Repeat bit
                    if ((flags & 0x2) != 0)
                    {
                        current_address = ADPCM_RepeatAdress;

                    } else
                    {
                        adsr.setPhase(SPUADSR.Phase.Off);
                        adsr.adsrVolume = 0;
                        adsr.adsrCounter = 0;
                    }
                }
            }
        }

        private void changeCurrentSampleIndex(int value)
        {
            uint old = getCurrentSampleIndex();
            int newIndex = (int)(value + old);
            pitchCounter = (ushort)(pitchCounter & 0xFFF);
            pitchCounter |= (ushort)(newIndex << 12);
        }

        internal void keyOn()
        {
            adsr.adsrVolume = 0;
            adsr.adsrCounter = 0;
            adsr.setPhase(SPUADSR.Phase.Attack);
            ADPCM_RepeatAdress = ADPCM;
            current_address = ADPCM;
            old = 0;
            older = 0;
            ENDX = 0;
            isLoaded = false;
        }
        public uint getCurrentSampleIndex()
        {
            return (pitchCounter >> 12) & 0x1F;
        }
        public uint getInterpolationIndex()
        {
            return (pitchCounter >> 4) & 0xFF;
        }
        int signed4bits(byte value)
        {
            return value << 28 >> 28;
        }
        public short getVolumeLeft()
        {
            short vol;
            if (((volumeLeft >> 15) & 1) == 0)
            {
                vol = (short)(volumeLeft << 1);
                return vol;
            } else
            {
                return 0x7FFF;
            }
        }
        public short getVolumeRight()
        {
            short vol;

            if (((volumeRight >> 15) & 1) == 0)
            {
                vol = (short)(volumeRight << 1);
                return vol;
            } else
            {
                return 0x7FFF;
            }
        }
        public void keyOff()
        {
            adsr.setPhase(SPUADSR.Phase.Release);
            adsr.adsrCounter = 0;
        }
    }

    public class SPUADSR
    {
        public enum Phase
        {
            Attack,
            Decay,
            Sustain,
            Release,
            Off
        }

        enum Mode
        {
            Linear,
            Exponential
        }

        enum Direction
        {
            Increase,
            Decrease
        }

        public Phase phase = Phase.Off;
        public Phase nextphase = Phase.Off;

        Mode mode;
        Direction direction;
        public int shift;         //(0..1Fh = Fast..Slow) - for decay it is up to 0x0F becuase it is just 4 bits instead of 5
        int step;           //(0..3 = "+7,+6,+5,+4")

        public ushort adsrLOW;
        public ushort adsrHI;

        public ushort adsrVolume;
        public int adsrCounter = 0;
        public int limit;

        int[] positiveSteps = { +7, +6, +5, +4 };
        int[] negativeSteps = { -8, -7, -6, -5 };

        int adsrCycles;
        int adsrStep;

        public void setPhase(Phase phase)
        {
            this.phase = phase;

            switch (phase)
            {
                case Phase.Attack:

                    switch ((adsrLOW >> 15) & 1)
                    {
                        case 0:
                            mode = Mode.Linear;
                            break;
                        case 1:
                            mode = Mode.Exponential;
                            break;
                    }

                    direction = Direction.Increase;
                    shift = (adsrLOW >> 10) & 0X1F;
                    step = positiveSteps[(adsrLOW >> 8) & 0x3];
                    limit = 0x7FFF;
                    nextphase = Phase.Decay;
                    break;

                case Phase.Decay:
                    mode = Mode.Exponential;
                    direction = Direction.Decrease;
                    shift = (adsrLOW >> 4) & 0x0F;
                    step = -8;
                    limit = ((adsrLOW & 0xF) + 1) * 0x800; //Level=(N+1)*800h
                    nextphase = Phase.Sustain;

                    break;

                case Phase.Sustain:
                    switch ((adsrHI >> 15) & 1)
                    {
                        case 0:
                            mode = Mode.Linear;
                            break;
                        case 1:
                            mode = Mode.Exponential;
                            break;
                    }

                    switch ((adsrHI >> 14) & 1)
                    {
                        case 0:
                            direction = Direction.Increase;
                            step = positiveSteps[(adsrHI >> 6) & 0x3];
                            break;

                        case 1:
                            direction = Direction.Decrease;
                            step = negativeSteps[(adsrHI >> 6) & 0x3];
                            break;
                    }


                    shift = (adsrHI >> 8) & 0x1F;
                    limit = 0x0000;
                    nextphase = Phase.Sustain;

                    break;

                case Phase.Release:

                    switch ((adsrHI >> 5) & 1)
                    {
                        case 0:
                            mode = Mode.Linear;
                            break;
                        case 1:
                            mode = Mode.Exponential;
                            break;
                    }

                    direction = Direction.Decrease;
                    limit = 0x0000;
                    shift = adsrHI & 0x1F;
                    step = -8;

                    nextphase = Phase.Off;
                    break;

                case Phase.Off:

                    limit = 0x0000;
                    shift = 0;
                    step = 0;
                    nextphase = Phase.Off;

                    break;
            }
        }

        public void ADSREnvelope()
        {

            if (adsrCounter > 0)
            {
                adsrCounter--;
                return;
            }

            int shiftAmount = 0;

            shiftAmount = Math.Max(0, shift - 11);
            adsrCycles = 1 << shiftAmount;

            shiftAmount = Math.Max(0, 11 - shift);
            adsrStep = step << shiftAmount;

            if (mode == Mode.Exponential && direction == Direction.Increase && adsrVolume > 0x6000)
            {
                adsrCycles = adsrCycles << 2;
            }

            if (mode == Mode.Exponential && direction == Direction.Decrease)
            {
                adsrStep = adsrStep * adsrVolume >> 15;
            }

            adsrVolume = (ushort)Math.Clamp(adsrVolume + adsrStep, 0, 0x7FFF);

            adsrCounter = adsrCycles;

            switch (phase)
            {
                case Phase.Attack:

                    if (adsrVolume >= limit)
                    {
                        setPhase(nextphase);
                        adsrCounter = 0;
                    }

                    break;

                case Phase.Decay:
                    if (adsrVolume <= limit)
                    {
                        setPhase(nextphase);
                        adsrCounter = 0;
                    }

                    break;

                case Phase.Sustain:

                    break;

                case Phase.Release:
                    if (adsrVolume <= limit)
                    {
                        setPhase(nextphase);
                        adsrVolume = 0;
                        adsrCounter = 0;
                    }

                    break;

                case Phase.Off:
                    break;
            }

        }

    }

}
