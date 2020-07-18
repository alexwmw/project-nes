using System;
using System.IO;
using System.Linq;
using System.Text;
using HelperMethods;

namespace project_nes
{
    public class Cartridge
    {
        private BinaryReader reader;
        private Header header;
        private byte[] memory = new byte[0xBFE0];
        private byte[] trainer;
        private byte[] prgRom;
        private byte[] chrRom;
        private byte prg_banks;
        private byte chr_banks;

        private byte mapperId;
        private string format;
        private string fileName;   
        private string path;

        private bool invalid_format;


        public Cartridge(string fileName, DirectoryInfo directory)
        {
            this.fileName = fileName;
            path = directory.FullName + "/" + fileName;
            reader = new BinaryReader(File.Open(path, FileMode.Open));

            header = new Header();
            header.Parse(reader);

            if (header.Identification != "NES")
                invalid_format = true;

            format = header.Nes_20 ? header.Identification + " 2.0" : "i" + header.Identification;
            mapperId = header.Mapper_id;

            if (header.Trainer)
                trainer = reader.ReadBytes(512);
            if (header.Prg_banks > 0)
                prgRom = reader.ReadBytes(header.Prg_banks * 16384);
            if (header.Chr_banks > 0)
                chrRom = reader.ReadBytes(header.Chr_banks * 8192);

            this.Report();
        }



        


        struct Header
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

            public string Identification { get; private set; }
            public bool Nes_20 { get; private set; }
            public byte Mapper_id { get; private set; }
            public bool Trainer { get; private set; }
            public byte Prg_banks { get; private set; }
            public byte Chr_banks { get; private set; }
            public char Mirroring_type {
                get => nt_mirroring  ? 'V' : 'H';
            }


            public void Parse(BinaryReader reader)
            {
                Console.WriteLine();
                Console.WriteLine("Parsing header......");
                Console.WriteLine("--------------------------------");

                // Bytes 0 - 3
                byte[] str = reader.ReadBytes(4);
                Identification = Encoding.UTF8.GetString(str);
                Console.WriteLine($"Identification: {Identification}");
                Console.WriteLine("--------------------------------");

                // Bytes 4 - 5
                p_rom_lsb = reader.ReadByte();
                c_rom_lsb = reader.ReadByte();
                Console.WriteLine($"p_rom_lsb : {p_rom_lsb}");
                Console.WriteLine($"c_rom_lsb: {c_rom_lsb}");
                Console.WriteLine("--------------------------------");

                // Byte 6
                flags6 = reader.ReadByte();
                nt_mirroring    = (flags6 & (1 << 0)) > 0;
                battery         = (flags6 & (1 << 1)) > 0;
                Trainer         = (flags6 & (1 << 2)) > 0;
                four_screen     = (flags6 & (1 << 3)) > 0;
                mapper_D0_D3    = (byte)((flags6 & 0xF0) >> 4);
                Console.WriteLine($"nt_mirroring {nt_mirroring}");
                Console.WriteLine($"battery: {battery}");
                Console.WriteLine($"Trainer: {c_rom_lsb}");
                Console.WriteLine($"four_screen: {four_screen}");
                Console.WriteLine($"mapper_D0-D3: {mapper_D0_D3}");
                Console.WriteLine("--------------------------------");


                // Byte 7
                flags7 = reader.ReadByte();
                console_type    = (byte)(flags7 & 0x03);
                Nes_20          = (byte)(flags7 & 0x0C) == 0x08;
                mapper_D4_D7    = (byte)((flags7 & 0xF0) >> 4);
                Console.WriteLine($"console_type: {console_type}");
                Console.WriteLine($"NES_2.0: {Nes_20}");
                Console.WriteLine($"mapper_D4_D7: {mapper_D4_D7}");
                Console.WriteLine("--------------------------------");

                // Byte 8
                temp = reader.ReadByte();
                mapper_D8_D11 = (byte)((temp & 0x0F));
                submapper = (byte)((temp & 0xF0) >> 4);
                Console.WriteLine($"mapper_D8_D11: {mapper_D8_D11}");
                Console.WriteLine($"submapper: {submapper}");
                Console.WriteLine("--------------------------------");

                // Byte 9
                temp = reader.ReadByte();
                p_rom_msb = (byte)((temp & 0x0F));
                c_rom_msb = (byte)((temp & 0xF0) >> 4);
                Console.WriteLine($"p_rom_msb: {p_rom_msb}");
                Console.WriteLine($"c_rom_msb: {c_rom_msb}");
                Console.WriteLine("--------------------------------");

                // Byte 10
                temp = reader.ReadByte();
                p_ram_size = (byte)((temp & 0x0F));
                eeprom_size = (byte)((temp & 0xF0) >> 4);
                Console.WriteLine($"p_ram: {p_ram_size}");
                Console.WriteLine($"eeprom: {eeprom_size}");
                Console.WriteLine("--------------------------------");

                // Byte 11
                c_ram_size = reader.ReadByte();
                Console.WriteLine($"c_ram: {c_ram_size}");
                Console.WriteLine("--------------------------------");
                Console.WriteLine();

                // Bytes 12 - 15
                byte12 = reader.ReadByte();
                byte13 = reader.ReadByte();
                byte14 = reader.ReadByte();
                byte15 = reader.ReadByte();

                Mapper_id = (byte)((mapper_D4_D7 << 4) | mapper_D0_D3);
                Prg_banks = (byte)(p_rom_lsb | p_rom_msb << 4);
                Chr_banks = (byte)(p_rom_lsb | p_rom_msb << 4);
            }
        }



        public byte this[int i]    // Indexer declaration  
        {
            get { return this.memory[i]; }
            set { this.memory[i] = value; }
        }


        public void Report()
        {
            Console.WriteLine($"CARTRIDGE REPORT");
            Console.WriteLine($"--------------------------------");
            Console.WriteLine($"File:           {fileName}");
            Console.WriteLine($"Path:           {path}");
            Console.WriteLine($"Format:         {format}");
            Console.WriteLine($"Mapper:         {mapperId}");
            Console.WriteLine($"Prg Rom Size:   0x{prgRom.Length.Hex()}");
            Console.WriteLine($"Chr Rom Size:   0x{chrRom.Length.Hex()}");
        }




    }
}
