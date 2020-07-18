using System;
using HelperMethods;

namespace project_nes
{
    public class Bus : iBus
    {

        // See https://wiki.nesdev.com/w/index.php/CPU_memory_map

        private byte[] cpuRam = new byte[0x0800];

        private byte[] PPU = new byte[0x0008];

        private byte[] APU_IO = new byte[0x0018];

        private byte[] APU_IO_TEST_MODE = new byte[0x0008];

        private byte[] cartridge = new byte[0xBFE0];


        public Bus()
        {

        }


        // A Read-Write signal is not needed as it is implied by the method being called

        public byte Read(ushort address)
        {
            if (address < 0)
                throw new ArgumentOutOfRangeException($"Invalid address less than 0: {address.Hex()}");

            if (address <= 0x1FFF)

                return cpuRam[address & 0x7FF];

            if (address <= 0x3FFF)

                return PPU[(address - 0x2000) & 0x007];

            if (address <= 0x4017)

                return APU_IO[address - 0x4000];

            if (address <= 0x401F)

                return APU_IO_TEST_MODE[address - 0x4000];

            if (address <= 0xFFFF)
                return cartridge[address - 0x4020];

            else
                throw new ArgumentOutOfRangeException($"Invalid address greater than 0xFFFF: {address.Hex()}");
        }
               

        public void Write(ushort address, byte data)
        {
            if (address < 0)
                throw new ArgumentOutOfRangeException($"Invalid address less than 0: {address.Hex()}");

            if (address <= 0x1FFF)

                cpuRam[address & 0x7FF] = data;

            if (address <= 0x3FFF)

                PPU[(address - 0x2000) & 0x007] = data;

            if (address <= 0x4017)

                APU_IO[address - 0x4000] = data;

            if (address <= 0x401F)

                APU_IO_TEST_MODE[address - 0x4000] = data;

            if (address <= 0xFFFF)
                cartridge[address - 0x4020] = data;

            else
                throw new ArgumentOutOfRangeException($"Invalid address greater than 0xFFFF: {address.Hex()}");
        }
    }
}
