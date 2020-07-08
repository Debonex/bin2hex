using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;

namespace Bin2Hex
{
    class Program
    {
        #region CONST Variables
        public const string VERSION = "1.0.0";
            #region regex
            private const string RE_BINFILE = @"^[a-zA-Z0-9 ._-]*.bin$";
            private const string RE_HEXFILE = @"^[a-zA-Z0-9 ._-]*.hex$";
            #endregion
            #region Hint Strings
            private const string HINT_PARAM_ERROR = "Wrong params,usage:\n\tbin2hex.exe binfile.bin hexfile.hex";
            #endregion
        /// <summary>
        /// the max bytes count of one segment
        /// </summary>
        private const int SEGMENT_BYTES_COUNT = 0x10000;
        #endregion
        /// <summary>
        /// temparory checksum for one line
        /// </summary>
        private static int checksum_line = 0;
        /// <summary>
        /// count for segment
        /// </summary>
        private static int count_segment = 0;
        /// <summary>
        /// buffer for line string
        /// </summary>
        private static string lineBuff = string.Empty;
        /// <summary>
        /// the index of byte which is read from bin file
        /// </summary>
        private static int i;
        /// <summary>
        /// byte which is read from bin file
        /// </summary>
        private static int b;

        private static StringBuilder sb = new StringBuilder();
        static void Main(string[] args)
        {
            if (args.Length != 2 || !Regex.IsMatch(args[0],RE_BINFILE) || !Regex.IsMatch(args[1],RE_HEXFILE))
            {
                Console.WriteLine(HINT_PARAM_ERROR);
                return;
            }
            FileInfo binFile = new FileInfo(args[0]);
            using (StreamReader reader = new StreamReader(binFile.OpenRead()))
            {
                for (i = 0; i < reader.BaseStream.Length; i++)
                {
                    b = reader.BaseStream.ReadByte();
                    lineBuff += b.ToString("X2");
                    checksum_line += b;

                    if (i % SEGMENT_BYTES_COUNT == 0) generateSegmentLine();                    
                    if (i % 0x10 == 0) generateDataLine();
                }
            }
            Console.Write(sb.ToString());
        }

        /// <summary>
        /// Generate Extended Linear Address Record Line
        /// </summary>
        private static void generateSegmentLine()
        {          
            sb.AppendLine(":02000004"+
                count_segment.ToString("X4")+
                //256-(2 + 4 + (count_segment >> 8) + (byte)count_segment)
                ((byte)(250 - (count_segment >> 8) - (byte)count_segment)).ToString("X2"));
            count_segment++;
        }

        /// <summary>
        /// Generate normal data line
        /// </summary>
        private static void generateDataLine()
        {
            int addressFiled = i%SEGMENT_BYTES_COUNT;
            checksum_line += (addressFiled>>8)+(byte)addressFiled+0x10;
            lineBuff = ":10" + (i % SEGMENT_BYTES_COUNT).ToString("X4") + "00" + lineBuff + (256-(byte)checksum_line).ToString("X2");
            sb.AppendLine(lineBuff);
            lineBuff = string.Empty;
            checksum_line = 0;
        }
    }
}
