//# define DisplayPatternMemory
//# define force_nestest_all_tests
# define GameLoop

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace project_nes
{
    class Program
    {
        static void Main(string[] args)
        {
            uint systemClock = 0;

            //ROM directories
            DirectoryInfo macRoms = new DirectoryInfo(@"/Users/alexwright/Documents/MSc Files/Project/mac_roms/");
            DirectoryInfo winRoms = new DirectoryInfo(@"C:\Users\Alex\Documents\MSc Files\Project\win_roms\");
            DirectoryInfo dirForCurrentOS = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? winRoms : macRoms;

            //File names
            string nestest = @"nestest.nes";
            string dk = @"Donkey Kong (World) (Rev A).nes";

            //File to open
            string selectedFile = dk;
            string fileName = args.Length == 0 ? dirForCurrentOS.FullName + selectedFile : args[0];


            // Emulator component objects
            Cartridge cartridge = new Cartridge(fileName);
            CpuBus cpuBus = new CpuBus();
            PpuBus ppuBus = new PpuBus();
            CPU cpu = new CPU();
            PPU ppu = new PPU();

            // IO device setup         
            uint displayScale = 2;
            uint winW = 256;
            uint winH = 240;
            IODevice IODevice = new IODevice(winW, winH, displayScale);
            IODevice.AddKeyPressEvent(Events.KeyEvent);
            IODevice.AddClosedEvent(Events.OnClose);

            // Emulator connections
            cpuBus.ConnectPPU(ppu);
            cpuBus.InsertCartridge(cartridge);
            ppuBus.ConnectCartridge(cartridge);
            ppu.ConnectBus(ppuBus);
            cpu.ConnectBus(cpuBus);
            ppu.ConnectIO(IODevice);
            ppu.SetPalette(Palette.DefaultPalette);

            // Emulator init procedure
            cpu.Reset();
#if force_nestest_all_tests
            cpu.PC = 0xC000;
#endif

#if DisplayPatternMemory

            displayScale = 4;
            winW = 16 * 8;
            winH = 16 * 8;
            IODevice screen = new IODevice(winW, winH, displayScale);
            screen.AddKeyPressEvent(Events.KeyEvent);
            screen.AddClosedEvent(Events.OnClose);

            ppu.GetPatternTable(1, 1);
            ppu.GetPatternTable(0, 1);

            for (int i = 0; i < (winW); i++)
            {
                for (int j = 0; j < (winH); j++)
                {
                    screen.SetPixel(i, j, ppu.PatternMemory[0, i, j]); 
                }
            }

            while(screen.WindowIsOpen)
            {
            
                screen.Clear();
                screen.DispatchEvents();
                screen.DrawToWindow(); 
                screen.Display();
            }
#endif

#if GameLoop

            // Emulation loop
            void LoopEmulator()
            {
                ppu.Clock();
                if (systemClock % 3 == 0)
                {
                    cpu.Clock();
                }
                if (ppu.Nmi)
                {
                    ppu.Nmi = false;
                    cpu.Nmi();
                }
                systemClock++;
            }

            // Screen Loop
            while (IODevice.WindowIsOpen)
            {
                IODevice.DispatchEvents();
                LoopEmulator();
                if (ppu.FrameComplete)
                {
                    ppu.FrameComplete = false;
                    IODevice.DrawToWindow();
                    IODevice.Display();
                }
            }
#endif



        }
    }
}
