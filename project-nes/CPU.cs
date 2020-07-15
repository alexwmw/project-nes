using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExtensionMethods;


namespace project_nes
{
    public class CPU : iCPU
    {

        // Fields

        private const ushort stkBase = 0x0100;
        private byte opcode;                // Current instruction byte
        private byte data;                  // Fetched data
        private int cycles;                 // Cycles required by current instruction
        private ushort address;             // Fetched Address
        private int clock_count;            // Total number of clock cycles passed
        private iBus bus;
        private InstructionSet instructionSet;

        // Constructors

        public CPU()
        {
            A = 0;
            X = 0;
            Y = 0;
            Stkp = 0;
            Pc = 0;
            Status = 0;

            /* This will be a 16 * 16 length list 
             * (inside an IEnumerable class object 'InstructionSet')
             * 
             * The Add method of InstructionSet allows the list to be initialised 
             * using initializer lists - which should make this long section much
             * easier to read instead of repeating new Instruction(...) 256 times. 
             * But I did have to use a List<Instruction> instead 
             * of an array Instruction[], and create an indexer
             */
            instructionSet = new InstructionSet()
            {
                {ADC, Imp, 4}
            };
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


        // Properties

        private byte A { get; set; }        // Accumulator
        private byte X { get; set; }        // X Register
        private byte Y { get; set; }        // Y register
        private byte Stkp { get; set; }     // Stack pointer
        private ushort Pc { get; set; }     // Program counter
        private byte Status { get; set; }   // Status byte



        // Public Methods

        public void Clock()
        {
            if(cycles == 0)
            {
                opcode = Read(Pc);
                Instruction current = instructionSet[opcode];

                cycles += current.Cycles;
                bool addrm = current.AddrMode();
                bool operation = current.Operation();

                if (addrm & operation)
                    cycles++;
            }
            clock_count++;
            cycles--;
        }

        public void ConnectBus(iBus bus)
        {
            this.bus = bus;
        }

        public void Irq()
        {
            throw new NotImplementedException();
        }

        public void Nmi()
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }


        //Public TestOpCode function for testing
        public byte TestOpCode(string opcode)
        {
            try
            {
                MethodInfo mi = this.GetType().GetMethod(opcode);
                return (byte)mi.Invoke(this, null);
            }
            catch (AmbiguousMatchException e)
            {
                Console.WriteLine("Unknown Opcode");
                throw e;
            }
        }

        //Public AddrMode function for testing
        public byte TestAddrMode(string addrmode)
        {
            try
            {
                MethodInfo mi = this.GetType().GetMethod(addrmode);
                return (byte)mi.Invoke(null, null);
            }
            catch (AmbiguousMatchException e)
            {
                Console.WriteLine("Unknown Addressing Mode");
                throw e;
            }
        }


        // Private Methods

        private byte Read(ushort address)
            => bus.Read(address);


        private byte Read(int address)
            => address >= 0 & address <= 0xFFFF
            ? bus.Read((ushort)address)
            : throw new ArgumentOutOfRangeException(
                $"Invalid address in CPU Read(int address). Address must be between 0 and 0xFFFF. Argument: {address}");

        private void Write(ushort address, byte data)
            => bus.Write(address, data);

        private byte GetFlag(Flags f)
            => (byte)(Status & (byte)f);

        private void SetFlags(Flags f, bool b)
        {
            if (b)
                Status |= (byte)f;
            else
                Status &= (byte)~f;
        }

        private void SetFlags(Flags f1, bool b1, Flags f2, bool b2)
        {
            if (b1) Status |= (byte)f1;
            else    Status &= (byte)~f1;
            if (b2) Status |= (byte)f2;
            else    Status &= (byte)~f2;
        }

        private void SetFlags(Flags f1, bool b1, Flags f2, bool b2, Flags f3, bool b3)
        {
            if (b1) Status |= (byte)f1;
            else    Status &= (byte)~f1;
            if (b2) Status |= (byte)f2;
            else    Status &= (byte)~f2;
            if (b3) Status |= (byte)f3;
            else    Status &= (byte)~f3;
        }


        private void Fetch()
        {
            if (CurrentModeImplicit())
                data = A;
            else
                data = Read(address);
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
            byte lowByte = Read(Pc++);
            byte highByte = Read(Pc++);
            address = (ushort)((highByte << 8) | lowByte);
            return false;
        }

        //Absolute Indexed  a,x     Cycles: 4+
        private bool AbsX()
        {
            byte lowByte = Read(Pc++);
            byte highByte = Read(Pc++);
            address = (ushort)
                (
                ((highByte << 8) | lowByte) + X
                );

            if ((address & 0xFF00) != (highByte << 8))
                return true;
            else
                return false;
        }

        //Absolute Indexed  a,y     Cycles: 4+
        private bool AbsY()
        {
            byte lowByte = Read(Pc++);
            byte highByte = Read(Pc++);
            address = (ushort)
                (
                ((highByte << 8) | lowByte) + Y
                );

            if ((address & 0xFF00) != (highByte << 8))
                return true;
            else
                return false;
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
            address = Read(Pc++);
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
            byte lowByte = Read(Pc++);
            byte highByte = Read(Pc++);

            ushort tempAddress = (ushort)((highByte << 8) | lowByte);

            if (tempAddress == 0x00FF) // Simulate hardware bug
                address = (ushort)
                    (Read(tempAddress & 0xFF00) << 8 | Read(tempAddress));
            else
                address = (ushort)
                    (Read(tempAddress + 1) << 8 | Read(tempAddress));

            return false;
            
        }

        //Indexed Indirect  (d,x)   Cycles: 6
        private bool IndX()
        {
            byte temp = Read(Pc++);
            byte lowByte  = Read(temp + X & 0x00FF);
            byte highByte = Read(temp + X + 1 & 0x00FF);

            address = (ushort)((highByte << 8) | lowByte);

            return false;
        }

        //Indirect Indexed  (d),y   Cycles: 5+
        private bool IndY()
        {
            byte temp = Read(Pc++);
            byte lowByte = Read(temp & 0x00FF);
            byte highByte = Read(temp + 1 & 0x00FF);

            address = (ushort)(((highByte << 8) | lowByte) + Y);

            if ((address & 0xFF00) != (highByte << 8))
                return true;
            else
                return false;
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
            address = Read(Pc++);
            if (address.IsNegative())
                address |= 0x00FF;

            return false;
        }

        //Zero Page         d       Cycles:
        private bool Zpg()
        {
            address = Read(Pc++);
            address &= 0x00FF;

            return false;
        }

        //Zero Page Indexed d,x     Cycles: 4
        private bool ZpX()
        {
            address = (ushort)(Read(Pc++) + X);
            address &= 0x00FF;

            return false;
        }

        //Zero Page Indexed d,y     Cycles: 4
        private bool ZpY()
        {
            address = (ushort)(Read(Pc++) + Y);
            address &= 0x00FF;

            return false;
        }




        // Opcodes

        //Add with Carry
        private bool ADC()
        {
            return false;
        }

        /** ANS - Logical AND
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
                Flags.N, A.IsNegative()
                );

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
                Flags.N, temp.IsNegative()
                );

            if (CurrentModeImplicit())
                A = (byte)(temp & 0x00FF);
            else
                Write(address, (byte)(temp & 0x00FF));

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
                Pc += address;

                // Add one cycle if same page, two for page change
                cycles += address.GetPage() == Pc.GetPage()
                    ? 1
                    : 2;
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
            if (GetFlag(Flags.C) == 1)
            {
                cycles++;
                address += Pc;

                if (address.GetPage() != Pc.GetPage())
                    cycles++;

                Pc = address;
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
            if (GetFlag(Flags.Z) == 1)
            {
                cycles++;
                address += Pc;

                if (address.GetPage() != Pc.GetPage())
                    cycles++;

                Pc = address;
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
                Flags.V, (data & 1 << 6) > 0
                );
            return false;
        }

        /** BMI - Branch if Minus
         * 
         * If the negative flag is set then add the relative displacement to the 
         * program counter to cause a branch to a new location.
         */
        private bool BMI()
        {
            if (GetFlag(Flags.N) == 1)
            {
                cycles++;
                address += Pc;

                if (address.GetPage() != Pc.GetPage())
                    cycles++;

                Pc = address;
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
            if (GetFlag(Flags.Z) == 1)
            {
                cycles++;
                address += Pc;

                if (address.GetPage() != Pc.GetPage())
                    cycles++;

                Pc = address;
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
                cycles++;
                address += Pc;

                if (address.GetPage() != Pc.GetPage())
                    cycles++;

                Pc = address;
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
            Pc++;
            SetFlags(Flags.I, true);
            byte highByte  = (byte)((Pc & 0xFF00) >> 8);
            byte lowByte = (byte)(Pc & 0x00FF);
            Write(((ushort)(stkBase + Stkp--)), highByte);
            Write(((ushort)(stkBase + Stkp--)), lowByte);
            Write(((ushort)(stkBase + Stkp--)), Status);

            Pc = (ushort)(Read(0xFFFE) | Read(0xFFF) << 8);

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
                cycles++;
                address += Pc;

                if (address.GetPage() != Pc.GetPage())
                    cycles++;

                Pc = address;
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
            if (GetFlag(Flags.V) == 1)
            {
                cycles++;
                address += Pc;

                if (address.GetPage() != Pc.GetPage())
                    cycles++;

                Pc = address;
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
         * CID - Clear Interrupt Flag
         */
        private bool CID()
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
                Flags.N, data.IsNegative()
                );

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
                Flags.N, data.IsNegative()
                );

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
                Flags.N, data.IsNegative()
                );

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
            Write(address, (byte)(data - 1));
            SetFlags(
                Flags.Z, Read(address).IsZero(),
                Flags.N, Read(address).IsNegative()
                );
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
                Flags.N, X.IsNegative()
                );
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
                Flags.N, Y.IsNegative()
                );
            return false;
        }


        private bool EOR()
        {
            Fetch();
            A = (byte)(A ^ data);
            SetFlags(
                Flags.Z, A.IsZero(),
                Flags.N, A.IsNegative()
                );
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
            Write(address, (byte)(data + 1));
            SetFlags(
                Flags.Z, Read(address).IsZero(),
                Flags.N, Read(address).IsNegative()
                );
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
                Flags.N, X.IsNegative()
                );
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
                Flags.N, Y.IsNegative()
                );
            return false;
        }

        /** JMP - Jump
         * 
         * Sets the program counter to the address specified by the operand.
         */
        private bool JMP()
        {
            Fetch();
            Pc = data;
            return false;
        }

        //Add with Carry
        private byte JSR()
        {
            return 0;
        }

        /** Load the Accumuator
         */
        private bool LDA()
        {
            Fetch();
            A = data;
            SetFlags(
                Flags.Z, A.IsZero(),
                Flags.N, A.IsNegative()
                );
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
                Flags.N, X.IsNegative()
                );
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
                Flags.N, Y.IsNegative()
                );
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
                Flags.N, temp.IsNegative()
                );

            if (CurrentModeImplicit())
                A = temp;
            else
                Write(address, temp);

            return false;
        }

        /** No Op
         */
        private bool NOP()
        {
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
                Flags.N, A.IsNegative()
                );
            return true;
        }

        /** PHA - Push Accumulator
         * 
         * Pushes a copy of the accumulator on to the stack
         */
        private bool PHA()
        {
            Write((ushort)(stkBase + Stkp--), A);
            return false;
        }

        /** PHP - Push Status
         * 
         * Pushes a copy of the staus register on to the stack
         * todo: Read that break flag is set to 1 before push - investigate
         */
        private bool PHP()
        {
            Write((ushort)(stkBase + Stkp--), Status);
            return false;
        }


        /** PLA - Pop Accumulator
         * 
         * Pop the accumulator off of the stack
         */
        private bool PLA()
        {
            A = Read(stkBase + ++Stkp);
            SetFlags(
                Flags.Z, A.IsZero(),
                Flags.N, A.IsNegative()
                );
            return false;
        }

        /** PLP - Pop Status
         * 
         * Pop the staus register off of the stack
         * todo: Read that U flag is set to 1 after pull - investigate
         */
        private bool PLP()
        {
            Status = Read(stkBase + ++Stkp);
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
                Flags.N, (temp & 0x00FF) == 0,
                Flags.Z, temp.IsNegative()
                );
            if (CurrentModeImplicit())
                A = (byte)(temp & 0x00FF);
            else
                Write(address, (byte)(temp & 0x00FF));
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
                Flags.N, (temp & 0x00FF) == 0,
                Flags.Z, temp.IsNegative()
                );
            if (CurrentModeImplicit())
                A = (byte)(temp & 0x00FF);
            else
                Write(address, (byte)(temp & 0x00FF));
            return false;
        }

        /** RTI - Return from Interrupt
         * 
         * The RTI instruction is used at the end of an interrupt processing routine. 
         * It pulls the processor flags from the stack followed by the program counter.
         * todo: set U and B to not U and B?
         */
        private bool RFI()
        {
            Status = Read(stkBase + ++Stkp);
            Pc = Read(stkBase + ++Stkp);
            Pc |= (ushort)(Read(stkBase + ++Stkp) << 8);
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
            Pc = Read(stkBase + ++Stkp);
            Pc |= (ushort)(Read(stkBase + ++Stkp) << 8);
            Pc++;
            return false;
        }

        /** SBC - Subtract with Carry
         * 
         */
        private bool SBC()
        {
            return false;
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
            Write(address, A);
            return false;
        }

        /** STX - Store X
         * 
         */
        private bool STX()
        {
            Write(address, X);
            return false;
        }

        /** STY - Store Y
         * 
         */
        private bool STY()
        {
            Write(address, Y);
            return false;
        }

        /** TAX - Transfer A to X
         * 
         */
        private bool TAX()
        {
            X = A;
            SetFlags(
                Flags.N, X == 0,
                Flags.Z, X.IsNegative()
                );
            return false;
        }

        /** TAY - Transfer A to Y
         * 
         */
        private bool TAY()
        {
            Y = A;
            SetFlags(
                Flags.N, Y == 0,
                Flags.Z, Y.IsNegative()
                );
            return false;
        }

        /** TSX - Transfer Stackp to X
         * 
         */
        private bool TSX()
        {
            X = Stkp;
            SetFlags(
                Flags.N, X == 0,
                Flags.Z, X.IsNegative()
                );
            return false;
        }

        /** TXA - Transfer X to A
         * 
         */
        private bool TXA()
        {
            A = X;
            SetFlags(
                Flags.N, A == 0,
                Flags.Z, A.IsNegative()
                );
            return false;
        }

        /** TXS - Transfer X to Stackp
         * 
         */
        private bool TXS()
        {
            Stkp = X;
            return false;
        }

        /** TYA - Transfer Y to A
         * 
         */
        private bool TYA()
        {
            A = Y;
            SetFlags(
                Flags.N, A == 0,
                Flags.Z, A.IsNegative()
                );
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
                Name = nameof(this.Operation);
            }

            public string Name { get; }

            public Func<bool> Operation { get; }

            public Func<bool> AddrMode { get; }

            public int Cycles { get; }
        }


        // Classes

        private class InstructionSet : IEnumerable<Instruction>
        {
            private List<Instruction> ins = new List<Instruction>();

            public IEnumerator<Instruction> GetEnumerator()
                => ins.GetEnumerator();
            

            IEnumerator IEnumerable.GetEnumerator()
                =>ins.GetEnumerator();
            

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
