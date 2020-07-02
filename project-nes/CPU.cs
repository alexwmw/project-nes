using System;
using i = project_nes.Instruction;

namespace project_nes
{
    public class CPU : iCPU
    {
        public CPU()
        {
        }

        public byte A { get; set; }        // Accumulator
        public byte X { get; set; }        // X Register
        public byte Y { get; set; }        // Y register
        public byte Stkp { get; set; }     // Stack pointer
        public ushort Pc { get; set; }     // Program counter
        public byte Status { get; set; }   // Status byte

        private byte fetched;               // Fetched value

        private int clock_count;            // Absolute number of clock cycles

        private iBus bus;

        private enum Flags
        {
            C = 1 << 0, // Carry Bit
            Z = 1 << 1, // Zero
            I = 1 << 2, // Disable Interrupts
            D = 1 << 3, // Decimal Mode (unused in this implementation)
            B = 1 << 4, // Break
            U = 1 << 5, // Unused
            V = 1 << 6, // Overflow
            N = 1 << 7, // Negative
        };

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

        // Addressing modes

        private static Func<byte> Immediate = () =>
        {
            return 0;
        }; 

        private static Func<byte> ZeroPage = () =>
        {
            return 0;
        };

        private static Func<byte> Absolute = () =>
        {
            return 0;
        };

        private static Func<byte> Implied = () =>
        {
            return 0;
        };

        private static Func<byte> Accumulator = () =>
        {
            return 0;
        };

        private static Func<byte> Indexed = () =>
        {
            return 0;
        };

        private static Func<byte> ZeroPageIndexed = () =>
        {
            return 0;
        };

        private static Func<byte> Indirect = () =>
        {
            return 0;
        };

        private static Func<byte> PreIndexedIndirect = () =>
        {
            return 0;
        };

        private static Func<byte> PostIndexedIndirect = () =>
        {
            return 0;
        };

        private static Func<byte> Relative = () =>
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

    }
}
