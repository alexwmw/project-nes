using System;
namespace project_nes
{
    public interface iBus
    {
        void Write(ushort address, byte data);
        byte Read(ushort address);

        /*
        void ConnectRam(byte[] ram);
        void ConnectApu();
        void ConnectPPU(iPPU ppu);
        void InsertCartridge(iCartridge cartridge);
        */

    }
}
