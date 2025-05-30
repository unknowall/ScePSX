using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ScePSX.Disassembler;

namespace ScePSX
{
    [Serializable]
    public unsafe class CPU
    {
        // 静态 Action，用于存储绑定的逻辑
        private static Action<CPU> handleFetchDecodeException;
        private static Action<CPU, uint> handleLWException;
        private static Action<CPU, uint> handleSWException;
        private static Action<CPU, uint, uint, uint> handleAddException;
        private static Action<CPU, uint, uint, uint> handleAddiException;
        private static Action<CPU, uint, uint, uint> handleSubException;
        private static Action<CPU, uint> handleLHException;
        private static Action<CPU, uint> handleLHUException;
        private static Action<CPU, uint> handleSHException;
        private static Action<CPU, uint> handleLWC2Exception;
        private static Action<CPU, uint> handleSWC2Exception;

        private uint PC_Now;
        private uint PC;
        private uint PC_Predictor;

        private uint[] GPR;
        private uint HI;
        private uint LO;

        private bool opcodeIsBranch;
        private bool opcodeIsDelaySlot;

        private bool opcodeTookBranch;
        private bool opcodeInDelaySlotTookBranch;

        private static uint[] ExceptionAdress;

        //CoPro Regs
        private uint[] COP0_GPR;
        private const int SR = 12;
        private const int CAUSE = 13;
        private const int EPC = 14;
        private const int BADA = 8;
        private const int JUMPDEST = 6;

        private bool dontIsolateCache;

        [Serializable]
        private struct MEM
        {
            public uint register;
            public uint value;
        }
        private MEM writeBack;
        private MEM memoryread;
        private MEM delayedMemoryread;

        [Serializable]
        public struct Instr
        {
            public uint value;                     //raw
            public uint opcode => value >> 26;     //Instr opcode

            //I-Type
            public uint rs => (value >> 21) & 0x1F;  //Register Source
            public uint rt => (value >> 16) & 0x1F;  //Register Target
            public uint imm => (ushort)value;        //Immediate value
            public uint imm_s => (uint)(short)value; //Immediate value sign extended

            //R-Type
            public uint rd => (value >> 11) & 0x1F;
            public uint sa => (value >> 6) & 0x1F;  //Shift Amount
            public uint function => value & 0x3F;   //Function

            //J-Type                                       
            public uint addr => value & 0x3FFFFFF;  //Target Address

            //id / Cop
            public uint id => opcode & 0x3;
        }
        private Instr instr;

        public enum EX
        {
            INTERRUPT = 0x0,
            READ_ADRESS_ERROR = 0x4,
            WRITE_ADRESS_ERROR = 0x5,
            BUS_ERROR_FETCH = 0x6,
            SYSCALL = 0x8,
            BREAK = 0x9,
            ILLEGAL_INSTR = 0xA,
            COPROCESSOR_ERROR = 0xB,
            OVERFLOW = 0xC
        }

        private GTE gte;
        private BUS bus;

        private BIOS_Disassembler bios;
        private MIPS_Disassembler mips;

        public bool debug = false;
        public bool biosdebug = false;
        public bool ttydebug = false;
        public int cylesfix = 2;

        public CPU(BUS bus, bool isExecept)
        {
            this.bus = bus;
            gte = new GTE();

            Reset();

            SetExecution(isExecept);
        }

        public void Reset()
        {
            PC = 0xbfc0_0000; // Bios Entry Point
            PC_Predictor = 0xbfc0_0004;

            GPR = new uint[32];
            opcodeIsBranch = false;
            opcodeIsDelaySlot = false;

            opcodeTookBranch = false;
            opcodeInDelaySlotTookBranch = false;

            ExceptionAdress = new uint[] { 0x8000_0080, 0xBFC0_0180 };

            COP0_GPR = new uint[16];
            COP0_GPR[15] = 0x2; //PRID Processor ID

            bios = new BIOS_Disassembler(bus);
            mips = new MIPS_Disassembler(ref HI, ref LO, GPR, COP0_GPR);
        }

        public void disassemblePC()
        {
            mips.PrintRegs();
            mips.disassemble(instr, PC_Now, PC_Predictor);
        }

        private void TTY()
        {
            if (PC == 0x00000B0 && GPR[9] == 0x3D || PC == 0x00000A0 && GPR[9] == 0x3C)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write((char)GPR[4]);
                Console.ResetColor();
            }
        }

        public static void SetExecution(bool enableExceptions)
        {

            if (enableExceptions)
            {
                handleFetchDecodeException = cpu =>
                {
                    if ((cpu.PC_Now & 0x3) != 0)
                    {
                        cpu.COP0_GPR[BADA] = cpu.PC_Now;
                        EXCEPTION(cpu, EX.READ_ADRESS_ERROR);
                    }
                };

                handleLWException = (cpu, addr) =>
                {
                    if ((addr & 0x3) != 0)
                    {
                        cpu.COP0_GPR[BADA] = addr;
                        EXCEPTION(cpu, EX.READ_ADRESS_ERROR, cpu.instr.id);
                    }
                };

                handleSWException = (cpu, addr) =>
                {
                    if ((addr & 0x3) != 0)
                    {
                        cpu.COP0_GPR[BADA] = addr;
                        EXCEPTION(cpu, EX.WRITE_ADRESS_ERROR, cpu.instr.id);
                    }
                };

                handleAddException = (cpu, rs, rt, result) =>
                {
                    if (checkOverflow(rs, rt, result))
                    {
                        EXCEPTION(cpu, EX.OVERFLOW, cpu.instr.id);
                    } else
                    {
                        cpu.setGPR(cpu.instr.rd, result);
                    }
                };

                handleAddiException = (cpu, rs, imm_s, result) =>
                {
                    if (checkOverflow(rs, imm_s, result))
                    {
                        EXCEPTION(cpu, EX.OVERFLOW, cpu.instr.id);
                    } else
                    {
                        cpu.setGPR(cpu.instr.rt, result);
                    }
                };

                handleSubException = (cpu, rs, rt, result) =>
                {
                    if (checkUnderflow(rs, rt, result))
                    {
                        EXCEPTION(cpu, EX.OVERFLOW, cpu.instr.id);
                    } else
                    {
                        cpu.setGPR(cpu.instr.rd, result);
                    }
                };

                handleLHException = (cpu, addr) =>
                {
                    if ((addr & 0x1) != 0)
                    {
                        cpu.COP0_GPR[BADA] = addr;
                        EXCEPTION(cpu, EX.READ_ADRESS_ERROR, cpu.instr.id);
                    }
                };

                handleLHUException = (cpu, addr) =>
                {
                    if ((addr & 0x1) != 0)
                    {
                        cpu.COP0_GPR[BADA] = addr;
                        EXCEPTION(cpu, EX.READ_ADRESS_ERROR, cpu.instr.id);
                    }
                };

                handleSHException = (cpu, addr) =>
                {
                    if ((addr & 0x1) != 0)
                    {
                        cpu.COP0_GPR[BADA] = addr;
                        EXCEPTION(cpu, EX.WRITE_ADRESS_ERROR, cpu.instr.id);
                    }
                };

                handleLWC2Exception = (cpu, addr) =>
                {
                    if ((addr & 0x3) != 0)
                    {
                        cpu.COP0_GPR[BADA] = addr;
                        EXCEPTION(cpu, EX.READ_ADRESS_ERROR, cpu.instr.id);
                    }
                };

                handleSWC2Exception = (cpu, addr) =>
                {
                    if ((addr & 0x3) != 0)
                    {
                        cpu.COP0_GPR[BADA] = addr;
                        EXCEPTION(cpu, EX.READ_ADRESS_ERROR, cpu.instr.id);
                    }
                };
            } else
            {
                handleFetchDecodeException = cpu => { /* 不执行任何操作 */ };
                handleLWException = (cpu, addr) => { /* 不执行任何操作 */ };
                handleSWException = (cpu, addr) => { /* 不执行任何操作 */ };
                handleAddException = (cpu, rs, rt, result) => cpu.setGPR(cpu.instr.rd, result);
                handleAddiException = (cpu, rs, imm_s, result) => cpu.setGPR(cpu.instr.rt, result);
                handleSubException = (cpu, rs, rt, result) => cpu.setGPR(cpu.instr.rd, result);
                handleLHException = (cpu, addr) => { /* 不执行任何操作 */ };
                handleLHUException = (cpu, addr) => { /* 不执行任何操作 */ };
                handleSHException = (cpu, addr) => { /* 不执行任何操作 */ };
                handleLWC2Exception = (cpu, addr) => { /* 不执行任何操作 */ };
                handleSWC2Exception = (cpu, addr) => { /* 不执行任何操作 */ };
            }
        }

        private static delegate*<CPU, void>[] opcodeMainTable = new delegate*<CPU, void>[] {
                &SPECIAL,  &BCOND,  &J,      &JAL,    &BEQ,    &BNE,    &BLEZ,   &BGTZ,
                &ADDI,     &ADDIU,  &SLTI,   &SLTIU,  &ANDI,   &ORI,    &XORI,   &LUI,
                &COP0,     &NOP,    &COP2,   &NOP,    &NA,     &NA,     &NA,     &NA,
                &NA,       &NA,     &NA,     &NA,     &NA,     &NA,     &NA,     &NA,
                &LB,       &LH,     &LWL,    &LW,     &LBU,    &LHU,    &LWR,    &NA,
                &SB,       &SH,     &SWL,    &SW,     &NA,     &NA,     &SWR,    &NA,
                &NOP,      &NOP,    &LWC2,   &NOP,    &NA,     &NA,     &NA,     &NA,
                &NOP,      &NOP,    &SWC2,   &NOP,    &NA,     &NA,     &NA,     &NA,
            };

        private static delegate*<CPU, void>[] opcodeSpecialTable = new delegate*<CPU, void>[] {
                &SLL,   &NA,    &SRL,   &SRA,   &SLLV,    &NA,     &SRLV, &SRAV,
                &JR,    &JALR,  &NA,    &NA,    &SYSCALL, &BREAK,  &NA,   &NA,
                &MFHI,  &MTHI,  &MFLO,  &MTLO,  &NA,      &NA,     &NA,   &NA,
                &MULT,  &MULTU, &DIV,   &DIVU,  &NA,      &NA,     &NA,   &NA,
                &ADD,   &ADDU,  &SUB,   &SUBU,  &AND,     &OR,     &XOR,  &NOR,
                &NA,    &NA,    &SLT,   &SLTU,  &NA,      &NA,     &NA,   &NA,
                &NA,    &NA,    &NA,    &NA,    &NA,      &NA,     &NA,   &NA,
                &NA,    &NA,    &NA,    &NA,    &NA,      &NA,     &NA,   &NA,
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int tick()
        {
            int cyles = fetchDecode();

            if (instr.value != 0)
            {
                opcodeMainTable[instr.opcode](this);
            }

            MemAccess();
            WriteBack();

            if (debug)
            {
                disassemblePC();
            }
            if (biosdebug)
            {
                bios.verbose(PC_Now, GPR);
            }
            if (ttydebug)
            {
                TTY();
            }

            return cyles;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void handleInterrupts()
        {
            uint maskedPC = PC & 0x1FFF_FFFF;
            uint read;
            if (maskedPC < 0x1F00_0000)
            {
                read = bus.ReadRam(maskedPC);
            } else
            {
                read = bus.ReadBios(maskedPC);
            }
            //This is actually the "next" opcode if it's a GTE one
            //just postpone the interrupt so it doesn't glitch out
            //Crash Bandicoot intro is a good example for this
            uint instr = read >> 26;
            if (instr == 0x12)
            { //COP2 MTC2
                //Console.ForegroundColor = ConsoleColor.Red;
                //Console.WriteLine("[CPU] WARNING COP2 OPCODE ON INTERRUPT");
                //Console.ResetColor();
                return;
            }
            if (bus.IRQCTL.interruptPending())
            {
                COP0_GPR[CAUSE] |= 0x400;
            } else
            {
                COP0_GPR[CAUSE] &= ~(uint)0x400;
            }
            bool IEC = (COP0_GPR[SR] & 0x1) == 1;
            byte IM = (byte)((COP0_GPR[SR] >> 8) & 0xFF);
            byte IP = (byte)((COP0_GPR[CAUSE] >> 8) & 0xFF);
            if (IEC && (IM & IP) > 0)
            {
                EXCEPTION(this, EX.INTERRUPT);
            }
        }

        public bool FastBoot = false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int fetchDecode()
        {
            uint maskedPC = PC & 0x1FFF_FFFF;

            PC_Now = PC;
            PC = PC_Predictor;
            PC_Predictor += 4;

            if (FastBoot)
            {
                if (PC_Now == 0x80030000)
                {
                    PC = GPR[31];
                    PC_Now = PC;
                    PC_Predictor = PC + 4;
                    FastBoot = false;
                    opcodeIsBranch = false;
                    opcodeTookBranch = false;
                    return 20;
                }
            }

            opcodeIsDelaySlot = opcodeIsBranch;
            opcodeInDelaySlotTookBranch = opcodeTookBranch;
            opcodeIsBranch = false;
            opcodeTookBranch = false;

            handleFetchDecodeException(this);

            if (maskedPC < 0x1F00_0000)
            {
                instr.value = bus.ReadRam(maskedPC);
                return 1;
            } else
            {
                instr.value = bus.ReadBios(maskedPC);
                return 20;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MemAccess()
        {
            if (delayedMemoryread.register != memoryread.register)
            { //if readDelay on same reg it is lost/overwritten (amidog tests)
                ref uint r0 = ref MemoryMarshal.GetArrayDataReference(GPR);
                Unsafe.Add(ref r0, (nint)memoryread.register) = memoryread.value;
            }
            memoryread = delayedMemoryread;
            delayedMemoryread.register = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteBack()
        {
            ref uint r0 = ref MemoryMarshal.GetArrayDataReference(GPR);
            Unsafe.Add(ref r0, (nint)writeBack.register) = writeBack.value;
            writeBack.register = 0;
            r0 = 0;
        }

        private static void NOP(CPU cpu)
        { /*nop*/
        }

        private static void NA(CPU cpu) => EXCEPTION(cpu, EX.ILLEGAL_INSTR, cpu.instr.id);

        private static void SPECIAL(CPU cpu) => opcodeSpecialTable[cpu.instr.function](cpu);

        private static void BCOND(CPU cpu)
        {
            cpu.opcodeIsBranch = true;
            uint op = cpu.instr.rt;

            bool should_link = (op & 0x1E) == 0x10;
            bool should_branch = (int)(cpu.GPR[cpu.instr.rs] ^ (op << 31)) < 0;

            if (should_link)
                cpu.GPR[31] = cpu.PC_Predictor;
            if (should_branch)
                BRANCH(cpu);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void J(CPU cpu)
        {
            cpu.opcodeIsBranch = true;
            cpu.opcodeTookBranch = true;
            cpu.PC_Predictor = (cpu.PC_Predictor & 0xF000_0000) | (cpu.instr.addr << 2);
        }

        private static void JAL(CPU cpu)
        {
            cpu.setGPR(31, cpu.PC_Predictor);
            J(cpu);
        }

        private static void BEQ(CPU cpu)
        {
            cpu.opcodeIsBranch = true;
            if (cpu.GPR[cpu.instr.rs] == cpu.GPR[cpu.instr.rt])
            {
                BRANCH(cpu);
            }
        }

        private static void BNE(CPU cpu)
        {
            cpu.opcodeIsBranch = true;
            if (cpu.GPR[cpu.instr.rs] != cpu.GPR[cpu.instr.rt])
            {
                BRANCH(cpu);
            }
        }

        private static void BLEZ(CPU cpu)
        {
            cpu.opcodeIsBranch = true;
            if (((int)cpu.GPR[cpu.instr.rs]) <= 0)
            {
                BRANCH(cpu);
            }
        }

        private static void BGTZ(CPU cpu)
        {
            cpu.opcodeIsBranch = true;
            if (((int)cpu.GPR[cpu.instr.rs]) > 0)
            {
                BRANCH(cpu);
            }
        }

        private static void ADDI(CPU cpu)
        {
            uint rs = cpu.GPR[cpu.instr.rs];
            uint imm_s = cpu.instr.imm_s;
            uint result = rs + imm_s;

            handleAddiException(cpu, rs, imm_s, result);
        }

        private static void ADDIU(CPU cpu) => cpu.setGPR(cpu.instr.rt, cpu.GPR[cpu.instr.rs] + cpu.instr.imm_s);

        private static void SLTI(CPU cpu)
        {
            bool condition = (int)cpu.GPR[cpu.instr.rs] < (int)cpu.instr.imm_s;
            cpu.setGPR(cpu.instr.rt, Unsafe.As<bool, uint>(ref condition));
        }

        private static void SLTIU(CPU cpu)
        {
            bool condition = cpu.GPR[cpu.instr.rs] < cpu.instr.imm_s;
            cpu.setGPR(cpu.instr.rt, Unsafe.As<bool, uint>(ref condition));
        }

        private static void ANDI(CPU cpu) => cpu.setGPR(cpu.instr.rt, cpu.GPR[cpu.instr.rs] & cpu.instr.imm);

        private static void ORI(CPU cpu) => cpu.setGPR(cpu.instr.rt, cpu.GPR[cpu.instr.rs] | cpu.instr.imm);

        private static void XORI(CPU cpu) => cpu.setGPR(cpu.instr.rt, cpu.GPR[cpu.instr.rs] ^ cpu.instr.imm);

        private static void LUI(CPU cpu) => cpu.setGPR(cpu.instr.rt, cpu.instr.imm << 16);

        private static void COP0(CPU cpu)
        {
            if (cpu.instr.rs == 0b0_0000)
                MFC0(cpu);
            else if (cpu.instr.rs == 0b0_0100)
                MTC0(cpu);
            else if (cpu.instr.rs == 0b1_0000)
                RFE(cpu);
            else
                EXCEPTION(cpu, EX.ILLEGAL_INSTR, cpu.instr.id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MFC0(CPU cpu)
        {
            uint mfc = cpu.instr.rd;
            if (mfc == 3 || mfc >= 5 && mfc <= 9 || mfc >= 11 && mfc <= 15)
            {
                delayedread(cpu, cpu.instr.rt, cpu.COP0_GPR[mfc]);
            } else
            {
                EXCEPTION(cpu, EX.ILLEGAL_INSTR, cpu.instr.id);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MTC0(CPU cpu)
        {
            uint value = cpu.GPR[cpu.instr.rt];
            uint register = cpu.instr.rd;

            if (register == CAUSE)
            { //only bits 8 and 9 are writable
                cpu.COP0_GPR[CAUSE] &= ~(uint)0x300;
                cpu.COP0_GPR[CAUSE] |= value & 0x300;
            } else if (register == SR)
            {
                //This can trigger soft interrupts
                cpu.dontIsolateCache = (value & 0x10000) == 0;
                bool prevIEC = (cpu.COP0_GPR[SR] & 0x1) == 1;
                bool currentIEC = (value & 0x1) == 1;

                cpu.COP0_GPR[SR] = value;

                uint IM = (value >> 8) & 0x3;
                uint IP = (cpu.COP0_GPR[CAUSE] >> 8) & 0x3;

                if (!prevIEC && currentIEC && (IM & IP) > 0)
                {
                    cpu.PC = cpu.PC_Predictor;
                    EXCEPTION(cpu, EX.INTERRUPT, cpu.instr.id);
                }

            } else
            {
                cpu.COP0_GPR[register] = value;
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RFE(CPU cpu)
        {
            uint mode = cpu.COP0_GPR[SR] & 0x3F;
            cpu.COP0_GPR[SR] &= ~(uint)0xF;
            cpu.COP0_GPR[SR] |= mode >> 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EXCEPTION(CPU cpu, EX cause, uint coprocessor = 0)
        {
            uint mode = cpu.COP0_GPR[SR] & 0x3F;
            cpu.COP0_GPR[SR] &= ~(uint)0x3F;
            cpu.COP0_GPR[SR] |= (mode << 2) & 0x3F;

            uint OldCause = cpu.COP0_GPR[CAUSE] & 0xff00;
            cpu.COP0_GPR[CAUSE] = (uint)cause << 2;
            cpu.COP0_GPR[CAUSE] |= OldCause;
            cpu.COP0_GPR[CAUSE] |= coprocessor << 28;

            if (cause == EX.INTERRUPT)
            {
                cpu.COP0_GPR[EPC] = cpu.PC;
                //hack: related to the delay of the ex interrupt
                cpu.opcodeIsDelaySlot = cpu.opcodeIsBranch;
                cpu.opcodeInDelaySlotTookBranch = cpu.opcodeTookBranch;
            } else
            {
                cpu.COP0_GPR[EPC] = cpu.PC_Now;
            }

            if (cpu.opcodeIsDelaySlot)
            {
                cpu.COP0_GPR[EPC] -= 4;
                cpu.COP0_GPR[CAUSE] |= (uint)1 << 31;
                cpu.COP0_GPR[JUMPDEST] = cpu.PC;

                if (cpu.opcodeInDelaySlotTookBranch)
                {
                    cpu.COP0_GPR[CAUSE] |= (1 << 30);
                }
            }

            cpu.PC = ExceptionAdress[cpu.COP0_GPR[SR] & 0x400000 >> 22];
            cpu.PC_Predictor = cpu.PC + 4;
        }

        private static void COP2(CPU cpu)
        {
            if ((cpu.instr.rs & 0x10) == 0)
            {
                switch (cpu.instr.rs)
                {
                    case 0b0_0000:
                        MFC2(cpu);
                        break;
                    case 0b0_0010:
                        CFC2(cpu);
                        break;
                    case 0b0_0100:
                        MTC2(cpu);
                        break;
                    case 0b0_0110:
                        CTC2(cpu);
                        break;
                    default:
                        EXCEPTION(cpu, EX.ILLEGAL_INSTR, cpu.instr.id);
                        break;
                }
            } else
            {
                cpu.gte.execute(cpu.instr.value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MFC2(CPU cpu) => delayedread(cpu, cpu.instr.rt, cpu.gte.readData(cpu.instr.rd));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CFC2(CPU cpu) => delayedread(cpu, cpu.instr.rt, cpu.gte.readControl(cpu.instr.rd));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MTC2(CPU cpu) => cpu.gte.writeData(cpu.instr.rd, cpu.GPR[cpu.instr.rt]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CTC2(CPU cpu) => cpu.gte.writeControl(cpu.instr.rd, cpu.GPR[cpu.instr.rt]);

        private static void LWC2(CPU cpu)
        {
            uint addr = cpu.GPR[cpu.instr.rs] + cpu.instr.imm_s;

            handleLWC2Exception(cpu, addr);
            uint value = cpu.bus.read32(addr);
            cpu.gte.writeData(cpu.instr.rt, value);
        }

        private static void SWC2(CPU cpu)
        {
            uint addr = cpu.GPR[cpu.instr.rs] + cpu.instr.imm_s;

            handleSWC2Exception(cpu, addr);
            cpu.bus.write32(addr, cpu.gte.readData(cpu.instr.rt));
        }

        private static void LB(CPU cpu)
        { //todo redo this as it unnecesary read32
            if (cpu.dontIsolateCache)
            {
                uint value = (uint)(sbyte)cpu.bus.read32(cpu.GPR[cpu.instr.rs] + cpu.instr.imm_s);
                delayedread(cpu, cpu.instr.rt, value);
            } //else Console.WriteLine("IsolatedCache: Ignoring read");
        }

        private static void LBU(CPU cpu)
        {
            if (cpu.dontIsolateCache)
            {
                uint value = (byte)cpu.bus.read32(cpu.GPR[cpu.instr.rs] + cpu.instr.imm_s);
                delayedread(cpu, cpu.instr.rt, value);
            } //else Console.WriteLine("IsolatedCache: Ignoring read");
        }

        private static void LH(CPU cpu)
        {
            if (cpu.dontIsolateCache)
            {
                uint addr = cpu.GPR[cpu.instr.rs] + cpu.instr.imm_s;
                handleLHException(cpu, addr);
                uint value = (uint)(short)cpu.bus.read32(addr);
                delayedread(cpu, cpu.instr.rt, value);
            } //else Console.WriteLine("IsolatedCache: Ignoring read");
        }

        private static void LHU(CPU cpu)
        {
            if (cpu.dontIsolateCache)
            {
                uint addr = cpu.GPR[cpu.instr.rs] + cpu.instr.imm_s;
                handleLHUException(cpu, addr);
                uint value = (ushort)cpu.bus.read32(addr);
                delayedread(cpu, cpu.instr.rt, value);
            } //else Console.WriteLine("IsolatedCache: Ignoring read");
        }

        private static void LW(CPU cpu)
        {
            if (cpu.dontIsolateCache)
            {
                uint addr = cpu.GPR[cpu.instr.rs] + cpu.instr.imm_s;
                handleLWException(cpu, addr);
                uint value = cpu.bus.read32(addr);
                delayedread(cpu, cpu.instr.rt, value);
            } //else Console.WriteLine("IsolatedCache: Ignoring read");
        }

        private static void LWL(CPU cpu)
        {
            uint addr = cpu.GPR[cpu.instr.rs] + cpu.instr.imm_s;
            uint aligned_addr = addr & 0xFFFF_FFFC;
            uint aligned_read = cpu.bus.read32(aligned_addr);

            uint LRValue = cpu.GPR[cpu.instr.rt];

            if (cpu.instr.rt == cpu.memoryread.register)
            {
                LRValue = cpu.memoryread.value;
            }

            int shift = (int)((addr & 0x3) << 3);
            uint mask = (uint)0x00FF_FFFF >> shift;
            uint value = (LRValue & mask) | (aligned_read << (24 - shift));

            delayedread(cpu, cpu.instr.rt, value);
        }

        private static void LWR(CPU cpu)
        {
            uint addr = cpu.GPR[cpu.instr.rs] + cpu.instr.imm_s;
            uint aligned_addr = addr & 0xFFFF_FFFC;
            uint aligned_read = cpu.bus.read32(aligned_addr);

            uint LRValue = cpu.GPR[cpu.instr.rt];

            if (cpu.instr.rt == cpu.memoryread.register)
            {
                LRValue = cpu.memoryread.value;
            }

            int shift = (int)((addr & 0x3) << 3);
            uint mask = 0xFFFF_FF00 << (24 - shift);
            uint value = (LRValue & mask) | (aligned_read >> shift);

            delayedread(cpu, cpu.instr.rt, value);
        }

        private static void SB(CPU cpu)
        {
            if (cpu.dontIsolateCache)
                cpu.bus.write8(cpu.GPR[cpu.instr.rs] + cpu.instr.imm_s, (byte)cpu.GPR[cpu.instr.rt]);
            //else Console.WriteLine("IsolatedCache: Ignoring Write");
        }

        private static void SH(CPU cpu)
        {
            if (cpu.dontIsolateCache)
            {
                uint addr = cpu.GPR[cpu.instr.rs] + cpu.instr.imm_s;
                handleSHException(cpu, addr);
                cpu.bus.write16(addr, (ushort)cpu.GPR[cpu.instr.rt]);
            } //else Console.WriteLine("IsolatedCache: Ignoring Write");
        }

        private static void SW(CPU cpu)
        {
            if (cpu.dontIsolateCache)
            {
                uint addr = cpu.GPR[cpu.instr.rs] + cpu.instr.imm_s;
                handleSWException(cpu, addr);
                cpu.bus.write32(addr, cpu.GPR[cpu.instr.rt]);
            } //else Console.WriteLine("IsolatedCache: Ignoring Write");
        }

        private static void SWR(CPU cpu)
        {
            uint addr = cpu.GPR[cpu.instr.rs] + cpu.instr.imm_s;
            uint aligned_addr = addr & 0xFFFF_FFFC;
            uint aligned_read = cpu.bus.read32(aligned_addr);

            int shift = (int)((addr & 0x3) << 3);
            uint mask = (uint)0x00FF_FFFF >> (24 - shift);
            uint value = (aligned_read & mask) | (cpu.GPR[cpu.instr.rt] << shift);

            cpu.bus.write32(aligned_addr, value);
        }

        private static void SWL(CPU cpu)
        {
            uint addr = cpu.GPR[cpu.instr.rs] + cpu.instr.imm_s;
            uint aligned_addr = addr & 0xFFFF_FFFC;
            uint aligned_read = cpu.bus.read32(aligned_addr);

            int shift = (int)((addr & 0x3) << 3);
            uint mask = 0xFFFF_FF00 << shift;
            uint value = (aligned_read & mask) | (cpu.GPR[cpu.instr.rt] >> (24 - shift));

            cpu.bus.write32(aligned_addr, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BRANCH(CPU cpu)
        {
            cpu.opcodeTookBranch = true;
            cpu.PC_Predictor = cpu.PC + (cpu.instr.imm_s << 2);
        }

        // Special Table Opcodes (Nested on Opcode 0x00 with additional function param)

        private static void SLL(CPU cpu) => cpu.setGPR(cpu.instr.rd, cpu.GPR[cpu.instr.rt] << (int)cpu.instr.sa);

        private static void SRL(CPU cpu) => cpu.setGPR(cpu.instr.rd, cpu.GPR[cpu.instr.rt] >> (int)cpu.instr.sa);

        private static void SRA(CPU cpu) => cpu.setGPR(cpu.instr.rd, (uint)((int)cpu.GPR[cpu.instr.rt] >> (int)cpu.instr.sa));

        private static void SLLV(CPU cpu) => cpu.setGPR(cpu.instr.rd, cpu.GPR[cpu.instr.rt] << (int)(cpu.GPR[cpu.instr.rs] & 0x1F));

        private static void SRLV(CPU cpu) => cpu.setGPR(cpu.instr.rd, cpu.GPR[cpu.instr.rt] >> (int)(cpu.GPR[cpu.instr.rs] & 0x1F));

        private static void SRAV(CPU cpu) => cpu.setGPR(cpu.instr.rd, (uint)((int)cpu.GPR[cpu.instr.rt] >> (int)(cpu.GPR[cpu.instr.rs] & 0x1F)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void JR(CPU cpu)
        {
            cpu.opcodeIsBranch = true;
            cpu.opcodeTookBranch = true;
            cpu.PC_Predictor = cpu.GPR[cpu.instr.rs];
        }

        private static void SYSCALL(CPU cpu) => EXCEPTION(cpu, EX.SYSCALL, cpu.instr.id);

        private static void BREAK(CPU cpu) => EXCEPTION(cpu, EX.BREAK);

        private static void JALR(CPU cpu)
        {
            cpu.setGPR(cpu.instr.rd, cpu.PC_Predictor);
            JR(cpu);
        }

        private static void MFHI(CPU cpu) => cpu.setGPR(cpu.instr.rd, cpu.HI);

        private static void MTHI(CPU cpu) => cpu.HI = cpu.GPR[cpu.instr.rs];

        private static void MFLO(CPU cpu) => cpu.setGPR(cpu.instr.rd, cpu.LO);

        private static void MTLO(CPU cpu) => cpu.LO = cpu.GPR[cpu.instr.rs];

        private static void MULT(CPU cpu)
        {
            long value = (long)(int)cpu.GPR[cpu.instr.rs] * (long)(int)cpu.GPR[cpu.instr.rt]; //sign extend to pass amidog cpu test

            cpu.HI = (uint)(value >> 32);
            cpu.LO = (uint)value;
        }

        private static void MULTU(CPU cpu)
        {
            ulong value = (ulong)cpu.GPR[cpu.instr.rs] * (ulong)cpu.GPR[cpu.instr.rt]; //sign extend to pass amidog cpu test

            cpu.HI = (uint)(value >> 32);
            cpu.LO = (uint)value;
        }

        private static void DIV(CPU cpu)
        {
            int n = (int)cpu.GPR[cpu.instr.rs];
            int d = (int)cpu.GPR[cpu.instr.rt];

            if (d == 0)
            {
                cpu.HI = (uint)n;
                if (n >= 0)
                {
                    cpu.LO = 0xFFFF_FFFF;
                } else
                {
                    cpu.LO = 1;
                }
            } else if ((uint)n == 0x8000_0000 && d == -1)
            {
                cpu.HI = 0;
                cpu.LO = 0x8000_0000;
            } else
            {
                cpu.HI = (uint)(n % d);
                cpu.LO = (uint)(n / d);
            }
        }

        private static void DIVU(CPU cpu)
        {
            uint n = cpu.GPR[cpu.instr.rs];
            uint d = cpu.GPR[cpu.instr.rt];

            if (d == 0)
            {
                cpu.HI = n;
                cpu.LO = 0xFFFF_FFFF;
            } else
            {
                cpu.HI = n % d;
                cpu.LO = n / d;
            }
        }

        private static void ADD(CPU cpu)
        {
            uint rs = cpu.GPR[cpu.instr.rs];
            uint rt = cpu.GPR[cpu.instr.rt];
            uint result = rs + rt;

            handleAddException(cpu, rs, rt, result);
        }

        private static void ADDU(CPU cpu) => cpu.setGPR(cpu.instr.rd, cpu.GPR[cpu.instr.rs] + cpu.GPR[cpu.instr.rt]);

        private static void SUB(CPU cpu)
        {
            uint rs = cpu.GPR[cpu.instr.rs];
            uint rt = cpu.GPR[cpu.instr.rt];
            uint result = rs - rt;

            handleSubException(cpu, rs, rt, result);
        }

        private static void SUBU(CPU cpu) => cpu.setGPR(cpu.instr.rd, cpu.GPR[cpu.instr.rs] - cpu.GPR[cpu.instr.rt]);

        private static void AND(CPU cpu) => cpu.setGPR(cpu.instr.rd, cpu.GPR[cpu.instr.rs] & cpu.GPR[cpu.instr.rt]);

        private static void OR(CPU cpu) => cpu.setGPR(cpu.instr.rd, cpu.GPR[cpu.instr.rs] | cpu.GPR[cpu.instr.rt]);

        private static void XOR(CPU cpu) => cpu.setGPR(cpu.instr.rd, cpu.GPR[cpu.instr.rs] ^ cpu.GPR[cpu.instr.rt]);

        private static void NOR(CPU cpu) => cpu.setGPR(cpu.instr.rd, ~(cpu.GPR[cpu.instr.rs] | cpu.GPR[cpu.instr.rt]));

        private static void SLT(CPU cpu)
        {
            bool condition = (int)cpu.GPR[cpu.instr.rs] < (int)cpu.GPR[cpu.instr.rt];
            cpu.setGPR(cpu.instr.rd, Unsafe.As<bool, uint>(ref condition));
        }

        private static void SLTU(CPU cpu)
        {
            bool condition = cpu.GPR[cpu.instr.rs] < cpu.GPR[cpu.instr.rt];
            cpu.setGPR(cpu.instr.rd, Unsafe.As<bool, uint>(ref condition));
        }

        // Accesory methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool checkOverflow(uint a, uint b, uint r) => ((r ^ a) & (r ^ b) & 0x8000_0000) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool checkUnderflow(uint a, uint b, uint r) => ((r ^ a) & (a ^ b) & 0x8000_0000) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void setGPR(uint regN, uint value)
        {
            writeBack.register = regN;
            writeBack.value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void delayedread(CPU cpu, uint regN, uint value)
        {
            cpu.delayedMemoryread.register = regN;
            cpu.delayedMemoryread.value = value;
        }

    }

}
