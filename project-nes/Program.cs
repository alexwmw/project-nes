using System;
using HelperMethods;
using System.IO;

namespace project_nes
{
    class Program
    {
        static void Main(string[] args)
        {

            DirectoryInfo testRoms = new DirectoryInfo(@"/Users/alexwright/Documents/MSc Files/Project/test_roms");

            string rom = @"nestest.nes";

            Cartridge cartridge = new Cartridge(rom, testRoms);

            iBus bus = new Bus();

            iCPU cpu = new CPU();

            cpu.ConnectBus(bus);


        }
    }
}
