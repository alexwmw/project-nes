using System;
using System.Collections;
using System.Collections.Generic;
//using System.Drawing;
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

        /** PPU registers
         * These are the backing fields of register properties.
         * When each register is r/w to, various actions are triggered.
         * Property getters and setters are a convenient medium for this
         */
        private byte control;       // VBHB SINN | NMI enable (V), PPU master/slave (P), sprite height (H), background tile select (B), sprite tile select (S), increment mode (I), nametable select (NN)
        private byte mask;          // BGRs bMmG | color emphasis (BGR), sprite enable (s), background enable (b), sprite left column enable (M), background left column enable (m), greyscale (G)
        private byte status;        // VSO- ---- | vblank (V), sprite 0 hit (S), sprite overflow (O); read resets write pair for $2005/$2006
        private byte oamAddress;    // OAM r/w address
        private byte oamData;       // OAM r/w data
        private byte scroll;        // fine scroll position (two writes: X scroll, Y scroll)
        private byte ppuAddress;    // PPU read/write address (two writes: most significant byte, least significant byte)
        private byte ppuData;       // PPU data read/write
        private byte oamDma;        // OAM DMA high address
        private byte[] ppuRegisters;// Hold the register properties so they can be accessed via an index

        private int cycle;
        private int scanLine;
        private Palette palette;

        PpuBus ppuBus;

        public PPU()
        {
            ppuRegisters = new byte[8]
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

            // Two adjacent 16 * 16 2D arrays of 8 * 8 tiles
            PatternMemory = new Color[2, (16 * 8) , (16 * 8)];

            palette = new Palette
            {
                { 84,  84,  84 },
                {  0,  30, 116 },
                {  8,  16, 144 },
                { 48,   0, 136 },
                { 68,   0, 100 },
                { 92,   0,  48 },
                { 84,   4,   0 },
                { 60,  24,   0 },
                { 32,  42,   0 },
                {  8,  58,   0 },
                {  0,  64,   0 },
                {  0,  60,   0 },
                {  0,  50,  60 },
                {  0,   0,   0 },
                {  0,   0,   0 },
                {  0,   0,   0 },

                { 152, 150, 152 },
                {   8,  76, 196 },
                {  48,  50, 236 },
                {  92,  30, 228 },
                { 136,  20, 176 },
                { 160,  20, 100 },
                { 152,  34,  32 },
                { 120,  60,   0 },
                {  84,  90,   0 },
                {  40, 114,   0 },
                {   8, 124,   0 },
                {   0, 118,  40 },
                {   0, 102, 120 },
                {   0,   0,   0 },
                {   0,   0,   0 },
                {   0,   0,   0 },

                { 236, 238, 236 },
                {  76, 154, 236 },
                { 120, 124, 236 },
                { 176,  98, 236 },
                { 228,  84, 236 },
                { 236,  88, 180 },
                { 236, 106, 100 },
                { 212, 136,  32 },
                { 160, 170,   0 },
                { 116, 196,   0 },
                {  76, 208,  32 },
                {  56, 204, 108 },
                {  56, 180, 204 },
                {  60,  60,  60 },
                {   0,   0,   0 },
                {   0,   0,   0 },
  
                { 236, 238, 236 },
                { 168, 204, 236 },
                { 188, 188, 236 },
                { 212, 178, 236 },
                { 236, 174, 236 },
                { 236, 174, 212 },
                { 236, 180, 176 },
                { 228, 196, 144 },
                { 204, 210, 120 },
                { 180, 222, 120 },
                { 168, 226, 144 },
                { 152, 226, 180 },
                { 160, 214, 228 },
                { 160, 162, 160 },
                {   0,   0,   0 },
                {   0,   0,   0 }
            };
        }

        public Color[, ,] PatternMemory { get; }

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
                            Console.WriteLine($"" +
                                $"pixelval: {pixelVal} \n" +
                                $"pixelval: {pixelVal} \n");

                            // At the pattern memory (bank) specifed by paramater i:
                            //   At index row,col of tile x,y - set the pixel colour
                            PatternMemory[i, row + (tileY * 8), col + (tileX * 8)] = GetColour(palette, pixelVal);
                        }
                    }
                }
            }
        }

        private Color GetColour(byte palet, byte pixel)
        {
            byte index = PpuRead((ushort)(0x3f00 + (palet * 4) + pixel));
            Console.WriteLine("Index: " + index + "\n");
            return palette[index];
        }

        public struct RgbPixel
        {
            private byte[] rgb;

            public RgbPixel(byte red, byte green, byte blue)
            {
                Red = red;
                Green = green;
                Blue = blue;
                rgb = new byte[3] { Red, Green, Blue };
            }
            public byte Red { get; }
            public byte Green { get; }
            public byte Blue { get; }

            public byte this[int i]
            {
                get => rgb[i];
            }

            public int HexValue()
            {
                return (Red << 16) & (Green << 8) & (Blue);
            }
        }

        private class Palette : IEnumerable<Color>
        {
            private List<Color> colors = new List<Color>();

            public IEnumerator<Color> GetEnumerator()
                => colors.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => colors.GetEnumerator();

            public void Add(byte r, byte g, byte b)
                => colors.Add(new Color(r, g, b));

            public Color this[int i]    // Indexer declaration  
            {
                get { return this.colors[i]; }
                set { this.colors[i] = value; }
            }
        }
    } 
}
