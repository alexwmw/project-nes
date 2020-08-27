using System;
namespace project_nes
{
    public interface IMapper
    {

        public byte CpuRead(ushort adr);

        public void CpuWrite(ushort adr, byte data);

        public byte PpuRead(ushort adr);

        public void PpuWrite(ushort adr, byte data);

        public byte PrgBanks {set;}

        public byte[] PrgRom { set; }

        public byte[] ChrRom { set; }
    }


    public class Mapper0 : IMapper
    {

        private byte prgBanks;
        private byte[] prgRom, chrRom;

        public byte PrgBanks { set => prgBanks = value; }
        public byte[] PrgRom { set => prgRom = value; }
        public byte[] ChrRom { set => chrRom = value; }

        public byte CpuRead(ushort adr)
        {
            ushort mappedAdr =
                    adr < 0x6000 ? throw new ArgumentOutOfRangeException($"Cartridge non-CPU range read by CPU at {adr}") :
                    adr < 0x8000 ? throw new ArgumentOutOfRangeException($"Cartridge CHR Rom range read by CPU at {adr}") :
                    (ushort)(adr & (prgBanks == 32 ? 0x7FFF : 0x3FFF));
            return prgRom[mappedAdr];
        }

        public void CpuWrite(ushort adr, byte data)
        {
            ushort mappedAdr =
                adr < 0x6000? throw new ArgumentOutOfRangeException($"Cartridge non-CPU range read by CPU at {adr}") :
                adr <= 0x8000 ? throw new ArgumentOutOfRangeException($"Cartridge CHR Rom range read by CPU at {adr}"):
                (ushort)(adr & (prgBanks == 32 ? 0x7FFF : 0x3FFF));
            prgRom[mappedAdr] = data;
        }

        public byte PpuRead(ushort adr)
        {
            ushort mappedAdr =
                (adr >= 0x0000 & adr <= 0x1FFF) ? adr :
                throw new ArgumentOutOfRangeException($"Cartridge: non-PPU range read by PPU at {adr}");
            return chrRom[mappedAdr];
        }

        public void PpuWrite(ushort adr, byte data)
        {
            ushort mappedAdr =
                (adr >= 0x6000 & adr <= 0x7FFF) ? adr :
                throw new ArgumentOutOfRangeException($"Cartridge: non-PPU range attempted write by PPU at {adr}");
            chrRom[mappedAdr] = data;
        }
    }
}
