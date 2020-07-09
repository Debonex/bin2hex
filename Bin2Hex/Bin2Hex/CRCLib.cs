using System;
namespace Bin2Hex
{
    /// <summary>
    /// CRC algorithm
    /// </summary>
    public class CRCLib
    {
        private CRCTYPE _crcType;
        private uint[] TABLE_CRC32;
        private const uint POLY_CRC32 = 0xEDB88320;
        //private const uint POLY_CRC32 = 0x04C11DB7;

        public CRCLib(CRCTYPE type)
        {
            _crcType = type;
            switch (_crcType)
            {
                case CRCTYPE.CRC32:{
                    generateTable_CRC32();
                    break;
                }
                default:{
                    break;
                }
            }
        }

        public object Update(object crc, byte b)
        {
            switch (_crcType)
            {
                case CRCTYPE.CRC32:
                    {
                        return update_CRC32((UInt32)crc, b);
                    }
                default:
                    {
                        break;
                    }
            }
            return null;
        }

        private void generateTable_CRC32()
        {
            TABLE_CRC32 = new uint[256];
            uint crc;
            for (uint i = 0; i < 256; i++)
            {
                crc = i;
                for (uint j = 0; j < 8; j++)
                {
                    if ((crc & 0x00000001) != 0) crc = (crc >> 1) ^ POLY_CRC32;
                    else crc >>= 1;
                }
                TABLE_CRC32[i] = crc;
            }
        }

        private uint update_CRC32(uint crc,byte b)
        {
            uint long_b = 0x000000FF & (uint)b;
            uint tmp = crc ^ long_b;
            crc = (crc >> 8) ^ (TABLE_CRC32[tmp & 0xFF]);
            //return crc ^ 0xFFFFFFFF;
            return crc & 0xFFFFFFFF;
        }



        public void print()
        {
            generateTable_CRC32();
            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    Console.Write(TABLE_CRC32[i*16+j].ToString("X8") + " ");
                }
                Console.WriteLine();
            }
        }
    }

    public enum CRCTYPE
    {
        CRC32=0
    }
}
