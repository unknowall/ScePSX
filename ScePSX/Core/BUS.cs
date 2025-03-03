using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenGL;
using ScePSX.CdRom2;

namespace ScePSX
{
    [Serializable]
    public class BUS : IDisposable
    {
        [NonSerialized]
        public unsafe byte* ramPtr = (byte*)Marshal.AllocHGlobal(2048 * 1024);
        [NonSerialized]
        private unsafe byte* scrathpadPtr = (byte*)Marshal.AllocHGlobal(1024);
        [NonSerialized]
        private unsafe byte* biosPtr = (byte*)Marshal.AllocHGlobal(512 * 1024);
        [NonSerialized]
        private unsafe byte* memoryControl1 = (byte*)Marshal.AllocHGlobal(0x40);
        [NonSerialized]
        private unsafe byte* memoryControl2 = (byte*)Marshal.AllocHGlobal(0x10);

        //to Serializable
        private byte[] ram = new byte[2048 * 1024];
        private byte[] scrathpadram = new byte[1024];
        private byte[] biosram = new byte[512 * 1024];
        private byte[] sio = new byte[0x10];
        private byte[] memctl1 = new byte[0x40];
        private byte[] memctl2 = new byte[0x10];
        private byte[] spuram = new byte[512 * 1024];

        private uint memoryCache;

        public IRQController IRQCTL;
        public DMA dma;
        public CPU cpu;
        public GPU gpu;
        public CDData cddata;
        public CDROM cdrom;
        public TIMERS timers;
        public JoyBus joybus;
        [NonSerialized]
        public MDEC mdec;
        public SPU spu;
        public Expansion exp2;
        public Controller controller1, controller2;
        public MemCard memoryCard, memoryCard2;
        [NonSerialized]
        public SerialIO SIO;

        public string DiskID = "";

        public BUS(ICoreHandler Host, string BiosFile, string RomFile)
        {
            IRQCTL = new IRQController();

            cddata = new CDData(RomFile);
            DiskID = cddata.DiskID;
            if (DiskID == "")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ISO {RomFile} Non PSX CDROM!");
                Console.ResetColor();
                return;
            }

            LoadBios(BiosFile);

            dma = new DMA(this);

            spu = new SPU(Host, IRQCTL);
            cdrom = new CDROM(cddata, spu);

            SIO = new SerialIO();

            controller1 = new Controller();
            controller2 = new Controller();
            memoryCard = new MemCard("./Save/" + DiskID + ".dat");
            memoryCard2 = new MemCard("./Save/MemCard2.dat");
            joybus = new JoyBus(controller1, controller2, memoryCard, memoryCard2);

            gpu = new GPU(Host);

            timers = new TIMERS();
            mdec = new MDEC();
            exp2 = new Expansion();

            cpu = new CPU(this);
        }

        public void SwapDisk(string RomFile)
        {
            cddata = new CDData(RomFile);
            var sDiskID = cddata.DiskID;
            if (sDiskID == "")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ISO {RomFile} Non PSX CDROM!");
                Console.ResetColor();
                return;
            }

            cdrom.SwapDisk(cddata);
        }

        public unsafe void ReadySerializable()
        {
            Marshal.Copy((IntPtr)ramPtr, ram, 0, ram.Length);
            Marshal.Copy((IntPtr)scrathpadPtr, scrathpadram, 0, scrathpadram.Length);
            Marshal.Copy((IntPtr)biosPtr, biosram, 0, biosram.Length);
            Marshal.Copy((IntPtr)memoryControl1, memctl1, 0, memctl1.Length);
            Marshal.Copy((IntPtr)memoryControl2, memctl2, 0, memctl2.Length);

            Marshal.Copy((IntPtr)spu.ram, spuram, 0, spuram.Length);
        }

        public unsafe void DeSerializable(ICoreHandler Host)
        {
            ramPtr = (byte*)Marshal.AllocHGlobal(2048 * 1024);
            scrathpadPtr = (byte*)Marshal.AllocHGlobal(1024);
            biosPtr = (byte*)Marshal.AllocHGlobal(512 * 1024);
            memoryControl1 = (byte*)Marshal.AllocHGlobal(0x40);
            memoryControl2 = (byte*)Marshal.AllocHGlobal(0x10);

            spu.ram = (byte*)Marshal.AllocHGlobal(512 * 1024);

            Marshal.Copy(ram, 0, (IntPtr)ramPtr, ram.Length);
            Marshal.Copy(scrathpadram, 0, (IntPtr)scrathpadPtr, scrathpadram.Length);
            Marshal.Copy(biosram, 0, (IntPtr)biosPtr, biosram.Length);
            Marshal.Copy(memctl1, 0, (IntPtr)memoryControl1, memctl1.Length);
            Marshal.Copy(memctl2, 0, (IntPtr)memoryControl2, memctl2.Length);

            Marshal.Copy(spuram, 0, (IntPtr)spu.ram, spuram.Length);

            mdec = new MDEC();

            SIO = new SerialIO();

            spu.host = Host;
            gpu.host = Host;

            cddata.LoadFileStream();
        }

        public unsafe void Dispose()
        {
            Marshal.FreeHGlobal((nint)ramPtr);
            Marshal.FreeHGlobal((nint)scrathpadPtr);
            Marshal.FreeHGlobal((nint)biosPtr);
            Marshal.FreeHGlobal((nint)memoryControl1);
            Marshal.FreeHGlobal((nint)memoryControl2);
        }

        internal unsafe void LoadBios(string biosfile)
        {
            try
            {
                byte[] rom = File.ReadAllBytes(biosfile);
                Marshal.Copy(rom, 0, (IntPtr)biosPtr, rom.Length);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[BIOS] {Path.GetFileName(biosfile)} loaded.");
                Console.ResetColor();
            } catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[BIOS] {Path.GetFileName(biosfile)} not found.\n" + e.Message);
            }
            //write32(0x1F02_0018, 0x1); //Enable exp flag
        }

        //PSX executables are having an 800h-byte header, followed by the code/data.
        //
        // 000h-007h ASCII ID "PS-x EXE"
        // 008h-00Fh Zerofilled
        // 010h Initial PC(usually 80010000h, or higher)
        // 014h Initial GP/R28(usually 0)
        // 018h Destination Address in RAM(usually 80010000h, or higher)
        // 01Ch Filesize(must be N*800h)    (excluding 800h-byte header)
        // 020h Unknown/Unused(usually 0)
        // 024h Unknown/Unused(usually 0)
        // 028h Memfill Start Address(usually 0) (when below Size = None)
        // 02Ch Memfill Size in bytes(usually 0) (0=None)
        // 030h Initial SP/R29 & FP/R30 Base(usually 801FFFF0h) (or 0=None)
        // 034h Initial SP/R29 & FP/R30 Offs(usually 0, added to above Base)
        // 038h-04Bh Reserved for A(43h) Function(should be zerofilled in exefile)
        // 04Ch-xxxh ASCII marker
        //            "Sony Computer Entertainment Inc. for Japan area"
        //            "Sony Computer Entertainment Inc. for Europe area"
        //            "Sony Computer Entertainment Inc. for North America area"
        //            (or often zerofilled in some homebrew files)
        //            (the BIOS doesn't verify this string, and boots fine without it)
        // xxxh-7FFh Zerofilled
        // 800h...   Code/Data(readed to entry[018h] and up)
        public unsafe void LoadEXE(String fileName)
        {
            byte[] exe = File.ReadAllBytes(fileName);
            uint PC = Unsafe.As<byte, uint>(ref exe[0x10]);
            uint R28 = Unsafe.As<byte, uint>(ref exe[0x14]);
            uint R29 = Unsafe.As<byte, uint>(ref exe[0x30]);
            uint R30 = R29; //base
            R30 += Unsafe.As<byte, uint>(ref exe[0x34]); //offset

            uint DestAdress = Unsafe.As<byte, uint>(ref exe[0x18]);

            Console.WriteLine($"SideLoad PSX EXE: PC {PC:x8} R28 {R28:x8} R29 {R29:x8} R30 {R30:x8}");

            Marshal.Copy(exe, 0x800, (IntPtr)(ramPtr + (DestAdress & 0x1F_FFFF)), exe.Length - 0x800);

            // Patch Bios readRunShell() at 0xBFC06FF0 before the jump to 0x80030000 so we don't poll the address every cycle
            // Instructions are LUI and ORI duos that read to the specified register but PC that reads to R8/Temp0
            // The last 2 instr are a JR to R8 and a NOP.
            write(0x6FF0 + 0, 0x3C080000 | PC >> 16, biosPtr);
            write(0x6FF0 + 4, 0x35080000 | PC & 0xFFFF, biosPtr);

            write(0x6FF0 + 8, 0x3C1C0000 | R28 >> 16, biosPtr);
            write(0x6FF0 + 12, 0x379C0000 | R28 & 0xFFFF, biosPtr);

            if (R29 != 0)
            {
                write(0x6FF0 + 16, 0x3C1D0000 | R29 >> 16, biosPtr);
                write(0x6FF0 + 20, 0x37BD0000 | R29 & 0xFFFF, biosPtr);

                write(0x6FF0 + 24, 0x3C1E0000 | R30 >> 16, biosPtr);
                write(0x6FF0 + 28, 0x37DE0000 | R30 & 0xFFFF, biosPtr);

                write(0x6FF0 + 32, 0x01000008, biosPtr);
                write(0x6FF0 + 36, 0x00000000, biosPtr);
            } else
            {
                write(0x6FF0 + 16, 0x01000008, biosPtr);
                write(0x6FF0 + 20, 0x00000000, biosPtr);
            }
        }

        public unsafe uint read32(uint address)
        {
            uint i = address >> 29;
            uint addr = address & RegionMask[i];
            if (addr < 0x1F00_0000)
            {
                return read<uint>(addr & 0x1F_FFFF, ramPtr);
            } else if (addr < 0x1F80_0000)
            {
                return 0;//read<uint>(addr & 0x7_FFFF, ex1Ptr);
            } else if (addr < 0x1f80_0400)
            {
                return read<uint>(addr & 0x3FF, scrathpadPtr);
            } else if (addr < 0x1F80_1040)
            {
                return read<uint>(addr & 0xF, memoryControl1);
            } else if (addr < 0x1F80_1050)
            {
                return joybus.read(addr);
            } else if (addr < 0x1F80_1060)
            {
                return SIO.read(addr);
                //SIO_STAT
                //if (addr == 0x1F80_1054)
                //    return 0x0000_0805;
                //return sio[addr & 0xF];
            } else if (addr < 0x1F80_1070)
            {
                return read<uint>(addr & 0xF, memoryControl2);
            } else if (addr < 0x1F80_1080)
            {
                return IRQCTL.read(addr);
            } else if (addr < 0x1F80_1100)
            {
                return dma.read(addr);
            } else if (addr < 0x1F80_1140)
            {
                return timers.read(addr);
            } else if (addr <= 0x1F80_1803)
            {
                return cdrom.read(addr);
            } else if (addr == 0x1F80_1810)
            {
                return gpu.GPUREAD();
            } else if (addr == 0x1F80_1814)
            {
                return gpu.GPUSTAT();
            } else if (addr == 0x1F80_1820)
            {
                return mdec.readMDEC0_Data();
            } else if (addr == 0x1F80_1824)
            {
                return mdec.readMDEC1_Status();
            } else if (addr < 0x1F80_2000)
            {
                return spu.read(addr);
            } else if (addr < 0x1F80_4000)
            {
                return exp2.read(addr);
            } else if (addr < 0x1FC8_0000)
            {
                return read<uint>(addr & 0x7_FFFF, biosPtr);
            } else if (addr == 0xFFFE0130)
            {
                return memoryCache;
            } else
            {
                Console.WriteLine("[BUS] read32 Unsupported: " + addr.ToString("x8"));
                return 0xFFFF_FFFF;
            }
        }

        public unsafe void write32(uint address, uint value)
        {
            uint i = address >> 29;
            uint addr = address & RegionMask[i];
            if (addr < 0x1F00_0000)
            {
                write(addr & 0x1F_FFFF, value, ramPtr);
            } else if (addr < 0x1F80_0000)
            {
                //write(addr & 0x7_FFFF, value, ex1Ptr);
            } else if (addr < 0x1f80_0400)
            {
                write(addr & 0x3FF, value, scrathpadPtr);
            } else if (addr < 0x1F80_1040)
            {
                write(addr & 0x3F, value, memoryControl1);
            } else if (addr < 0x1F80_1050)
            {
                joybus.write(addr, value);
            } else if (addr < 0x1F80_1060)
            {
                SIO.write(addr, value);
            } else if (addr < 0x1F80_1070)
            {
                write(addr & 0xF, value, memoryControl2);
            } else if (addr < 0x1F80_1080)
            {
                IRQCTL.write(addr, value);
            } else if (addr < 0x1F80_1100)
            {
                dma.write(addr, value);
            } else if (addr < 0x1F80_1140)
            {
                timers.write(addr, value);
            } else if (addr < 0x1F80_1810)
            {
                cdrom.write(addr, (byte)value);
            } else if (addr < 0x1F80_1820)
            {
                gpu.write(addr, value);
            } else if (addr < 0x1F80_1830)
            {
                mdec.write(addr, value);
            } else if (addr < 0x1F80_2000)
            {
                spu.write(addr, (ushort)value);
            } else if (addr < 0x1F80_4000)
            {
                exp2.write(addr, value);
            } else if (addr == 0xFFFE_0130)
            {
                memoryCache = value;
            } else
            {
                Console.WriteLine($"[BUS] Write32 Unsupported: {addr:x8}");
            }
        }

        public unsafe void write16(uint address, ushort value)
        {
            uint i = address >> 29;
            uint addr = address & RegionMask[i];
            if (addr < 0x1F00_0000)
            {
                write(addr & 0x1F_FFFF, value, ramPtr);
            } else if (addr < 0x1F80_0000)
            {
                //write(addr & 0x7_FFFF, value, ex1Ptr);
            } else if (addr < 0x1F80_0400)
            {
                write(addr & 0x3FF, value, scrathpadPtr);
            } else if (addr < 0x1F80_1040)
            {
                write(addr & 0x3F, value, memoryControl1);
            } else if (addr < 0x1F80_1050)
            {
                joybus.write(addr, value);
            } else if (addr < 0x1F80_1060)
            {
                SIO.write(addr, value);
            } else if (addr < 0x1F80_1070)
            {
                write(addr & 0xF, value, memoryControl2);
            } else if (addr < 0x1F80_1080)
            {
                IRQCTL.write(addr, value);
            } else if (addr < 0x1F80_1100)
            {
                dma.write(addr, value);
            } else if (addr < 0x1F80_1140)
            {
                timers.write(addr, value);
            } else if (addr < 0x1F80_1810)
            {
                cdrom.write(addr, (byte)value);
            } else if (addr < 0x1F80_1820)
            {
                gpu.write(addr, value);
            } else if (addr < 0x1F80_1830)
            {
                mdec.write(addr, value);
            } else if (addr < 0x1F80_2000)
            {
                spu.write(addr, value);
            } else if (addr < 0x1F80_4000)
            {
                exp2.write(addr, value);
            } else if (addr == 0xFFFE_0130)
            {
                memoryCache = value;
            } else
            {
                Console.WriteLine($"[BUS] Write16 Unsupported: {addr:x8}");
            }
        }

        public unsafe void write8(uint address, byte value)
        {
            uint i = address >> 29;
            uint addr = address & RegionMask[i];
            if (addr < 0x1F00_0000)
            {
                write(addr & 0x1F_FFFF, value, ramPtr);
            } else if (addr < 0x1F80_0000)
            {
                //write(addr & 0x7_FFFF, value, ex1Ptr);
            } else if (addr < 0x1f80_0400)
            {
                write(addr & 0x3FF, value, scrathpadPtr);
            } else if (addr < 0x1F80_1040)
            {
                write(addr & 0x3F, value, memoryControl1);
            } else if (addr < 0x1F80_1050)
            {
                joybus.write(addr, value);
            } else if (addr < 0x1F80_1060)
            {
                SIO.write(addr, value);
            } else if (addr < 0x1F80_1070)
            {
                write(addr & 0xF, value, memoryControl2);
            } else if (addr < 0x1F80_1080)
            {
                IRQCTL.write(addr, value);
            } else if (addr < 0x1F80_1100)
            {
                dma.write(addr, value);
            } else if (addr < 0x1F80_1140)
            {
                timers.write(addr, value);
            } else if (addr < 0x1F80_1810)
            {
                cdrom.write(addr, value);
            } else if (addr < 0x1F80_1820)
            {
                gpu.write(addr, value);
            } else if (addr < 0x1F80_1830)
            {
                mdec.write(addr, value);
            } else if (addr < 0x1F80_2000)
            {
                spu.write(addr, value);
            } else if (addr < 0x1F80_4000)
            {
                exp2.write(addr, value);
            } else if (addr == 0xFFFE_0130)
            {
                memoryCache = value;
            } else
            {
                Console.WriteLine($"[BUS] Write8 Unsupported: {addr:x8}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void tick(int cycles)
        {
            if (gpu.tick(cycles))
                IRQCTL.set(Interrupt.VBLANK);
            if (cdrom.tick(cycles))
                IRQCTL.set(Interrupt.CDROM);
            if (dma.tick())
                IRQCTL.set(Interrupt.DMA);

            timers.syncGPU(gpu.GetBlanksAndDot());

            if (timers.tick(0, cycles))
                IRQCTL.set(Interrupt.TIMER0);
            if (timers.tick(1, cycles))
                IRQCTL.set(Interrupt.TIMER1);
            if (timers.tick(2, cycles))
                IRQCTL.set(Interrupt.TIMER2);
            if (joybus.tick())
                IRQCTL.set(Interrupt.CONTR);
            if (spu.tick(cycles))
                IRQCTL.set(Interrupt.SPU);
            if (SIO.tick(cycles))
                IRQCTL.set(Interrupt.SIO);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe T read<T>(uint addr, byte* ptr) where T : unmanaged
        {
            return *(T*)(ptr + addr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void write<T>(uint addr, T value, byte* ptr) where T : unmanaged
        {
            *(T*)(ptr + addr) = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe uint ReadRam(uint addr)
        {
            return *(uint*)(ramPtr + (addr & 0x1F_FFFF));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe uint ReadBios(uint addr)
        {
            return *(uint*)(biosPtr + (addr & 0x7_FFFF));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Span<uint> DmaFromRam(uint addr, uint size)
        {
            return new Span<uint>(ramPtr + (addr & 0x1F_FFFF), (int)size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void DmaToRam(uint addr, uint value)
        {
            *(uint*)(ramPtr + (addr & 0x1F_FFFF)) = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void DmaToRam(uint addr, byte[] buffer, uint size)
        {
            Marshal.Copy(buffer, 0, (IntPtr)(ramPtr + (addr & 0x1F_FFFF)), (int)size * 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DmaFromGpu(uint address, int size)
        {
            for (int i = 0; i < size; i++)
            {
                var word = gpu.GPUREAD();
                DmaToRam(address, word);
                address += 4;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DmaToGpu(Span<uint> buffer)
        {
            gpu.ProcessDma(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void DmaFromCD(uint address, int size)
        {
            var dma = cdrom.processDmaRead(size);
            var dest = new Span<uint>(ramPtr + (address & 0x1F_FFFC), size);
            dma.CopyTo(dest);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DmaToMdecIn(Span<uint> dma)
        {
            foreach (uint word in dma)
                mdec.writeMDEC0_Command(word);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void DmaFromMdecOut(uint address, int size)
        {
            var dma = mdec.processDmaLoad(size);
            var dest = new Span<uint>(ramPtr + (address & 0x1F_FFFC), size);
            dma.CopyTo(dest);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DmaToSpu(Span<uint> dma)
        {
            spu.processDmaWrite(dma);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void DmaFromSpu(uint address, int size)
        {
            var dma = spu.processDmaRead(size);
            var dest = new Span<uint>(ramPtr + (address & 0x1F_FFFC), size);
            dma.CopyTo(dest);
        }

        public unsafe void DmaOTC(uint baseAddress, int size)
        {
            for (int i = 0; i < size - 1; i++)
            {
                DmaToRam(baseAddress, baseAddress - 4);
                baseAddress -= 4;
            }

            DmaToRam(baseAddress, 0xFF_FFFF);
        }

        private static uint[] RegionMask = {
            0xFFFF_FFFF, 0xFFFF_FFFF, 0xFFFF_FFFF, 0xFFFF_FFFF, // KUSEG: 2048MB
            0x7FFF_FFFF,                                        // KSEG0:  512MB
            0x1FFF_FFFF,                                        // KSEG1:  512MB
            0xFFFF_FFFF, 0xFFFF_FFFF,                           // KSEG2: 1024MB
        };

        public uint GetMask(uint address)
        {
            uint i = address >> 29;
            uint addr = address & RegionMask[i];
            return addr;
        }

    }

    [Serializable]
    public class Expansion
    {

        public uint read(uint addr)
        {
            Console.WriteLine($"[BUS] Read Unsupported to EXP2 address: {addr:x8}");
            return 0xFF;
        }

        public void write(uint addr, uint value)
        {
            switch (addr)
            {
                case 0x1F802041:
                    //Console.ForegroundColor = ConsoleColor.Cyan;
                    //Console.WriteLine($"[EXP2] PSX: POST [{value:x1}]");
                    //Console.ResetColor();
                    break;
                case 0x1F802023:
                case 0x1F802080:
                    //Console.ForegroundColor = ConsoleColor.Cyan;
                    //Console.Write((char)value);
                    //Console.ResetColor();
                    break;
                default:
                    Console.WriteLine($"[BUS] Write Unsupported to EXP2: {addr:x8} Value: {value:x8}");
                    break;
            }
        }
    }

    public enum Interrupt
    {
        VBLANK = 0x1,
        GPU = 0x2,
        CDROM = 0x4,
        DMA = 0x8,
        TIMER0 = 0x10,
        TIMER1 = 0x20,
        TIMER2 = 0x40,
        CONTR = 0x80,
        SIO = 0x100,
        SPU = 0x200,
        PIO = 0x400
    }

    [Serializable]
    public class IRQController
    {
        private uint ISTAT; //IF Trigger that needs to be ack
        private uint IMASK; //IE Global Interrupt enable

        internal void set(Interrupt interrupt)
        {
            ISTAT |= (uint)interrupt;
            //Console.WriteLine($"ISTAT SET MANUAL FROM DEVICE: {ISTAT:x8} IMASK {IMASK:x8}");
        }

        internal void writeISTAT(uint value)
        {
            ISTAT &= value & 0x7FF;
            //Console.ForegroundColor = ConsoleColor.Magenta;
            //Console.WriteLine($"[IRQ] [ISTAT] Write {value:x8} ISTAT {ISTAT:x8}");
            //Console.ResetColor();
            //Console.ReadLine();
        }

        internal void writeIMASK(uint value)
        {
            IMASK = value & 0x7FF;
            //Console.WriteLine($"[IRQ] [IMASK] Write {IMASK:x8}");
            //Console.ReadLine();
        }

        internal uint readSTAT()
        {
            //Console.WriteLine($"[IRQ] [ISTAT] read {ISTAT:x8}");
            //Console.ReadLine();
            return ISTAT;
        }

        internal uint readIMASK()
        {
            //Console.WriteLine($"[IRQ] [IMASK] read {IMASK:x8}");
            //Console.ReadLine();
            return IMASK;
        }

        internal bool interruptPending()
        {
            return (ISTAT & IMASK) != 0;
        }

        internal void write(uint addr, uint value)
        {
            uint register = addr & 0xF;
            if (register == 0)
            {
                ISTAT &= value & 0x7FF;
            } else if (register == 4)
            {
                IMASK = value & 0x7FF;
            }
        }

        internal uint read(uint addr)
        {
            uint register = addr & 0xF;
            if (register == 0)
            {
                return ISTAT;
            } else if (register == 4)
            {
                return IMASK;
            } else
            {
                return 0xFFFF_FFFF;
            }
        }
    }

}
