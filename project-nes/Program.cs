using System;
using System.IO;
using HelperMethods;
using SFML.Window;
using SFML.System;
using SFML.Graphics;
using System.Threading;

namespace project_nes
{
    class Program
    {
        static void Main(string[] args)
        {
            uint systemClock = 0;
            uint scale = 2;
            uint winW = 16 * 8;
            uint winH = 16 * 8;
            uint winWSc = winW * scale;
            uint winHSc = winH * scale;


            //ROM directories
            DirectoryInfo testRoms = new DirectoryInfo(@"/Users/alexwright/Documents/MSc Files/Project/test_roms");
            DirectoryInfo gameRoms = new DirectoryInfo(@"/Users/alexwright/Documents/MSc Files/Project/game_roms");
            DirectoryInfo winRoms = new DirectoryInfo(@"C:\Users\Alex\Documents\MSc Files\Project\win_roms");

            string nestest = @"nestest.nes";
            string dk = @"Donkey Kong (World) (Rev A).nes";

            // Component objects
            CpuBus cpuBus = new CpuBus();
            PpuBus ppuBus = new PpuBus();
            CPU cpu = new CPU();
            PPU ppu = new PPU();
            Cartridge cartridge = new Cartridge(dk, winRoms);
            VirtualScreen screen = new VirtualScreen(winWSc, winHSc, scale, Color.Yellow);
            RenderWindow window = new RenderWindow(new VideoMode(winWSc, winHSc), "Title");

            // Connections
            cpuBus.ConnectPPU(ppu);
            cpuBus.InsertCartridge(cartridge);
            ppuBus.ConnectCartridge(cartridge);
            ppu.ConnectBus(ppuBus);
            cpu.ConnectBus(cpuBus);

            // Init procedure
            cpu.Reset();
            // cpu.PC = 0xC000;  // Force nestest all tests


            
            ppu.GetPatternTable(1, 1);

            for (int i = 0; i < (winW); i++)
            {
                for (int j = 0; j < (winH); j++)
                {
                    screen.SetPixel(i, j, ppu.PatternMemory[1, i, j]);
                    //Console.Write(ppu.PatternMemory[1, i, j]);
                    //screen.SetPixel(j,i, Color.Cyan);   
                }
            }

            
            while (window.IsOpen)
            {
                // clear the window with black color
                window.Clear(Color.Black);

                // draw everything here...
                // window.draw(...);
                window.Draw(screen);
                // end the current frame
                window.Display();
            }


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
