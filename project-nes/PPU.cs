//#define generate_a_nice_pattern

using System;
using System.Collections;
using System.Collections.Generic;
using SFML.Graphics;

namespace project_nes
{
    public class PPU
    {
        /**Overview of PPU timing: https://wiki.nesdev.com/w/index.php/File:Ntsc_timing.png
         * 
         * For 'visible frame' scanlines (<= 239)
         *   CYCLE    ACTION
         *   1-257    Pixels are drawn
         *   258-319  Idle
         *   321-336  First two tiles on next scanline
         *   337-340  Unused fetches
         */

        private const int maxCycles = 341;
        private const int maxSLs = 261;
        private const int firstBlankSL = 241;
        private const int firstBlankCycle = 257;

        /** PPU registers
         * These are the backing fields of register properties.
         * When each register is r/w to, various actions are triggered.
         * Property getters and setters are a convenient medium for this
         */
        private byte control;       // VPHB SINN | NMI enable (V), PPU master/slave (P), sprite height (H), background tile select (B), sprite tile select (S), increment mode (I), nametable select (NN)
        private byte mask;          // BGRs bMmG | color emphasis (BGR), sprite enable (s), background enable (b), sprite left column enable (M), background left column enable (m), greyscale (G)
        private byte status;        // VSO- ---- | vblank (V), sprite 0 hit (S), sprite overflow (O); read resets write pair for $2005/$2006
        private byte oamAddress;    // OAM r/w address
        private byte oamData;       // OAM r/w data
        private byte scroll;        // fine scroll position (two writes: X scroll, Y scroll)
        private byte ppuData;       // PPU data read/write
        private byte oamDma;        // OAM DMA high address

        // Emulation variables
        private byte latch;   
        private ushort ppuAddress;
        private int cycle;
        private int scanLine;

        // Internal references
        private Palette palette;
        private IODevice IODevice;
        private PpuBus ppuBus;
        private int frameCount;

        public PPU()
        {
            latch = 0;
            frameCount = 0;

            // Two adjacent 16 * 16 2D arrays of 8 * 8 tiles. Used only for testing
            PatternMemory = new Color[2, (16 * 8) , (16 * 8)];
        }

        private enum CtrlFlags
        {
            ntable_0 = 1 << 0,
            ntable_1 = 1 << 1,
            increment_mode = 1 << 2,
            sprite_select = 1 << 3,
            backgr_select = 1 << 4,
            sprite_height = 1 << 5,
            slave_mode = 1 << 6,
            nmi_enable = 1 << 7
        }

        private enum StatFlags
        {
            sprite_overflow = 1 << 5,
            sprite_zero_hit = 1 << 6,
            vertical_blank = 1 << 7,
        }

        private enum MaskFlags
        {
            greyscale = 1 << 0,
            backgr_left = 1 << 1,
            sprite_left = 1 << 2,
            backgr_enable = 1 << 3,
            sprite_enable = 1 << 4,
            red_emph = 1 << 5,
            green_emph = 1 << 6,
            blue_emph = 1 << 7,
        }




        // Properties * * * * * * * * *

        public bool Nmi { get; set; }

        public bool FrameComplete { get; set; }

        public Color[, ,] PatternMemory { get; }



        // Registers Getters & Setters * * * * * * * * * *

        private byte GetControl()
            => throw new AccessViolationException("Read of unreadable register"); // Not actually readable

        //todo
        private void SetControl(byte value)
        {
            control = value;
        //tram_addr.nametable_x = control.nametable_x;
        //tram_addr.nametable_y = control.nametable_y;
        }

        private byte Mask()
             => throw new AccessViolationException("Read of unreadable register"); // Not actually readable
        
        private void SetMask(byte value) => mask = value; // No further actions

        private byte GetStatus()
        {
            try
            {
                return status;
            }
            finally
            {
                SetFlag(StatFlags.vertical_blank, false);
                latch = 0;
            }
        }

        //todo
        private byte OamAddress { get; set; }

        //todo
        private byte OamData { get; set; }

        // Used to offset into a tile when scrolling
        private byte GetScroll() 
             => throw new AccessViolationException("Read of unreadable register"); // Not actually readable

        //todo
        private void SetScroll(byte value)
        {
            if (latch == 0)
            {
                //fine_x = value & 0x07;
                //tram_addr.coarse_x = value >> 3;
                //address_latch = 1;
            }
            else
            { 
                //tram_addr.fine_y = value & 0x07;
                //tram_addr.coarse_y = value >> 3;
                //address_latch = 0;
            }
        }
        

        private byte GetPpuAddressByte() 
            => throw new AccessViolationException("Read of unreadable register"); // Not actually readable

        private void SetPpuAddressByte(byte value)
        {
            // When latch is 0 write the high byte
            // When latch is 1 write the low byte
            if (latch == 0)
            {
                ppuAddress = (ushort)((value << 8) | (ppuAddress & 0x00FF));
                latch = 1;
            }
            else
            {

                ppuAddress = (ushort)((ppuAddress & 0xFF00) | value);
                latch = 0;
            }
        }
            

        private byte GetPpuData()
        {
            try
            {
                /* Reads should be delayed by one cycle - 
                * UNLESS reading from palette ROM
                */

                // store ppuData that was set during the last cycle
                byte ppuDataDelayedBy1 = ppuData;

                // update ppuData during this cycle
                ppuData = PpuRead(ppuAddress);

                // If palette location, return current, else return previous
                return ppuAddress >= 0x3F00 ? ppuData : ppuDataDelayedBy1;
            }
            finally
            {
                // If set to vertical mode (1), the increment is 32, so it skips
                // one whole nametable row; in horizontal mode (0) it just increments
                // by 1, moving to the next column
                ppuAddress += (ushort)(GetFlag(CtrlFlags.increment_mode) ? 32 : 1);
            }
        }

        private void SetPpuData(byte value)
        {
            PpuWrite(ppuAddress, value);

            // If set to vertical mode (1), the increment is 32, so it skips
            // one whole nametable row; in horizontal mode (0) it just increments
            // by 1, moving to the next column
            ppuAddress += (ushort)(GetFlag(CtrlFlags.increment_mode) ? 32 : 1);
        }


        // Methods * * * * * * * *
       


        public void Clock()
        {
            #if generate_a_nice_pattern
            // If within visible portion of screen, set pixel of screen
            if (cycle > 0 & scanLine > 0 & cycle < firstBlankCycle & scanLine < firstBlankSL)
            {
                int i = Math.Abs((scanLine * cycle + frameCount) % ((frameCount + 1) % 64));
                IODevice.SetPixel(cycle - 1, scanLine - 1, palette[i]);
            }
            #endif
            bool at_init_point = scanLine == -1 & cycle == 1;
            bool first_blank_SL = scanLine == 241 & cycle == 1;
            bool SL_complete = cycle >= 341;
            bool visible_SL = scanLine >= -1 & scanLine < 240;
            bool in_read_section = cycle >= 2 & cycle < 258 | cycle >= 321 & cycle < 338;
            bool frame_complete = scanLine >= 261;
            bool at_end_of_SL = cycle == 256;


            if (at_init_point)
                SetFlag(StatFlags.vertical_blank, false);

            if (in_read_section & visible_SL)
            {

            }
            if (first_blank_SL)
            {
                SetFlag(StatFlags.vertical_blank, true);
                Nmi = GetFlag(CtrlFlags.nmi_enable);
            }

            cycle++;

            if (SL_complete)
            {
                cycle = 0;
                scanLine++;
                
                // If scanline gets to 261 (maxSL), the frame is complete
                if (frame_complete)
                {
                    scanLine = -1;
                    frameCount++;
                    FrameComplete = true;
                }
            }
        }

        // Ctrl get set
        private void SetFlag(CtrlFlags f, bool b)
            => control = (byte)(b ? control | (byte)f : control & (byte)~f);

        private bool GetFlag(CtrlFlags f)
            => (control & (byte)f) > 0;
        
        // Mask get set
        private void SetFlag(MaskFlags f, bool b)
            => mask = (byte)(b ? mask | (byte)f : mask & (byte)~f);

        private bool GetFlag(MaskFlags f)
            => (mask & (byte)f) > 0;

        // Status get set
        private void SetFlag(StatFlags f, bool b)
            => status = (byte)(b ? status | (byte)f : status & (byte)~f);

        private bool GetFlag(StatFlags f)
            => (status & (byte)f) > 0;



        public void CpuWrite(ushort addr, byte data)
        {
            switch (addr)
            {
                case 0:
                    SetControl(data);
                    break;
                case 1:
                    SetMask(data);
                    break;
                case 2:
                    throw new AccessViolationException("Write to unwritable register");
                case 3:
                    OamAddress = data;
                    break;
                case 4:
                    OamData = data;
                    break;
                case 5:
                    SetScroll(data);
                    break;
                case 6:
                    SetPpuAddressByte(data);
                    break;
                case 7:
                    SetPpuData(data);
                    break;
                default:
                    throw new IndexOutOfRangeException($"Cpu Write to unknown Register {addr}");
            } 
        }

        public byte CpuRead(ushort addr)
            => addr switch
            {
                0 => GetControl(),
                1 => Mask(),
                2 => GetStatus(),
                3 => OamAddress,
                4 => OamData,
                5 => GetScroll(),
                6 => GetPpuAddressByte(),
                7 => GetPpuData(),
                _ => throw new IndexOutOfRangeException($"Cpu Read of unknown Register {addr}"),
            };

            public void ConnectBus(PpuBus bus)
        {
            ppuBus = bus;
        }

        public void ConnectIO(IODevice IO)
        {
            this.IODevice = IO;
            IODevice.Clear();
        }

        public void SetPalette(Palette p) => palette = p;

        public void PpuWrite(ushort addr, byte data)
        {
            ppuBus.Write(addr, data);
        }

        public byte PpuRead(ushort addr)
        {
            return ppuBus.Read(addr);
        }



        //Adapted from olcnes https://github.com/OneLoneCoder/olcNES/tree/master/Part%20%234%20-%20PPU%20Backgrounds
        public void GetPatternTable(int i, byte palette)
        {
            // Nested 16 by 16 for-loops -- for each tile in the 16x16 tile grid in memory
            for (int tileY = 0; tileY < 16; tileY++)
            {
                for (int tileX = 0; tileX < 16; tileX++)
                {
                    //Translate XY coordinates into 1D coordinate with (Y * Width + X)  byte-offset
                    // Y is multiplied by 256 (16*16)   i.e. 16 tiles width, each tile being 16 bytes of information
                    // X is multiplied by 16            i.e. 16 bytes (one tile) 
                    int offset = tileY * 256 + tileX * 16;

                    //For each row in the 8px * 8px tile...
                    for (int row = 0; row < 8; row++)
                    {
                        byte lsb = PpuRead((ushort)(i * 0x1000 + offset + row + 0));
                        byte msb = PpuRead((ushort)(i * 0x1000 + offset + row + 8));
                        

                        //For each pixel in the 8px row...
                        for (int col = 0; col < 8; col++)
                        {
                            byte pixelVal = (byte)((lsb & 0x01) + (msb & 0x01));
                            lsb >>= 1;
                            msb >>= 1;

                            // At the pattern memory (bank) specifed by paramater i:
                            //   At index row,col of tile x,y - set the pixel colour
                            PatternMemory[
                                i, 
                                tileX * 8 + (7 - col),  // was row + (tileY * 8)
                                row + (tileY * 8)]      // was col + (tileX * 8)

                                = GetColour(palette, pixelVal);
                        }
                    }
                }
            }
        }

        public Color GetColour(byte palet, byte pixel)
        {
            byte index = PpuRead((ushort)(0x3f00 + (palet * 4) + pixel));

            // For tesing only - delete this line:
            index = (byte)((pixel + 6 )* 5);

            return palette[index];
        }

        private class LoopyRegister
        {
            private ushort address;

            public byte Coarse_x { get; set; } //5
            public byte Coarse_y { get; set; }  //5
            public byte NTable_x { get; set; }  //1
            public byte NTable_y { get; set; }  //1
            public byte Fine_y { get; set; }    //3

            public ushort Address
            {
                get => address;

                set => Set(value);
            }

            public void Set(ushort word)
            {
                Coarse_x = (byte)(word & 0x001F >> 0);
                Coarse_y = (byte)(word & 0x03E0 >> 5);
                NTable_x = (byte)(word & 0x0400 >> 10);
                NTable_y = (byte)(word & 0x0800 >> 11);
                Fine_y = (byte)(word & 0x7000 >> 12);

                address = word;
            }
        }
    } 
}
