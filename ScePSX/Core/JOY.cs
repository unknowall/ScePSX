using System;

namespace ScePSX
{
    [Serializable]
    public class JoyBus
    {
        private byte TX_DATA; //1F801040h JOY_TX_DATA(W)
        private byte RX_DATA; //1F801040h JOY_RX_DATA(R) FIFO
        private bool fifoFull;

        //1F801044 JOY_STAT(R)
        private bool TXreadyFlag1 = true;
        private bool TXreadyFlag2 = true;
        private bool RXparityError;
        private bool ackInputLevel;
        private bool interruptRequest;
        private int baudrateTimer;

        //1F801048 JOY_MODE(R/W)
        private uint baudrateReloadFactor;
        private uint characterLength;
        private bool parityEnable;
        private bool parityTypeOdd;
        private bool clkOutputPolarity;

        //1F80104Ah JOY_CTRL (R/W) (usually 1003h,3003h,0000h)
        private bool TXenable;
        private bool Output;
        private bool RXenable;
        private bool Control_unknow_bit3;
        private bool controlAck;
        private bool Control_unknow_bit5;
        private bool controlReset;
        private uint RXinterruptMode;
        private bool TXinterruptEnable;
        private bool RXinterruptEnable;
        private bool ACKinterruptEnable;
        private uint desiredSlotNumber;

        private ushort BUS_BAUD;    //1F80104Eh JOY_BAUD(R/W) (usually 0088h, ie.circa 250kHz, when Factor = MUL1)

        [Serializable]
        private enum ControllerDevice
        {
            None,
            Controller,
            MemoryCard
        }
        ControllerDevice Device = ControllerDevice.None;

        Controller controller1, controller2;
        MemCard memoryCard1, memoryCard2;

        int counter;

        public JoyBus(Controller controller1, Controller controller2, MemCard memoryCard1, MemCard memoryCard2)
        {
            this.controller1 = controller1;
            this.controller2 = controller2;
            this.memoryCard1 = memoryCard1;
            this.memoryCard2 = memoryCard2;
        }

        public bool tick()
        {
            if (counter > 0)
            {
                counter -= 100;
                if (counter == 0)
                {
                    //Console.WriteLine("[IRQ] TICK Triggering JOYPAD");
                    ackInputLevel = false;
                    interruptRequest = true;
                }
            }

            if (interruptRequest)
                return true;

            return false;
        }

        private void reloadTimer()
        {
            //Console.WriteLine("[JOYPAD] RELOAD TIMER");
            baudrateTimer = (int)(BUS_BAUD * baudrateReloadFactor) & ~0x1;
        }

        public void write(uint addr, uint value)
        {
            switch (addr & 0xFF)
            {
                case 0x40:
                    //Console.WriteLine("[JOYPAD] TX DATA ENQUEUE " + value.ToString("x2"));
                    TX_DATA = (byte)value;
                    RX_DATA = 0xFF;
                    fifoFull = true;

                    TXreadyFlag1 = true;
                    TXreadyFlag2 = false;

                    if (Output)
                    {
                        TXreadyFlag2 = true;


                        if (Device == ControllerDevice.None)
                        {
                            //Console.ForegroundColor = ConsoleColor.Red;
                            if (value == 0x01)
                            {
                                //Console.ForegroundColor = ConsoleColor.Green;
                                Device = ControllerDevice.Controller;
                            } else if (value == 0x81)
                            {
                                //Console.ForegroundColor = ConsoleColor.Blue;
                                Device = ControllerDevice.MemoryCard;
                            }
                        }

                        if (Device == ControllerDevice.Controller)
                        {
                            if (desiredSlotNumber == 0)
                            {
                                RX_DATA = controller1.process(TX_DATA);
                                ackInputLevel = controller1.ack;
                            } else
                            {
                                RX_DATA = controller2.process(TX_DATA);
                                ackInputLevel = controller2.ack;
                            }

                            if (ackInputLevel)
                                counter = 500;
                            //Console.WriteLine($"[JOYPAD] Conroller TICK Enqueued RX response {JOY_RX_DATA:x2} ack: {ackInputLevel}");
                            //Console.ReadLine();
                        } else if (Device == ControllerDevice.MemoryCard)
                        {
                            if (desiredSlotNumber == 0)
                            {
                                RX_DATA = memoryCard1.process(TX_DATA);
                                ackInputLevel = memoryCard1.ack;
                            } else
                            {
                                RX_DATA = memoryCard2.process(TX_DATA);
                                ackInputLevel = memoryCard2.ack;
                            }

                            if (ackInputLevel)
                                counter = 500;
                            //Console.WriteLine($"[JOYPAD] MemCard TICK Enqueued RX response {JOY_RX_DATA:x2} ack: {ackInputLevel}");
                            //Console.ReadLine();
                        } else
                        {
                            ackInputLevel = false;
                        }
                        if (ackInputLevel == false)
                            Device = ControllerDevice.None;
                    } else
                    {
                        Device = ControllerDevice.None;
                        memoryCard1.resetToIdle();
                        memoryCard2.resetToIdle();
                        controller1.resetToIdle();
                        controller2.resetToIdle();
                        ackInputLevel = false;
                    }


                    break;
                case 0x48:
                    //Console.WriteLine($"[JOYPAD] SET MODE {value:x4}");
                    set_MODE(value);
                    break;
                case 0x4A:
                    //Console.WriteLine($"[JOYPAD] SET CONTROL {value:x4}");
                    set_CTRL(value);
                    break;
                case 0x4E:
                    //Console.WriteLine($"[JOYPAD] SET BAUD {value:x4}");
                    BUS_BAUD = (ushort)value;
                    reloadTimer();
                    break;
                default:
                    Console.WriteLine($"Unhandled JOYPAD Write {addr:x8} {value:x8}");
                    //Console.ReadLine();
                    break;
            }
        }

        private void set_CTRL(uint value)
        {
            TXenable = (value & 0x1) != 0;
            Output = ((value >> 1) & 0x1) != 0;
            RXenable = ((value >> 2) & 0x1) != 0;
            Control_unknow_bit3 = ((value >> 3) & 0x1) != 0;
            controlAck = ((value >> 4) & 0x1) != 0;
            Control_unknow_bit5 = ((value >> 5) & 0x1) != 0;
            controlReset = ((value >> 6) & 0x1) != 0;
            RXinterruptMode = (value >> 8) & 0x3;
            TXinterruptEnable = ((value >> 10) & 0x1) != 0;
            RXinterruptEnable = ((value >> 11) & 0x1) != 0;
            ACKinterruptEnable = ((value >> 12) & 0x1) != 0;
            desiredSlotNumber = (value >> 13) & 0x1;

            if (controlAck)
            {
                //Console.WriteLine("[JOYPAD] CONTROL ACK");
                RXparityError = false;
                interruptRequest = false;
                controlAck = false;
            }

            if (controlReset)
            {
                //Console.WriteLine("[JOYPAD] CONTROL RESET");
                Device = ControllerDevice.None;
                memoryCard1.resetToIdle();
                memoryCard2.resetToIdle();
                controller1.resetToIdle();
                controller2.resetToIdle();
                fifoFull = false;

                set_MODE(0);
                set_CTRL(0);
                BUS_BAUD = 0;

                RX_DATA = 0xFF;
                TX_DATA = 0xFF;

                TXreadyFlag1 = true;
                TXreadyFlag2 = true;

                controlReset = false;
            }

            if (!Output)
            {
                Device = ControllerDevice.None;
                memoryCard1.resetToIdle();
                memoryCard2.resetToIdle();
                controller1.resetToIdle();
                controller2.resetToIdle();
            }
        }

        private void set_MODE(uint value)
        {
            baudrateReloadFactor = value & 0x3;
            characterLength = (value >> 2) & 0x3;
            parityEnable = ((value >> 4) & 0x1) != 0;
            parityTypeOdd = ((value >> 5) & 0x1) != 0;
            clkOutputPolarity = ((value >> 8) & 0x1) != 0;
        }

        public uint read(uint addr)
        {
            switch (addr & 0xFF)
            {
                case 0x40:
                    //Console.WriteLine($"[JOYPAD] GET RX DATA {JOY_RX_DATA:x2}");
                    fifoFull = false;
                    return RX_DATA;
                case 0x44:
                    //Console.WriteLine($"[JOYPAD] GET STAT {getJOY_STAT():x8}");
                    return get_STAT();
                case 0x48:
                    //Console.WriteLine($"[JOYPAD] GET MODE {getJOY_MODE():x8}");
                    return get_MODE();
                case 0x4A:
                    //Console.WriteLine($"[JOYPAD] GET CONTROL {getJOY_CTRL():x8}");
                    return get_CTRL();
                case 0x4E:
                    //Console.WriteLine($"[JOYPAD] GET BAUD {JOY_BAUD:x8}");
                    return BUS_BAUD;
                default:
                    //Console.WriteLine($"[JOYPAD] Unhandled Read at {addr}"); Console.ReadLine();
                    return 0xFFFF_FFFF;
            }
        }

        private uint get_CTRL()
        {
            uint joy_ctrl = 0;
            joy_ctrl |= TXenable ? 1u : 0u;
            joy_ctrl |= (Output ? 1u : 0u) << 1;
            joy_ctrl |= (RXenable ? 1u : 0u) << 2;
            joy_ctrl |= (Control_unknow_bit3 ? 1u : 0u) << 3;
            //joy_ctrl |= (ack ? 1u : 0u) << 4; // only writeable
            joy_ctrl |= (Control_unknow_bit5 ? 1u : 0u) << 5;
            //joy_ctrl |= (reset ? 1u : 0u) << 6; // only writeable
            //bit 7 allways 0
            joy_ctrl |= RXinterruptMode << 8;
            joy_ctrl |= (TXinterruptEnable ? 1u : 0u) << 10;
            joy_ctrl |= (RXinterruptEnable ? 1u : 0u) << 11;
            joy_ctrl |= (ACKinterruptEnable ? 1u : 0u) << 12;
            joy_ctrl |= desiredSlotNumber << 13;
            return joy_ctrl;
        }

        private uint get_MODE()
        {
            uint joy_mode = 0;
            joy_mode |= baudrateReloadFactor;
            joy_mode |= characterLength << 2;
            joy_mode |= (parityEnable ? 1u : 0u) << 4;
            joy_mode |= (parityTypeOdd ? 1u : 0u) << 5;
            joy_mode |= (clkOutputPolarity ? 1u : 0u) << 4;
            return joy_mode;
        }

        private uint get_STAT()
        {
            uint joy_stat = 0;
            joy_stat |= TXreadyFlag1 ? 1u : 0u;
            joy_stat |= (fifoFull ? 1u : 0u) << 1;
            joy_stat |= (TXreadyFlag2 ? 1u : 0u) << 2;
            joy_stat |= (RXparityError ? 1u : 0u) << 3;
            joy_stat |= (ackInputLevel ? 1u : 0u) << 7;
            joy_stat |= (interruptRequest ? 1u : 0u) << 9;
            joy_stat |= (uint)baudrateTimer << 11;

            ackInputLevel = false;

            return joy_stat;
        }
    }


}
