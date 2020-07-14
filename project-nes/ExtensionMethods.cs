using System;
namespace ExtensionMethods
{
    public static class MyExtensions
    { 
        public static bool isNegative(this ushort n)
        {
            return (n & 0x80) == 0;
        }

        public static bool isNegative(this byte n)
        {
            return (n & 0x80) == 0;
        }
    }
}
