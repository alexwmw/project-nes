using System;
using HelperMethods;

namespace project_nes
{
    public class CpuBus
    {
        /** 
         * See https://wiki.nesdev.com/w/index.php/CPU_memory_map
         */

        // $0000-$07FF
        private byte[] cpuRam = new byte[0x0800];

        //$2000-$2007
        private PPU ppu;

        //$4000-$4017
        private byte[] APU_IO = new byte[0x0018]; //todo: not yet used

        //$4018-$401F
        private byte[] APU_IO_TEST_MODE = new byte[0x0008]; //todo: not yet used

        //$4020-$FFFF
        private Cartridge cartridge;


        public CpuBus()
        {

        }

        public byte Read(ushort adr)
        {
            if (adr >=0 & adr <= 0x1FFF)
            {
                return cpuRam[adr & 0x7FF];
            }
            if (adr >= 0x2000 & adr <= 0x3FFF)
            {
                return ppu.CpuRead((ushort)(adr & 0x007));
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
            if (adr < 0xFFFF)
            {
                throw new ArgumentOutOfRangeException($"Something weird happened: 0x{adr.x()} or 0d{adr}");
            }
            throw new ArgumentOutOfRangeException($"Invalid address greater than 0xFFFF: 0x{adr.x()} or 0d{adr}");
        }
               

        public void Write(ushort adr, byte data)
        {
            if(adr >= 0x000 & adr <= 0x1FFF)
            {
                cpuRam[adr & 0x7FF] = data;
            }
            else if (adr >= 0x2000 & adr <= 0x3FFF)
            {
                ppu.CpuWrite((ushort)(adr & 0x007), data);
            }
            else if (adr >= 0x4000 & adr <= 0x4017)
            {
                APU_IO[adr - 0x4000] = data;
            }
            else if (adr > 0x4017 & adr <= 0x401F)
            {
                APU_IO_TEST_MODE[adr - 0x4018] = data;
            }
            else if (adr > 0x401F & adr <= 0xFFFF)
            {
                cartridge.CpuWrite(adr, data);
            }
            else
            {
                throw new ArgumentOutOfRangeException($"Invalid address greater than 0xFFFF: {adr.x()}");
            }
        }

        public void InsertCartridge(Cartridge cart)
        {
            cartridge = cart;
        }

        public void ConnectPPU(PPU p)
        {
            ppu = p; 
        }
    }
}
