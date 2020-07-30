using System;
namespace project_nes
{
    public class PpuBus
    {
        byte[] dummy;

        byte[] paletteRam; 
        byte[] vRam;

        public PpuBus()
        {
            dummy = new byte[100];
            vRam = new byte[2048];
            paletteRam = new byte[32];
        }


        public void Write(ushort addr, byte data)
        {
            dummy[addr] = data;
        }

        public byte Read(ushort addr)
        {
            return dummy[addr];
        }
    }
}
