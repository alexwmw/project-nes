﻿#define LOGGING

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using HelperMethods;
using static HelperMethods.StaticMethods;


namespace project_nes
{
    public class CPU
    {

        // Fields

        //6502 Registers
        private byte A;                     // Accumulator
        private byte X;                     // X Register
        private byte Y;                     // Y register
        private byte status;                // Status byte
        private byte stkp;                  // Stack pointer
        public ushort PC;                  // Program counter

        private CpuBus bus;
        private InstructionSet instructionSet;
        private int clockRate;

        //Emulation variables
        private byte opcode;                // Current instruction byte
        private byte data;                  // Fetched data
        private ushort address;             // Fetched address
        private ushort branch;              // Fetched branch operand
        private int cycles;                 // Cycles required by current instruction
        private Instruction currentInstr;   // Curently executing instruction

        //Debugging and monitoring
        private int clock_count;            // Total number of clock cycles passed
        private int logLineNo = 2;          // Line no. used in console logging. Offset by 2 to match Excel lines
        public State state;


        //CSV file variables;
        string csvFileName;
        string filePath;
        CultureInfo cultInfo;
        DateTime dateTime;
        DirectoryInfo csvLogs;
        public int cpuClockCount;

        // Constructors

        public CPU()
        {
            /* A 16 * 16 length list 
             * (inside an IEnumerable class 'InstructionSet')
             * 
             * The Add method of InstructionSet allows the list to be initialised 
             * using initializer lists - which make this long section more 
             * succint instead of repeating new Instruction(...) 256 times. 
             * But I did have to use a List<Instruction> instead 
             * of an array Instruction[], and create an indexer
             */
            instructionSet = new InstructionSet()
            {
              //----- 0 -----  ----- 1 -----  ----- 2 -----  ----- 3 -----  ----- 4 -----  ----- 5 -----  ----- 6 -----  ----- 7 -----  ----- 8 -----  ----- 9 -----  ----- A -----  ----- B -----  ----- C -----  ----- D -----  ----- E -----  ----- F -----  
                {BRK, Imm, 7}, {ORA, InX, 6}, {UNK, Imp, 2}, {UNK, Imp, 8}, {NOP, Imp, 3}, {ORA, Zpg, 3}, {ASL, Zpg, 5}, {UNK, Imp, 5}, {PHP, Imp, 3}, {ORA, Imm, 2}, {ASL, Imp, 2}, {UNK, Imp, 2}, {NOP, Imp, 4}, {ORA, Abs, 4}, {ASL, Abs, 6}, {UNK, Imp, 6}, // 0
                {BPL, Rel, 2}, {ORA, InY, 5}, {UNK, Imp, 2}, {UNK, Imp, 8}, {NOP, Imp, 4}, {ORA, ZpX, 4}, {ASL, ZpX, 6}, {UNK, Imp, 6}, {CLC, Imp, 2}, {ORA, AbY, 4}, {NOP, Imp, 2}, {UNK, Imp, 7}, {NOP, Imp, 4}, {ORA, AbX, 4}, {ASL, AbX, 7}, {UNK, Imp, 7}, // 1
                {JSR, Abs, 6}, {AND, InX, 6}, {UNK, Imp, 2}, {UNK, Imp, 8}, {BIT, Zpg, 3}, {AND, Zpg, 3}, {ROL, Zpg, 5}, {UNK, Imp, 5}, {PLP, Imp, 4}, {AND, Imm, 2}, {ROL, Imp, 2}, {UNK, Imp, 2}, {BIT, Abs, 4}, {AND, Abs, 4}, {ROL, Abs, 6}, {UNK, Imp, 6}, // 2
                {BMI, Rel, 2}, {AND, InY, 5}, {UNK, Imp, 2}, {UNK, Imp, 8}, {NOP, Imp, 4}, {AND, ZpX, 4}, {ROL, ZpX, 6}, {UNK, Imp, 6}, {SEC, Imp, 2}, {AND, AbY, 4}, {NOP, Imp, 2}, {UNK, Imp, 7}, {NOP, Imp, 4}, {AND, AbX, 4}, {ROL, AbX, 7}, {UNK, Imp, 7}, // 3
                {RTI, Imp, 6}, {EOR, InX, 6}, {UNK, Imp, 2}, {UNK, Imp, 8}, {NOP, Imp, 3}, {EOR, Zpg, 3}, {LSR, Zpg, 5}, {UNK, Imp, 5}, {PHA, Imp, 3}, {EOR, Imm, 2}, {LSR, Imp, 2}, {UNK, Imp, 2}, {JMP, Abs, 3}, {EOR, Abs, 4}, {LSR, Abs, 6}, {UNK, Imp, 6}, // 4
                {BVC, Rel, 2}, {EOR, InY, 5}, {UNK, Imp, 2}, {UNK, Imp, 8}, {NOP, Imp, 4}, {EOR, ZpX, 4}, {LSR, ZpX, 6}, {UNK, Imp, 6}, {CLI, Imp, 2}, {EOR, AbY, 4}, {NOP, Imp, 2}, {UNK, Imp, 7}, {NOP, Imp, 4}, {EOR, AbX, 4}, {LSR, AbX, 7}, {UNK, Imp, 7}, // 5
                {RTS, Imp, 6}, {ADC, InX, 6}, {UNK, Imp, 2}, {UNK, Imp, 8}, {NOP, Imp, 3}, {ADC, Zpg, 3}, {ROR, Zpg, 5}, {UNK, Imp, 5}, {PLA, Imp, 4}, {ADC, Imm, 2}, {ROR, Imp, 2}, {UNK, Imp, 2}, {JMP, Ind, 5}, {ADC, Abs, 4}, {ROR, Abs, 6}, {UNK, Imp, 6}, // 6
                {BVS, Rel, 2}, {ADC, InY, 5}, {UNK, Imp, 2}, {UNK, Imp, 8}, {NOP, Imp, 4}, {ADC, ZpX, 4}, {ROR, ZpX, 6}, {UNK, Imp, 6}, {SEI, Imp, 2}, {ADC, AbY, 4}, {NOP, Imp, 2}, {UNK, Imp, 7}, {NOP, Imp, 4}, {ADC, AbX, 4}, {ROR, AbX, 7}, {UNK, Imp, 7}, // 7
                {NOP, Imp, 2}, {STA, InX, 6}, {NOP, Imp, 2}, {UNK, Imp, 6}, {STY, Zpg, 3}, {STA, Zpg, 3}, {STX, Zpg, 3}, {UNK, Imp, 3}, {DEY, Imp, 2}, {NOP, Imp, 2}, {TXA, Imp, 2}, {UNK, Imp, 2}, {STY, Abs, 4}, {STA, Abs, 4}, {STX, Abs, 4}, {UNK, Imp, 4}, // 8
                {BCC, Rel, 2}, {STA, InY, 6}, {UNK, Imp, 2}, {UNK, Imp, 6}, {STY, ZpX, 4}, {STA, ZpX, 4}, {STX, ZpY, 4}, {UNK, Imp, 4}, {TYA, Imp, 2}, {STA, AbY, 5}, {TXS, Imp, 2}, {UNK, Imp, 5}, {NOP, Imp, 5}, {STA, AbX, 5}, {UNK, Imp, 5}, {UNK, Imp, 5}, // 9
                {LDY, Imm, 2}, {LDA, InX, 6}, {LDX, Imm, 2}, {UNK, Imp, 6}, {LDY, Zpg, 3}, {LDA, Zpg, 3}, {LDX, Zpg, 3}, {UNK, Imp, 3}, {TAY, Imp, 2}, {LDA, Imm, 2}, {TAX, Imp, 2}, {UNK, Imp, 2}, {LDY, Abs, 4}, {LDA, Abs, 4}, {LDX, Abs, 4}, {UNK, Imp, 4}, // A
                {BCS, Rel, 2}, {LDA, InY, 5}, {UNK, Imp, 2}, {UNK, Imp, 5}, {LDY, ZpX, 4}, {LDA, ZpX, 4}, {LDX, ZpY, 4}, {UNK, Imp, 4}, {CLV, Imp, 2}, {LDA, AbY, 4}, {TSX, Imp, 2}, {UNK, Imp, 4}, {LDY, AbX, 4}, {LDA, AbX, 4}, {LDX, AbY, 4}, {UNK, Imp, 4}, // B
                {CPY, Imm, 2}, {CMP, InX, 6}, {NOP, Imp, 2}, {UNK, Imp, 8}, {CPY, Zpg, 3}, {CMP, Zpg, 3}, {DEC, Zpg, 5}, {UNK, Imp, 5}, {INY, Imp, 2}, {CMP, Imm, 2}, {DEX, Imp, 2}, {UNK, Imp, 2}, {CPY, Abs, 4}, {CMP, Abs, 4}, {DEC, Abs, 6}, {UNK, Imp, 6}, // C
                {BNE, Rel, 2}, {CMP, InY, 5}, {UNK, Imp, 2}, {UNK, Imp, 8}, {NOP, Imp, 4}, {CMP, ZpX, 4}, {DEC, ZpX, 6}, {UNK, Imp, 6}, {CLD, Imp, 2}, {CMP, AbY, 4}, {NOP, Imp, 2}, {UNK, Imp, 7}, {NOP, Imp, 4}, {CMP, AbX, 4}, {DEC, AbX, 7}, {UNK, Imp, 7}, // D
                {CPX, Imm, 2}, {SBC, InX, 6}, {NOP, Imp, 2}, {UNK, Imp, 8}, {CPX, Zpg, 3}, {SBC, Zpg, 3}, {INC, Zpg, 5}, {UNK, Imp, 5}, {INX, Imp, 2}, {SBC, Imm, 2}, {NOP, Imp, 2}, {SBC, Imp, 2}, {CPX, Abs, 4}, {SBC, Abs, 4}, {INC, Abs, 6}, {UNK, Imp, 6}, // E
                {BEQ, Rel, 2}, {SBC, InY, 5}, {UNK, Imp, 2}, {UNK, Imp, 8}, {NOP, Imp, 4}, {SBC, ZpX, 4}, {INC, ZpX, 6}, {UNK, Imp, 6}, {SED, Imp, 2}, {SBC, AbY, 4}, {NOP, Imp, 2}, {UNK, Imp, 7}, {NOP, Imp, 4}, {SBC, AbX, 4}, {INC, AbX, 7}, {UNK, Imp, 7}, // F
            };
#if LOGGING
            //CSV log - file creation
            csvLogs = new DirectoryInfo(@"/Users/alexwright/Documents/MSc Files/Project/csv_logs");
            dateTime = DateTime.UtcNow;
            cultInfo = CultureInfo.CreateSpecificCulture("en-UK");
            csvFileName = $"project_nes_nestest_log_{dateTime.ToString("o", cultInfo)}.csv";
            filePath = $"{csvLogs.FullName}/{csvFileName}";
            using (File.Create(filePath)) { }
#endif
        }

        // Enums

        private enum Flags
        {
            C = 1 << 0, // Carry Bit
            Z = 1 << 1, // Zero
            I = 1 << 2, // Disable Interrupts
            D = 1 << 3, // Decimal Mode
            B = 1 << 4, // Break
            U = 1 << 5, // Unused
            V = 1 << 6, // Overflow
            N = 1 << 7, // Negative
        };


        // Public Methods

        public string OfficialTestResult
        {
            get
            {
                return ReadBus((ushort)0x02).x();
            }
        }

        public string UnofficialTestResult
        {
            get
            {
                return ReadBus((ushort)0x03).x();
            }
        }

        public void Clock()
        {
            cpuClockCount++;
            if (cycles == 0)
            {
                state = new State(this);
                opcode = ReadBus(PC++);
                currentInstr = instructionSet[opcode];
                cycles += currentInstr.Cycles;
                bool addrm = currentInstr.AddrMode();
                bool operation = currentInstr.Operation();
#if LOGGING
                //LogState();
                using (StreamWriter file = new StreamWriter(filePath, true))
                    file.WriteLine(state);
#endif
                if (addrm & operation)
                    cycles++;
            }
            clock_count++;
            cycles--;
        }



        public void Irq()
        {
            if (GetFlag(Flags.I) == 0)
            {
                //Push PC to stack
                WriteBus((ushort)(0x0100 + stkp--), PC.GetPage());
                WriteBus((ushort)(0x0100 + stkp--), PC.GetOffset());

                //Clear B, set I, U; and push status to stack
                SetFlags(
                    Flags.B, false,
                    Flags.I, true,
                    Flags.U, true);
                WriteBus((ushort)(0x0100 + stkp--), status);

                //Load interrupt vector from $FFFE/F 
                PC = Address16Bit(ReadBus(0xFFFE), ReadBus(0xFFFF));

                cycles = 7;
            }
        }

        public void Nmi()
        {
            //Push PC to stack
            WriteBus((ushort)(0x0100 + stkp--), PC.GetPage());
            WriteBus((ushort)(0x0100 + stkp--), PC.GetOffset());

            //Clear B, set I, U; and push status to stack
            SetFlags(
                Flags.B, false,
                Flags.I, true,
                Flags.U, true);
            WriteBus((ushort)(0x0100 + stkp--), status);

            //Load interrupt vector from $FFFA/B 
            PC = Address16Bit(ReadBus(0xFFFA), ReadBus(0xFFFB));

            cycles = 8;
        }

        public void Reset()
        {
            //Set PC
            PC = Address16Bit(ReadBus(0xFFFC), ReadBus(0xFFFD));

            //Reset registers
            A = X = Y = 0;
            stkp = 0xFD;
            status |= (byte)(Flags.U);

            //Reset variables
            address = branch = data = 0;

            cycles = 7; 

            SetFlags(Flags.I, true);
        }

        public void PowerOn()
        {
            throw new NotImplementedException();
        }

        public void ConnectBus(CpuBus bus)
        {
            this.bus = bus;
        }


        // Private Methods

        private byte ReadBus(ushort address)
        {
            return bus == null ? throw new NullReferenceException("CPU tried to read. Bus not connected.")
                       : bus.Read(address);
        }

        private byte ReadBus(int address)
        {
            return bus == null ? throw new NullReferenceException("CPU tried to read. Bus not connected.")
                       : bus.Read((ushort)((ushort)address & 0xFFFF));
        }

        private void WriteBus(ushort address, byte data)
        {
            if (bus == null)
                throw new Exception("CPU tried to write. Bus not connected.");
            bus.Write(address, data);
        }

        private byte GetFlag(Flags f)
            => (byte)(status & (byte)f);

        private void SetFlags(Flags f, bool b)
        {
            if (b)
                status |= (byte)f;
            else
                status &= (byte)~f;
        }

        private void SetFlags(Flags f1, bool b1, Flags f2, bool b2)
        {
            if (b1) status |= (byte)f1;
            else status &= (byte)~f1;
            if (b2) status |= (byte)f2;
            else status &= (byte)~f2;
        }

        private void SetFlags(Flags f1, bool b1, Flags f2, bool b2, Flags f3, bool b3)
        {
            if (b1) status |= (byte)f1;
            else status &= (byte)~f1;
            if (b2) status |= (byte)f2;
            else status &= (byte)~f2;
            if (b3) status |= (byte)f3;
            else status &= (byte)~f3;
        }

        private void SetFlags(Flags f1, bool b1, Flags f2, bool b2, Flags f3, bool b3, Flags f4, bool b4)
        {
            if (b1) status |= (byte)f1;
            else status &= (byte)~f1;
            if (b2) status |= (byte)f2;
            else status &= (byte)~f2;
            if (b3) status |= (byte)f3;
            else status &= (byte)~f3;
            if (b4) status |= (byte)f4;
            else status &= (byte)~f4;
        }

        private void Fetch()
        {
            if (CurrentModeImplicit())
                data = A;
            else
                data = ReadBus(address);
        }

        public void LogState()
        {
            string rowN = string.Format("{0,5}", logLineNo++);
            byte byte1 = opcode;
            byte byte2 = (byte)(CurrentMode() == Rel ? branch : address & 0x00FF);
            byte byte3 = (byte)(CurrentMode() == Rel ? 0 : (address & 0xFF00) >> 8);
            string line =
                $"{rowN}.   {state.PC.x()}  {byte1.x()} {byte2.x()} {byte3.x()}  " +
                $"{currentInstr.Name} {address.x()}            " +
                $"A:{A.x()} X:{X.x()} Y:{Y.x()} P:{status.x()} SP:{stkp.x()} PPU:  x,xxx CYC:{clock_count}";
            Console.WriteLine(line);
        }



        private Func<bool> CurrentMode()
            => instructionSet[opcode].AddrMode;

        private bool CurrentModeImplicit()
            => CurrentMode() == Imp | CurrentMode() == Acc;


        // Addressing modes

        //Absolute          a       Cycles:
        // The operand is a memory address
        // Addresses are little-endian
        private bool Abs()
        {
            byte lowByte = ReadBus(PC++);
            byte highByte = ReadBus(PC++);
            address = Address16Bit(lowByte, highByte);
            return false;
        }

        //Absolute Indexed  a,x     Cycles: 4+
        private bool AbX()
        {
            byte lowByte = ReadBus(PC++);
            byte highByte = ReadBus(PC++);
            address = Address16Bit(lowByte, highByte);
            address += X;
            return address.GetPage() != highByte;
        }

        //Absolute Indexed  a,y     Cycles: 4+
        private bool AbY()
        {
            byte lowByte = ReadBus(PC++);
            byte highByte = ReadBus(PC++);
            address = Address16Bit(lowByte, highByte);
            address += Y;
            return address.GetPage() != highByte;
        }

        //Accumulator       A       Cycles:
        private bool Acc()
        {
            return false;
        }

        //Immediate         #v      Cycles:
        // Data expected in the next (immediate) byte.
        private bool Imm()
        {
            address = PC++;
            return false;
        }

        //Implicit                  Cycles:
        // No additional data required for the instruction
        private bool Imp()
        {
            return false;
        }

        //Indirect          (a)     Cycles:
        private bool Ind()
        {
            //get address start location in two reads
            byte lowByte = ReadBus(PC++);
            byte highByte = ReadBus(PC++);

            ushort target = Address16Bit(lowByte, highByte);

            //construct address
            ushort addrLoLocation = target;
            ushort addrHiLocation = (ushort)
                (lowByte == 0xFF
                ? //simulate hardware bug
                (target & 0xFF00)
                : //otherwise, correct behaviour
                (target + 1));

            byte addrHi = ReadBus(addrHiLocation);
            byte addrLo = ReadBus(addrLoLocation);

            address = Address16Bit(addrLo, addrHi);
            return false;
        }

        //Indexed Indirect  (d,x)   Cycles: 6
        private bool InX()
        {
            ushort temp = ReadBus(PC++);
            byte lowByte = ReadBus(temp + X & 0x00FF);
            byte highByte = ReadBus(temp + X + 1 & 0x00FF);
            address = Address16Bit(lowByte, highByte);
            return false;
        }

        //Indirect Indexed  (d),y   Cycles: 5+
        private bool InY()
        {
            byte temp = ReadBus(PC++);
            byte lowByte = ReadBus(temp & 0x00FF);
            byte highByte = ReadBus(temp + 1 & 0x00FF);
            address = Address16Bit(lowByte, highByte);
            address += Y;
            return address.GetPage() != highByte;
        }

        //Relative         label    Cycles:
        /*
        Relative Addressing is used on the various Branch-On-Condition instructions.
        A 1 byte signed operand is added to the program counter, and the program continues execution
        from the new address.
        Because this value is signed, values #00-#7F are positive, and values #FF-#80 are negative.
        Keep in mind that the program counter will be set to the address after the branch instruction,
        so take this into account when calculating your new position.
        Since branching works by checking a relevant status bit, make sure it is set to the proper value
        prior to calling the branch instruction. This is usually done with a CMP instruction.
        If you need to move the program counter to a location greater or less than 127 bytes away from
        the current location, make a nearby JMP instruction, and move the program counter to the JMP line.
        */
        private bool Rel()
        {
            branch = ReadBus(PC++);
            if (branch.IsNegative())
                branch |= 0xFF00;
            return false;
        }

        //Zero Page         d       Cycles:
        private bool Zpg()
        {
            address = ReadBus(PC++);
            address &= 0x00FF;
            return false;
        }

        //Zero Page Indexed d,x     Cycles: 4
        private bool ZpX()
        {
            address = (ushort)(ReadBus(PC++) + X);
            address &= 0x00FF;

            return false;
        }

        //Zero Page Indexed d,y     Cycles: 4
        private bool ZpY()
        {
            address = (ushort)(ReadBus(PC++) + Y);
            address &= 0x00FF;
            return false;
        }


        // Opcodes

        /** ADC - Add with Carry
         * 
         * This instruction adds the contents of a memory location to 
         * the accumulator together with the carry bit. 
         * If overflow occurs the carry bit is set, 
         * this enables multiple byte addition to be performed.
         */
        private bool ADC()
        {
            Fetch();
            ushort temp = (ushort)(A + data + GetFlag(Flags.C));
            bool isSignedOverflow = (~(A ^ data) & (A ^ temp) & 0x0080) > 0;
            SetFlags(
                Flags.C, (ushort)(temp & 0x0100) > 0,
                Flags.Z, ((byte)(temp & 0x00FF)).IsZero(),
                Flags.V, isSignedOverflow,
                Flags.N, temp.IsNegative());
            A = (byte)(temp & 0x00FF);
            return true;
        }

        /** AND - Logical AND
         * 
         * A logical AND is performed, bit by bit, on the accumulator 
         * contents using the contents of a byte of memory.
         */
        private bool AND()
        {
            Fetch();
            A &= data;
            SetFlags(
                Flags.Z, A.IsZero(),
                Flags.N, A.IsNegative());
            return true;
        }

        /** ASL - Artihmetic Shift Left
         * 
         * This operation shifts all the bits of the accumulator or memory contents one bit left. 
         * Bit 0 is set to 0 and bit 7 is placed in the carry flag. 
         * Flags: C, N, Z 
         */
        private bool ASL()
        {
            Fetch();
            ushort temp = (ushort)(data << 1);
            SetFlags(
                Flags.C, temp.GetPage() > 0,
                Flags.Z, (temp & 0x00FF) == 0,
                Flags.N, temp.IsNegative());

            if (CurrentModeImplicit())
                A = (byte)(temp & 0x00FF);
            else
                WriteBus(address, (byte)(temp & 0x00FF));

            return false;
        }

        /** BCC - Branch if Carry Clear
         * 
         * If the carry flag is clear then add the relative displacement to the 
         * program counter to cause a branch to a new location.
         */
        private bool BCC()
        {
            if (GetFlag(Flags.C) == 0)
            {
                address = (ushort)(PC + branch);
                // Add one cycle if same page, two for page change
                cycles += address.GetPage() == PC.GetPage() ? 1 : 2;
                PC = address;
            }
            return false;
        }

        /** BCS - Branch if Carry Set
         * 
         * If the carry flag is set then add the relative displacement to the 
         * program counter to cause a branch to a new location.
         */
        private bool BCS()
        {
            if (GetFlag(Flags.C) > 0)
            {
                address = (ushort)(PC + branch);
                cycles += address.GetPage() == PC.GetPage() ? 1 : 2;
                PC = address;
            }
            return false;
        }


        /** BEQ - Branch if Equal
         * 
         * If the zero flag is set then add the relative displacement to the 
         * program counter to cause a branch to a new location.
         */
        private bool BEQ()
        {
            if (GetFlag(Flags.Z) > 0)
            {
                address = (ushort)(PC + branch);
                cycles += address.GetPage() == PC.GetPage() ? 1 : 2;
                PC = address;
            }
            return false;
        }

        /** BIT - Bit Test
         * 
         * This instructions is used to test if one or more bits are set in a target memory location. 
         * The mask pattern in A is ANDed with the value in memory to set or clear the zero flag, 
         * but the result is not kept. 
         * 
         * Bits 7 and 6 of the value from memory are copied into the N and V flags.
         */
        private bool BIT()
        {
            Fetch();
            byte temp = (byte)(A & data);
            SetFlags(
                Flags.Z, temp.IsZero(),
                Flags.N, data.IsNegative(),
                Flags.V, (data & (1 << 6)) > 0);
            return false;
        }

        /** BMI - Branch if Minus
         * 
         * If the negative flag is set then add the relative displacement to the 
         * program counter to cause a branch to a new location.
         */
        private bool BMI()
        {
            if (GetFlag(Flags.N) > 0)
            {
                address = (ushort)(PC + branch);
                cycles += address.GetPage() == PC.GetPage() ? 1 : 2;
                PC = address;
            }
            return false;
        }

        /** BNE - Branch if Not Equal
         * 
         * If the zero flag is clear then add the relative displacement to the 
         * program counter to cause a branch to a new location.
         */
        private bool BNE()
        {
            if (GetFlag(Flags.Z) == 0)
            {
                address = (ushort)(PC + branch);
                cycles += address.GetPage() == PC.GetPage() ? 1 : 2;
                PC = address;
            }
            return false;
        }

        /** BPL - Branch if Positive
         * 
         * If the negative flag is clear then add the relative displacement to the 
         * program counter to cause a branch to a new location.
         */
        private bool BPL()
        {
            if (GetFlag(Flags.N) == 0)
            {
                address = (ushort)(PC + branch);
                cycles += address.GetPage() == PC.GetPage() ? 1 : 2;
                PC = address;
            }
            return false;
        }

        /** BRK - Force Interrupt
         * 
         * The BRK instruction forces the generation of an interrupt request. 
         * The program counter and processor status are pushed on the stack 
         * then the IRQ interrupt vector at $FFFE/F is loaded into the PC 
         * and the break flag in the status set to one.
         */
        private bool BRK()
        {
            //Increment before break
            PC++;

            SetFlags(Flags.I, true);

            //Push PC to stack
            byte highByte = (byte)((PC & 0xFF00) >> 8);
            byte lowByte = (byte)(PC & 0x00FF);
            WriteBus(((ushort)(0x0100 + stkp--)), highByte);
            WriteBus(((ushort)(0x0100 + stkp--)), lowByte);

            //Push status to stack
            WriteBus(((ushort)(0x0100 + stkp--)), status);

            //Load interrupt vector from $FFFE/F 
            PC = Address16Bit(ReadBus(0xFFFE), ReadBus(0xFFFF));

            SetFlags(Flags.B, true);
            return false;
        }

        /** BVC - Branch if Overflow Clear
         * 
         * If the overflow flag is clear then add the relative displacement to the 
         * program counter to cause a branch to a new location.
         */
        private bool BVC()
        {
            if (GetFlag(Flags.V) == 0)
            {
                address = (ushort)(PC + branch);
                cycles += address.GetPage() == PC.GetPage() ? 1 : 2;
                PC = address;
            }
            return false;
        }

        /** BVS - Branch if Overflow Set
         * 
         * If the overflow flag is set then add the relative displacement to the 
         * program counter to cause a branch to a new location.
         */
        private bool BVS()
        {
            if (GetFlag(Flags.V) > 0)
            {
                address = (ushort)(PC + branch);
                cycles += address.GetPage() == PC.GetPage() ? 1 : 2;
                PC = address;
            }
            return false;
        }

        /** CLC - Clear Carry Flag
         * 
         * Set the carry flag to zero.
         */
        private bool CLC()
        {
            SetFlags(Flags.C, false);
            return false;
        }

        /** CLD - Clear Decimal Flag
         */
        private bool CLD()
        {
            SetFlags(Flags.D, false);
            return false;
        }

        /**
         * CLI - Clear Interrupt Flag
         */
        private bool CLI()
        {
            SetFlags(Flags.I, false);
            return false;
        }

        /** CLV - Clear Overflow Flag
         */
        private bool CLV()
        {
            SetFlags(Flags.V, false);
            return false;
        }

        /** CMP - compare
         * 
         * This instruction compares the contents of the accumulator with another memory 
         * held value and sets the zero and carry flags as appropriate.
         */
        private bool CMP()
        {
            Fetch();
            SetFlags(
                Flags.C, A >= data,
                Flags.Z, A == data,
                Flags.N, ((byte)(A - data)).IsNegative());
            return true;
        }

        /** CPX - Compare X Register
         * 
         * This instruction compares the contents of the X register with another memory 
         * held value and sets the zero and carry flags as appropriate.
         */
        private bool CPX()
        {
            Fetch();
            SetFlags(
                Flags.C, X >= data,
                Flags.Z, X == data,
                Flags.N, ((byte)(X - data)).IsNegative());
            return false;
        }

        /** CPY - Compare X Register
         * 
         * This instruction compares the contents of the X register with another memory 
         * held value and sets the zero and carry flags as appropriate.
         */
        private bool CPY()
        {
            Fetch();
            SetFlags(
                Flags.C, Y >= data,
                Flags.Z, Y == data,
                Flags.N, ((byte)(Y - data)).IsNegative());
            return false;
        }

        /** DEC Decrement Memory
         * 
         * Subtracts one from the value held at a specified memory location setting 
         * the zero and negative flags as appropriate.
         */
        private bool DEC()
        {
            Fetch();
            WriteBus(address, (byte)(data - 1));
            SetFlags(
                Flags.Z, ReadBus(address).IsZero(),
                Flags.N, ReadBus(address).IsNegative());
            return false;
        }

        /** Decrement X
         * Subtracts one from the value held at X setting 
         * the zero and negative flags as appropriate.
         */
        private bool DEX()
        {
            X--;
            SetFlags(
                Flags.Z, X.IsZero(),
                Flags.N, X.IsNegative());
            return false;
        }

        /** DEY Decrement Y
         * 
         * Subtracts one from the value held at Y setting 
         * the zero and negative flags as appropriate.
         */
        private bool DEY()
        {
            Y--;
            SetFlags(
                Flags.Z, Y.IsZero(),
                Flags.N, Y.IsNegative());
            return false;
        }

        private bool EOR()
        {
            Fetch();
            A = (byte)(A ^ data);
            SetFlags(
                Flags.Z, A.IsZero(),
                Flags.N, A.IsNegative());
            return true;
        }

        /** INC - Increment Memory
         * 
         * Adds one to the value held at a specified memory location setting 
         * the zero and negative flags as appropriate.
         */
        private bool INC()
        {
            Fetch();
            WriteBus(address, (byte)(data + 1));
            SetFlags(
                Flags.Z, ReadBus(address).IsZero(),
                Flags.N, ReadBus(address).IsNegative());
            return false;
        }

        /** INX - Increment X
         * 
         * Adds one to the value held at X setting 
         * the zero and negative flags as appropriate.
         */
        private bool INX()
        {
            X++;
            SetFlags(
                Flags.Z, X.IsZero(),
                Flags.N, X.IsNegative());
            return false;
        }

        /** INY - Increment Y
         * 
         * Adds one to the value held at Y setting 
         * the zero and negative flags as appropriate.
         */
        private bool INY()
        {
            Y++;
            SetFlags(
                Flags.Z, Y.IsZero(),
                Flags.N, Y.IsNegative());
            return false;
        }

        /** JMP - Jump
         * 
         * Sets the program counter to the address specified by the operand.
         */
        private bool JMP()
        {
            PC = address;
            return false;
        }

        /** JSR - Jump to Subroutine
         * 
         * pushes the address-1 of the next operation on to the stack before 
         * transferring program control to the following address.
         */
        private bool JSR()
        {
            //Previous PC (or else this instruction will be stored)
            PC--;

            WriteBus((ushort)(0x0100 + stkp--), PC.GetPage());
            WriteBus((ushort)(0x0100 + stkp--), PC.GetOffset());
            PC = address;
            return false;
        }

        /** Load the Accumuator
         */
        private bool LDA()
        {
            Fetch();
            A = data;
            SetFlags(
                Flags.Z, A.IsZero(),
                Flags.N, A.IsNegative());
            return true;
        }

        /** Load X
         */
        private bool LDX()
        {
            Fetch();
            X = data;
            SetFlags(
                Flags.Z, X.IsZero(),
                Flags.N, X.IsNegative());
            return true;
        }

        /** Load Y
         */
        private bool LDY()
        {
            Fetch();
            Y = data;
            SetFlags(
                Flags.Z, Y.IsZero(),
                Flags.N, Y.IsNegative());
            return true;
        }

        /** LSR - Logical Shift Right
         * 
         * Each of the bits in A or M is shift one place to the right. 
         * The bit that was in bit 0 is shifted into the carry flag. 
         * Bit 7 is set to zero.
         * 
         */
        private bool LSR()
        {
            Fetch();
            byte temp = (byte)(data >> 1);
            SetFlags(
                Flags.C, (data & 0x01) == 1,
                Flags.Z, temp.IsZero(),
                Flags.N, temp.IsNegative());

            if (CurrentModeImplicit())
                A = temp;
            else
                WriteBus(address, temp);
            return false;
        }

        /** No Op
         */
        private bool NOP()
        {
            switch (opcode)
            {
                case 0x04:
                case 0x14:
                case 0x34:
                case 0x44:
                case 0x54:
                case 0x64:
                case 0x74:
                case 0x80:
                case 0xD4:
                case 0xF4:
                    PC++;
                    return true;
                case 0x0C:
                    PC += 2;
                    return true;
                case 0x1C:
                case 0x3C:
                case 0x5C:
                case 0x7C:
                case 0xDC:
                case 0xFC:
                    PC += 2;
                    cycles++;
                    return true;
            }
            return false;
        }

        /** ORA - Logical Inclusive OR
         * 
         * An inclusive OR is performed, bit by bit, on the accumulator 
         * contents using the contents of a byte of memory.
         */
        private bool ORA()
        {
            Fetch();
            A |= data;
            SetFlags(
                Flags.Z, A.IsZero(),
                Flags.N, A.IsNegative());
            return true;
        }

        /** PHA - Push Accumulator
         * 
         * Pushes a copy of the accumulator on to the stack
         */
        private bool PHA()
        {
            WriteBus((ushort)(0x0100 + stkp--), A);
            return false;
        }

        /** PHP - Push Status
         * 
         * Pushes a copy of the staus register on to the stack
         */
        private bool PHP()
        {
            SetFlags(Flags.B, true); 
            WriteBus((ushort)(0x0100 + stkp--), status);
            SetFlags(Flags.B, false);
            return false;
        }


        /** PLA - Pop Accumulator
         * 
         * Pop the accumulator off of the stack
         */
        private bool PLA()
        {
            A = ReadBus(0x0100 + ++stkp);
            SetFlags(
                Flags.Z, A.IsZero(),
                Flags.N, A.IsNegative(),
                Flags.B, false,
                Flags.U, true);          //<------------ set according to nestest
            return false;
        }

        /** PLP - Pop Status
         * 
         * Pop the staus register off of the stack
         * todo: Read that U flag is set to 1 after pull - investigate
         */
        private bool PLP()
        {
            status = ReadBus(0x0100 + ++stkp);
            SetFlags(Flags.U, true,
                     Flags.B, false);
            return false;
        }

        /** ROL - Rotate Left
         * 
         * Move each of the bits in either A or M one place to the left. 
         * Bit 0 is filled with the current value of the carry flag whilst 
         * the old bit 7 becomes the new carry flag value.
         */
        private bool ROL()
        {
            Fetch();
            ushort temp = (ushort)((data << 1) | GetFlag(Flags.C));
            SetFlags(
                Flags.C, (temp & 0xFF00) > 0,
                Flags.Z, (temp & 0x00FF) == 0,
                Flags.N, temp.IsNegative());

            if (CurrentModeImplicit())
                A = (byte)(temp & 0x00FF);
            else
                WriteBus(address, (byte)(temp & 0x00FF));
            return false;
        }

        /** ROR - Rotate Right
         * 
         * Move each of the bits in either A or M one place to the right. 
         * Bit 7 is filled with the current value of the carry flag 
         * whilst the old bit 0 becomes the new carry flag value.
         * 
         */
        private bool ROR()
        {
            Fetch();
            ushort temp = (ushort)(GetFlag(Flags.C) << 7 | data >> 1);
            SetFlags(
                Flags.C, (data & 0x0001) == 1,
                Flags.Z, (temp & 0x00FF) == 0,
                Flags.N, temp.IsNegative());

            if (CurrentModeImplicit())
                A = (byte)(temp & 0x00FF);
            else
                WriteBus(address, (byte)(temp & 0x00FF));
            return false;
        }

        /** RTI - Return from Interrupt
         * 
         * The RTI instruction is used at the end of an interrupt processing routine. 
         * It pulls the processor flags from the stack followed by the program counter.
         */
        private bool RTI()
        {
            status = ReadBus(0x0100 + ++stkp);
            PC = ReadBus(0x0100 + ++stkp);
            PC |= (ushort)(ReadBus(0x0100 + ++stkp) << 8);
            SetFlags(Flags.U, true);
            return false;
        }

        /** RTS - Return from Subroutine
         * 
         * The RTS instruction is used at the end of a subroutine to 
         * return to the calling routine. 
         * It pulls the program counter (minus one) from the stack.
         */
        private bool RTS()
        {
            PC = ReadBus(0x0100 + ++stkp);
            PC |= (ushort)(ReadBus(0x0100 + ++stkp) << 8);
            PC++;
            return false;
        }

        /** SBC - Subtract with Carry
         * 
         * This instruction subtracts the contents of a memory location 
         * to the accumulator together with the not of the carry bit. 
         * If overflow occurs the carry bit is clear, 
         * this enables multiple byte subtraction to be performed.
         */
        private bool SBC()
        {
            Fetch();
            ushort value = (ushort)(data ^ 0x00FF);
            ushort temp = (ushort)(A + value + GetFlag(Flags.C));
            bool isSignedOverflow = ((temp ^ A) & (temp ^ value) & 0x0080) > 0;
            SetFlags(
                Flags.C, temp.GetPage() > 0,
                Flags.Z, temp.GetOffset().IsZero(),
                Flags.V, isSignedOverflow,
                Flags.N, temp.IsNegative());
            A = (byte)(temp & 0x00FF);
            return true;
        }

        /** SEC - Set Carry Flag
         * 
         */
        private bool SEC()
        {
            SetFlags(Flags.C, true);
            return false;
        }

        /** SED - Set Decimal Flag
         * 
         */
        private bool SED()
        {
            SetFlags(Flags.D, true);
            return false;
        }

        /** SEI - Set Interrupt Disable Flag
         * 
         */
        private bool SEI()
        {
            SetFlags(Flags.I, true);
            return false;
        }

        /** STA - Store Accumulator
         * 
         * Stores the contents of the accumulator into memory.
         */
        private bool STA()
        {
            WriteBus(address, A);
            return false;
        }

        /** STX - Store X
         * 
         */
        private bool STX()
        {
            WriteBus(address, X);
            return false;
        }

        /** STY - Store Y
         * 
         */
        private bool STY()
        {
            WriteBus(address, Y);
            return false;
        }

        /** TAX - Transfer A to X
         * 
         */
        private bool TAX()
        {
            X = A;
            SetFlags(
                Flags.Z, X == 0,
                Flags.N, X.IsNegative());
            return false;
        }

        /** TAY - Transfer A to Y
         * 
         */
        private bool TAY()
        {
            Y = A;
            SetFlags(
                Flags.Z, Y == 0,
                Flags.N, Y.IsNegative());
            return false;
        }

        /** TSX - Transfer Stackp to X
         * 
         */
        private bool TSX()
        {
            X = stkp;
            SetFlags(
                Flags.Z, X == 0,
                Flags.N, X.IsNegative());
            return false;
        }

        /** TXA - Transfer X to A
         * 
         */
        private bool TXA()
        {
            A = X;
            SetFlags(
                Flags.Z, A == 0,
                Flags.N, A.IsNegative());
            return false;
        }

        /** TXS - Transfer X to Stackp
         * 
         */
        private bool TXS()
        {
            stkp = X;
            return false;
        }

        /** TYA - Transfer Y to A
         * 
         */
        private bool TYA()
        {
            A = Y;
            SetFlags(
                Flags.Z, A == 0,
                Flags.N, A.IsNegative());
            return false;
        }


        /**UNK - Unofficial/unknown opcode
         * 
         */
        private bool UNK() => NOP();



        // Structs

        private struct Instruction
        {
            public Instruction(Func<bool> op, Func<bool> addrm, int cycles)
            {
                Operation = op;
                AddrMode = addrm;
                Cycles = cycles;
            }

            public string Name {
                get => Operation.Method.Name;
            }

            public string NameOfAddrMode {
                get => AddrMode.Method.Name;
            }

            public Func<bool> Operation { get; }

            public Func<bool> AddrMode { get; }

            public int Cycles { get; }
        }

        // Classes

        public struct State
        {
            public ushort PC;
            public byte A, X, Y, stkp, status;
            CPU cpuRef;

            public State(CPU cpu)
            {
                cpuRef = cpu;
                PC = cpu.PC;
                A = cpu.A;
                X = cpu.X;
                Y = cpu.Y;
                stkp = cpu.stkp;
                status = cpu.status;
            }

            public override string ToString()
               =>
               $"{PC.x()}," +
               $"{cpuRef.opcode.x()}," +
               $"{(cpuRef.address & 0x00FF).x()}," +
               $"{(cpuRef.address & 0xFF00).x()}," +
               $"{cpuRef.currentInstr.Name}," +
               $"{cpuRef.address.x()}," +
               $"{A.x()}," +
               $"{X.x()}," +
               $"{Y.x()}," +
               $"{status.x()}," +
               $"{stkp.x()}," +
               $"{0},{000}," +
               $"{cpuRef.clock_count}";
        }
            

        private class InstructionSet : IEnumerable<Instruction>
        {
            private List<Instruction> ins = new List<Instruction>();

            public IEnumerator<Instruction> GetEnumerator()
                => ins.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => ins.GetEnumerator();

            public void Add(Func<bool> op, Func<bool> addrm, int cycles)
                => ins.Add(new Instruction(op, addrm, cycles));

            public Instruction this[int i]    // Indexer declaration  
            {
                get { return this.ins[i]; }
                set { this.ins[i] = value; }
            }
        }


    }
}
