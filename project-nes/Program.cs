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

            //ROM directories
            DirectoryInfo testRoms = new DirectoryInfo(@"/Users/alexwright/Documents/MSc Files/Project/test_roms");
            DirectoryInfo gameRoms = new DirectoryInfo(@"/Users/alexwright/Documents/MSc Files/Project/game_roms");

            string nestest = @"nestest.nes";

            //CSV log - file creation
            DirectoryInfo csvLogs = new DirectoryInfo(@"/Users/alexwright/Documents/MSc Files/Project/csv_logs");
            DateTime dateTime = new DateTime();
            CultureInfo cultInfo = CultureInfo.CreateSpecificCulture("en-UK");
            string csvFileName = $"project_nes_nestest_log_{dateTime.ToString("s", cultInfo)}.csv";
            string filePath = $"{csvLogs.FullName}/{csvFileName}";
            using (File.Create(filePath)) { }
  

            Bus bus = new Bus();
            CPU cpu = new CPU();
            Cartridge cartridge = new Cartridge(nestest, testRoms);
            bus.InsertCartridge(cartridge);
            cpu.ConnectBus(bus);
            cpu.Reset();
            cpu.PC = 0xC000;
            while (true)
            {
                cpu.Clock();
            }


        }
    }
}
