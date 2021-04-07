# ProjectNES: Emulating the Nintendo Entertainment System 
### MSc Computer Science Project Report Highlights

## Abstract
Computer emulation has been used increasing in video game-based entertainment over the past decades, with a number of obsolete gaming systems re-emerging in the marketplace as emulators. One of the most popular consoles of the 1980s was the 8-bit Nintendo Entertainment System (NES). This paper describes the development of ProjectNES, an emulator for the NES written in the C# programming language. ProjectNES is a low-level emulator that re-implements the hardware architecture of the NES, including a MOS Technology 6502 CPU and an implementation of the sophisticated, proprietary Ricoh 2C02 Picture Processing Unit (PPU). The result is a largely successful emulator with a fully functioning CPU and a PPU at an advanced stage of development. 

## Introduction
The Nintendo Entertainment System (NES), released in 1985, is one of the most famous pieces of computer hardware of all time. At its height, the market for NES software exceeded the market for all other home computer software [1]. It spawned many of the present day’s most famous game franchises, and to this day is one of the top ten highest selling home video games consoles. 
Many factors contributed to the popularity of the NES, including its well thought-out design. The machine features a MOS Technology 6502 processor; a mainstay of the age, making the device familiar for programmers. Its graphics processor had an intelligent design, maximising efficiency of memory and allowing programmers to take full advantage of the hardware.
This project report details the development of ProjectNES: an emulator for the NES. It aims to model the system in software and produce a desktop application capable of running NES programs. The result is a project with great attention paid to the NES hardware architecture, leading to some fascinating insights about the machine, and some difficult challenges faced in implementing what is in places a deceptively complex design.

## Specification & Design
### Objectives
The overall objective of this project is quite straightforward. Emulation software, as defined above, is software that imitates or reproduces the effects of another system. In this case the target system is the NES as described above. However, the simplicity of this objective does not reveal much of the challenge involved, nor does it give much indication of what might be considered a success or a partial success.
With this in mind, four statements or ‘wishes’ are given below which provide a clearer indication of the expectations for this software: 

_“The emulator should have a fully functional CPU.”_

At the heart of the emulator there should be an emulation of the 6502 processor, with a fully implemented instruction set. This CPU should produce the same output as a real 6502 for any given input.

_“The emulator should be able to read and run original NES software.”_

Obviously, a software emulator cannot read from proprietary ‘hard’ storage such as a physical games cartridge without some form of hardware interface. However, the emulator should be able to execute, in binary form, programs written for the NES. In this case, program files with be in the iNES format describe in Section 3.3.

_“The emulator should be visual and interactive.”_

Conceivably, a NES emulator could be fully functional and able to run software without any means of displaying its output – much like a real computer console not attached to any television or monitor. Indeed, some emulators are designed this way such as those that target the Libretro API [19]. However, for this project the emulator should have a self-contained means of displaying graphical output and taking user input.

_“The emulator should be able to render graphical output that visually identical to a real NES.”_

The ultimate and most challenging objective of this project is to produce output such that a typical user would not be able to distinguish it from that of a real NES console, or a known good emulator. The picture should be identical in its layout and timing, and not introduce any additional bugs or glitches. A caveat of this is that it was not expected that there would be sufficient time to implement both the background and foreground layers of the image. However, this objective should still hold for whatever is displayed.

### Software Framework and Language
ProjectNES was developed in the .NET Core software framework. It was written in the C# programming language.
While most emulators of this kind are written in C++, C# had the advantage of being familiar to the programmer and was deemed to have all of the relevant features for a project of this sort; that it is object oriented, suitable for desktop applications and offers sufficient performance. Furthermore, its support for first-class functions (not supported in Java, for example) offered additional flexibility when developing the instruction set and execution cycle.

### Third Party Libraries
A third party library was used for graphics processing, allowing ProjectNES’s graphical output to be visualised. Two such libraries were used during development. These were Simple DirectMedia Layer (SDL) [23], and Simple and Fast Multimedia Library (SFML) [24]. Both are developed in C or C++ but have C# bindings. Both were experimented with, but ultimately SFML was found to be easier to use. 


Implementation
5.6	Emulator Class
An Emulator class holds the Main method that is the entry point into the application. Here, each component class is instantiated, and basic parameters are set:

000	            // Emulator component objects

001	            Cartridge cartridge = new Cartridge(fileName);

002	            CpuBus cpuBus = new CpuBus();

003	            PpuBus ppuBus = new PpuBus();

004	            CPU cpu = new CPU();

005	            PPU ppu = new PPU(cpu);

006	

007	            // IO device setup         

008	            uint displayScale = 2;

009	            uint window_W = 256;

010	            uint window_H = 240;

011	            IODevice IODevice = new IODevice(window_W, window_H, displayScale);

012	            IODevice.AddKeyPressEvent(Events.KeyEvent);

013	            IODevice.AddClosedEvent(Events.OnClose);

014	

015	            // Emulator connections

016	            cpuBus.ConnectPPU(ppu);

017	            cpuBus.ConnectCartridge(cartridge);

018	            ppuBus.ConnectCartridge(cartridge);

019	            ppu.ConnectBus(ppuBus);

020	            cpu.ConnectBus(cpuBus);

021	            ppu.ConnectIO(IODevice);

022	            ppu.SetPalette(Palette.DefaultPalette);

023	

024	            // Emulator init procedure

025	            cpu.Reset();

026	            ppu.Reset();



A systemClock int in Main is the driving clock behind the system. An emulation loop in Main is called continually. In this loop, PPU.Clock is called on each clock tick and the CPU.Clock is called on every third tick. Non-maskable Interrupt (NMI) signals sent by the PPU are also received by the CPU here.
 
A screen loop encompasses the emulation loop. In this loop the window of the IODevice is continually refreshed. A boolean FrameComplete property in the PPU is read and, if true, the contents of the screen are updated:

027	            // Emulation loop

028	            void LoopEmulator()

029	            {

030	                ppu.Clock();

031	                if (systemClock % 3 == 0)

032	                {

033	                    cpu.Clock();

034	                }

035	                if (ppu.Nmi)

036	                {

037	                    ppu.Nmi = false;

038	                    cpu.Nmi();

039	                }

040	                systemClock++;

041	            }

042	

043	            // Screen Loop

044	            while (IODevice.WindowIsOpen)

045	            {

046	                IODevice.DispatchEvents();

047	                LoopEmulator();

048	                if (ppu.FrameComplete)

049	                {

050	                    ppu.FrameComplete = false;

051	                    IODevice.DrawToWindow();

052	                    IODevice.Display();

053	                }

054	            }



5.7	CPU & CPU Bus

5.7.1	Basic Infrastructure

Table 6.1. Registers of the 6502 Processor
Name	Size	Implementation
Program Counter (PC)	16 bits	ushort
Accumulator (A)	8 bits	byte
Index Registers (X and Y)	8 bits each	byte
Stack Pointer (stkp)	8 bits	byte
Processor Status Flags (status)
	Carry Flag (C)
	Zero Flag (Z)
	Interrupt Disable (I)
	Decimal Mode (D)
	Overflow Flag (V)
	Negative Flag (N)	8 bits 
(2 bits unused)	byte

Each of the CPU’s six registers (Table 6.1) were implemented as private byte (unsigned 8-bit) or ushort (unsigned 16-bit) integer fields inside the CPU class. An enum labelled Flags containing eight integer values, each a ‘1’ left-shifted by n, represented the eight flags of the status register:

055	        private enum Flags

056	        {

057	            C = 1 << 0, // Carry Bit

058	            Z = 1 << 1, // Zero

059	            I = 1 << 2, // Disable Interrupts

060	            D = 1 << 3, // Decimal Mode

061	            B = 1 << 4, // Break

062	            U = 1 << 5, // Unused

063	            V = 1 << 6, // Overflow

064	            N = 1 << 7, // Negative

065	        };





GetFlag and SetFlag methods were used to return or set any given flag in the status register:

066	     	 private byte GetFlag(Flags f)

067	            => (byte)(status & (byte)f);

068	

069	        private void SetFlags(Flags f, bool b)

070	        {

071	            if (b)

072	                status |= (byte)f;

073	            else

074	                status &= (byte)~f;

075	        }



Several member variables were created to assist with the execution of instructions. These were: opcode, the current instruction byte; an index 
into the instruction set; instruction, the decoded instruction (see Instruction Class below); cycles, the number of cycles required for the current instruction; address, a ushort to store a fetched address operand; data, a byte to store a fetched data operand; and branch, a byte to store a fetched branch operand.
A Fetch method was created in order to fetch data as required by reading CpuBus at a given address, using the ReadBus method. To accommodate the implicit/accumulator addressing modes, an if statement predicated on the current addressing mode would instead load data with the value of the accumulator A, i.e.:

076	  	 private void Fetch()

077	        {

078	            if (CurrentModeImplicit())

079	                data = A;

080	            else

081	                data = ReadBus(address);

082	        }



5.7.2	Instruction Set 

6502 Instructions

The 6502 instruction set is composed of 151 instructions, consisting of 56 operations across thirteen addressing modes. Each instruction takes a 
variable number of cycles – at least two and at most seven. The opcode matrix in Table 6.2 details each combination of operation, addressing mode 
and required number of cycles. The x,y index of each instruction in this F by F (16 by 16 in decimal) table when concatenated produces the one-
byte opcode of that instruction.

Instructions vary in length between 1 and 3 bytes, with the length dependent on the addressing mode. Some instructions support only one addressing mode, whereas others support several. Most of these addressing modes are familiar, including implicit, absolute, indirect, indexed, and relative modes, as well as the aforementioned zero-page modes.
In general, each byte read from or written to memory requires one cycle. However, there are a large number of cases in which additional clock cycles are required. In fact, no instruction takes a single clock cycle; in hardware, single-byte instructions waste a cycle reading and ignoring the byte that comes immediately after. 
Instructions requiring additional cycles include where an indexed addressing mode crosses a page boundary, branch instructions that actually branch, and instructions that pull data from the stack (in order to increment the stack pointer).

Instruction Class

The Instruction type (nested in CPU) has three properties: two function delegates, Operation and AddrMode; and an integer value Cycles. The 
cycles value per instruction is taken from the opcode matrix in Table 6.2. Blank spaces in the opcode matrix were filled with an ‘unknown’ (UNK) 
operation equivalent to NOP.

To facilitate instructions where the number of cycles varies, the Operation and AddrMode methods each return a boolean value to indicate whether 
an additional clock cycle may be required. 

Table 6.2. 6502 Opcode Matrix in [25]



InstructionSet Class

A nested class InstructionSet implements the IEnumerable interface [26] and holds a list of type Instruction. This implementation allows 
instructions to be assigned to the set using an initializer list: a way of initiating objects in a declarative manner without explicitly invoking 
a constructor for the type [27]:

083	        instructionSet = new InstructionSet()

084	        {

085	            {BRK,Imm,7}, {ORA,InX,6},{UNK,Imp,2},{UNK,Imp,8},

086	            //...so on for 256 instructions...

087	            {UNK,Imp,7}

088	        };



This implementation of the instruction set can then be indexed using the currently held opcode, much like the x,y index of the opcode matrix in 
Table 6.2. E.g. instructionSet[0xA2] will return an LDX instruction.

Addressing Modes

Thirteen addressing modes were implemented as methods within the CPU class. Some are very simplistic, such as the implicit modes Imm and Acc, 
which simply return false. 

An example of a more complex instruction and one which potentially requires an additional cycle is Indirect Indexed (InY):

089		 //Indirect Indexed  (d),y   Cycles: 5+

090	        private bool InY()

091	        {

092	            	byte temp = ReadBus(PC);

093			PC++;

094	            	byte lowByte = ReadBus(temp & 0x00FF);

095	            	byte highByte = ReadBus(temp + 1 & 0x00FF);

096	            	address = Address16Bit(lowByte, highByte);

097	            	address += Y;

098	            	return address.GetPage() != highByte;

099	        }



This indirect method reads two bytes, at PC and PC + 1. These bytes are then concatenated to form an address, which is assigned to the address 
variable. In lines 094 and 095 we can see examples of masking: the value in temp is ANDed with the mask $00FF to produce a ushort with eight 
leading zeros.

Address16Bit and GetPage are helper methods. Address16Bit produces an address from a low byte and a high byte by left-shifting the high byte 
eight places and ORing it with the low byte.



GetPage is an extension method that reverses this process to obtain the high byte of an address, by masking a given address with 0xFF00 and 
right-shifting it eight positions.

Finally, the value of the Y register is added to the address. In line 098 a value of true is returned if the page of the resulting address has 
incremented following the addition of Y; i.e., if a page boundary has been crossed.

The 6502 has several hardware bugs, and one of the most prominent is a bug in indirect jump (JMP). If a page boundary is crossed between the 
address of a low address byte and that of a high address byte (i.e. where the low byte’s address a is $xxFF), the CPU fails to target the high 
byte in a + 1, and instead targets address a - $FF.

This bug is implemented in the Indirect (Ind) addressing mode. JMP is the only operation to use this mode:

100	        //Indirect          

101	        private bool Ind()

102	        {

103	            //get address start location in two reads

104	            byte lowByte = ReadBus(PC++);

105	            byte highByte = ReadBus(PC++);

106	            ushort location = Address16Bit(lowByte, highByte);

107	            //construct address

108	            byte addrLo = ReadBus(location);

109	            byte addrhi = lowByte == 0x00FF

110	                ? //simulate hardware bug

111	                Readbus(location & 0xFF00)

112	                : //otherwise, correct behaviour

113	                ReadBus(location + 1);

114	            address = ReadBus(addrLo, addrHi)

115	            return false;

116	        } 



Operations

56 operations were implemented as methods in the CPU class. Much like addressing modes, these methods ranged in their complexity and each return 
a boolean true if they may require an additional clock cycle. There is little use in covering every example here, as most are quite basic. Two 
examples are presented below.

Increment Memory (INC) is a fairly typical example. It adds one to a value held in a specified memory location. First, the Fetch method is 
called. This sets the data byte by reading from the address in PC. In the following line, the WriteBus method is called. The address variable has 
already been updated according to the addressing mode of the current instruction. The fetched data plus-one is written. Then, the Z and N flags 
are set depending on the result. This operation never requires an additional cycle, so false is returned:

117	        private bool INC()

118	        {

119	            Fetch();

120	            WriteBus(address, (byte)(data + 1));

121	            SetFlags(

122	                Flags.Z, ReadBus(address).IsZero(),

123	                Flags.N, ReadBus(address).IsNegative());

124	            return false;

125	        }



Add with Carry (ADC) and Subtract with Carry (SBC) are perhaps the most complicated operations. These operate with signed integers and must be 
able to identify signed overflow. Because signed numbers are stored in two’s compliment, negative numbers can be identified if the most 
significant bit (MSB; in this case, bit 7) is set. The N flag is often updated to reflect a potentially negative result. ADC is examined below. 

126	 private bool ADC()

127	 {

128	     Fetch();

129	     ushort result = (ushort)(A + data + GetFlag(Flags.C));

130	     bool isSignedOverflow = ((A ^ result) & ~(A ^ result) & 0x0080) > 0;

131	     SetFlags(

132	         Flags.C, (ushort)(result & 0x0100) > 0,

133	         Flags.Z, ((byte)(result & 0x00FF)).IsZero(),

134	         Flags.V, isSignedOverflow,

135	         Flags.N, temp.IsNegative());

136	     A = (byte)(result & 0x00FF);

137	     return true;

138	 }



Table 6.3 Truth Table for A, Data and Result, Indicating Overflow

Most significant bit of:	Has overflow occurred?	A^R	~(A^Data)

A	Data	Result			

0	0	0 	No	0	1

0	0	1	Yes	1	1

0	1	0	No	0	0

0	1	1	No	1	0

1	0	0	No	1	0

1	0	1	No	0	0

1	1	0	Yes	1	1

1	1	1	No	0	1



As before, the Fetch method updates the data variable. In line 129, the value in A is added with the fetched data and the carry bit (C) of the 
status register. The carry bit allows chaining of addition operations. The result is stored in a temporary ushort variable. Note that at this 
point the value may be larger than the accumulator’s eight bits.

Then, in line 130 a check is performed to identify signed overflow. Overflow has occurred if two positive numbers are added and a negative 
result is produced, or vice versa. 

Table 6.3 examines the truth table for the sum of A and data. This shows how line 130 is constructed. 

The truth table demonstrates that overflow has occurred whenever A XORed with result is true, and when A XORed with data is NOT true:

(A ^ result) & ~(A ^ data)

If the MSB of this expression (i.e. its output masked with $80) is 1, overflow has occurred:

bool isSignedOverflow = ((A ^ result) & ~(A ^ data) & 0x0080) > 0;

The V flag can then be set accordingly in line 134. If the result exceeds 8 bits, the carry bit is set. Finally, in line 136 the result is 
masked with $00FF and stored in A; and the method returns true to indicate an additional cycle may be required.

5.7.3	Instruction Execution

Instruction execution occurs within the Clock method. This method is called continually from the main loop. However, instructions are not 
executed on every cycle. This is one way in which this emulator fundamentally differs from the behaviour of the NES.

In real hardware, instruction execution occurs over a number of stages in which an instruction is fetched, decoded and executed. Some 
instructions may require additional stages, such as indirect address fetches. Instructions are driven through these stages by the system clock. 
In hardware, the timing of each stage must be very precise according to the very low-level design of the hardware circuitry.

However, for this emulation the precise circuitry-level timing is not important. What is important is that the instruction itself takes the 
required number of cycles according to actual NES behaviour. The reason that the cycle count is important is that clock-synchronisation with 
other components of the emulator, the PPU in particular, is essential to a successful emulation.

The CPU cycles variable is used to keep track of this. On each clock tick, if cycles is equal to zero, the following procedure occurs: the 
ReadBus function is called at the address in the program counter, which in turn calls the Read function of the CpuBus. The returned value is 
assigned to opcode, and PC is incremented. The opcode byte acts as an index into the InstructionSet array, and the Operation and AddrMode 
functions provided by the returned Instruction are executed. The instruction’s Cycles value is assigned to the CPU member cycles:

139	        public void Clock()

140	        {

141	            if (cycles == 0)

142	            {

143	                opcode = ReadBus(PC++);

144	                instruction = instructionSet[opcode];

145	                cycles += instruction.Cycles;

146	                bool addrm = instruction.AddrMode();

147	                bool operation = instruction.Operation();

148	                if (addrm & operation)

149	                    cycles++;

150	            }

151	            cycles--;

152	        }





Table 6.4. Interrupt Vector Locations

Interrupt	Vector

LSB	MSB

IRQ/BRK	$FFFE	$FFFF

NMI	$FFFA	$FFFB

RESET	$FFFC	$FFFD



On each clock cycle, cycles is decremented. While cycles is not equal to zero, no execution occurs. Clock cycles are simply counted down until 
the required number has passed. The boolean values produced by AddrMode and Operation, taken together, indicate whether an additional clock 
cycle is required. 

5.7.4	Interrupts

The 6502 supports three interrupts: reset, which reverts to system initialization; interrupt request (IRQ), which can be ignored depending on 
the interrupt disable (I) flag; and non-maskable interrupt (NMI). 

The last six bytes of system memory are reserved for the respective interrupt vectors (see Table 6.4). These must be programmed with the address 
of the routine to be called in the event of each interrupt

The two interrupt functions, Nmi and Irq, and the Reset function are quite similar in their implementation. Nmi and Irq both push the current PC 
to the stack. The stack address is the sum of $0100 and the stkp variable. Writing to the stack takes two writes. stkp is decremented each time. 
Several status flags are set, and then status is also pushed to the stack. 

The PC is then set to the appropriate interrupt vector. As with instructions, the appropriate number of cycles is added to the cycles variable.

For example:



153	        public void Irq()

154	        {

155	            if (GetFlag(Flags.I) == 0) //If interrupt disable is false

156	            {

157	                //Push PC to stack

158	                WriteBus((ushort)(0x0100 + stkp--), PC.GetPage());

159	                WriteBus((ushort)(0x0100 + stkp--), PC.GetOffset());

160	                //Clear B, set I, U; and push status to stack

161	                SetFlags(

162	                    Flags.B, false,

163	                    Flags.I, true,

164	                    Flags.U, true);

165	                WriteBus((ushort)(0x0100 + stkp--), status);

166	                //Load interrupt vector from $FFFE/F 

167	                PC = Address16Bit(Read(0xFFFE), Read(0xFFFF));

168	                cycles += 7;

169	            }

170	        }

5.7.5	CPU Bus 

The NES CPU has access to several devices via the CPU bus: 2 KB of system RAM, the PPU, the Audio Processing Unit (APU), the cartridge and an 
I/O device interface. Each of these is accessed via a fixed address range, as shown Table 6.5.

This mapping is implemented in the Read and Write methods, which are sensitive to each address range. For example, say CPU.ReadBus calls the 
CpuBus.Read method with the address $0808. This maps to the mirror of the 2 KB RAM. Inside the Read method, an if statement is sensitive to any 
address (mirrored or otherwise) that should return a value in the cpuRam object, i.e. in the range $0 - $1FFF. Inside the method, the address is ANDed with the 2 KB ($800 byte) cpuRam capacity to produce $0008 – a valid index into the array. 

Addresses in the cartridge and PPU ranges call cartridge.CpuRead and ppu.CpuRead methods in the respective classes.





Table 6.5. CPU Memory Map

Address range	Size	Device

$0000-$07FF	$0800	2 KB internal RAM

$0800-$0FFF	$0800	Mirrors of $0000-$07FF

$1000-$17FF	$0800	

$1800-$1FFF	$0800	

$2000-$2007	$0008	PPU registers

$2008-$3FFF	$1FF8	Mirrors of $2000-2007 

$4000-$4017	$0018	APU and I/O registers

$4018-$401F	$0008	APU and I/O functionality that is normally disabled

$4020-$FFFF	$BFE0	Cartridge space



5.8	Cartridge & Mappers

5.8.1	Cartridge

The Cartridge class represents the NES console’s removable storage. It stores the program to be run (PRG memory) and the graphical data that 
makes up the game’s sprites (CHR memory). ProjectNES allows any specified file to be run by entering its file path as a command line argument, 
which is then passed to the Cartridge via its constructor. However, for development purposes, test ROMs’ file paths were hard coded into the 
main sequence. 

Private to the cartridge are two byte arrays: prgRom and chrRom, representing PRG memory and CHR memory respectively. Any RAM capabilities would 
be added in future Mapper implementations (see Section 6.3.3). 

Also within the cartridge are references to a mapper (type IMapper), reader (type BinaryReader) and header (type Header). The BinaryReader class 
is part of .NET’s System.IO namespace, which takes a stream of binary data to be read byte-by-byte. The IMapper declaration, and Mapper0 and 
Header implementations are described below.

As the cartridge is accessible by both the CPU and PPU (each with independent memory maps), Cartridge contains both CpuRead and CpuWrite, and 
PpuRead and PpuWrite methods. Each of these methods calls a corresponding method in the mapper.

5.8.2	iNES Header

An iNES file consists of the following sections, in order:

1.	Header (16 bytes)

2.	Trainer, if present (0 or 512 bytes)

3.	PRG ROM data ( x * 16 KB)

4.	CHR ROM data, if present (y * 8 KB)

The iNES header is 16 bytes long, with each byte conveying information about the cartridge such as how many chunks of PRG and CHR memory are 
present, mapper configuration, mirroring configuration and so on. The header contents are summarised in Table 6.6.



Table 6.6. iNES Header Format

Byte(s)	Assignment

0 – 3	Constant $4E $45 $53 $1A (“NES” followed by MS-DOS end-of-file)

4	PRG ROM size in 16 KB units

5	CHR ROM size in 8 KB units

6	Flags 6: Mapper (lo-nibble), mirroring, battery present, trainer present

7	Flags 7: Mapper (hi-nibble), iNES format

8	Flags 8: PRG RAM size

9	Flags 9: TV system

10	Flags 10: TV system, PRG-RAM presence 

11 - 15	Unused padding





The Header class represents the iNES header. It has many public properties matching those given in Table 6.6. Its purpose is to read data from 
the specified file and processing each byte in accordance with the iNES header format. Header is passed a reference to reader via its 
constructor.

Header information is decoded via the header’s sole method, Parse, which is shown below. This function repeatedly reads a byte from the reader, 
and updates the header’s internal variables and properties:

171		     public void Parse()

172	            {

173	                // Bytes 0 - 3

174	                byte[] str = reader.ReadBytes(4);

175	                HeaderFormat = Encoding.UTF8.GetString(str);

176	                // Bytes 4 - 5

177	                p_rom_lsb = reader.ReadByte();

178	                c_rom_lsb = reader.ReadByte();

179	                // Byte 6

180	                flags6 = reader.ReadByte();

181	                nt_mirroring    = (flags6 & (1 << 0)) > 0;

182	                battery         = (flags6 & (1 << 1)) > 0;

183	                Trainer         = (flags6 & (1 << 2)) > 0;

184	                four_screen     = (flags6 & (1 << 3)) > 0;

185	                mapper_D0_D3    = (byte)((flags6 & 0xF0) >> 4);

186	                // Byte 7

187	                flags7 = reader.ReadByte();

188	                console_type    = (byte)(flags7 & 0x03);

189	                Nes_20          = (byte)(flags7 & 0x0C) == 0x08;

190	                mapper_D4_D7    = (byte)((flags7 & 0xF0) >> 4);

191	                // Byte 8

192	                temp            = reader.ReadByte();

193	                mapper_D8_D11   = (byte)((temp & 0x0F));

194	                submapper       = (byte)((temp & 0xF0) >> 4);

195	                // Byte 9

196	                temp = reader.ReadByte();

197	                p_rom_msb        = (byte)((temp & 0x0F));

198	                c_rom_msb        = (byte)((temp & 0xF0) >> 4);

199	                // Byte 10

200	                temp = reader.ReadByte();

201	                p_ram_size       = (byte)((temp & 0x0F));

202	                eeprom_size      = (byte)((temp & 0xF0) >> 4);

203	                // Byte 11

204	                c_ram_size       = reader.ReadByte();

205	                // Bytes 12 – 15 (padding – not stored)

206	                reader.ReadBytes(4);

207	                Mapper_id = (byte)((mapper_D4_D7 << 4) | mapper_D0_D3);

208	                Prg_banks = (byte)(p_rom_lsb | p_rom_msb << 4);

209	                Chr_banks = (byte)(p_rom_lsb | p_rom_msb << 4);

210	            }

5.8.3	ROM Objects

The header is instantiated and parsed within the Cartridge constructor. The header’s PrgBanks and ChrBanks properties are used in indicate the 
presence of and number of bytes that should be read into the prgRom and chrRom arrays, respectively. 

A 512 byte memory chunk called a trainer is occasionally present. This usually contains CHR-RAM caching and translation code. The trainer is not 
relevant to this emulation.

From the Cartridge constructor:

211	            // Read data from file into memory

212	            if (header.Trainer)

213			  // trainer is 512 bytes. Junk. Not stored

214	                reader.ReadBytes(0x200); 

215	            if (header.Prg_banks > 0)

216		         // PrgBanks = no. of 16kb banks to read

217	                int prgSize = header.Prg_banks * 0x4000

218	                prgRom = reader.ReadBytes(prgSize); 

219	            if (header.Chr_banks > 0)

220		         // ChrBanks = no. of 8kb banks to read

221	                int chrSize = header.Chr_banks * 0x2000

222	                chrRom = reader.ReadBytes(chrSize);   

5.8.4	Mappers

Mappers are represented by the IMapper interface:

223	    public interface IMapper

224	    {

225	        public byte PrgBanks { set; }

226	        public byte ChrBanks { set; }

227	        public byte[] PrgRom { set; }

228	        public byte[] ChrRom { set; }

229	

230	        public byte CpuRead(ushort adr);


231	        public void CpuWrite(ushort adr, byte data);

232	        public byte PpuRead(ushort adr);

233	        public void PpuWrite(ushort adr, byte data);

234	    }



The use of an interface here thus ensures any mapper class conforming to the interface can be assigned to the cartridge’s mapper variable. 
Individual class implementations of the IMapper interface may introduce various bank-switching functions and even add additional memory objects. 

The following lines in the Cartridge constructor show how a mapper of type MapperN is instantiated, where N is the header’s Mapper_id property:

235	            Type t = Type.GetType($"project_nes.Mapper{header.Mapper_id}");

236	            mapper = (IMapper)Activator.CreateInstance(t);

237	

238	            mapper.PrgBanks = header.PrgBanks; //int value

239	            mapper.ChrBanks = header.ChrBanks; //int value

240	            mapper.PrgRom = prgRom;  //array reference

241	            mapper.ChrRom = chrRom;  //array reference



The most basic mapper circuitry used in lots of early NES games is Mapper 0 (aka NROM) [28]. These games can be said to have “no mapper”; they 
have no bank-switching function and are limited to 32 KB of PRG-ROM and 8 KB of CHR-ROM. 

Mapper 0 is the only mapper to have so far been implemented in this project, in a Mapper0 class. This is according to the project proposal, 
which placed additional mappers outside of the scope of this project.

5.9	PPU & PPU Bus

5.9.1	PPU Registers

On the NES, the CPU communicates with the PPU via eight 8-bit registers. Unlike the registers of the CPU, in many cases the act of reading or 
writing to these PPU registers will trigger specific actions within the PPU. The PPU registers and associated actions are summarised in Table 
6.7.

Transfer of data from CPU-mapped to PPU-mapped locations occurs via the PPUADDR and PPUDATA registers. Because PPUADDR is 8-bit and VRAM is 2 
KB, two successive CPU writes are required to write addresses to the PPU. An address latch is employed to keep track of this. To reduce the 
number of address writes required by the CPU, PPUADDR is auto-incremented after each PPUDATA read.

Each of the registers listed above is implemented as a single byte variable inside the PPU class in much the same way as the CPU registers. 
However, unlike the CPU registers, in order to facilitate the various actions described in Table 6.7, each PPU register is accessed via private 
getters and setters . These getter and setter methods are in turn accessed via a switch statement (for each CPU-mapped address in Table 6.7) 
inside the PPU’s CpuRead and CpuWrite methods. 

An example of a setter is the SetPpuAddress method, which writes the high byte of an address if a member variable latch is zero, and writes the 
low byte if latch is 1. latch is set or unset on each successive write.

A particular quirk of reading from the PPUDATA register in hardware is that the read will source its data differently depending upon the address 
being accessed. When reading data in address range 0-$3FFF, the contents of an internal read buffer are returned; whereas when reading in 
address range $3F00-$3FFF (i.e. palette data), data is placed immediately on the bus and the buffer is updated afterwards.

The GetPpuData method facilitates this with a temporary variable to store the previous ppuData value before updating ppuData  with a read at the 
PPU bus:

242	        private byte GetPpuData()

243	        {

244	            // store ppuData that was set during the last cycle

245	            byte ppuDataDelayed = ppuData;

246	

247	            // update ppuData during this cycle

248	            ppuData = ReadBus(vreg.Address);

249	

250		     //Store the address for the return expression before incrementing

251	            ushort tempAddress = vreg.Address;

252	            vreg.Address += (ushort)(GetFlag(CtrlFlags.increment_mode) ? 32 : 1);

253	

254	            // If palette location, return current, else return previous

255	            return tempAddress >= 0x3F00 ? ppuData : ppuDataDelayedBy1;

256	        }





Table 6.7. CPU-Mapped Registers of the RP2C02 PPU

Address	Description, flags	Action on Read	Action on Write

PPUCTRL

$2000	Various flags controlling PPU operation

NN 	Base name table address; most significant bits of scrolling coordinates x,y 

I	RAM address increment

S	Foreground pattern table address

B	Background pattern table address

H	Sprite size

P	PPU master/slave select

V	Generate an NMI at the start of vertical blanking	N/A	None

PPUMASK

$2001	Controls the rendering of sprites and backgrounds

G	Greyscale

m	Show background (1)

M	Show foreground (1)

b	Show background (2)

s	Show foreground (2)

R	Emphasise red

G	Emphasise green

B	Emphasise blue

N/A	None

PPUSTATUS

$2002	Reflects the state of various functions inside the PPU

O	Sprite overflow

S	Sprite zero hit

V	Vertical blank has started	Address latch is set to 0;

V bit is set to 0

N/A

OAMADDR

$2003	OAM address that is to be accessed	N/A	None



OAMDATA

$2004	Data to be written to the OAM	None	None

SCROLL

$2005	Used to change the scroll position of the rendered screen; an offset into the name table selected in PPUCTRL. 

Requires two writes.	None	Toggle address latch

PPUADDR

$2006	VRAM address that is to be accessed. 

Requires two writes	N/A	Toggle address latch

PPUDATA

$2007	VRAM read/write data register

Increment PPUADDR;

Update internal read buffer	Increment PPUADDR



The auto-increment on the address can be seen in line 252. If the increment mode flag of the control register is set to indicate vertical mode, 
the address increments 32 instead of 1. The effect of this is to skip an entire row of tiles in the name table.

Flags

enums for the status, mask and control register were implemented in a similar fashion to the CPU’s status register, with equivalent GetFlag and 
SetFlag methods overloaded per register. An example of GetFlag is seen in line 252.

5.9.2	PpuBus

Any time that the PPU reads or writes to its memory-mapped devices (VRAM aka name tables, the cartridge aka pattern tables, or palette memory), 
it does so via the PPU bus. In this implementation, the PPU’s ReadBus and WriteBus methods call the Read and Write methods of PpuBus.

A two-dimensional array nameTables, a one-dimensional array paletteRam, and a Cartridge reference are held within PpuBus. The PpuBus’s Read and 
Write methods are responsible for mapping the incoming address onto each device. The full PPU memory map is given in Table 6.8.



Table 6.8. PPU Memory Map 

Address range	Size	Description	Device	Image Layer

$0000-$0FFF	$1000	Pattern Table 0	Cartridge (CHR)	Background (typically)

$1000-$1FFF	$1000	Pattern Table 1	Cartridge (CHR)	Foreground (typically)

$2000-$23FF	$0400	Name Table 0	VRAM	Background

$2400-$27FF	$0400	Name Table 1	VRAM	Background

$2800-$2BFF	$0400	Name Table 2	VRAM	Background

$2C00-$2FFF	$0400	Name Table 3	VRAM	Background

$3000-$3EFF	$0F00	Mirrors of $2000-$2EFF	VRAM	Background

$3F00-$3F1F	$0020	Palette Indexes 	PPU (Palette RAM)	Both

$3F20-$3FFF	$00E0	Mirrors of $3F00-$3F1F	PPU (Palette RAM)	Both



5.9.3	Pattern Tables Configuration

A game’s sprite tiles are stored on the cartridge in CHR memory and are arranged in two adjacent tables known as pattern tables. Typically, one 
table holds background sprites and the other foreground sprites. Each pattern table holds a grid of 16 x 16 sprites and is in effect 128 x 128 
pixels in size.

Each tile in the table is defined by two bit planes. The combination of each bit in the two bit planes produces an index into a selected four-
colour palette. The overall configuration of the pattern table can be inferred from the pattern table address configuration illustrated in 
Figure 6.1.



Figure 6.1. Pattern Table Address Configuration

As pattern tables reside with CHR memory of the cartridge, the PpuBus Read and Write methods, when receiving addresses in the range $0000-$1FFF, 
call the PpuRead and PpuWrite methods of the Cartridge.

5.9.4	Name Table Configuration

A name table is a 1024 byte area of memory which holds layouts for the background layer. On the NES, two name tables are held adjacently in the 
2 KB VRAM. Typically, one entire name table is used to represent the visible screen, and the adjacent name table is used to represent an 
adjacent, unseen area of the screen. This is done to facilitate scrolling.

Each name table has a height and width of 256 pixels, or 32 tiles. However, notice that this is greater than the NES’s screen height of 240. 
While the first 240 rows of the table hold tile layout information, the bottom 16 rows store an attribute table. Each attribute table is 64 
bytes in size, and controls which of four background palettes are assigned to each portion of the screen. 

Figure 6.2 illustrates the configuration of the name table, and how each byte of the attribute table is used to assign palettes to different 
regions. Each byte in the attribute table is assigned to a 4 x 4 tile section of the screen (0 – 3F). Because there are only four available 
palettes, only two bits are required for each palette index. Therefore, each attribute byte can hold four palette indexes. This means that each 
4 x 4 tile section can be further split into four 2 x 2 sections (A, B, C, D), each with a different two-bit palette index. 

The name tables are implemented inside the PpuBus class as a two dimensional array nameTables[2,0x400], which are accessed via the Read and 
Write methods at addresses $2000-$2FFF. Name tables are populated by the CPU during execution, with each byte written using the PPUADDR and 
PPUDATA registers.

5.9.5	Name Table Mirroring

While there is only physical space in VRAM for two name tables, the NES’s PPU is able to address four conceptual tables in a 2 x 2 pattern. In 
actuality, two of these tables are mirrors: their addresses map to identical locations within the true name tables. 

Mirroring configuration is dictated by the cartridge can be configured in a vertical or horizontal arrangement, affecting what is shown when 
scrolling past the right and bottom edges of the current name table. Some mappers are able to configure mirroring on-the-fly, allowing scrolling 
in multiple directions. 

Name table mirroring is implemented inside the PpuBus Read and Write methods. For example, in the Read method:





257	        public byte Read(ushort addr)

258	        {

259	            if (addr >= 0x0000 && addr <= 0x1FFF)

260	            //cartridge space i.e. pattern tables

261	            {

262	                return cartridge.PpuRead(addr);

263	            }

264	            else if (addr >= 0x2000 && addr <= 0x3EFF)

265	            //name tables address range

266	            {

267	                //address is masked because VRAM capacity is 0x800

268	                addr &= 0x0FFF;

269	                if (cartridge.Mirroring == 'V')

270	                {

271	                    //each time, addr is masked with 0x03FF

272	                    //to create an index into the 1 kb name table

273	                    if (addr >= 0x0000 && addr <= 0x03FF)

274	                        return nameTables[0, addr & 0x03FF];

275	                    if (addr >= 0x0400 && addr <= 0x07FF)

276	                        return nameTables[1, addr & 0x03FF];

277	                    if (addr >= 0x0800 && addr <= 0x0BFF)

278	                        return nameTables[0, addr & 0x03FF];

279	                    if (addr >= 0x0C00 && addr <= 0x0FFF)

280	                        return nameTables[1, addr & 0x03FF];

281	                }

282	                else if (cartridge.Mirroring == 'H')

283	                {

284	                    if (addr >= 0x0000 && addr <= 0x03FF)

285	                        return nameTables[0, addr & 0x03FF];

286	                    if (addr >= 0x0400 && addr <= 0x07FF)

287	                        return nameTables[0, addr & 0x03FF];

288	                    if (addr >= 0x0800 && addr <= 0x0BFF)

289	                        return nameTables[1, addr & 0x03FF];

290	                    if (addr >= 0x0C00 && addr <= 0x0FFF)

291	                        return nameTables[1, addr & 0x03FF];

292	                }

293	            }

294	            //else if (palette range)

295	//CONTINUED BELOW...



5.9.6	Palette RAM & Palette Class

The contents of the 32 byte palette RAM dictate which colours are available to display on screen at any one time.  This memory is divided into 
eight four-colour palettes: four for the foreground, and four for background. Palette RAM is illustrated in Figure 6.3.



Figure 6.3. Example Palette RAM Configuration 

The first byte of palette memory holds one additional colour value: a universal background colour. The last entry of each four colour palette is 
hard-wired to mirror the universal background colour, effectively making one of the four colours in each palette a ‘transparent’ option.

The values in palette RAM are updated by the CPU (via the PPU) with data from program memory. Reading of the palette RAM and palette index 
mirroring is implemented thus:

296	//...CONTINUED FROM ABOVE 

297	            else if (addr >= 0x3F00 && addr <= 0x3FFF)

298	            //palette address range

299	            { 

300	                addr &= 0x001F; //address is masked for 32 kb

301	

302	                if (addr % 0x4 == 0)

303	                    addr = 0x0000;

304	                return paletteRam[addr];

305	            } 

306	            throw new IndexOutOfRangeException();

307	        }

308	    }

309	}



In hardware, the PPU is able to convert the values held in palette RAM into composite video signals for one of 64 (54 unique) colours it is 
capable of displaying. Values held in palette RAM are in the range $0-$40 (0-64). 

Obviously, the ProjectNES host hardware does not have this same capability, so a Palette class was created to convert palette values into colour 
values. Palette implements the IEnumerable interface [26] and holds a list of type Color. Palette objects can be indexed (e.g. palette[0x10]) to 
return a Color. 

Color is part of the SFML.Graphics namespace, the third-party library identified in the design section [24]. Each Color object has Red, Green 
and Blue properties.

The Palette class has a DefaultPalette property which returns a new Palette of 64 colours. 64 RGB colour values approximate to those used in NES 
hardware are taken from the NesDev wiki [29]. A reference to the default palette is held within the PPU class and set via a SetPalette method.

5.9.7	Additional Registers, Counters & Latches

The PPU contains a number of other registers and latches to facilitate graphics rendering.

A shift register is a register in which the value in each bit is shifted (left or right) on each clock tick. Two ushort variables – 
patternShifterHi and  patternShifterLo – act as shift registers and contain pattern table data for two tiles. Every eight cycles, the data for 
the next tile is loaded into the upper 8 bits of each shift register. Meanwhile, the pixel to render is fetched from one of the lower 8 bits. A 
further two ushort variables – attribShifterHi and attribShifterLo – contain palette indexes for the pattern shifter tiles. 

A further three registers, usually labelled t, v and x, hold the current VRAM address, temporary VRAM address and fine X scroll position 
respectively. v is the internal data register that the PPU increments as required. t is a separate register that is updated by the CPU when it 
writes to PPUADDR. Parts of v are periodically updated with the contents of t. These registers are also updated with scrolling information as 
well as the PPU location in order to access the correct bytes of memory.

Within the NES emulation literature, a well-known structure for handling the t and v registers is something called a Loopy Register, named after 
forum user Loopy [30]. Loopy registers are implemented here in the nested LoopyRegister class. Two instances, tReg and vReg, exist within the 
PPU class.

Each LoopyRegister has an Address property. This address is essentially made up of nametable x and y coordinates, course x and y scroll 
coordinates (for scrolling tiles) and a fine y coordinate (for scrolling at the pixel level). The fine x position is held in a separate PPU 
member fine_x. 

Within the LoopyRegister are five masks:

310	            private const ushort course_x = 0x001F;

311	            private const ushort course_y = 0x03E0;

312	            private const ushort ntable_x = 0x0400;

313	            private const ushort ntable_y = 0x0800;

314	            private const ushort fine_y   = 0x7000;



A further set of five properties use these masks to access the different parts of the address:

315	            public byte Coarse_x

316	            {

317	                get => (byte)((Address & course_x) >> 0);

318	                set => Address = (ushort)((Address & ~course_x) | (value << 0));

319	            }

320	            public byte Coarse_y

321	            {

322	                get => (byte)((Address & course_y) >> 5);

323	                set => Address = (ushort)((Address & ~course_y) | (value << 5));

324	            }

325	            public byte NTable_x

326	            {

327	                get => (byte)((Address & ntable_x) >> 10);

328	                set => Address = (ushort)((Address & ~ntable_x) | (value << 10));

329	            }

330	            public byte NTable_y

331	            {

332	                get => (byte)((Address & ntable_y) >> 11);

333	                set => Address = (ushort)((Address & ~ntable_y) | (value << 11));

334	            }

335	            public byte Fine_y 

336	            {

337	                get => (byte)((Address & fine_y) >> 12);

338	                set => Address = (ushort)((Address & ~fine_y) | (value << 12));

339	            }



5.9.8	Rendering Cycle

Timing

The PPU outputs a video display composed of 240 scanlines. However, 262 scanlines are rendered per frame. The rendering duration for scanlines 
outside of the visible portion of the screen is known as the vertical blank interval (VBlank). 

During rendering, the PPU is continually reading data from the name tables. Any CPU writes to the name tables during this time would result in a 
corrupted image. Therefore, it is imperative that the CPU only write to the VRAM during VBlank.

Each scanline lasts for 341 PPU clock cycles (approximately 113.667 CPU cycles) and each clock cycle produces one pixel. For 256 cycles, the 
name table byte, attribute table byte, and pattern table tile address (high and low) are fetched on successive cycles. Each access takes two 
cycles to complete. This data is then fed to the appropriate shift registers when it is time to do so, every 8 cycles. This process is 
illustrated in Figure 6.4. In cycles 321-336, the first two name table bytes, etc., for the next scanline are fetched, and fed to the 
appropriate shift registers.

During scanline 241 the VBlank flag in the status register is set. Rendering is disabled, and an NMI signal is sent to the CPU signalling that 
VRAM can be accessed safely. The CPU’s NMI routine will usually write to the PPUSCROLL register at this point. Following the VBlank period, at 
scanline 261 (or conceptually scanline -1) the shift registers are filled with data for the first two tiles of scanline 0.

Clock Method

The PPU class’s Clock method contains a long and complex procedure (over 120 lines in the implementation) for reading bytes from the name tables 
and associated attribute tables and updating a single pixel of the graphical output for each cycle. Two member variables – scanLine and cycle – 
are incremented on each call.

When scanLine is at -1 and cycle at 1, the vertical blank flag is set as false to indicate to the CPU it should not write to the name tables. 
Later in the procedure, where scanLine is at 241, the vertical blank flag is set to true and a bool Nmi property is set to true, which will 
trigger an NMI in the CPU.







Figure 6.4 PPU Frame Timing Diagram 

Then, for the 240 visible scanlines of the display and where the cycle is less than 258 (and again for the final 8 cycles), a switch((cycles - 
1) % 8) statement is executed. This reads the appropriate data according to the frame timing diagram in Figure 6.4. The following cases are 
2) specified:
Cas
e 0: Shift registers are updated with the previously fetched values. A member variable tileID is updated by reading from the ppuBus at 
vReg.Address.:

340	        //Load the lower byte of the pattern shifters with the

341	        //next tile's two bit planes

342	        var PS_Lo_highbyte = patternShifterLo & 0xFF00; 

343	        var PS_Hi_highbyte = patternShifterHi & 0xFF00; 

344	        patternShifterLo = (ushort)(PS_Lo_highbyte | tileLSB);

345	        patternShifterHi = (ushort)(PS_Hi_highbyte | tileMSB);

346	

347	        //Do the same for the attribute shifters

348	        var AS_Lo_highbyte = attribShifterLo & 0xFF00; 

349	        var AS_Hi_highbyte = attribShifterHi & 0xFF00; 

350	

351	        //Take the lower 2 bits of the attribute word and 'inflate’ them to 8 bits

352	        //so that the 2 bit attribute is synchronised with the 8 bit tile

353	        var TA_Lo_inflated = (tileAttrib & 0b01) > 0 ? 0xFF : 0x00;

354	        var TA_Hi_inflated = (tileAttrib & 0b10) > 0 ? 0xFF : 0x00;

355	

356	        attribShifterLo = (ushort)(AS_Lo_highbyte | TA_Lo_inflated);

357	        attribShifterHi = (ushort)(AS_Hi_highbyte | TA_Hi_inflated);

358	

359	        //Fetch tile id

360	        tileID = ReadBus((ushort)(0x2000 | vReg.Address & 0x0FFF));



Case 2:  A member variable tileAttrib is updated by reading again from vreg.Address, and extracting the relevant 2-bit tile attribute:

361		tileAttrib = ReadBus(vReg.Address);

362		if ((vReg.Coarse_y & 0x02) > 0)

363	    	    tileAttrib >>= 4;

364		if ((vReg.Coarse_x & 0x02) > 0)

365	  	    tileAttrib >>= 2;

366		tileAttrib &= 0b11;



Case 4: The lower significant bit plane is extracted and stored in a tileLSB variable. the address to read is composed of the bit of the 
backgr_select flag of the control register, the tile ID, and the fine y position:

367		tileLSB = ReadBus((ushort)((bg << 12) + (tileID << 4) +  vReg.Fine_y));



Case 6: The higher significant bit plane is extracted and stored in a tileMSB variable:

368		tileMSB = ReadBus((ushort)((bg << 12) + (tileID << 4) + vReg.Fine_y + 8));



Case 7: The X scroll position is incremented. If the X position surpasses 31, the end of the name table has been reached, and so it wraps-around 
and the name table bit is inverted: 

369		if (vReg.Coarse_x == 31)

370		{

371		    vReg.Coarse_x = 0;

372		    vReg.NTable_x = (byte)~vReg.NTable_x;

373		}

374		else

375		    vReg.Coarse_x++;



Two variables, bgPixel and bgPalette, are updated once per cycle by reading from the most significant bits of the pattern and attribute shifters 
respectively. A pixel is then set by indexing the Palette object and setting the pixel of an IODevice (described below) .

An internal cycle count is incremented and, finally, if the final cycle of the final scanline is reached, the FrameComplete property is set to 
true.

5.10	Host System Graphics & IO

5.10.1	Dependencies

A third party graphics library SFML [24] was employed to handle graphical output and to display out in an application window. Several libraries 
were required for both CSFML (bindings for the C language) and SFML.NET. Since the name of CSFML libraries appear in the source code, and are 
different on each OS, an xml config file was created to map between the names of the CSFML DLLs to the names of the corresponding CSFML shared 
libraries [31].





Table 6.9 IODevice Member Variables and Type Descriptions

Member name	Type	Class Description

initColor	SFML.Graphics.Color	Utility class for manipulating 32-bits RGBA colours

vertices	SFML.Graphics.VertexArray	A set of 2D primitives

window	SFML.Graphics.RenderWindow	A target for 2D drawing

screenSize	SFML.System.Vector2u	A utility class for manipulating 2D vectors with unsigned integer components



5.10.2	IODevice Class

An IODevice class was created to handle graphical output and user input. IODevice implements the Drawable interface of SFML.Graphics, which 
requires the inclusion of a Draw(RenderTarget target, RenderStates states) method.

IODevice has four private member variables: initColor, vertices, window and screenSize, summarised in Table 6.9.

The VertexArray class also implements Drawable. Its constructor takes a PrimitiveType enum, and a vertexCount.

The vertices member effectively holds the pixels of the ProjectNES screen. It uses PrimitiveType.Triangles as its primitive type. Each (square) 
pixel of the display is represented by two triangles. Each vector in the array (three per triangle) represents a 2D point on the screen.

A constructor for the IODevice class takes three unsigned integer (uint) variables: width, height and pixel_size. In the constructor, height and 
width are scaled using the pixel_size variable. The screenSize vector is initialized with height and width, and vertices is initialized with 
type Triangles and vertex count width * height * 6.

In width * height * 6, the six represents the six points of the two triangles per pixel. So, if pixel_size is 1, width is 256, and height 240, a 
vertex array of length 368,640 is created representing 61,440 ($F000) pixels.

Then, nested for loops initialise each pixel of the screen by setting six vertices per pixel. Note that vertices 0 & 5 and 2 & 3 share their 
coordinates, representing the edge where the two triangles meet:

376	    for (uint x = 0; x < width; ++x)

377	    {

378	        for (uint y = 0; y < height; ++y)

379	        {


380	            //i = vertices array index to set

381	            uint i = (x * screenSize.Y + y) * 6;

382	            //coord2d is the base location for this pixel

383	            Vector2f coord2d = new Vector2f(x * pixel_size, y * pixel_size);

384	            //six vertices are set per pixel

385	            vertices[i + 0] = new Vertex(coord2d, initColor);

386	            vertices[i + 1] = new Vertex(coord2d 

387	                             + new Vector2f(pixel_size, 0), initColor);

388	            vertices[i + 2] = new Vertex(coord2d 

389	                             + new Vector2f(pixel_size, pixel_size), initColor);

390	            vertices[i + 3] = new Vertex(coord2d 

391	                             + new Vector2f(pixel_size, pixel_size), initColor);

392	            vertices[i + 4] = new Vertex(coord2d 

393	                             + new Vector2f(0, pixel_size), initColor);

394	            vertices[i + 5] = new Vertex(coord2d, initColor);

395	        }

396	    }





A SetPixel method updates pixels in the array. This is the method used in the PPU Clock method to set one pixel per cycle. It takes int x, int y 
and a Color as its three parameters:

397	        public void SetPixel(int x, int y, Color color)

398	        {

399	            //i = vertices array index to update

400	            uint i = (uint)((x * screenSize.Y + y) * 6);

401	            if (i >= vertices.VertexCount)

402	                return;

403	            Vertex v;

404	            //six vertices are updated per pixel

405	            for (uint j = 0; j < 6; ++j)

406	            {

407	                v = vertices[i + j]; // get existing vertex

408	                v.Color = color;     // update color

409	                vertices[i + j] = v; // assign to array

410	            }

411	        }



The IODevice’s draw method calls the draw method of the vertices array. A call to this method, and a following call to the Display method, are 
called in a continual loop when rendering to the screen. Display and all other methods in the class call corresponding methods in window:

412	        public bool WindowIsOpen => window.IsOpen;

413	

414	        public void Close() => window.Close();

415	

416	        public void DrawToWindow() => window.Draw(this);

417	

418	        public void Display() => window.Display();

419	

420	        public void AddKeyPressEvent(EventHandler<KeyEventArgs> action) => 

421	                      window.KeyPressed += action;

422	

423	        public void AddClosedEvent(EventHandler action) => window.Closed += 

424	                      action;

425	

426	        public void Clear() => window.Clear();

427	

428	        public void Clear(Color color) => window.Clear(color);

429	

430	        public void DispatchEvents() => window.DispatchEvents();



Events

A static class Events was created to define key events for the IODevice window. Only one key was mapped, the Escape key to close the 
application. However, this could be updated to handle controller input at a later stage.
