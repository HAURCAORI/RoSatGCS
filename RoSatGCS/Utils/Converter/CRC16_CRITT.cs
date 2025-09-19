using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoSatGCS.Utils.Converter
{
    public class CRC16_CCITT_REF
    {
        private const ushort Poly = 0x8408; // reflected
        private const ushort Init = 0xFFFF;
        private const ushort XorOut = 0x0000;

        private static readonly ushort[] Table = BuildTable();

        private static ushort[] BuildTable()
        {
            var t = new ushort[256];
            for (int i = 0; i < 256; i++)
            {
                ushort crc = (ushort)i;
                for (int b = 0; b < 8; b++)
                    crc = (ushort)(((crc & 1) != 0) ? ((crc >> 1) ^ Poly) : (crc >> 1));
                t[i] = crc;
            }
            return t;
        }

        public static ushort Compute(ReadOnlySpan<byte> data)
        {
            ushort crc = Init;
            foreach (byte b in data)
            {
                int idx = (crc ^ b) & 0xFF;
                crc = (ushort)((crc >> 8) ^ Table[idx]);
            }
            return (ushort)(crc ^ XorOut);
        }
    }

    public static class CRC16_CCITT
    {
        private const ushort Poly = 0x1021;
        private const ushort Init = 0xFFFF;
        private const ushort XorOut = 0x0000;

        private static readonly ushort[] Table = BuildTable();

        private static ushort[] BuildTable()
        {
            var t = new ushort[256];
            for (int i = 0; i < 256; i++)
            {
                ushort crc = (ushort)(i << 8);
                for (int b = 0; b < 8; b++)
                    crc = (ushort)(((crc & 0x8000) != 0) ? ((crc << 1) ^ Poly) : (crc << 1));
                t[i] = crc;
            }
            return t;
        }

        public static ushort Compute(ReadOnlySpan<byte> data)
        {
            ushort crc = Init;
            foreach (byte b in data)
            {
                int idx = ((crc >> 8) ^ b) & 0xFF;
                crc = (ushort)((crc << 8) ^ Table[idx]);
            }
            return (ushort)(crc ^ XorOut);
        }
    }
}
