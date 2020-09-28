using System;
namespace project_nes
{
    public interface IMapper
    {
        public byte PrgBanks {set;}

        public byte ChrBanks { set; }

        public byte[] PrgRom { set; }

        public byte[] ChrRom { set; }

        public byte CpuRead(ushort adr);

        public void CpuWrite(ushort adr, byte data);

        public byte PpuRead(ushort adr);

        public void PpuWrite(ushort adr, byte data);
    }


    public class Mapper0 : IMapper
    {

        public byte PrgBanks { get; set; }
        public byte ChrBanks { get; set; }
        public byte[] PrgRom { get; set; }
        public byte[] ChrRom { get; set; }

        public byte CpuRead(ushort adr)
        {
            if (adr >= 0x8000)
            {
                // Mask implements mirroring of address range 0xC000 - 0xFFFF
                // if PRG ROM is 16kb
                var mask = (ushort)(PrgBanks > 1 ? 0x7FFF : 0x3FFF);
                var mappedAdr = (ushort)(adr & mask);
                return PrgRom[mappedAdr];
            }
            else
            {
                throw new ArgumentOutOfRangeException($"Cartridge non-CPU range read by CPU at {adr}");
            }
        }

        public void CpuWrite(ushort adr, byte data)
        {
            if (adr >= 0x8000)
            {
                // Mask implements mirroring of address range 0xC00 - 0xFFF
                // if PRG ROM is 16kb
                var mask = (ushort)(PrgBanks > 1 ? 0x7FFF : 0x3FFF);
                var mappedAdr = (ushort)(adr & mask);
                PrgRom[mappedAdr] = data;
            }
            else
            {
                throw new ArgumentOutOfRangeException($"Cartridge non-CPU range write by CPU at {adr}");
            }
        }

        public byte PpuRead(ushort adr)
        {
            if (adr >= 0x0000 & adr <= 0x1FFF)
            {
                ushort mappedAdr = adr;
                return ChrRom[mappedAdr];
            }
            else
            {
                throw new ArgumentOutOfRangeException($"Cartridge: non-PPU range read by PPU at {adr}");
            }
        }

        public void PpuWrite(ushort adr, byte data)
        {
            if (adr >= 0x0000 & adr <= 0x1FFF)
            {
                ushort mappedAdr = adr;
                ChrRom[mappedAdr] = data;
            }
            else
            {
                throw new ArgumentOutOfRangeException($"Cartridge: non-PPU range read by PPU at {adr}");
            }
        }
    }
}
