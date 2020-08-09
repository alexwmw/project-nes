using System;
using System.IO;
using System.Threading;
using HelperMethods;
using CsvHelper;
using System.Globalization;

namespace project_nes
{
    class Program
    {
        static void Main(string[] args)
        {

            int systemClock = 0;

            //ROM directories
            DirectoryInfo testRoms = new DirectoryInfo(@"/Users/alexwright/Documents/MSc Files/Project/test_roms");
            DirectoryInfo gameRoms = new DirectoryInfo(@"/Users/alexwright/Documents/MSc Files/Project/game_roms");

            string nestest = @"Donkey Kong (World) (Rev A).nes";
  
            // New objects
            CpuBus cpuBus = new CpuBus();
            PpuBus ppuBus = new PpuBus();
            CPU cpu = new CPU();
            PPU ppu = new PPU();
            Cartridge cartridge = new Cartridge(nestest, gameRoms);


            // Dependency injection
            cpuBus.ConnectPPU(ppu);
            cpuBus.InsertCartridge(cartridge);
            ppuBus.ConnectCartridge(cartridge);
            ppu.ConnectBus(ppuBus);
            cpu.ConnectBus(cpuBus);

            // Init procedure
            cpu.Reset();
            // cpu.PC = 0xC000;  // Force nestest all tests

            ppu.GetPatternTable(1, 1);



            // Loop
            while (false)
            {
                ppu.Clock();
                if(systemClock % 3 == 0)
                {
                    cpu.Clock();
                }
                systemClock++;
            }


        }
    }
}
