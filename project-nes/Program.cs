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
            DirectoryInfo gameRoms = new DirectoryInfo(@"/Users/alexwright/Documents/MSc Files/Project/game_roms");

            //string nestest = @"nestest.nes";
            //string donkeyKong = @"Donkey Kong (World) (Rev A).nes";
            string kirby = @"Kirby's Adventure (USA) (Rev A).nes";

            Cartridge cartridge = new Cartridge(kirby, gameRoms);

            Bus bus = new Bus();

            CPU cpu = new CPU();

            cpu.ConnectBus(bus);

            cpu.Clock();



        }
    }
}
