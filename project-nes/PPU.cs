using System;
namespace project_nes
{
    public class PPU
    {
        byte[] dummy;

        public PPU()
        {
            dummy = new byte[0x0008];
        }




        public void CpuWrite(ushort addr, byte data)
        {
            dummy[addr] = data;
        }

        public byte CpuRead(ushort addr)
        {
            return dummy[addr];
        }
    }
}
