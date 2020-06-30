using System;
namespace project_nes
{
    public interface iBus
    {

        void Write(ushort address, byte data);

        byte Read(ushort address);


    }
}
