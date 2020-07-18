using System;
namespace HelperMethods
{
    public static class Extensions
    {
        public static bool IsNegative(this ushort n) => (n & 0x80) == 1;

        public static bool IsNegative(this byte n) => (n & 0x80) == 1;

        public static bool IsZero(this byte n) => n == 0;

        public static byte GetPage(this ushort u) => (byte)((u & 0xFF00) >> 8);

        public static byte GetOffset(this ushort u) => (byte)(u & 0x00FF);

        public static string Hex(this byte n) => n.ToString("x2").ToUpper();

        public static string Hex(this ushort u) => u.ToString("x4").ToUpper();

        public static string Hex(this int i) => i.ToString("x").ToUpper();
    }

    public static class StaticMethods
    {
        public static ushort LittleEndian(byte lo, byte hi) => (ushort)((hi<< 8) | lo);
    }
}
