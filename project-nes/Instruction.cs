using System;
namespace project_nes
{
    public struct Instruction
    {
        public Instruction(string name, Func<byte> op, Func<byte> addrm, int cycles)
        {
            Name = name;
            Operation = op;
            AddrMode = addrm;
            Cycles = cycles;
        }

        public string Name { get; }

        public Func<byte> Operation { get; }

        public Func<byte> AddrMode { get; }

        public int Cycles { get; }
    }
}
