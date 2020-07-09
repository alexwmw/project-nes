using System;

namespace project_nes
{
    class Program
    {
        static void Main(string[] args)
        {
            iBus bus = new Bus();

            iCPU cpu = new CPU();

            cpu.ConnectBus(bus);




        }
    }
}
