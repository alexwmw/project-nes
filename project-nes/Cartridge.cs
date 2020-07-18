using System;
using System.IO;
using System.Linq;
using HelperMethods;

namespace project_nes
{
    public class Cartridge
    {
        private BinaryReader reader;
        private byte[] header = new byte[16];
        private byte[] memory = new byte[0xBFE0];

        private byte[] prgRom;
        private byte[] chrRom;

        private byte prg_banks;
        private byte chr_banks;

        private byte mapperId;

        private string format;

        private string title;

        private string path;


        public Cartridge(string gameTitle, DirectoryInfo directory)
        {
            title = gameTitle;
            path = directory.FullName + "/" + gameTitle;
            reader = new BinaryReader(File.Open(path, FileMode.Open));

            //get header
            reader.Read(header, 0, 16);

            //confirm  format
            bool iNESFormat = false;
            bool NES20Format = false;
            if (header[0] == 'N' && header[1] == 'E' && header[2] == 'S' && header[3] == 0x1A)
                iNESFormat = true;
            if (iNESFormat == true && (header[7] & 0x0C) == 0x08)
                NES20Format = true;
            format = NES20Format ? "NES 2.0" : iNESFormat ? "iNES" : "Unknown";

            //get mapper id
            mapperId = (byte)(((byte)(header[7] >> 4)) << 4 | header[6] >> 4);

            //get bank sizes
            prg_banks = header[4];
            chr_banks = header[5];

            //instatiate ROMs
            prgRom = new byte[prg_banks * 16384];
            chrRom = new byte[chr_banks * 8192];

            //read into ROMs
            reader.Read(prgRom, 0, prgRom.Length);
            reader.Read(chrRom, 0, chrRom.Length);

        }

        public byte this[int i]    // Indexer declaration  
        {
            get { return this.memory[i]; }
            set { this.memory[i] = value; }
        }


        public void Report()
        {
            Console.WriteLine($"Title:  {title}");
            Console.WriteLine($"Path:   {path}");
            Console.WriteLine($"Format: {format}");
            Console.WriteLine($"Mapper: {mapperId}");
            Console.WriteLine($"Prg Rom Size: {prgRom.Length.Hex()}");
            Console.WriteLine($"Chr Rom Size: {chrRom.Length.Hex()}");
        }




    }
}
