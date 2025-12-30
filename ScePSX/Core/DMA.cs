using System;

namespace ScePSX
{
    [Serializable]
    public abstract class DMAChannel
    {
        public abstract void write(uint register, uint value);
        public abstract uint read(uint register);
    }

    [Serializable]
    public class DMA
    {

        DMAChannel[] channels = new DMAChannel[8];

        public DMA(BUS bus)
        {
            var interrupt = new InterruptChannel();
            channels[0] = new DmaChannels(0, interrupt, bus);
            channels[1] = new DmaChannels(1, interrupt, bus);
            channels[2] = new DmaChannels(2, interrupt, bus);
            channels[3] = new DmaChannels(3, interrupt, bus);
            channels[4] = new DmaChannels(4, interrupt, bus);
            channels[5] = new DmaChannels(5, interrupt, bus);
            channels[6] = new DmaChannels(6, interrupt, bus);
            channels[7] = interrupt;
        }

        public uint read(uint addr)
        {
            var channel = (addr & 0x70) >> 4;
            var register = addr & 0xF;
            //Console.WriteLine("DMA load " + channel + " " + register  + ":" + channels[channel].load(register).ToString("x8"));
            return channels[channel].read(register);
        }

        public void write(uint addr, uint value)
        {
            var channel = (addr & 0x70) >> 4;
            var register = addr & 0xF;
            //Console.WriteLine("DMA write " + channel + " " + register + ":" + value.ToString("x8"));

            channels[channel].write(register, value);
        }

        public bool tick()
        {
            for (var i = 0; i < 7; i++)
            {
                ((DmaChannels)channels[i]).transferBlockIfPending();
            }
            return ((InterruptChannel)channels[7]).tick();
        }
    }

    [Serializable]
    public sealed class DmaChannels : DMAChannel
    {

        private uint baseAddress;
        private uint blockSize;
        private uint blockCount;

        private uint transferDirection;
        private uint memoryStep;
        private uint choppingEnable;
        private uint syncMode;
        private uint choppingDMAWindowSize;
        private uint choppingCPUWindowSize;
        private bool enable;
        private bool trigger;

        private uint unknownBit29;
        private uint unknownBit30;

        private BUS bus;
        private InterruptChannel interrupt;
        private int channelNumber;

        private uint pendingBlocks;

        public DmaChannels(int channelNumber, InterruptChannel interrupt, BUS bus)
        {
            this.channelNumber = channelNumber;
            this.interrupt = interrupt;
            this.bus = bus;
        }

        public override uint read(uint register)
        {
            switch (register)
            {
                case 0:
                    return baseAddress;
                case 4:
                    return blockCount << 16 | blockSize;
                case 8:
                    return readChannelControl();
                default:
                    return 0;
            }
        }

        private uint readChannelControl()
        {
            uint channelControl = 0;

            channelControl |= transferDirection;
            channelControl |= (memoryStep == 4 ? 0 : 1u) << 1;
            channelControl |= choppingEnable << 8;
            channelControl |= syncMode << 9;
            channelControl |= choppingDMAWindowSize << 16;
            channelControl |= choppingCPUWindowSize << 20;
            channelControl |= (enable ? 1u : 0) << 24;
            channelControl |= (trigger ? 1u : 0) << 28;
            channelControl |= unknownBit29 << 29;
            channelControl |= unknownBit30 << 30;

            if (channelNumber == 6)
            {
                return channelControl & 0x5000_0002 | 0x2;
            }

            return channelControl;
        }

        public override void write(uint register, uint value)
        {
            switch (register)
            {
                case 0:
                    baseAddress = value & 0xFFFFFF;
                    break;
                case 4:
                    blockCount = value >> 16;
                    blockSize = value & 0xFFFF;
                    break;
                case 8:
                    writeChannelControl(value);
                    break;
                default:
                    Console.WriteLine($"Unhandled Write on DMA Channel: {channelNumber} register: {register} value: {value}");
                    break;
            }
        }

        private void writeChannelControl(uint value)
        {
            transferDirection = value & 0x1;
            memoryStep = (uint)((value >> 1 & 0x1) == 0 ? 4 : -4);
            choppingEnable = value >> 8 & 0x1;
            syncMode = value >> 9 & 0x3;
            choppingDMAWindowSize = value >> 16 & 0x7;
            choppingCPUWindowSize = value >> 20 & 0x7;
            enable = (value >> 24 & 0x1) != 0;
            trigger = (value >> 28 & 0x1) != 0;
            unknownBit29 = value >> 29 & 0x1;
            unknownBit30 = value >> 30 & 0x1;

            if (!enable)
                pendingBlocks = 0;

            HandleTransfer();
        }

        private void HandleTransfer()
        {
            if (!isActive() || !interrupt.isDMAControlMasterEnabled(channelNumber))
                return;

            //if (syncMode == 0)
            //{
            //    //if (choppingEnable == 1) {
            //    //    Console.WriteLine($"[DMA] Chopping Syncmode 0 not supported. DmaWindow: {choppingDMAWindowSize} CpuWindow: {choppingCPUWindowSize}");
            //    //}
            //    blockCopy(blockSize == 0 ? 0x10_000 : blockSize);
            //    finishDMA();

            //} else if (syncMode == 1)
            //{
            //    // HACK:
            //    // GPUIn: Bypass blocks to elude mdec/gpu desync as MDEC is actually too fast decoding blocks
            //    // MdecIn: GranTurismo produces some artifacts that still needs to be checked otherwise it's ok on other games i've checked
            //    if ((channelNumber == 2 && transferDirection == 1) || channelNumber == 0)
            //    {
            //        blockCopy(blockSize * blockCount);
            //        finishDMA();
            //        return;
            //    }

            //    trigger = false;
            //    pendingBlocks = blockCount;
            //    transferBlockIfPending();
            //} else if (syncMode == 2)
            //{
            //    linkedList();
            //    finishDMA();
            //}

            switch (syncMode)
            {
                case 0: // Immediate transfer
                    blockCopy(blockSize == 0 ? 0x10000 : blockSize);
                    FinishTransfer();
                    break;
                case 1: // Block transfer
                    if ((channelNumber == 2 && transferDirection == 1) || channelNumber == 0)
                    {
                        blockCopy(blockSize * blockCount);
                        FinishTransfer();
                        return;
                    }
                    trigger = false;
                    pendingBlocks = blockCount;
                    transferBlockIfPending();
                    break;
                case 2: // Linked list transfer
                    LinkedListTransfer();
                    FinishTransfer();
                    break;
            }
        }

        private void FinishTransfer()
        {
            enable = false;
            trigger = false;

            interrupt.handleInterrupt(channelNumber);
        }

        private void blockCopy(uint size)
        {
            if (transferDirection == 0)
            { //To Ram

                switch (channelNumber)
                {
                    case 1:
                        bus.DmaFromMdecOut(baseAddress, (int)size);
                        break;
                    case 2:
                        bus.DmaFromGpu(baseAddress, (int)size);
                        break;
                    case 3:
                        bus.DmaFromCD(baseAddress, (int)size);
                        break;
                    case 4:
                        bus.DmaFromSpu(baseAddress, (int)size);
                        break;
                    case 6:
                        bus.DmaOTC(baseAddress, (int)size);
                        break;
                    default:
                        Console.WriteLine($"[DMA] [BLOCK COPY] Unsupported Channel (to Ram) {channelNumber}");
                        break;
                }

                baseAddress += memoryStep * size;

            } else
            { //From Ram

                var dma = bus.DmaFromRam(baseAddress, size); //baseAddress & 0x1F_FFFC

                switch (channelNumber)
                {
                    case 0:
                        bus.DmaToMdecIn(dma);
                        break;
                    case 2:
                        bus.DmaToGpu(dma);
                        break;
                    case 4:
                        bus.DmaToSpu(dma);
                        break;
                    default:
                        Console.WriteLine($"[DMA] [BLOCK COPY] Unsupported Channel (from Ram) {channelNumber}");
                        break;
                }

                baseAddress += memoryStep * size;
            }

        }

        // OTC-specific handling: Generate ordered table and cache
        //private readonly Queue<uint> otcCache = new Queue<uint>();

        //private void ProcessOtcToRam(uint size)
        //{
        //    if (otcCache.Count == 0)
        //        GenerateOtcEntries((int)size);

        //    var address = baseAddress;
        //    for (var i = 0; i < size; i++)
        //    {
        //        if (otcCache.Count == 0)
        //            break;
        //        bus.WriteRam(address, otcCache.Dequeue());
        //        address = (uint)((int)address + memoryStep) & 0xFFFFFF;
        //    }
        //}

        //private void GenerateOtcEntries(int count)
        //{
        //    var current = baseAddress + (uint)(count * 4);
        //    for (var i = 0; i < count; i++)
        //    {
        //        current -= 4;
        //        otcCache.Enqueue(i == count - 1 ? 0xFFFFFF : current);
        //    }
        //}

        private void LinkedListTransfer()
        {
            uint header = 0;
            uint linkedListHardStop = 0xFFFF; //an arbitrary value to avoid infinity linked lists as we don't run the cpu in between blocks

            while ((header & 0x800000) == 0 && linkedListHardStop-- != 0)
            {
                header = bus.ReadRam(baseAddress);
                var size = header >> 24;
                //Console.WriteLine($"[DMA] [LinkedList] Header: {baseAddress:x8} size: {size}");

                if (size > 0)
                {
                    //if (channelNumber == 6) //OTC for ProcessOtcToRam
                    //    baseAddress = baseAddress & 0x1ffffc;
                    //else
                    //    baseAddress = baseAddress + 4 & 0x1ffffc;

                    baseAddress = baseAddress + 4 & 0x1ffffc;

                    var load = bus.DmaFromRam(baseAddress, size);
                    //Console.WriteLine($"[DMA] [LinkedList] DMAtoGPU size: {load.Length}");
                    bus.DmaToGpu(load);
                }

                if (baseAddress == (header & 0x1ffffc))
                    break; //Tekken2 hangs here if not handling this posible forever loop

                if (channelNumber == 6) //OTC
                    if (baseAddress == 0xFFFFFF || baseAddress == (header & 0x1ffffc))
                        break;

                baseAddress = header & 0x1ffffc;
            }

        }

        //0  Start immediately and transfer all at once (used for CDROM, OTC) needs TRIGGER
        private bool isActive() => syncMode == 0 ? enable && trigger : enable;

        public void transferBlockIfPending()
        {
            //TODO: check if device can actually transfer. Here we assume devices are always
            // capable of processing the dmas and never busy.
            if (pendingBlocks > 0)
            {
                pendingBlocks--;
                blockCopy(blockSize);

                if (pendingBlocks == 0)
                {
                    FinishTransfer();
                }
            }
        }

    }

    [Serializable]
    public sealed class InterruptChannel : DMAChannel
    {
        // 1F8010F0h DPCR - DMA Control register
        private uint control;
        // 1F8010F4h DICR - DMA Interrupt register
        private bool forceIRQ;
        private uint irqEnable;
        private bool masterEnable;
        private uint irqFlag;
        private bool masterFlag;

        private bool edgeInterruptTrigger;

        public InterruptChannel()
        {
            control = 0x07654321;
        }

        public override uint read(uint register)
        {
            switch (register)
            {
                case 0:
                    return control;
                case 4:
                    return readInterrupt();
                case 6:
                    return readInterrupt() >> 16; //castlevania symphony of the night and dino crisis 2 ask for this
                default:
                    Console.WriteLine("Unhandled register on interruptChannel DMA load " + register);
                    return 0xFFFF_FFFF;
            }
        }

        private uint readInterrupt()
        {
            uint interruptRegister = 0;

            interruptRegister |= (forceIRQ ? 1u : 0) << 15;
            interruptRegister |= irqEnable << 16;
            interruptRegister |= (masterEnable ? 1u : 0) << 23;
            interruptRegister |= irqFlag << 24;
            interruptRegister |= (masterFlag ? 1u : 0) << 31;

            return interruptRegister;
        }

        public override void write(uint register, uint value)
        {
            //Console.WriteLine("irqflag pre: " + irqFlag.ToString("x8"));
            switch (register)
            {
                case 0:
                    control = value;
                    break;
                case 4:
                    writeInterrupt(value);
                    break;
                case 6:
                    writeInterrupt(value << 16 | (forceIRQ ? 1u : 0) << 15);
                    break;
                default:
                    Console.WriteLine($"Unhandled write on DMA Interrupt register {register}");
                    break;
            }
            //Console.WriteLine("irqflag post: " + irqFlag.ToString("x8"));
        }

        private void writeInterrupt(uint value)
        {
            forceIRQ = (value >> 15 & 0x1) != 0;
            irqEnable = value >> 16 & 0x7F;
            masterEnable = (value >> 23 & 0x1) != 0;
            irqFlag &= ~(value >> 24 & 0x7F);

            masterFlag = updateMasterFlag();
        }

        public void handleInterrupt(int channel)
        {
            //IRQ flags in Bit(24 + n) are set upon DMAn completion - but caution - they are set ONLY if enabled in Bit(16 + n).
            if ((irqEnable & 1 << channel) != 0)
            {
                irqFlag |= (uint)(1 << channel);
            }
            //Console.WriteLine($"MasterFlag: {masterFlag} irqEnable16: {irqEnable:x8} irqFlag24: {irqFlag:x8} {forceIRQ} {masterEnable} {((irqEnable & irqFlag) > 0)}");
            masterFlag = updateMasterFlag();
            edgeInterruptTrigger |= masterFlag;
        }

        public bool isDMAControlMasterEnabled(int channelNumber)
        {
            return (control >> 3 >> 4 * channelNumber & 0x1) != 0;
        }

        private bool updateMasterFlag()
        {
            //Bit31 is a simple readonly flag that follows the following rules:
            //IF b15 = 1 OR(b23 = 1 AND(b16 - 22 AND b24 - 30) > 0) THEN b31 = 1 ELSE b31 = 0
            return forceIRQ || masterEnable && (irqEnable & irqFlag) > 0;
        }

        public bool tick()
        {
            if (edgeInterruptTrigger)
            {
                edgeInterruptTrigger = false;
                //Console.WriteLine("[IRQ] Triggering DMA");
                return true;
            }
            return false;
        }
    }
}
