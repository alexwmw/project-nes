using System.IO;
using SFML.Graphics;
using System.Runtime.InteropServices;
namespace project_nes
{
    class Program
    {
        static void Main(string[] args)
        {
            uint systemClock = 0;

            uint displayScale = 4;
            uint winW = 16 * 8;
            uint winH = 16 * 8;


            //ROM directories
            DirectoryInfo macRoms = new DirectoryInfo(@"/Users/alexwright/Documents/MSc Files/Project/mac_roms/");
            DirectoryInfo winRoms = new DirectoryInfo(@"C:\Users\Alex\Documents\MSc Files\Project\win_roms\");
            DirectoryInfo dirForCurrentOS =
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? winRoms : macRoms;

            //File names
            string nestest = @"nestest.nes";
            string dk = @"Donkey Kong (World) (Rev A).nes";
            string kir = @"Kirby's Adventure (USA) (Rev A).nes";

            //File to open
            string fileName = dk;


            // Emulator component objects
            Cartridge cartridge = args.Length > 0 ? new Cartridge(args[0]) : new Cartridge(fileName, dirForCurrentOS);
            CpuBus cpuBus = new CpuBus();
            PpuBus ppuBus = new PpuBus();
            CPU cpu = new CPU();
            PPU ppu = new PPU();


            // IO device setup
            VirtualScreen screen = new VirtualScreen(winW, winH, displayScale);
            screen.AddKeyPressEvent(KeyEvents.KeyEvent);

            // Emulator connections
            cpuBus.ConnectPPU(ppu);
            cpuBus.InsertCartridge(cartridge);
            ppuBus.ConnectCartridge(cartridge);
            ppu.ConnectBus(ppuBus);
            cpu.ConnectBus(cpuBus);

            // Emulator init procedure
            cpu.Reset();
            // cpu.PC = 0xC000;  // Force nestest all tests


            /* * * * * * * * * * * * * * * * * * * * * */
            ppu.GetPatternTable(1, 1);
            ppu.GetPatternTable(0, 1);

            for (int i = 0; i < (winW); i++)
            {
                for (int j = 0; j < (winH); j++)
                {
                    screen.SetPixel(i, j, ppu.PatternMemory[1, i, j]); 
                }
            }
            /* * * * * * * * * * * * * * * * * * * * * */


            // Loop(s)

            while (screen.IsOpen)
            {
                // clear the window with black color
                screen.Clear(Color.Black);

                // draw everything here...
                screen.DispatchEvents();
                screen.DrawToWindow();
                // end the current frame
                screen.Display();
            }


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
