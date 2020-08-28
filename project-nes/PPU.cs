//#define generate_a_nice_pattern
//#define LOGGING

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using HelperMethods;
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
         * edit: got rid of properties; too difficult to debug
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
        private LoopyRegister vReg;
        private LoopyRegister tReg;
        private byte fine_x;


        // Internal references
        private Palette palette;
        private IODevice IODevice;
        private PpuBus ppuBus;

        // Background rendering
        private byte tileID;
        private byte tileAttrib;
        private byte tileLSB;
        private byte tileMSB;
        private ushort patternShifterLo;
        private ushort patternShifterHi;
        private ushort attribShifterLo;
        private ushort attribShifterHi;

        //CSV file & debugging;
        private int frameCount;
        private string csvFileName;
        private string filePath;
        private CultureInfo cultInfo;
        private DateTime dateTime;
        private DirectoryInfo csvLogs;
        public PpuState state;

        public PPU()
        {
            latch = 0;
            frameCount = 0;

            // Two adjacent 16 * 16 2D arrays of 8 * 8 tiles. Used only for testing
            PatternMemory = new Color[2, (16 * 8) , (16 * 8)];

#if LOGGING
            //CSV log - file creation
            csvLogs = new DirectoryInfo(@"/Users/alexwright/Documents/MSc Files/Project/csv_logs");
            dateTime = DateTime.UtcNow;
            cultInfo = CultureInfo.CreateSpecificCulture("en-UK");
            csvFileName = $"project_nes_ppu_log_{dateTime.ToString("o", cultInfo)}.csv";
            filePath = $"{csvLogs.FullName}/{csvFileName}";
            using (File.Create(filePath)) { }

            using (StreamWriter file = new StreamWriter(filePath, true))
                file.WriteLine(
                   $"frameCount," +
                   $"scanLine," +
                   $"cycle," +
                   $"ppuAddress," +
                   $"read(address)," +
                   $"ppuData," +
                   $"patternShifterHi," +
                   $"patternShifterLo," +
                   $"attribShifterHi," +
                   $"attribShifterLo," +
                   $"tileID," +
                   $"tileAttrib," +
                   $"tileMSB," +
                   $"tileLSB"
                   );
#endif
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

        private enum LoopyFields
        {
            coarse_x = 0b11111 << 0,
            coarse_y = 0b11111 << 5,
            nTable_x = 0b1 << 10, 
            nTable_y = 0b1 << 11,
            fine_y   = 0b111 << 12
        }




        // Properties * * * * * * * * *

        public bool Nmi { get; set; }

        public bool FrameComplete { get; set; }

        public Color[, ,] PatternMemory { get; }



        // Register Getters & Setters * * * * * * * * * *

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
        
        private void SetMask(byte value)
            => mask = value; // No further actions

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
                // one whole nametable row;
                // In horizontal mode (0) it just increments by 1, moving to
                // the next column
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


        // Clock function * * * * * * * *
     

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
#if LOGGING
            state = new PpuState(this);
            using (StreamWriter file = new StreamWriter(filePath, true))
                file.WriteLine(state);
#endif
            bool at_init_point  =     scanLine  == -1   & cycle == 1;
            bool visible_SL     =     scanLine  >= -1   & scanLine < 240;
            bool last_visible_SL =    scanLine  == 240;
            bool first_blank_SL =     scanLine  == 241  & cycle == 1;
            bool frame_complete =     scanLine  >= 261;
            bool at_end_of_SL   =     cycle     == 256;
            bool visible_cycle  =     cycle     > 0     & cycle < 256;
            bool SL_complete    =     cycle     >= 341; 
            bool in_read_section =    (cycle >= 2 & cycle < 258 ) | (cycle >= 321  & cycle < 338);

            if (at_init_point)
                SetFlag(StatFlags.vertical_blank, false);

            if (visible_SL & in_read_section)
            {
                UpdateShifters();
                int stage_no = (cycle - 1) % 8;

                if (stage_no == 0)
                {
                    LoadShifters();
                    tileID = PpuRead((ushort)(0x2000 | ppuAddress & 0x0FFF));
                }
                else if(stage_no == 2)
                {
                    /** Tile attributes are stored in the last 64 (0x0040) byte of 
                     *  the name table, and thus start 960 (0x03C0) bytes into the 
                     *  name table.
                     *  
                     *  There is one byte of attribute memory per 16 tiles. 
                     */  
                    tileAttrib = PpuRead((ushort)(0x23C0 | ppuAddress & 0x0FFF));
                }
                else if (stage_no == 4)
                {
                    int bg = GetFlag(CtrlFlags.backgr_select) ? 1 : 0;
                    tileLSB = PpuRead((ushort)((bg << 12) + (tileID << 4)));
                }
                else if(stage_no == 6)
                {
                    int bg = GetFlag(CtrlFlags.backgr_select) ? 1 : 0;
                    tileMSB = PpuRead((ushort)((bg << 12) + (tileID << 4) + 8));
                }
            }

            if (visible_SL & visible_cycle)
            {
                byte pixel = 0;
                byte palet = 0;
                if (true)
                {
                    ushort bit_mux = 0x8000; //mux = multiplexer or data selector

                    byte pix0 = (byte)((patternShifterLo & bit_mux) > 0 ? 1 : 0);
                    byte pix1 = (byte)((patternShifterHi & bit_mux) > 0 ? 1 : 0);
                    pixel = (byte)((pix1 << 1) | pix0);

                    byte pal0 = (byte)((attribShifterLo & bit_mux) > 0 ? 1 : 0);
                    byte pal1 = (byte)((attribShifterHi & bit_mux) > 0 ? 1 : 0);
                    palet = (byte)((pal1 << 1) | pal0);
                }

                IODevice.SetPixel(cycle - 1, scanLine, GetColourFromPalette(palet, pixel));
            }
            if (last_visible_SL)
            {
                // Do nothing
            }
            else if (first_blank_SL)
            {
                SetFlag(StatFlags.vertical_blank, true);
                Nmi = GetFlag(CtrlFlags.nmi_enable);
            }

            // Actually set the pixel - once per cycle
            


            cycle++;
            //Console.WriteLine(state);

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


        // Rendering methods * * * * * * * *

        public Color GetColourFromPalette(byte palet, byte pixel)
        {
            byte index = PpuRead((ushort)(0x3f00 + (palet * 4) + pixel));

        #if generate_a_nice_pattern
            index = (byte)((pixel + 6) * 5);
        #endif
            return palette[index];
        }

        private void IncrementScrollX() { }

        private void IncrementScrollY() { }

        private void TransferAddressX() { }

        private void TransferAddressY() { }

        private void LoadShifters()
        {
            patternShifterLo = (ushort)((patternShifterLo & 0xFF00) | tileLSB);
            patternShifterHi = (ushort)((patternShifterHi & 0xFF00) | tileMSB);
            attribShifterLo = (ushort)((attribShifterLo & 0xFF00) | ((tileAttrib & 0b01) > 0 ? 0xFF : 0x00));
            attribShifterHi = (ushort)((attribShifterHi & 0xFF00) | ((tileAttrib & 0b10) > 0 ? 0xFF : 0x00));
        }

        private void UpdateShifters()
        {
            if(GetFlag(MaskFlags.backgr_enable))
            {
                patternShifterLo <<= 1;
                patternShifterHi <<= 1;
                attribShifterLo  <<= 1;
                attribShifterHi  <<= 1;
            }
        }

        // Flag handlers * * * * * * * * * *

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


        // Loopy get set
        private void SetLoopyField()
        {
            throw new NotImplementedException();
        }

        private byte GetLoopyField()
        {
            throw new NotImplementedException();
        }



        // R/W methods & object reference setters * * * * * * * * * * * * * *

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

        public void PpuWrite(ushort addr, byte data)
        {
            ppuBus.Write(addr, data);
        }

        public byte PpuRead(ushort addr)
        {
            return ppuBus.Read(addr);
        }

        public void SetPalette(Palette p) => palette = p;




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

                                = GetColourFromPalette(palette, pixelVal);
                        }
                    }
                }
            }
        }



        private class LoopyRegister
        {
            private ushort address;
            private byte coarse_x, course_y, ntable_x, ntable_y, fine_y;

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


        public struct PpuState
        {
            private PPU ppu;

            public PpuState(PPU ppu)
            {
                this.ppu = ppu;
            }

            public override string ToString()
               =>
               $"{ppu.frameCount}," +
               $"{ppu.scanLine}," +
               $"{ppu.cycle}," +
               $"{ppu.ppuAddress.x()}," +
               $"{ppu.PpuRead(ppu.ppuAddress).x()}," +
               $"{ppu.ppuData.x()}," +
               $"{ppu.patternShifterHi.x()}," +
               $"{ppu.patternShifterLo.x()}," +
               $"{ppu.attribShifterHi.x()}," +
               $"{ppu.attribShifterLo.x()}," +
               $"{ppu.tileID.x()}," +
               $"{ppu.tileAttrib.x()}," +
               $"{ppu.tileMSB.x()}," +
               $"{ppu.tileLSB.x()}";
        }
    } 
}
