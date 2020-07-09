using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace project_nes
{
    public class CPU : iCPU
    {

        // Fields

        private byte fetched;               // Fetched data
        private byte fetchedAddr;           // Fetched Address
        private int clock_count;            // Absolute number of clock cycles
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

        public byte A { get; set; }        // Accumulator
        public byte X { get; set; }        // X Register
        public byte Y { get; set; }        // Y register
        public byte Stkp { get; set; }     // Stack pointer
        public ushort Pc { get; set; }     // Program counter
        public byte Status { get; set; }   // Status byte


        // (Static) Delegates

        // Addressing modes

        //Absolute          a       Cycles:
        private byte Abs()
        {
            return 0;
        }

        //Absolute Indexed  a,x     Cycles: 4+
        private byte AbsX()
        {
            return 0;
        }

        //Absolute Indexed  a,y     Cycles: 4+
        private byte AbsY()
        {
            return 0;
        }

        //Accumulator       A       Cycles:
        private byte Acc()
        {
            return 0;
        }

        //Immediate         #v      Cycles:
        private byte Imm()
        {
            return 0;
        }


        //Implicit                  Cycles:
        private byte Imp()
        {
            fetched = A;
            return 0;
        }


        //Indirect          (a)     Cycles:
        private byte Ind()
        {
            return 0;
        }

        //Indexed Indirect  (d,x)   Cycles: 6
        private byte IndexIndX()
        {
            return 0;
        }

        //Indirect Indexed  (d),y   Cycles: 5+
        private byte IndIndexY()
        {
            return 0;
        }

        //Relative         label    Cycles:
        private byte Rel()
        {
            return 0;
        }

        //Zero Page         d       Cycles:
        private byte Zpg()
        {
            return 0;
        }

        //Zero Page Indexed d,x     Cycles: 4
        private byte ZpX()
        {
            return 0;
        }

        //Zero Page Indexed d,y     Cycles: 4   
        private byte ZpY()
        {
            return 0;
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


        // Methods

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


        private void Read(ushort address)
            => bus.Read(address);

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


        private struct Instruction
        {
            public Instruction(Func<byte> op, Func<byte> addrm, int cycles)
            {
                Operation = op;
                AddrMode = addrm;
                Cycles = cycles;
                Name = nameof(this.Operation);
            }

            public string Name { get; }

            public Func<byte> Operation { get; }

            public Func<byte> AddrMode { get; }

            public int Cycles { get; }
        }


        private class InstructionSet : IEnumerable<Instruction>
        {
            private List<Instruction> ins = new List<Instruction>();

            public IEnumerator<Instruction> GetEnumerator()
                => ins.GetEnumerator();
            

            IEnumerator IEnumerable.GetEnumerator()
                =>ins.GetEnumerator();
            

            public void Add(Func<byte> op, Func<byte> addrm, int cycles)
                => ins.Add(new Instruction(op, addrm, cycles));

            public Instruction this[int i]    // Indexer declaration  
            {
                get { return this.ins[i]; }
                set { this.ins[i] = value; }
            }
        }
    }
}
