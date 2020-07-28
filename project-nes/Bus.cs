using System;
using HelperMethods;

namespace project_nes
{
    public class Bus
    {

        /** 
         * See https://wiki.nesdev.com/w/index.php/CPU_memory_map
         */

        /**
         * $0000-$07FF
         */
        private byte[] cpuRam = new byte[0x0800];

        /**
         * $2000-$2007
         */
        private byte[] PPU = new byte[0x0008];

        /**
         * $4000-$4017
         */
        private byte[] APU_IO = new byte[0x0018];

        /**
         * $4018-$401F
         */
        private byte[] APU_IO_TEST_MODE = new byte[0x0008];

        /**
         * $4020-$FFFF
         */
        private Cartridge cartridge;


        public Bus()
        {

        }


        // A Read-Write signal is not needed as it is implied by the method being called

        public byte Read(ushort adr)
        {
            if (adr < 0)
            {
                return cpuRam[adr & 0x7FF];
            }

            if (adr >=0 & adr <= 0x1FFF)
            { 
                return cpuRam[adr & 0x7FF];
            }
            if (adr >= 0x2000 & adr <= 0x3FFF)
            {
                return PPU[(adr - 0x2000) & 0x007];
            }

            if (adr >= 0x4000 & adr <= 0x4017)
            {
                return APU_IO[adr - 0x4000];
            }

            if (adr >= 0x4018 & adr <= 0x401F)
            {
                return APU_IO_TEST_MODE[adr - 0x4018];
            }

            if (adr >= 0x4020 & adr <= 0xFFFF)
            {
                return cartridge.CpuRead(adr);
            }


            throw new ArgumentOutOfRangeException($"Invalid address greater than 0xFFFF: {adr.x()}");
        }
               

        public void Write(ushort adr, byte data)
        {

            if(adr >= 0x000 & adr <= 0x1FFF)
                cpuRam[adr & 0x7FF] = data;

            if (adr > 0x1FFF & adr <= 0x3FFF)
                PPU[(adr - 0x2000) & 0x007] = data;

            if (adr > 0x3FFF & adr <= 0x4017)
                APU_IO[adr - 0x4000] = data;

            if (adr > 0x4017 & adr <= 0x401F)
                APU_IO_TEST_MODE[adr - 0x4000] = data;

            if (adr > 0x401F & adr <= 0xFFFF)
                cartridge.CpuWrite(adr, data);

            else if (adr > 0xFFFF)
                throw new ArgumentOutOfRangeException(
                    $"Invalid address greater than 0xFFFF: {adr.x()}");
        }

        public void InsertCartridge(Cartridge cart)
        {
            this.cartridge = cart;
        }
    }
}
