using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace project_nes
{
    public class CPU : iCPU
    {

        // Fields

        private byte data;                  // Fetched data
        private ushort address;             // Fetched Address
        private byte additionalCycles;      // Additional cycles required for current instruction
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
            throw new NotImplementedException();
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

        private void SetFlag(Flags f, bool b)
        {
            if (b)
                Status |= (byte)f;
            else
                Status &= (byte)~f;
        }

        //todo: change from 0
        private void Fetch()
        {
            Action currentMode = instructionSet[0].AddrMode;
            if (currentMode != Imp & currentMode != Acc)
                data = Read(address);
        }



        // Addressing modes

        //Absolute          a       Cycles:
        // The operand is a memory address
        // Addresses are little-endian
        private void Abs()
        {
            byte lowByte = Read(Pc++);
            byte highByte = Read(Pc++);
            address = (ushort)((highByte << 8) | lowByte);
        }

        //Absolute Indexed  a,x     Cycles: 4+
        private void AbsX()
        {
            byte lowByte = Read(Pc++);
            byte highByte = Read(Pc++);
            address = (ushort)
                (
                ((highByte << 8) | lowByte) + X
                );

            if ((address & 0xFF00) != (highByte << 8))
                additionalCycles++;
        }

        //Absolute Indexed  a,y     Cycles: 4+
        private void AbsY()
        {
            byte lowByte = Read(Pc++);
            byte highByte = Read(Pc++);
            address = (ushort)
                (
                ((highByte << 8) | lowByte) + Y
                );

            if ((address & 0xFF00) != (highByte << 8))
                additionalCycles++;
        }

        //Accumulator       A       Cycles:
        private void Acc()
        {
            data = A;
        }

        //Immediate         #v      Cycles:
        // Data expected in the next (immediate) byte.
        private void Imm()
        {
            address = Read(Pc++);
        }


        //Implicit                  Cycles:
        // No additional data required for the instruction
        private void Imp()
        {
            //do nothing
        }


        //Indirect          (a)     Cycles:
        private void Ind()
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
            
        }

        //Indexed Indirect  (d,x)   Cycles: 6
        private void IndX()
        {
            byte temp = Read(Pc++);
            byte lowByte  = Read(temp + X & 0x00FF);
            byte highByte = Read(temp + X + 1 & 0x00FF);

            address = (ushort)((highByte << 8) | lowByte);
        }

        //Indirect Indexed  (d),y   Cycles: 5+
        private void IndY()
        {
            byte temp = Read(Pc++);
            byte lowByte = Read(temp & 0x00FF);
            byte highByte = Read(temp + 1 & 0x00FF);

            address = (ushort)(((highByte << 8) | lowByte) + Y);

            if ((address & 0xFF00) != (highByte << 8))
                additionalCycles++;
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
        private void Rel()
        {
            address = Read(Pc++);
            if (address >= 0x80)
                address |= 0x00FF;
        }

        //Zero Page         d       Cycles:
        private void Zpg()
        {
            address = Read(Pc++);
            address &= 0x00FF;
        }

        //Zero Page Indexed d,x     Cycles: 4
        private void ZpX()
        {
            address = (ushort)(Read(Pc++) + X);
            address &= 0x00FF;
        }

        //Zero Page Indexed d,y     Cycles: 4
        private void ZpY()
        {
            address = (ushort)(Read(Pc++) + Y);
            address &= 0x00FF;
        }




        // Opcodes

        //Add with Carry
        private byte ADC()
        {
            return 0;
        }

        //Add with Carry
        private byte AND()
        {
            return 0;
        }

        //Add with Carry
        private byte ASL()
        {
            return 0;
        }

        //Add with Carry
        private byte BCC()
        {
            return 0;
        }

        //Add with Carry
        private byte BCS()
        {
            return 0;
        }

        //Add with Carry
        private byte BEQ()
        {
            return 0;
        }

        //Add with Carry
        private byte BIT()
        {
            return 0;
        }

        //Add with Carry
        private byte BMI()
        {
            return 0;
        }

        //Add with Carry
        private byte BNE()
        {
            return 0;
        }

        //Add with Carry
        private byte BPL()
        {
            return 0;
        }

        //Add with Carry
        private byte BRK()
        {
            return 0;
        }

        //Add with Carry
        private byte BVC()
        {
            return 0;
        }

        //Add with Carry
        private byte BVS()
        {
            return 0;
        }

        //Add with Carry
        private byte CLC()
        {
            return 0;
        }

        //Add with Carry
        private byte CLD()
        {
            return 0;
        }

        //Add with Carry
        private byte CID()
        {
            return 0;
        }

        //Add with Carry
        private byte CLV()
        {
            return 0;
        }

        //Add with Carry
        private byte CMP()
        {
            return 0;
        }

        //Add with Carry
        private byte CPX()
        {
            return 0;
        }

        //Add with Carry
        private byte CPY()
        {
            return 0;
        }

        //Add with Carry
        private byte DEC()
        {
            return 0;
        }

        //Add with Carry
        private byte DEX()
        {
            return 0;
        }

        //Add with Carry
        private byte DEY()
        {
            return 0;
        }

        //Add with Carry
        private byte EOR()
        {
            return 0;
        }

        //Add with Carry
        private byte INC()
        {
            return 0;
        }

        //Add with Carry
        private byte INX()
        {
            return 0;
        }

        //Add with Carry
        private byte INY()
        {
            return 0;
        }

        //Add with Carry
        private byte JMP()
        {
            return 0;
        }

        //Add with Carry
        private byte JSR()
        {
            return 0;
        }

        //Add with Carry
        private byte LDA()
        {
            return 0;
        }

        //Add with Carry
        private byte LDX()
        {
            return 0;
        }

        //Add with Carry
        private byte LDY()
        {
            return 0;
        }

        //Add with Carry
        private byte LSR()
        {
            return 0;
        }

        //Add with Carry
        private byte NOP()
        {
            return 0;
        }

        //Add with Carry
        private byte ORA()
        {
            return 0;
        }

        //Add with Carry
        private byte PHA()
        {
            return 0;
        }

        //Add with Carry
        private byte PHP()
        {
            return 0;
        }

        //Add with Carry
        private byte PLA()
        {
            return 0;
        }

        //Add with Carry
        private byte PLP()
        {
            return 0;
        }

        //Add with Carry
        private byte ROL()
        {
            return 0;
        }

        //Add with Carry
        private byte ROR()
        {
            return 0;
        }

        //Add with Carry
        private byte RFI()
        {
            return 0;
        }

        //Add with Carry
        private byte RTS()
        {
            return 0;
        }

        //Add with Carry
        private byte SBC()
        {
            return 0;
        }

        //Add with Carry
        private byte SEC()
        {
            return 0;
        }

        //Add with Carry
        private byte SED()
        {
            return 0;
        }

        //Add with Carry
        private byte SEI()
        {
            return 0;
        }

        //Add with Carry
        private byte STA()
        {
            return 0;
        }

        //Add with Carry
        private byte STX()
        {
            return 0;
        }

        //Add with Carry
        private byte STY()
        {
            return 0;
        }

        //Add with Carry
        private byte TAX()
        {
            return 0;
        }

        //Add with Carry
        private byte TAY()
        {
            return 0;
        }

        //Add with Carry
        private byte TSX()
        {
            return 0;
        }

        //Add with Carry
        private byte TXA()
        {
            return 0;
        }

        //Add with Carry
        private byte TXS()
        {
            return 0;
        }

        //Add with Carry
        private byte TYA()
        {
            return 0;
        }

        //Unofficial/unknown opcode
        private byte UNK() => NOP();



        // Structs

        private struct Instruction
        {
            public Instruction(Func<byte> op, Action addrm, int cycles)
            {
                Operation = op;
                AddrMode = addrm;
                Cycles = cycles;
                Name = nameof(this.Operation);
            }

            public string Name { get; }

            public Func<byte> Operation { get; }

            public Action AddrMode { get; }

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
            

            public void Add(Func<byte> op, Action addrm, int cycles)
                => ins.Add(new Instruction(op, addrm, cycles));

            public Instruction this[int i]    // Indexer declaration  
            {
                get { return this.ins[i]; }
                set { this.ins[i] = value; }
            }
        }
    }
}
