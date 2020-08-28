using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using HelperMethods;

namespace project_nes
{
    public class Cartridge
    { 
        private string fileFormat;
        private string fileName;
        private string filePath;
        private byte mapperId;
        private byte prgBanks;
        private byte chrBanks;
        private byte[] prgRom; // program memory
        private byte[] chrRom; // character memory aka pattern memory or pattern tables

        private IMapper mapper;
        private BinaryReader reader;
        private Header header;

        public Cartridge(string path)
        {
            filePath = path;
            reader = new BinaryReader(File.Open(this.filePath, FileMode.Open, FileAccess.Read));
            fileName = this.filePath.Substring(path.LastIndexOf(Path.DirectorySeparatorChar) + 1);

            // Header is parsed and various flags are set inside the header
            header = new Header(reader);
            header.Parse();

            // Header flags determine...
            fileFormat = header.Nes_20 ? header.Identification + " 2.0" : "i" + header.Identification; //iNES or NES 2.0
            mapperId = header.Mapper_id;
            Mirroring = header.Mirroring_type;
            prgBanks = header.Prg_banks;
            chrBanks = header.Chr_banks;

            // Read data from file into memory
            if (header.Trainer)
                reader.ReadBytes(0x200); // trainer is 512 bytes. Junk. Not stored
            if (prgBanks > 0)
                prgRom = reader.ReadBytes(prgBanks * 0x4000); // prgBanks = no. of 16kb banks to read
            if (chrBanks > 0)
                chrRom = reader.ReadBytes(chrBanks * 0x2000);   // chrBanks = no. of 8kb banks to read

            // Mapper is instantiated based on string "Mapper" + mapperId
            mapper = (IMapper)Activator.CreateInstance(Type.GetType($"project_nes.Mapper{mapperId}")); 
            mapper.PrgBanks = prgBanks;
            mapper.PrgRom = prgRom;
            mapper.ChrRom = chrRom;
           
            reader.Close();
            Report();
        }

        // 'V' or 'H' for mirroring mode
        public char Mirroring { get; } 

        // Receives an address between 0x4020 - 0xFFFF
        public byte CpuRead(ushort adr) => mapper.CpuRead(adr);

        // Receives an address between 0x4020 - 0xFFFF
        public void CpuWrite(ushort adr, byte data) => mapper.CpuWrite(adr, data);

        //PPU should R/W only character (CHR) memory
        public byte PpuRead(ushort adr) => mapper.PpuRead(adr);

        //PPU should R/W only character (CHR) memory
        public void PpuWrite(ushort adr, byte data) => mapper.PpuWrite(adr, data);

        public void Report()
        {
            Console.WriteLine();
            Console.WriteLine($"CARTRIDGE REPORT");
            Console.WriteLine($"--------------------------------");
            Console.WriteLine($"File:               {fileName}");
            Console.WriteLine($"Path:               {filePath}");
            Console.WriteLine($"Format:             {fileFormat}");
            Console.WriteLine($"Mapper:             {mapperId}");
            Console.WriteLine($"Trainer Present:    {header.Trainer}");
            Console.WriteLine($"Prg ROM Size:       0x{prgRom.Length.x()} | {prgRom.Length / 1024} kb");
            Console.WriteLine($"Chr ROM Size:       0x{chrRom.Length.x()} | {chrRom.Length / 1024} kb");
            Console.WriteLine();
        }

        private class Header
        {
            private byte p_rom_lsb;
            private byte c_rom_lsb;
            private byte flags6;
            private byte flags7;
            private byte mapper_D0_D3;
            private byte mapper_D4_D7;
            private byte mapper_D8_D11;
            private byte submapper;
            private byte p_rom_msb;
            private byte c_rom_msb;
            private byte p_ram_size;
            private byte eeprom_size;
            private byte c_ram_size;
            private byte byte12;
            private byte byte13;
            private byte byte14;
            private byte byte15;
            private byte console_type;
            private bool nt_mirroring;
            private bool battery;
            private bool four_screen;
            private byte temp;
            private BinaryReader reader;

            public Header(BinaryReader reader)
            {
                this.reader = reader;
            }

            public string Identification { get; private set; }

            public bool Nes_20 { get; private set; }

            public byte Mapper_id { get; private set; }

            public bool Trainer { get; private set; }

            public byte Prg_banks { get; private set; }

            public byte Chr_banks { get; private set; }

            public char Mirroring_type {
                get => nt_mirroring  ? 'V' : 'H';
            }

            public void Parse(char arg = '_')
            {
                bool v = arg == 'v';

                if (v) Console.WriteLine(
                    $"\nPARSING HEADER" +
                    $"\n--------------------------------");
                // Bytes 0 - 3
                byte[] str = reader.ReadBytes(4);
                Identification = Encoding.UTF8.GetString(str);
                if (v) Console.WriteLine(
                    $"Identification:     {Identification}" +
                    $"\n--------------------------------");
                // Bytes 4 - 5
                p_rom_lsb = reader.ReadByte();
                c_rom_lsb = reader.ReadByte();
                if (v) Console.WriteLine(
                    $"p_rom_lsb :         {p_rom_lsb}" +
                    $"\nc_rom_lsb:          {c_rom_lsb}" +
                    $"\n--------------------------------");
                // Byte 6
                flags6 = reader.ReadByte();
                nt_mirroring    = (flags6 & (1 << 0)) > 0;
                battery         = (flags6 & (1 << 1)) > 0;
                Trainer         = (flags6 & (1 << 2)) > 0;
                four_screen     = (flags6 & (1 << 3)) > 0;
                mapper_D0_D3    = (byte)((flags6 & 0xF0) >> 4);
                if (v) Console.WriteLine(
                    $"nt_mirroring        {nt_mirroring}" +
                    $"\nbattery:            {battery}" +
                    $"\nTrainer:            {c_rom_lsb}" +
                    $"\nfour_screen:        {four_screen}" +
                    $"\nmapper_D0-D3:       {mapper_D0_D3}" +
                    "\n--------------------------------");
                // Byte 7
                flags7 = reader.ReadByte();
                console_type    = (byte)(flags7 & 0x03);
                Nes_20          = (byte)(flags7 & 0x0C) == 0x08;
                mapper_D4_D7    = (byte)((flags7 & 0xF0) >> 4);
                if (v) Console.WriteLine(
                    $"console_type:       {console_type}" +
                    $"\nNES_2.0:            {Nes_20}" +
                    $"\nmapper_D4_D7:       {mapper_D4_D7}" +
                    $"\n--------------------------------");
                // Byte 8
                temp            = reader.ReadByte();
                mapper_D8_D11   = (byte)((temp & 0x0F));
                submapper       = (byte)((temp & 0xF0) >> 4);
                if (v) Console.WriteLine(
                    $"mapper_D8_D11:      {mapper_D8_D11}" +
                    $"\nsubmapper:          {submapper}" +
                    $"\n--------------------------------");
                // Byte 9
                temp = reader.ReadByte();
                p_rom_msb        = (byte)((temp & 0x0F));
                c_rom_msb        = (byte)((temp & 0xF0) >> 4);
                if (v) Console.WriteLine(
                    $"p_rom_msb:          {p_rom_msb}" +
                    $"\nc_rom_msb:          {c_rom_msb}" +
                    $"\n--------------------------------");
                // Byte 10
                temp = reader.ReadByte();
                p_ram_size         = (byte)((temp & 0x0F));
                eeprom_size        = (byte)((temp & 0xF0) >> 4);
                if (v) Console.WriteLine(
                    $"p_ram:              {p_ram_size}" +
                    $"\neeprom:             {eeprom_size}" +
                    $"\n--------------------------------");
                // Byte 11
                c_ram_size         = reader.ReadByte();
                if (v) Console.WriteLine(
                    $"c_ram:              {c_ram_size}" +
                    $"\n--------------------------------");
                // Bytes 12 - 15
                byte12 = reader.ReadByte();
                byte13 = reader.ReadByte();
                byte14 = reader.ReadByte();
                byte15 = reader.ReadByte();

                Mapper_id = (byte)((mapper_D4_D7 << 4) | mapper_D0_D3);
                Prg_banks = (byte)(p_rom_lsb | p_rom_msb << 4);
                Chr_banks = (byte)(p_rom_lsb | p_rom_msb << 4);

                if (v)
                    Console.WriteLine("\nParsing complete\n");
            }
        }
    }
}
