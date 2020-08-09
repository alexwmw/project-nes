using System;
using System.Collections.Generic;
using HelperMethods;

namespace project_nes
{
    public class PpuBus
    {
        /**
         * Address range	Size	            Description
         * 
         * $0000-$0FFF	    $1000	4 kb        Pattern table 0 <<-- cartridge
         * $1000-$1FFF	    $1000	4 kb        Pattern table 1 <<-- cartridge
         * $2000-$23FF	    $0400	1 kb        Nametable 0
         * $2400-$27FF	    $0400	1 kb        Nametable 1
         * $2800-$2BFF	    $0400	1 kb        Nametable 2
         * $2C00-$2FFF	    $0400	1 kb        Nametable 3
         * $3000-$3EFF	    $0F00	3.75 kb     Mirrors of $2000-$2EFF
         * $3F00-$3F1F	    $0020	32 bytes    Palette RAM indexes
         * $3F20-$3FFF	    $00E0	224 bytes   Mirrors of $3F00-$3F1F (x7)
         * 
         */

        /** Palette memory
         *   Stores a BG colour at 0 (0x3F00) and then 8 four-colour palettes.
         *   In each palette, location 0/4/8/c is a mirror of location 0 (i.e. 
         *   the background).
         *   
         *   The byte at each address is actually an index (range 64) into an 
         *   array of RGB colour values.
         */
        private byte[] paletteRam;

        /** Name Tables or VRAM
         *   Hold the layout of background information in two adjacent banks.
         *   The two banks facilitate scrolling
         */
        private byte[,] nameTables;

        /** Pattern Memory or Character (CHR) Memory
         *   This is thr CHR rom on the cartridge
         */
        private Cartridge cartridge;

        public PpuBus()
        {
            nameTables = new byte[2,1024];
            paletteRam = new byte[32];
        }

        public void ConnectCartridge(Cartridge cart)
        {
            cartridge = cart;
        }

        public void Write(ushort addr, byte data)
        {
            if (addr >= 0x0000 & addr <= 0x1FFF) // Pattern tables 0 (< 1000) and 1 (< 2000)
            {
                cartridge.PpuWrite(addr, data);
            }
            else if (addr >= 0x2000 & addr <= 0x3EFF) // Name tables & mirrors
            {
                byte i = 0, j = 0;
                if (cartridge.Mirroring == 'V')
                {
                    addr -= 0x2000;
                    // i = MS bit
                    // j = address % 1 kb
                    i = (byte)((addr & 0xF000) >> 12);
                    j = (byte)(addr % 0x800);
                }
                else if (cartridge.Mirroring == 'H')
                {
                    addr &= 0x0FFF;
                    // i = addr < 1 kb ?
                    // j = addr - (0 kb or 1 kb)
                    i = (byte)(addr < 0x0800 ? 0 : 1);
                    j = (byte)(addr - (i * 0x800));
                }
                else
                    throw new SystemException("Cartridge.Mirroring was not V or H");
                nameTables[i, j] = data;
            }
            else if (addr >= 0x3F00 & addr <= 0x3FFF) // Palette RAM indexes & mirrors
            {
                paletteRam[addr & 0x001F] = data;
                // Hard code in mirroring? https://youtu.be/-THeUXqR3zY?t=860
            }
            else
                throw new ArgumentOutOfRangeException("Address exceeds 0x3FFF");
        }

        public byte Read(ushort addr)
        {
            if (addr >= 0x0000 & addr <= 0x1FFF) // Pattern tables 0 (< 1000) and 1 (< 2000)
            {
                return cartridge.PpuRead(addr);
            }
            if (addr >= 0x2000 & addr <= 0x3EFF) // Name tables & mirrors
            {
                byte i = 0, j = 0;
                if (cartridge.Mirroring == 'V')
                {
                    addr -= 0x2000;
                    // i = MS bit
                    // j = address % 1 kb
                    i = (byte)((addr & 0xF000) >> 12);
                    j = (byte)(addr % 0x800);
                }
                else if (cartridge.Mirroring == 'H')
                {
                    addr &= 0x0FFF;
                    // i = addr < 1 kb ?
                    // j = addr - (0 kb or 1 kb)
                    i = (byte)(addr < 0x0800 ? 0 : 1);
                    j = (byte)(addr - (i * 0x800));
                }
                else
                    throw new SystemException("Cartridge.Mirroring was not V or H");
                return nameTables[i, j];
            }
            if (addr >= 0x3F00 & addr <= 0x3FFF) // Palette RAM indexes & mirrors
            {
                return paletteRam[(addr & 0x001F)];
                // Hard code in mirroring? https://youtu.be/-THeUXqR3zY?t=860
            }
            throw new ArgumentOutOfRangeException($"Address {addr.x()} exceeds 0x3FFF");
        }
    }
}
