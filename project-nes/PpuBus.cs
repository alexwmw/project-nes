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
         *   In each palette, location 0/4/8/c is a mirror of location 0 (the background).
         *   
         *   The byte at each address is actually an index (range 64) into an 
         *   array of RGB colour values.
         */
        private byte[] paletteRam;

        /** Name Tables or VRAM
         *   Hold the layout of background information in two adjacent banks (NT0, NT1).
         *   The two banks facilitate scrolling, and are mirrored in NTs 2 and 3.
         */
        public byte[,] nameTables; 

        /** Pattern Memory or Character (CHR) Memory
         *   This is the CHR rom on the cartridge
         */
        private Cartridge chrRom;

        public PpuBus()
        {
            nameTables = new byte[2, 0x400];
            paletteRam = new byte[32];
        }

        public void ConnectCartridge(Cartridge cart)
        {
            chrRom = cart;
        }

        public void Write(ushort addr, byte data)
        {
            if (addr >= 0x0000 & addr <= 0x1FFF) // Pattern tables 0 (< 1000) and 1 (< 2000)
            {
                chrRom.PpuWrite(addr, data); // chrROM is sometimes a RAM
            }
            else if (addr >= 0x2000 & addr <= 0x3EFF) // Name tables & mirrors
            {
                byte i = 0, j = 0;
                if (chrRom.Mirroring == 'V')
                {
                    addr -= 0x2000;
                    i = (byte)((addr & 0xF000) >> 12);  // MS bit
                    j = (byte)(addr % 0x800);           // address % 1 kb
                }
                else if (chrRom.Mirroring == 'H')
                {
                    addr &= 0x0FFF;
                    i = (byte)(addr < 0x0800 ? 0 : 1);  // addr < 1 kb ?
                    j = (byte)(addr - (i * 0x800));     // addr - (0 kb or 1 kb)
                }
                else
                    throw new SystemException("Cartridge.Mirroring was not V or H");

                nameTables[i, j] = data;
            }
            else if (addr >= 0x3F00 & addr <= 0x3FFF) // Palette RAM indexes & mirrors
            {
                addr &= 0x001F;
                if (addr % 4 == 0 & addr >= 0x10)
                    addr -= 0x10;

                paletteRam[addr] = data;
            }
            else throw new ArgumentOutOfRangeException("Address exceeds 0x3FFF");
        }

        public byte Read(ushort addr)
        {
            if (addr >= 0x0000 & addr <= 0x1FFF) // Pattern tables 0 (< 1000) and 1 (< 2000)
            {
                return chrRom.PpuRead(addr);
            }
            if (addr >= 0x2000 & addr <= 0x3EFF) // Name tables & mirrors
            {
                byte i = 0, j = 0;
                if (chrRom.Mirroring == 'V')
                {
                    addr -= 0x2000;
                    i = (byte)((addr & 0xF000) >> 12);
                    j = (byte)(addr % 0x800);
                }
                else if (chrRom.Mirroring == 'H')
                {
                    addr &= 0x0FFF;
                    i = (byte)(addr < 0x0800 ? 0 : 1);
                    j = (byte)(addr - (i * 0x800));
                }
                else
                    throw new SystemException("Cartridge.Mirroring was not V or H");

                return nameTables[i, j];
            }
            if (addr >= 0x3F00 & addr <= 0x3FFF) // Palette RAM indexes & mirrors
            {
                addr &= 0x001F;
                if (addr % 4 == 0 & addr >= 0x10)
                    addr -= 0x10;
                return paletteRam[addr];
            }
            throw new ArgumentOutOfRangeException($"Address {addr.x()} exceeds 0x3FFF");
        }
    }
}
