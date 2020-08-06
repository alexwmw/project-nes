using System;
namespace project_nes
{
    public class PpuBus
    {


        /**
         * Address range	Size	Description
         * 
         * $0000-$0FFF	    $1000	Pattern table 0 <<-- cartridge?
         * $1000-$1FFF	    $1000	Pattern table 1 <<-- cartridge?
         * $2400-$27FF	    $0400	Nametable 1
         * $2800-$2BFF	    $0400	Nametable 2
         * $2C00-$2FFF	    $0400	Nametable 3
         * $3000-$3EFF	    $0F00	Mirrors of $2000-$2EFF
         * $3F00-$3F1F	    $0020	Palette RAM indexes
         * $3F20-$3FFF	    $00E0	Mirrors of $3F00-$3F1F
         * 
         */
        private byte[] dummy;
        private byte[] paletteRam;
        private byte[] vRam;
        private byte[][] patternMemory;

        private Cartridge cartridge;

        public PpuBus()
        {
            dummy = new byte[100];
            vRam = new byte[2048];
            paletteRam = new byte[32];
        }

        public void ConnectCartridge(Cartridge cart)
        {
            cartridge = cart;
        }

        public void Write(ushort addr, byte data)
        {
            dummy[addr] = data;
        }

        public byte Read(ushort addr)
        {
            return dummy[addr];
        }


        public void CartWrite(ushort addr, byte data)
        {
            cartridge.PpuWrite(addr, data);
        }

        public byte CartRead(ushort addr)
        {
            return cartridge.PpuRead(addr);
        }


        
    }
}
