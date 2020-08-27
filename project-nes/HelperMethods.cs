using System;
namespace HelperMethods
{
    public static class Extensions
    {
        public static bool IsNegative(this ushort n) => (n & 0x80) > 0;

        public static bool IsNegative(this byte n) => (n & 0x80) > 0;

        public static bool IsZero(this byte n) => n == 0;

        public static byte GetPage(this ushort u) => (byte)((u & 0xFF00) >> 8);

        public static byte GetOffset(this ushort u) => (byte)(u & 0x00FF);

        public static string x(this byte n) => n.ToString("x2").ToUpper();

        public static string x(this ushort u) => u.ToString("x4").ToUpper();

        public static string x(this int i) => i.ToString("x").ToUpper();

        public static void Log(this string s) => Console.WriteLine($"{s}");

        public static void Log(this string s, object value) => Console.WriteLine($"{s}:    {value}");

        public static void Log(this string s, object value, string units) => Console.WriteLine($"{s}:    {value} {units}");

        
    }



    public static class StaticMethods
    {
        public static ushort LittleEndian(byte lo, byte hi) => (ushort)((hi << 8) | lo);


        public static void Window_KeyPressed(object sender, SFML.Window.KeyEventArgs e)
        {
            var window = (SFML.Window.Window) sender;
            if(e.Code == SFML.Window.Keyboard.Key.Escape)
            {
                window.Close();
            }
        }




    }
}
