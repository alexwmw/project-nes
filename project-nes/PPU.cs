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
         * Basically, for an NES emulator what you'll be doing is blitting the 
         * framebuffer pixel-by-pixel to a 256 by 240 buffer, and then once the 
         * PPU finishes a frame you'll upload that to a texture and show it 
         * fullscreen. 
         * 
         * 
         ** https://www.reddit.com/r/EmuDev/comments/9spw6q/doubts_about_graphics_in_emulation/
         *    This is a somewhat complex subject, but you should be able to handle it. 
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

        private byte control;       // VBHB SINN | NMI enable (V), PPU master/slave (P), sprite height (H), background tile select (B), sprite tile select (S), increment mode (I), nametable select (NN)
        private byte mask;          // BGRs bMmG | color emphasis (BGR), sprite enable (s), background enable (b), sprite left column enable (M), background left column enable (m), greyscale (G)
        private byte status;        // VSO- ---- | vblank (V), sprite 0 hit (S), sprite overflow (O); read resets write pair for $2005/$2006
        private byte oamAddress;    // OAM r/w address
        private byte oamData;       // OAM r/w data
        private byte scroll;        // fine scroll position (two writes: X scroll, Y scroll)
        private byte ppuAddress;    // PPU read/write address (two writes: most significant byte, least significant byte)
        private byte ppuData;       // PPU data read/write
        private byte oamDma;        // OAM DMA high address
        private byte[] ppuRegisters; // Hold the register 'properties' so they can be accessed via addresses

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
        }

        private byte Control { get; set; }

        private byte Mask { get; set; }

        private byte Status { get; set; }

        private byte OamAddress { get; set; }

        private byte OamData { get; set; }

        private byte Scroll { get; set; }

        private byte PpuAddress { get; set; }

        private byte PpuData { get; set; }

        public byte[][] Display { get; }

        public void Clock()
        {
            cycle++;
            if(cycle >= maxCycles)
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
    }
}
