using System;


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
         *   
         ** https://www.reddit.com/r/EmuDev/comments/4j4ryc/resources_for_writing_graphics_renderer/
         * 
         *  Basically, for an NES emulator what you'll be doing is blitting the 
         *  framebuffer pixel-by-pixel to a 256 by 240 buffer, and then once the 
         *  PPU finishes a frame you'll upload that to a texture and show it 
         *  fullscreen. 
         * 
         * 
         ** https://www.reddit.com/r/EmuDev/comments/9spw6q/doubts_about_graphics_in_emulation/
         *    Using SDL will probably be the easiest way to do this.
         *    
         *    1. Create an SDL_Surface that's 256x240 pixels in size 
         *       (the size of the NES screen) which is an in-memory 
         *       image that you can draw to, but is not able to render. 
         *       Along with it, create an SDL_Texture with the 
         *       SDL_TEXTUREACCESS_STREAMING flag. 
         *       This is what lets you render your SDL_Surface to the screen in 
         *       SDL2.
         *       
         *    2. Now you're ready to start drawing. Every frame or scanline or 
         *       however your emulator works, set pixels in your SDL_Surface 
         *       using one of the set pixel routines you can find by googling.
         *       
         *    3. Once you're finished with a frame and want to display it, 
         *       use SDL_UpdateTexture to push the new SDL_Surface data to 
         *       the GPU and then use SDL_RenderCopy to actually render that 
         *       to the screen. Go back to step 2. 
         */

        private const int maxCycles = 341;
        private const int maxSLs = 261;

        // PPU registers
        private byte control;       // VBHB SINN | NMI enable (V), PPU master/slave (P), sprite height (H), background tile select (B), sprite tile select (S), increment mode (I), nametable select (NN)
        private byte mask;          // BGRs bMmG | color emphasis (BGR), sprite enable (s), background enable (b), sprite left column enable (M), background left column enable (m), greyscale (G)
        private byte status;        // VSO- ---- | vblank (V), sprite 0 hit (S), sprite overflow (O); read resets write pair for $2005/$2006
        private byte oamAddress;    // OAM r/w address
        private byte oamData;       // OAM r/w data
        private byte scroll;        // fine scroll position (two writes: X scroll, Y scroll)
        private byte ppuAddress;    // PPU read/write address (two writes: most significant byte, least significant byte)
        private byte ppuData;       // PPU data read/write
        private byte oamDma;        // OAM DMA high address
        private byte[] ppuRegisters; // Hold the register 'properties' so they can be accessed via an index

        private int cycle;
        private int scanLine;

        PpuBus ppuBus;

        public PPU()
        {
            ppuRegisters = new byte[]
            {
                Control,
                Mask,
                Status,
                OamAddress,
                OamData,
                Scroll,
                PpuAddress,
                PpuData
            };



            PatternMemory[0] = new Pixel[(16 * 8), (16 * 8)];
            PatternMemory[1] = new Pixel[(16 * 8), (16 * 8)];
        }

        public Pixel[][,] PatternMemory { get; }

        private byte Control { get; set; }

        private byte Mask { get; set; }

        private byte Status { get; set; }

        private byte OamAddress { get; set; }

        private byte OamData { get; set; }

        private byte Scroll { get; set; }

        private byte PpuAddress { get; set; }

        private byte PpuData { get; set; }


        public void Clock()
        {
            cycle++;
            if (cycle >= maxCycles)
            {
                cycle = 0;
                scanLine++;
                cycle++;

                if (scanLine >= maxSLs)
                {
                    scanLine = -1;
                    //frame complete
                }
            }
        }

        public void CpuWrite(ushort addr, byte data)
        {
            //incoming addresses start at 0x2000
            ppuRegisters[addr - 0x2000] = data;
        }

        public byte CpuRead(ushort addr)
        {
            //incoming addresses start at 0x2000
            return ppuRegisters[addr - 0x2000];
        }

        public void ConnectBus(PpuBus bus)
        {
            ppuBus = bus;
        }

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
                    //Translate XY coordinates into 1D coordinate with (Y * Width + X) byte-offset
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
                            byte pixelVal = (byte)((lsb * 0x01) + (msb * 0x01));
                            lsb >>= 1;
                            msb >>= 1;

                            // At the pattern memory (bank) specifed by paramater i:
                            //   At index row,col of tile x,y - set the pixel colour
                            PatternMemory[i][row + (tileY * 8), col + (tileX * 8)] = GetColour(palette, pixelVal);
                        }
                    }
                }
            }
        }

        private Pixel GetColour(byte palet, byte pixel)
        {
            return new Pixel(0,0,0);
        }


        public struct Pixel
        {
            private byte[] colours;

            public Pixel(byte red, byte green, byte blue)
            {
                Red = red;
                Green = green;
                Blue = blue;
                colours = new byte[3] { Red, Green, Blue };
            }
            public byte Red { get; }
            public byte Green { get; }
            public byte Blue { get; }

            public byte this[int i]
            {
                get => colours[i];
            }
        }
    } 
}
