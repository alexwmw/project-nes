using System;
namespace project_nes
{
    public interface iCPU
    {

        void Reset();
        void Irq();
        void Nmi();
        void Clock();
        void ConnectBus(iBus bus);
        void PowerOn();
    }
}
