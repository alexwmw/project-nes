using System;
namespace project_nes
{
    public class Bus : iBus
    {
        byte[] cpuRam;

        public Bus()
        {
            // RAM initially set to 64kb to test the full 16-bit addressable rangev
            cpuRam = new byte[64 * 1024];
        }

        // A Read-Write signal is not needed as it is implied by the method being called

        public byte Read(ushort address)
        {
            if (address < 0)
                throw new ArgumentOutOfRangeException($"Invalid address less than 0: {address}");

            if (address <= 0xFFFF)
                return cpuRam[address];

            else
                throw new ArgumentOutOfRangeException($"Invalid address: {address}");
        }


        public void Write(ushort address, byte data) {
            if (address < 0)
                throw new ArgumentOutOfRangeException($"Invalid address < 0: {address}");

            if (address <= 0xFFFF)
               cpuRam[address] = data;

            else
                throw new ArgumentOutOfRangeException($"Invalid address greater than 0xFFFF: {address}");
        }
    }







}
