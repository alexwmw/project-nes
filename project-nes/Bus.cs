using System;
namespace project_nes
{
    public class Bus : iBus
    {
        // Fake ram for testing - 64kb for the full 16-bit addressable range
        byte[] CpuRam = new byte[64 * 1024];


        public Bus()
        {
        }

        // A Read-Write signal is not needed as it is implied by the method being called

        public byte Read(ushort address)
            => address.IsValidAddress()
            ? CpuRam[address]
            : throw new ArgumentOutOfRangeException($"Invalid address: {address}");

        public void Write(ushort address, byte data) {
            if (address.IsValidAddress())
                CpuRam[address] = data;
            else
                throw new ArgumentOutOfRangeException($"Invalid address: {address}");
        }
    }


    internal static class ExtensionMethods
    {
        internal static bool IsValidAddress(this ushort address)
            => address >= 0x0 && address <= 0xFFFF;
    }
}
