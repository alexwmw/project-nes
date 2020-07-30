using System;
namespace project_nes
{
    public class PPU
    {

        private byte control;
        private byte mask;
        private byte status;
        private byte oamAddress;
        private byte oamData;
        private byte scroll;
        private byte ppuAddress;
        private byte ppuData;
        private byte[] ppuRegisters; // Hold the register 'properties' so they can be accessed via addresses

        PpuBus ppuBus;
        Cartridge cartridge;

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

        public void ConnectCartridge(Cartridge cart)
        {
            cartridge = cart;
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
