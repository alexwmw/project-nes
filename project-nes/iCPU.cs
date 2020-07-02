using System;
namespace project_nes
{
    public interface iCPU
    {
        byte A { get; set; }        // Accumulator
        byte X { get; set; }        // X register
        byte Y { get; set; }        // Y register
        byte Stkp { get; set; }     // Stack pointer
        ushort Pc { get; set; }     // Program counter
        byte Status { get; set; }   // Status byte

        void Reset();
        void Irq();
        void Nmi();
        void Clock();
        void ConnectBus(iBus bus);
        byte TestOpCode(string opcode);
        byte TestAddrMode(string addrmode);
    }
}
