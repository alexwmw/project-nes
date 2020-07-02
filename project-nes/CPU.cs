using System;
using System.Reflection;

namespace project_nes
{
    public class CPU : iCPU
    {

        // Fields

        private byte fetched;               // Fetched value
        private int clock_count;            // Absolute number of clock cycles
        private iBus bus;
        private Instruction[] instructionSet;

        // Constructors

        public CPU()
        {

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
        private static Func<byte> ABS = () =>
        {
            return 0;
        };

        //Absolute Indexed  a,x     Cycles: 4+
        private static Func<byte> ABX = () =>
        {
            return 0;
        };

        //Absolute Indexed  a,y     Cycles: 4+
        private static Func<byte> ABY = () =>
        {
            return 0;
        };

        //Accumulator       A       Cycles:
        private static Func<byte> ACC = () =>
        {
            return 0;
        };

        //Immediate         #v      Cycles:
        private static Func<byte> IMM = () =>
        {
            return 0;
        };


        //Implicit                  Cycles:
        private static Func<byte> IMP = () =>
        {
            return 0;
        };

        //Indirect          (a)     Cycles:
        private static Func<byte> IND = () =>
        {
            return 0;
        };

        //Indexed Indirect  (d,x)   Cycles: 6
        private static Func<byte> IIX = () =>
        {
            return 0;
        };

        //Indirect Indexed  (d),y   Cycles: 5+
        private static Func<byte> IIY = () =>
        {
            return 0;
        };

        //Relative         label    Cycles:
        private static Func<byte> REL = () =>
        {
            return 0;
        };

        //Zero Page         d       Cycles:
        private static Func<byte> ZPG = () =>
        {
            return 0;
        };

        //Zero Page Indexed d,x     Cycles: 4
        private static Func<byte> ZPX = () =>
        {
            return 0;
        };

        //Zero Page Indexed d,y     Cycles: 4   
        private static Func<byte> ZPY = () =>
        {
            return 0;
        };



        // Opcodes

        //Add with Carry
        private static Func<byte> ADC = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> AND = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> ASL = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> BCC = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> BCS = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> BEQ = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> BIT = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> BMI = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> BNE = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> BPL = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> BRK = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> BVC = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> BVS = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> CLC = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> CLD = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> CID = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> CLV = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> CMP = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> CPX = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> CPY = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> DEC = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> DEX = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> DEY = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> EOR = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> INC = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> INX = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> INY = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> JMP = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> JSR = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> LDA = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> LDX = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> LDY = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> LSR = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> NOP = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> ORA = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> PHA = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> PHP = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> PLA = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> PLP = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> ROL = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> ROR = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> RFI = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> RTS = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> SBC = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> SEC = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> SED = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> SEI = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> STA = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> STX = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> STY = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> TAX = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> TAY = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> TSX = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> TXA = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> TXS = () =>
        {
            return 0;
        };

        //Add with Carry
        private static Func<byte> TYA = () =>
        {
            return 0;
        };

        //Unofficial/unknown opcode
        private static Func<byte> UNK = () => NOP();


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
                return (byte)mi.Invoke(null, null);
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


    }
}
