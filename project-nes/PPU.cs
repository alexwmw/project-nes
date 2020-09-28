//#define generate_a_nice_pattern
//#define LOGGING
#define PRINT_TILE_IDS

using System;
using System.Globalization;
using System.IO;
using HelperMethods;
using SFML.Graphics;

namespace project_nes
{
    public class PPU
    {
        

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
        private byte fine_x;
        private int cycle;
        private int scanLine;
        private LoopyRegister vReg;
        private LoopyRegister tReg;


        // Internal references
        private Palette palette;
        private IODevice IODevice;
        private PpuBus ppuBus;
        private CPU cpu;

        // Background rendering
        private byte tileID;
        private byte tileAttrib;
        private byte tileLSB;
        private byte tileMSB;
        private byte bgPixel;
        private byte bgPalette;
        private ushort patternShifterLo;
        private ushort patternShifterHi;
        private ushort attribShifterLo;
        private ushort attribShifterHi;

        //CSV file & debugging;
        private int frameCount;
        private int clockCount;
        private string csvFileName;
        private string filePath;
        private CultureInfo cultInfo;
        private DateTime dateTime;
        private DirectoryInfo csvLogs;
        public PpuState state;
        private string tileIdString;


        public PPU(CPU cpu)
        {
            this.cpu = cpu;
            latch = 0;
            frameCount = 0;
            tReg = new LoopyRegister();
            vReg = new LoopyRegister();

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
                   $"vReg.Address," +
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


        /** Enums
         * 
         * Used to access specific flags within 
         * each register (control / status / mask)
         */
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
            vertical_blank = 1 << 7
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
            blue_emph = 1 << 7
        }

        // Properties * * * * * * * * *

        // Non-maskable interrupt flag
        // Used to trigger the CPU's NMI
        public bool Nmi { get; set; }

        public bool FrameComplete { get; set; }

        public Color[, ,] PatternMemory { get; }



        // Register Getters & Setters * * * * * * * * * *

        private byte GetControl()
            => throw new AccessViolationException("Read of unreadable register"); // Not actually readable

        private void SetControl(byte value)
        {
            control = value;
            tReg.NTable_x = (byte)(GetFlag(CtrlFlags.ntable_0) ? 1 : 0);
            tReg.NTable_y = (byte)(GetFlag(CtrlFlags.ntable_1) ? 1 : 0);
        }

        private byte GetMask()
             => throw new AccessViolationException("Read of unreadable register"); // Not actually readable
        
        private void SetMask(byte value)
            => mask = value; // No further actions

        private byte GetStatus()
        {
                byte temp = status;
                SetFlag(StatFlags.vertical_blank, false);
                latch = 0;
                return temp;
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
                fine_x = (byte)(value & 0x07);
                tReg.Coarse_x = (byte)(value >> 3);
                latch = 1;
            }
            else
            { 
                tReg.Fine_y = (byte)(value & 0x07);
                tReg.Coarse_y = (byte)(value >> 3);
                latch = 0;
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
                tReg.Address = (ushort)((value << 8) | (tReg.Address & 0x00FF));
                vReg.Address = tReg.Address;
                latch = 1;
            }
            else
            {
                tReg.Address = (ushort)((tReg.Address & 0xFF00) | value);
                latch = 0;
            }
        }
            

        private byte GetPpuData()
        {
            /* Reads should be delayed by one cycle - 
            * UNLESS reading from palette ROM
            */

            // store ppuData that was set during the last cycle
            byte ppuDataDelayed = ppuData;

            // update ppuData during this cycle
            ppuData = ReadBus(vReg.Address);

            ushort tempAdress = vReg.Address;

            vReg.Address += (ushort)(GetFlag(CtrlFlags.increment_mode) ? 32 : 1);

            // If palette location, return current, else return previous
            return tempAdress >= 0x3F00 ? ppuData : ppuDataDelayed;
        }

        private void SetPpuData(byte value)
        {
            WriteBus(vReg.Address, value);

            // If set to vertical mode (1), the increment is 32, so it skips
            // one whole nametable row; in horizontal mode (0) it just increments
            // by 1, moving to the next column
            vReg.Address += (ushort)(GetFlag(CtrlFlags.increment_mode) ? 32 : 1);


        }



        // Reset * * * * * * * * * * *

        public void Reset()
        {
	        fine_x = 0x00;
	        latch = 0x00;
	        ppuData = 0x00;
	        scanLine = 0;
	        cycle = 0;
	        tileID = 0x00;
	        tileAttrib = 0x00;
	        tileLSB = 0x00;
	        tileMSB = 0x00;
	        patternShifterLo = 0x0000;
	        patternShifterHi = 0x0000;
	        attribShifterLo = 0x0000;
	        attribShifterHi = 0x0000;
	        status = 0x00;
	        mask = 0x00;
	        control = 0x00;
	        vReg.Address = 0x0000;
	        tReg.Address = 0x0000;
        }

        // Clock function * * * * * * * *


        public void Clock0()
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
                bool cycleSkip      =     scanLine  == 0    & cycle == 0;
                bool visible_SL     =     scanLine  >= -1   & scanLine < 240;
                bool last_visible_SL =    scanLine  == 240;
                bool first_blank_SL =     scanLine  == 241;
                bool frame_complete =     scanLine  >= 261;
                bool at_end_of_SL   =     cycle     == 256;
                bool visible_cycle  =     cycle     > 0     & cycle < 256;
                bool SL_complete    =     cycle     >= 341; 
                bool in_read_section =    (cycle >= 2 & cycle < 258 ) | (cycle >= 321  & cycle < 338);

                if (visible_SL)
                {
                    if (cycleSkip)
                    {
                        cycle++;
                    }
                    if (at_init_point)
                    {
                        SetFlag(StatFlags.vertical_blank, false);
                        tileIdString = "";
                    }
                    if (in_read_section)
                    {
                        UpdateShifters();
                        int stage_no = (cycle - 1) % 8;
                        int bg = GetFlag(CtrlFlags.backgr_select) ? 1 : 0; ;
                        switch (stage_no)
                        {
                            case 0:
                                LoadShifters();
                                tileID = ReadBus((ushort)(0x2000 | vReg.Address & 0x0FFF));
                                break;
                            case 2:
                                /** Tile attributes are stored in the last 64 (0x0040) byte of 
                                 *  the name table, and thus start 960 (0x03C0) bytes into the 
                                 *  name table.
                                 *  
                                 *  There is one byte of attribute memory per 16 tiles. 
                                 */
                                tileAttrib = ReadBus(vReg.Address);
                                if ((vReg.Coarse_y & 0x02) > 0)
                                    tileAttrib >>= 4;
                                if ((vReg.Coarse_x & 0x02) > 0)
                                    tileAttrib >>= 2;
                                tileAttrib &= 0x03;
                                break;
                            case 4:
                                tileLSB = ReadBus((ushort)((bg << 12) + (tileID << 4) + vReg.Fine_y));
                                break;
                            case 6:
                                tileMSB = ReadBus((ushort)((bg << 12) + (tileID << 4) + vReg.Fine_y + 8));
                                break;
                            case 7:
                                IncrementScrollX();
                                if (scanLine % 8 == 0)
                                    tileIdString += tileID.x();
                                break;
                        }
                    }

                    if (cycle == 256)
                    {
                        IncrementScrollY();
                    }
                    if (cycle == 257)
                    {
                        LoadShifters();
                        TransferAddressX();
                    }
                    if (cycle == 338 | cycle == 340)
                    {
                        tileID = ReadBus((ushort)(0x2000 | (vReg.Address & 0x0FFF)));
                    }
                    if (scanLine == -1 & cycle >= 280 & cycle < 305)
                    {
                        TransferAddressY();
                    }
                }
                if (last_visible_SL)
                {
                    // Do nothing
                }
                if(scanLine >= 241 & scanLine < 261)
                {
                    if(first_blank_SL & cycle == 1)
                    {
                        SetFlag(StatFlags.vertical_blank, true);
                        Nmi = GetFlag(CtrlFlags.nmi_enable);
                    }
                }
            
                byte pixel = 0;
                byte palet = 0;

                if (GetFlag(MaskFlags.backgr_enable))
                {
                    ushort bit_mux = (ushort)(0x8000 >> fine_x); //mux = multiplexer or data selector

                    byte pix0 = (byte)((patternShifterLo & bit_mux) > 0 ? 1 : 0);
                    byte pix1 = (byte)((patternShifterHi & bit_mux) > 0 ? 1 : 0);
                    pixel = (byte)((pix1 << 1) | pix0);

                    byte pal0 = (byte)((attribShifterLo & bit_mux) > 0 ? 1 : 0);
                    byte pal1 = (byte)((attribShifterHi & bit_mux) > 0 ? 1 : 0);
                    palet = (byte)((pal1 << 1) | pal0);
                }

                IODevice.SetPixel(cycle - 1, scanLine, GetColourFromPalette(palet, pixel));

            
            
                cycle++;

            


                if (SL_complete)
                {
                    cycle = 0;
                    scanLine++;
                    if (scanLine % 8 == 0)
                        tileIdString += "\n";
                
                    // If scanline gets to 261 (maxSL), the frame is complete
                    if (frame_complete)
                    {
                        scanLine = -1;
                        frameCount++;
                        FrameComplete = true;

                        Console.WriteLine($"Frame: {frameCount}");
                        Console.WriteLine(tileIdString);
                    }
                }
            }



        public void Clock()
        {
            clockCount++;
            if (scanLine >= -1 && scanLine < 240)
            {
                if (scanLine == 0 && cycle == 0)
                {
                    cycle = 1;
                }

                if (scanLine == -1 && cycle == 1)
                {
                    SetFlag(StatFlags.vertical_blank, false);
                    tileIdString = "";
                }

                if ((cycle >= 2 && cycle < 258) || (cycle >= 321 && cycle < 338))
                {
                    UpdateShifters();
                    int bg = GetFlag(CtrlFlags.backgr_select) ? 1 : 0;
                    switch ((cycle - 1) % 8)
                    {
                        case 0:
                            LoadShifters();
                            tileID = ReadBus((ushort)(0x2000 | (vReg.Address & 0x0FFF)));
                            break;
                        case 2:
                            tileAttrib = ReadBus((ushort)(0x23C0 | (vReg.NTable_y << 11)
                                                                    | (vReg.NTable_x << 10)
                                                                    | ((vReg.Coarse_y >> 2) << 3)
                                                                    | (vReg.Coarse_x >> 2)));
                            if ((vReg.Coarse_y & 0x02) > 0)
                            {
                                tileAttrib >>= 4;
                            }

                            if ((vReg.Coarse_x & 0x02) > 0)
                            {
                                tileAttrib >>= 2;
                            }
                            tileAttrib &= 0x03;
                            break;

                        case 4:
                            tileLSB = ReadBus((ushort)((bg << 12)
                                                        + (tileID << 4)
                                                        + (vReg.Fine_y) + 0));
                            break;
                        case 6:
                            tileMSB = ReadBus((ushort)((bg << 12)
                                                        + (tileID << 4)
                                                        + (vReg.Fine_y) + 8));
                            break;
                        case 7:
                            IncrementScrollX();
                            if (scanLine % 8 == 0 & scanLine <= 256)
                                tileIdString += tileID.x() + " ";
                            break;
                    }
                }
                if (cycle == 256)
                {
                    IncrementScrollY();
                }
                if (cycle == 257)
                {
                    LoadShifters();
                    TransferAddressX();
                }
                if (cycle == 338 || cycle == 340)
                {
                    tileID = ReadBus((ushort)(0x2000 | (vReg.Address & 0x0FFF)));
                }

                if (scanLine == -1 && cycle >= 280 && cycle < 305)
                {
                    TransferAddressY();
                }
            }
            if (scanLine == 240)
            {
                // Do nothing
            }
            if (scanLine >= 241 && scanLine < 261)
            {
                if (scanLine == 241 && cycle == 1)
                {
                    SetFlag(StatFlags.vertical_blank, true);
                    if (GetFlag(CtrlFlags.nmi_enable))
                        Nmi = true;
                }
            }

            bgPixel = 0;
            bgPalette = 0;

            if (GetFlag(MaskFlags.backgr_enable))
            {
                ushort bit_mux = (ushort)(0x8000 >> fine_x);

                byte p0_pixel = (byte)((patternShifterLo & bit_mux) > 0 ? 1 : 0);
                byte p1_pixel = (byte)((patternShifterHi & bit_mux) > 0 ? 1 : 0);

                bgPixel = (byte)((p1_pixel << 1) | p0_pixel);

                byte bg_pal0 = (byte)((attribShifterLo & bit_mux) > 0 ? 1 : 0);
                byte bg_pal1 = (byte)((attribShifterHi & bit_mux) > 0 ? 1 : 0);
                bgPalette = (byte)((bg_pal1 << 1) | bg_pal0);
            }

            IODevice.SetPixel(cycle - 1, scanLine, GetColourFromPalette(bgPalette, bgPixel));

            cycle++;

            if (cycle >= 341)
            {
                cycle = 0;
                scanLine++;
                if (scanLine % 8 == 0)
                {
                    tileIdString += "\n";
                }

                if (scanLine >= 261)
                {
                    scanLine = -1;
                    FrameComplete = true;
                    frameCount++;
#if PRINT_TILE_IDS
                    Console.WriteLine($"Frame: {frameCount}");
                    Console.WriteLine(tileIdString);
                    Console.WriteLine($"Frame: {frameCount} PPU Clock: {clockCount} CPU Clock: {cpu.cpuClockCount}");
                    Console.WriteLine();
                    Console.WriteLine("    00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F 10 11 12 13 14 15 16 17 18 19 1A 1B 1C 1D 1E 1F");
                    Console.WriteLine();
#endif

                    clockCount = 0;
                    cpu.cpuClockCount = 0;

                    for (int j = 0; j < 30; j++)
                    {
                        Console.Write((j < 16 ? "0" + j.x() : j.x() )+ "  ");
                        for (int i = 0; i < 32; i++)
                        {
                            Console.Write(ppuBus.nameTables[0, j * 32 + i].x() + " ");
                        }
                        Console.Write("\n");
                    }
                    Console.WriteLine();
                }
            }
        }

        // Rendering methods * * * * * * * *

        public Color GetColourFromPalette(byte palet, byte pixel)
        {
            byte index = ReadBus((ushort)(0x3f00 + (palet * 4) + pixel));

#if generate_a_nice_pattern
            index = (byte)((pixel + 6) * 5);
#endif
            return palette[index];
        }
        
        private void IncrementScrollX()
        {
            if (GetFlag(MaskFlags.backgr_enable) | GetFlag(MaskFlags.sprite_enable))
            {
                if (vReg.Coarse_x == 31)
                {
                    vReg.Coarse_x = 0;
                    vReg.NTable_x = (byte)~vReg.NTable_x;
                }
                else
                {
                    vReg.Coarse_x++;
                }
            }
        }

        private void IncrementScrollY()
        {
            if (GetFlag(MaskFlags.backgr_enable) | GetFlag(MaskFlags.sprite_enable))
            {
                if (vReg.Fine_y < 7)
                {
                    vReg.Fine_y++;
                }
                else
                { 
                    vReg.Fine_y = 0;

                    if (vReg.Coarse_y == 29)
                    {
                        vReg.Coarse_y = 0;
                        vReg.NTable_y = (byte)~vReg.NTable_y;
                    }
                    else if (vReg.Coarse_y == 31)
                    {
                        vReg.Coarse_y = 0;
                    }
                    else
                    {
                        vReg.Coarse_y++;
                    }
                }
            }
        }

        private void TransferAddressX()
        {
            if (GetFlag(MaskFlags.backgr_enable) | GetFlag(MaskFlags.sprite_enable))
            {
                vReg.NTable_x = tReg.NTable_x;
                vReg.Coarse_x = tReg.Coarse_x;
            }
        }

        private void TransferAddressY()
        {
            if (GetFlag(MaskFlags.backgr_enable) | GetFlag(MaskFlags.sprite_enable))
            {
                vReg.NTable_y = tReg.NTable_y;
                vReg.Coarse_y = tReg.Coarse_y;
                vReg.Fine_y = tReg.Fine_y;
            }
        }

        private void LoadShifters()
        {
            //Load the lower byte of the pattern shifters with the
            //next tile's two bit planes

            var PS_Lo_highbyte = patternShifterLo & 0xFF00; // highbyte of the lo shifter
            var PS_Hi_highbyte = patternShifterHi & 0xFF00; // highbyte of the hi shifter

            patternShifterLo = (ushort)(PS_Lo_highbyte | tileLSB);
            patternShifterHi = (ushort)(PS_Hi_highbyte | tileMSB);

            //Do the same for the attribute shifters

            var AS_Lo_highbyte = attribShifterLo & 0xFF00; // highbyte of the lo shifter
            var AS_Hi_highbyte = attribShifterHi & 0xFF00; // highbyte of the lo shifter

            //Take the lower 2 bits of the attribute word and 'inflate them' to 8 bits
            //so that the 2 bit attribute is synchorised with the 8 bit tile

            var TA_Lo_inflated = (tileAttrib & 0b01) > 0 ? 0xFF : 0x00;
            var TA_Hi_inflated = (tileAttrib & 0b10) > 0 ? 0xFF : 0x00;

            attribShifterLo = (ushort)(AS_Lo_highbyte | TA_Lo_inflated);
            attribShifterHi = (ushort)(AS_Hi_highbyte | TA_Hi_inflated);
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

        private bool GetFlag(CtrlFlags f)
            => (control & (byte)f) > 0;

        private bool GetFlag(MaskFlags f)
            => (mask & (byte)f) > 0;

        private void SetFlag(StatFlags f, bool b)
            => status = (byte)(b ? status | (byte)f : status & (byte)~f);




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
                1 => GetMask(),
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

        public void WriteBus(ushort addr, byte data)
        {
            ppuBus.Write(addr, data);
        }

        public byte ReadBus(ushort addr)
        {
            var temp = ppuBus.Read(addr);

            return temp;
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
                        byte lsb = ReadBus((ushort)(i * 0x1000 + offset + row + 0));
                        byte msb = ReadBus((ushort)(i * 0x1000 + offset + row + 8));
                        

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



        public class LoopyRegister
        {
            private const ushort course_x = 0x001F;
            private const ushort course_y = 0x03E0;
            private const ushort ntable_x = 0x0400;
            private const ushort ntable_y = 0x0800;
            private const ushort fine_y   = 0x7000;

            public LoopyRegister()
            {
                Address = 0x00;
            }

            public ushort Address
            {
                get;
                set;
            }

            public byte Coarse_x
            {
                get => (byte)((Address & course_x) >> 0);
                set => Address = (ushort)((Address & ~course_x) | (value << 0));
            }

            public byte Coarse_y
            {
                get => (byte)((Address & course_y) >> 5);
                set => Address = (ushort)((Address & ~course_y) | (value << 5));
            }

            public byte NTable_x
            {
                get => (byte)((Address & ntable_x) >> 10);
                set => Address = (ushort)((Address & ~ntable_x) | (value << 10));
            }

            public byte NTable_y
            {
                get => (byte)((Address & ntable_y) >> 11);
                set => Address = (ushort)((Address & ~ntable_y) | (value << 11));
            }

            public byte Fine_y 
            {
                get => (byte)((Address & fine_y) >> 12);
                set => Address = (ushort)((Address & ~fine_y) | (value << 12));
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
               $"{ppu.vReg.Address.x()}," +
               $"{ppu.ReadBus(ppu.vReg.Address).x()}," +
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
