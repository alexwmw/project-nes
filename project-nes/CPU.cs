using System;
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

        private byte Immediate()
        {
            throw new NotImplementedException();
        }

        private byte ZeroPage()
        {
            throw new NotImplementedException();
        }

        private byte Absolute()
        {
            throw new NotImplementedException();
        }

        private byte Implied()
        {
            throw new NotImplementedException();
        }

        private byte Accumulator()
        {
            throw new NotImplementedException();
        }

        private byte Indexed()
        {
            throw new NotImplementedException();
        }

        private byte ZeroPageIndexed()
        {
            throw new NotImplementedException();
        }

        private byte Indirect()
        {
            throw new NotImplementedException();
        }

        private byte PreIndexedIndirect()
        {
            throw new NotImplementedException();
        }

        private byte PostIndexedIndirect()
        {
            throw new NotImplementedException();
        }

        private byte Relative()
        {
            throw new NotImplementedException();
        }

        string t = "Test";
    }
}
