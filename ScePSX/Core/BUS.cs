using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MessagePack;
using ScePSX.CdRom;

namespace ScePSX
{
    public class BUS : IDisposable
    {
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
        [IgnoreMember]
        public MDEC mdec;
        public SPU spu;
        public Expansion exp2;
        [IgnoreMember]
        public Controller controller1, controller2;
        public MemCard memoryCard1, memoryCard2;
        [IgnoreMember]
        public SerialIO SIO;

        public string DiskID = "";

        private delegate uint ReadHandler(uint address);
        private delegate void WriteHandler(uint address, uint value);
        private delegate void Write16Handler(uint address, ushort value);
        private delegate void Write8Handler(uint address, byte value);

        private struct AddressRange
        {
            public uint Start;
            public uint End;
            public ReadHandler Read32;
            public WriteHandler Write32;
            public Write16Handler Write16;
            public Write8Handler Write8;
        }

        [IgnoreMember]
        private List<AddressRange> _read32JumpTable;
        [IgnoreMember]
        private List<AddressRange> _write32JumpTable;
        [IgnoreMember]
        private List<AddressRange> _write16JumpTable;
        [IgnoreMember]
        private List<AddressRange> _write8JumpTable;

        [IgnoreMember]
        public unsafe byte* ramPtr = (byte*)Marshal.AllocHGlobal(2048 * 1024);
        [IgnoreMember]
        private unsafe byte* scrathpadPtr = (byte*)Marshal.AllocHGlobal(1024);
        [IgnoreMember]
        private unsafe byte* biosPtr = (byte*)Marshal.AllocHGlobal(512 * 1024);
        [IgnoreMember]
        private unsafe byte* memoryControl1 = (byte*)Marshal.AllocHGlobal(0x40);
        [IgnoreMember]
        private unsafe byte* memoryControl2 = (byte*)Marshal.AllocHGlobal(0x10);

        public BUS()
        {
        }

        public BUS(ICoreHandler Host, string BiosFile, string RomFile, GPUType gputype, string diskid = "", string apppath = ".")
        {
            InitializeJumpTables();

            cddata = new CDData(RomFile, diskid);
            DiskID = cddata.DiskID;
            if (DiskID == "")
            {
                Console.WriteLine($"ISO {RomFile} Non PSX CDROM!");
                return;
            }

            if (!LoadBios(BiosFile))
            {
                return;
            }
            if (RomFile.EndsWith(".exe"))
            {
                LoadEXE(RomFile);
            }

            IRQCTL = new IRQController();

            dma = new DMA(this);

            spu = new SPU(Host, cddata, IRQCTL);

            cdrom = new CDROM(IRQCTL, cddata);

            SIO = new SerialIO();

            controller1 = new Controller();
            controller2 = new Controller();
            memoryCard1 = new MemCard(apppath + "/Save/" + DiskID + ".dat");
            memoryCard2 = new MemCard(apppath + "/Save/MemCard2.dat");
            joybus = new JoyBus(controller1, controller2, memoryCard1, memoryCard2);

            gpu = new GPU(Host, gputype);

            timers = new TIMERS();
            mdec = new MDEC();
            exp2 = new Expansion();

            cpu = new CPU(this, false);
        }

        public void SwapDisk(string RomFile)
        {
            CDData swapcddata = new CDData(RomFile);
            if (swapcddata.DiskID == "")
            {
                Console.WriteLine($"ISO {RomFile} Non PSX CDROM!");
                return;
            }
            cddata = swapcddata;
            DiskID = cddata.DiskID;
            spu.CDDataControl = cddata;
            cdrom.SwapDisk(cddata);
        }

        public unsafe void ReadySerializable()
        {
            Marshal.Copy((IntPtr)ramPtr, ram, 0, ram.Length);
            Marshal.Copy((IntPtr)scrathpadPtr, scrathpadram, 0, scrathpadram.Length);
            Marshal.Copy((IntPtr)biosPtr, biosram, 0, biosram.Length);
            Marshal.Copy((IntPtr)memoryControl1, memctl1, 0, memctl1.Length);
            Marshal.Copy((IntPtr)memoryControl2, memctl2, 0, memctl2.Length);

            //Marshal.Copy((IntPtr)spu.ram, spuram, 0, spuram.Length);

            gpu.ReadySerialized();
        }

        public unsafe void DeSerializable(ICoreHandler Host, GPUType gputype)
        {
            ramPtr = (byte*)Marshal.AllocHGlobal(2048 * 1024);
            scrathpadPtr = (byte*)Marshal.AllocHGlobal(1024);
            biosPtr = (byte*)Marshal.AllocHGlobal(512 * 1024);
            memoryControl1 = (byte*)Marshal.AllocHGlobal(0x40);
            memoryControl2 = (byte*)Marshal.AllocHGlobal(0x10);

            //spu.ram = (byte*)Marshal.AllocHGlobal(512 * 1024);

            Marshal.Copy(ram, 0, (IntPtr)ramPtr, ram.Length);
            Marshal.Copy(scrathpadram, 0, (IntPtr)scrathpadPtr, scrathpadram.Length);
            Marshal.Copy(biosram, 0, (IntPtr)biosPtr, biosram.Length);
            Marshal.Copy(memctl1, 0, (IntPtr)memoryControl1, memctl1.Length);
            Marshal.Copy(memctl2, 0, (IntPtr)memoryControl2, memctl2.Length);

            //Marshal.Copy(spuram, 0, (IntPtr)spu.ram, spuram.Length);

            InitializeJumpTables();

            cddata.ReLoadFS();

            cpu.bus = this;
            dma.ReSet(this);

            controller1 = new Controller();
            controller2 = new Controller();

            joybus.controller1 = controller1;
            joybus.controller2 = controller2;
            joybus.memoryCard1 = memoryCard1;
            joybus.memoryCard2 = memoryCard2;

            mdec = new MDEC();

            SIO = new SerialIO();

            spu.host = Host;
            gpu.host = Host;

            spu.CDDataControl = cddata;
            spu.IrqController = IRQCTL;

            cdrom.DATA = cddata;
            cdrom.IRQCTL = IRQCTL;

            gpu.Backend = new GPUBackend();
            gpu.SelectGPU(gputype);
            gpu.DeSerialized();
        }

        public unsafe void Dispose()
        {
            Marshal.FreeHGlobal((nint)ramPtr);
            Marshal.FreeHGlobal((nint)scrathpadPtr);
            Marshal.FreeHGlobal((nint)biosPtr);
            Marshal.FreeHGlobal((nint)memoryControl1);
            Marshal.FreeHGlobal((nint)memoryControl2);
            //Marshal.FreeHGlobal((nint)spu.ram);
            gpu.Backend.Dispose();
            cddata.chdReader?.Dispose();
        }

        public unsafe bool LoadBios(string biosfile)
        {
            try
            {
                byte[] rom = File.ReadAllBytes(biosfile);
                Marshal.Copy(rom, 0, (IntPtr)biosPtr, rom.Length);

                Console.WriteLine($"[BIOS] {Path.GetFileName(biosfile)} loaded.");

                return true;
            } catch (Exception e)
            {
                Console.WriteLine($"[BIOS] {Path.GetFileName(biosfile)} not found.\n" + e.Message);
                return false;
            }
            //write32(0x1F02_0018, 0x1); //Enable exp flag
        }

        private void InitializeJumpTables()
        {
            _read32JumpTable = new();
            _write32JumpTable = new();
            _write16JumpTable = new();
            _write8JumpTable = new();

            // --------------------------
            //  read 跳转表 按优先级
            // --------------------------

            // RAM 区域：0x0000_0000 - 0x1F00_0000  
            //AddReadHandler(0x00000000, 0x1F000000, addr => BusReadRam(addr));

            // GPU 寄存器：0x1F80_1810 和 0x1F80_1814
            AddReadHandler(0x1F801810, 0x1F801811, addr => gpu.GPUREAD());
            AddReadHandler(0x1F801814, 0x1F801815, addr => gpu.GPUSTAT());

            // SPU：其余范围：0x1F80_1830 - 0x1F80_2000  
            AddReadHandler(0x1F801830, 0x1F802000, addr => spu.read(addr));

            // DMA 控制器：0x1F80_1080 - 0x1F80_1100  
            AddReadHandler(0x1F801080, 0x1F801100, addr => dma.read(addr));

            // CD-ROM 控制器：0x1F80_1140 - 0x1F80_1804  
            AddReadHandler(0x1F801140, 0x1F801804, addr => cdrom.read(addr));

            // 高速缓存：0x1F80_0000 - 0x1F80_0400  
            AddReadHandler(0x1F800000, 0x1F800400, addr => ReadScratchpad(addr));

            // 内存控制1：0x1F80_0400 - 0x1F80_1040  
            AddReadHandler(0x1F800400, 0x1F801040, addr => ReadMemoryControl1(addr));

            // 内存控制2：0x1F80_1060 - 0x1F80_1070  RamSize
            AddReadHandler(0x1F801060, 0x1F801070, addr => ReadMemoryControl2(addr));

            // 中断控制器：0x1F80_1070 - 0x1F80_1080  
            AddReadHandler(0x1F801070, 0x1F801080, addr => IRQCTL.read(addr));

            // 内存缓存：0xFFFE_0130（单独注册）
            AddReadHandler(0xFFFE0130, 0xFFFE0131, addr => memoryCache);

            // BIOS 区域：0x1FC0_0000 - 0x1FC8_0000  
            //AddReadHandler(0x1FC00000, 0x1FC80000, addr => BusReadBios(addr));

            // 手柄总线：0x1F80_1040 - 0x1F80_1050  
            AddReadHandler(0x1F801040, 0x1F801050, addr => joybus.read(addr));

            // 串口：0x1F80_1050 - 0x1F80_1060  
            AddReadHandler(0x1F801050, 0x1F801060, addr => SIO.read(addr));

            // 定时器：0x1F80_1100 - 0x1F80_1140  
            AddReadHandler(0x1F801100, 0x1F801140, addr => timers.read(addr));

            // MDEC 解码器：0x1F80_1820 和 0x1F80_1824  
            AddReadHandler(0x1F801820, 0x1F801821, addr => mdec.readMDEC0_Data());
            AddReadHandler(0x1F801824, 0x1F801825, addr => mdec.readMDEC1_Status());

            // 扩展设备 EXP2：0x1F80_2000 - 0x1F80_4000  
            AddReadHandler(0x1F802000, 0x1F804000, addr => exp2.read(addr));

            // 扩展区域1：0x1F00_0000 - 0x1F80_0000（暂未实现）
            AddReadHandler(0x1F000000, 0x1F800000, addr => 0);

            // --------------------------
            // write 跳转表
            // --------------------------

            // RAM 写入：0x0000_0000 - 0x1F00_0000  
            //AddWriteHandler(0x00000000, 0x1F000000, (addr, value) => WriteRam(addr, value));

            // GPU 寄存器写入：0x1F80_1810 - 0x1F80_1820  
            AddWriteHandler(0x1F801810, 0x1F801820, (addr, value) => gpu.write(addr, value));

            // SPU 写入：0x1F80_1830 - 0x1F80_2000  
            AddWriteHandler(0x1F801830, 0x1F802000, (addr, value) => spu.write(addr, (ushort)value));

            // DMA 控制器写入：0x1F80_1080 - 0x1F80_1100  
            AddWriteHandler(0x1F801080, 0x1F801100, (addr, value) => dma.write(addr, value));

            // CD-ROM 控制器写入：0x1F80_1140 - 0x1F80_1810 写入时转换为 byte 值 
            AddWriteHandler(0x1F801140, 0x1F801810, (addr, value) => cdrom.write(addr, (byte)value));

            // 高速缓存写入：0x1F80_0000 - 0x1F80_0400  
            AddWriteHandler(0x1F800000, 0x1F800400, (addr, value) => WriteScratchpad32(addr, value));

            // 内存控制1写入：0x1F80_0400 - 0x1F80_1040  
            AddWriteHandler(0x1F800400, 0x1F801040, (addr, value) => WriteMemoryContro1_32(addr, value));

            // 内存控制2写入：0x1F80_1060 - 0x1F80_1070  RamSize
            AddWriteHandler(0x1F801060, 0x1F801070, (addr, value) => WriteMemoryContro2_32(addr, value));

            // 中断控制器写入：0x1F80_1070 - 0x1F80_1080  
            AddWriteHandler(0x1F801070, 0x1F801080, (addr, value) => IRQCTL.write(addr, value));

            // 内存缓存写入：0xFFFE_0130（单独注册）
            AddWriteHandler(0xFFFE0130, 0xFFFE0131, (addr, value) => memoryCache = value);

            // 手柄总线写入：0x1F80_1040 - 0x1F80_1050  
            AddWriteHandler(0x1F801040, 0x1F801050, (addr, value) => joybus.write(addr, value));

            // 串口写入：0x1F80_1050 - 0x1F80_1060  
            AddWriteHandler(0x1F801050, 0x1F801060, (addr, value) => SIO.write(addr, value));

            // 定时器写入：0x1F80_1100 - 0x1F80_1140  
            AddWriteHandler(0x1F801100, 0x1F801140, (addr, value) => timers.write(addr, value));

            // MDEC 解码器写入：0x1F80_1820 - 0x1F80_1830  
            AddWriteHandler(0x1F801820, 0x1F801830, (addr, value) => mdec.write(addr, value));

            // 扩展设备写入：EXP2，0x1F80_2000 - 0x1F80_4000  
            AddWriteHandler(0x1F802000, 0x1F804000, (addr, value) => exp2.write(addr, value));

            // 扩展区域1：0x1F00_0000 - 0x1F80_0000（未处理）
            AddWriteHandler(0x1F000000, 0x1F800000, (addr, value) => { });

            // --------------------------
            //  write16 跳转表
            // --------------------------

            //AddWrite16Handler(0x0000_0000, 0x1F00_0000, (addr, value) => WriteRam16(addr, value));
            AddWrite16Handler(0x1F80_1810, 0x1F80_1820, (addr, value) => gpu.write(addr, value));
            AddWrite16Handler(0x1F80_1830, 0x1F80_2000, (addr, value) => spu.write(addr, value));
            AddWrite16Handler(0x1F80_1080, 0x1F80_1100, (addr, value) => dma.write(addr, value));
            AddWrite16Handler(0x1F80_0000, 0x1F80_0400, (addr, value) => WriteScratchpad16(addr, value));
            // 对于 CD-ROM 控制器，写入时转换为 byte 值
            AddWrite16Handler(0x1F80_1140, 0x1F80_1810, (addr, value) => cdrom.write(addr, (byte)value));
            AddWrite16Handler(0x1F80_1070, 0x1F80_1080, (addr, value) => IRQCTL.write(addr, value));
            // 对于内存控制1，整个区域 0x1F80_0400 ~ 0x1F80_1040 均做写入
            AddWrite16Handler(0x1F80_0400, 0x1F80_1040, (addr, value) => WriteMemoryContro1_16(addr, value));
            AddWrite16Handler(0x1F80_1060, 0x1F80_1070, (addr, value) => WriteMemoryContro2_16(addr, value));
            AddWrite16Handler(0xFFFE_0130, 0xFFFE_0131, (addr, value) => memoryCache = value);
            AddWrite16Handler(0x1F80_1040, 0x1F80_1050, (addr, value) => joybus.write(addr, value));
            AddWrite16Handler(0x1F80_1100, 0x1F80_1140, (addr, value) => timers.write(addr, value));
            AddWrite16Handler(0x1F80_1820, 0x1F80_1830, (addr, value) => mdec.write(addr, value));
            AddWrite16Handler(0x1F80_2000, 0x1F80_4000, (addr, value) => exp2.write(addr, value));
            AddWrite16Handler(0x1F80_1050, 0x1F80_1060, (addr, value) => SIO.write(addr, value));
            AddWrite16Handler(0x1F00_0000, 0x1F80_0000, (addr, value) => { });

            // --------------------------
            //  write8 跳转表
            // --------------------------

            //AddWrite8Handler(0x0000_0000, 0x1F00_0000, (addr, value) => WriteRam8(addr, value));
            AddWrite8Handler(0x1F80_1810, 0x1F80_1820, (addr, value) => gpu.write(addr, value));
            AddWrite8Handler(0x1F80_1830, 0x1F80_2000, (addr, value) => spu.write(addr, value));
            AddWrite8Handler(0x1F80_1080, 0x1F80_1100, (addr, value) => dma.write(addr, value));
            AddWrite8Handler(0x1F80_1140, 0x1F80_1810, (addr, value) => cdrom.write(addr, value));
            AddWrite8Handler(0x1F80_0000, 0x1F80_0400, (addr, value) => WriteScratchpad8(addr, value));
            AddWrite8Handler(0x1F80_0400, 0x1F80_1040, (addr, value) => WriteMemoryContro1_8(addr, value));
            AddWrite8Handler(0x1F80_1060, 0x1F80_1070, (addr, value) => WriteMemoryContro2_8(addr, value));
            AddWrite8Handler(0x1F80_1070, 0x1F80_1080, (addr, value) => IRQCTL.write(addr, value));
            AddWrite8Handler(0xFFFE_0130, 0xFFFE_0131, (addr, value) => memoryCache = value);
            AddWrite8Handler(0x1F80_1040, 0x1F80_1050, (addr, value) => joybus.write(addr, value));
            AddWrite8Handler(0x1F80_1100, 0x1F80_1140, (addr, value) => timers.write(addr, value));
            AddWrite8Handler(0x1F80_1820, 0x1F80_1830, (addr, value) => mdec.write(addr, value));
            AddWrite8Handler(0x1F80_2000, 0x1F80_4000, (addr, value) => exp2.write(addr, value));
            AddWrite8Handler(0x1F80_1050, 0x1F80_1060, (addr, value) => SIO.write(addr, value));
            AddWrite8Handler(0x1F00_0000, 0x1F80_0000, (addr, value) => { });
        }

        private void AddReadHandler(uint start, uint end, ReadHandler handler)
        {
            _read32JumpTable.Add(new AddressRange
            {
                Start = start,
                End = end,
                Read32 = handler
            });
            _read32JumpTable.Sort((a, b) => a.Start.CompareTo(b.Start));
        }

        private void AddWriteHandler(uint start, uint end, WriteHandler handler)
        {
            _write32JumpTable.Add(new AddressRange
            {
                Start = start,
                End = end,
                Write32 = handler
            });
            _write32JumpTable.Sort((a, b) => a.Start.CompareTo(b.Start));
        }

        private void AddWrite16Handler(uint start, uint end, Write16Handler handler)
        {
            _write16JumpTable.Add(new AddressRange
            {
                Start = start,
                End = end,
                Write16 = handler
            });
            _write16JumpTable.Sort((a, b) => a.Start.CompareTo(b.Start));
        }

        private void AddWrite8Handler(uint start, uint end, Write8Handler handler)
        {
            _write8JumpTable.Add(new AddressRange
            {
                Start = start,
                End = end,
                Write8 = handler
            });
            _write8JumpTable.Sort((a, b) => a.Start.CompareTo(b.Start));
        }

        private unsafe uint BusReadRam(uint address)
        {
            return *(uint*)(ramPtr + (address & 0x1F_FFFF));
        }

        private unsafe uint BusReadBios(uint address)
        {
            return *(uint*)(biosPtr + (address & 0x7_FFFF));
        }

        private unsafe uint ReadScratchpad(uint address)
        {
            return *(uint*)(scrathpadPtr + (address & 0x3FF));
        }

        public unsafe uint FixedMemoryControlAddress(uint address)
        {
            const uint EXPANSION1_BASE = 0x1F801000;
            const uint EXPANSION2_BASE = 0x1F801004;
            const uint EXPANSION1_DELAY = 0x1F801008;
            const uint EXPANSION3_DELAY = 0x1F80100C;
            const uint BIOS_ROM = 0x1F801010;
            const uint SPU_DELAY = 0x1F801014;
            const uint CDROM_DELAY = 0x1F801018;
            const uint EXPANSION2_DELAY = 0x1F80101C;
            const uint COMMON_DELAY = 0x1F801020;

            switch (address)
            {
                case EXPANSION1_BASE:
                    return 0x1F000000;
                case EXPANSION2_BASE:
                    return 0x1F802000;
                case EXPANSION1_DELAY:
                    return 0x0013243F;
                case EXPANSION3_DELAY:
                    return 0x00003022;
                case BIOS_ROM:
                    return 0x0013243F;
                case SPU_DELAY:
                    return 0x200931E1;
                case CDROM_DELAY:
                    return 0x00020843;
                case EXPANSION2_DELAY:
                    return 0x00070777;
                case COMMON_DELAY:
                    return 0x00031125;
                default:
                    Console.WriteLine($"[BUS] Memory Control load at address: {address:X}");
                    return *(uint*)(memoryControl1 + (address & 0x3F));
            }
        }

        private unsafe uint ReadMemoryControl1(uint address)
        {
            return *(uint*)(memoryControl1 + (address & 0x3F));
        }

        private unsafe uint ReadMemoryControl2(uint address)
        {
            return *(uint*)(memoryControl2 + (address & 0xF));
        }

        public unsafe void WriteRam(uint address, uint value)
        {
            *(uint*)(ramPtr + (address & 0x1F_FFFF)) = value;
        }

        public unsafe void WriteRam16(uint addr, ushort value)
        {
            *(ushort*)(ramPtr + (addr & 0x1F_FFFF)) = value;
        }

        public unsafe void WriteRam8(uint addr, byte value)
        {
            *(byte*)(ramPtr + (addr & 0x1F_FFFF)) = value;
        }

        private unsafe void WriteScratchpad32(uint address, uint value)
        {
            *(uint*)(scrathpadPtr + (address & 0x3FF)) = value;
        }

        private unsafe void WriteScratchpad16(uint address, ushort value)
        {
            *(ushort*)(scrathpadPtr + (address & 0x3FF)) = value;
        }

        private unsafe void WriteScratchpad8(uint address, byte value)
        {
            *(byte*)(scrathpadPtr + (address & 0x3FF)) = value;
        }

        private unsafe void WriteMemoryContro1_32(uint addr, uint value)
        {
            *(uint*)(memoryControl1 + (addr & 0x3F)) = value;
        }

        private unsafe void WriteMemoryContro1_16(uint addr, ushort value)
        {
            *(ushort*)(memoryControl1 + (addr & 0x3F)) = value;
        }

        private unsafe void WriteMemoryContro1_8(uint addr, byte value)
        {
            *(byte*)(memoryControl1 + (addr & 0x3F)) = value;
        }

        private unsafe void WriteMemoryContro2_32(uint addr, uint value)
        {
            *(uint*)(memoryControl2 + (addr & 0xF)) = value;
        }

        private unsafe void WriteMemoryContro2_16(uint addr, ushort value)
        {
            *(ushort*)(memoryControl2 + (addr & 0xF)) = value;
        }

        private unsafe void WriteMemoryContro2_8(uint addr, byte value)
        {
            *(byte*)(memoryControl2 + (addr & 0xF)) = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe uint read32(uint address)
        {
            // 二分查找匹配的地址范围
            uint addr = GetMask(address);

            //内存不查表
            if (addr < 0x1F00_0000)
                return *(uint*)(ramPtr + (addr & 0x1F_FFFF));
            //BIOS不查表
            if (addr >= 0x1FC00000 && addr < 0x1FC80000)
                return *(uint*)(biosPtr + (addr & 0x7_FFFF));

            int low = 0, high = _read32JumpTable.Count - 1;
            while (low <= high)
            {
                int mid = (low + high) / 2;
                var range = _read32JumpTable[mid];

                if (addr < range.Start)
                {
                    high = mid - 1;
                } else if (addr >= range.End)
                {
                    low = mid + 1;
                } else
                {
                    return range.Read32(addr);
                }
            }

            Console.WriteLine($"[BUS] Read32 Unsupported: {address:x8} mask {addr:x8}");
            return 0xFFFF_FFFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void write32(uint address, uint value)
        {
            uint addr = GetMask(address);

            if (addr < 0x1F00_0000)
            {
                *(uint*)(ramPtr + (addr & 0x1F_FFFF)) = value;
                return;
            }

            int low = 0, high = _write32JumpTable.Count - 1;
            while (low <= high)
            {
                int mid = (low + high) / 2;
                var range = _write32JumpTable[mid];

                if (addr < range.Start)
                {
                    high = mid - 1;
                } else if (addr >= range.End)
                {
                    low = mid + 1;
                } else
                {
                    range.Write32(addr, value);
                    return;
                }
            }

            Console.WriteLine($"[BUS] Write32 Unsupported: {address:x8} mask {addr:x8}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void write16(uint address, ushort value)
        {
            uint addr = GetMask(address);

            if (addr < 0x1F00_0000)
            {
                *(ushort*)(ramPtr + (addr & 0x1F_FFFF)) = value;
                return;
            }

            int low = 0, high = _write16JumpTable.Count - 1;
            while (low <= high)
            {
                int mid = (low + high) / 2;
                var range = _write16JumpTable[mid];

                if (addr < range.Start)
                {
                    high = mid - 1;
                } else if (addr >= range.End)
                {
                    low = mid + 1;
                } else
                {
                    range.Write16(addr, value);
                    return;
                }
            }
            Console.WriteLine($"[BUS] Write16 Unsupported: {address:x8} mask {addr:x8}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void write8(uint address, byte value)
        {
            uint addr = GetMask(address);

            if (addr < 0x1F00_0000)
            {
                *(byte*)(ramPtr + (addr & 0x1F_FFFF)) = value;
                return;
            }

            int low = 0, high = _write8JumpTable.Count - 1;
            while (low <= high)
            {
                int mid = (low + high) / 2;
                var range = _write8JumpTable[mid];

                if (addr < range.Start)
                {
                    high = mid - 1;
                } else if (addr >= range.End)
                {
                    low = mid + 1;
                } else
                {
                    range.Write8(addr, value);
                    return;
                }
            }
            Console.WriteLine($"[BUS] Write8 Unsupported: {address:x8} mask {addr:x8}");
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
            uint[] buffer = new uint[size];
            uint readAddr = addr;
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = ReadRam(readAddr & 0x1F_FFFC);
                readAddr += sizeof(uint);
            }
            return buffer.AsSpan();
            //return new Span<uint>(ramPtr + (addr & 0x1F_FFFF), (int)size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void DmaToRam(uint addr, uint value)
        {
            *(uint*)(ramPtr + (addr & 0x1F_FFFF)) = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void DmaToRam(uint addr, byte[] buffer, uint size)
        {
            fixed (byte* src = buffer)
            {
                byte* dest = ramPtr + (addr & 0x1F_FFFF);
                if (((ulong)dest & 0x3) == 0) // 地址4字节对齐
                    Buffer.MemoryCopy(src, dest, size, size);
                else
                    ManualAlignedCopy(src, dest, size); // 手动处理非对齐
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void ManualAlignedCopy(byte* src, byte* dest, uint size)
        {
            for (uint i = 0; i < size; i++)
            {
                *(dest + i) = *(src + i);
            }
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetMask(uint address)
        {
            uint i = address >> 29;
            uint addr = address & RegionMask[i];
            return addr;
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

    }

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
