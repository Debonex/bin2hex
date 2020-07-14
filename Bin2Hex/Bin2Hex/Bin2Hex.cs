using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Bin2Hex
{
    class Bin2Hex
    {
        #region CONST Variables
        public const string VERSION = "1.0.0";
        private const string INFO_FILE_PATH = "VerInfo.txt";
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
        /// the offset in segment
        /// </summary>
        private static UInt16 segment_off = 0x0000;
        /// <summary>
        /// byte which is read from bin file
        /// </summary>
        private static int b;
        /// <summary>
        /// CRC Computer
        /// </summary>
        private static CRCLib crcComputer;
        /// <summary>
        /// crc value
        /// </summary>
        private static uint crc;

        private static StringBuilder sb = new StringBuilder();
        static void Main(string[] args)
        {
            
            if (args.Length != 2 || !Regex.IsMatch(args[0], RE_BINFILE) || !Regex.IsMatch(args[1], RE_HEXFILE))
            {
                Console.WriteLine(HINT_PARAM_ERROR);
                return;
            }
            Stopwatch sw = new Stopwatch();
            sw.Start();
            FileInfo binFile = new FileInfo(args[0]);
            crcComputer = new CRCLib(CRCTYPE.CRC32);
            crc = 0xFFFFFFFF;
            Console.WriteLine("解析bin文件...");
            using (StreamReader reader = new StreamReader(binFile.OpenRead()))
            {
                for (i = 0; i < reader.BaseStream.Length; i++)
                {
                    b = reader.BaseStream.ReadByte();
                    lineBuff += b.ToString("X2");
                    checksum_line += b;
                    crc = (uint)crcComputer.Update(crc, (byte)b);

                    if (i % SEGMENT_BYTES_COUNT == 0) generateSegmentLine();
                    if (i % 0x10 == 0xF) generateDataLine();
                }
                while (i % 0x10 != 0)
                {
                    checksum_line += 0xFF;
                    crc = (uint)crcComputer.Update(crc, 0xFF);
                    lineBuff += "FF";
                    i++;
                }
                if (lineBuff != string.Empty) generateDataLine();
            }
            Console.WriteLine("开始填充剩余空间...");
            //fulfill the remain space with 0xFF
            while (true)
            {
                if (segment_off == 0x0000) generateSegmentLine();
                if (count_segment > 0x80) 
                    break;
                if (!(count_segment == 0x80 && segment_off >= 0xFFE0)) fillDataLine();
                else fillInfoLine();
            }
            //end of hex
            sb.AppendLine(":00000001FF");
            using (StreamWriter writer = new StreamWriter(File.OpenWrite(args[1])))
            {
                writer.Write(sb.ToString());
                writer.Close();
            }
            Console.WriteLine("Completed in: "+sw.ElapsedMilliseconds + " ms");
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
            
            checksum_line += (segment_off >> 8) + (segment_off & 0x00FF) + 0x10;
            lineBuff = ":10" + (segment_off).ToString("X4") + "00" + lineBuff + lineChecksum();
            sb.AppendLine(lineBuff);
            lineBuff = string.Empty;
            checksum_line = 0;
            
            segment_off += 0x10;
        }


        /// <summary>
        /// fill data line with "0xFF"
        /// </summary>
        private static void fillDataLine()
        {
            checksum_line = (segment_off >> 8) + (segment_off & 0x00FF);
            for (int k = 0; k < 16; k++) crc = (uint)crcComputer.Update(crc, 0xFF);
            sb.AppendLine(":10" + (segment_off).ToString("X4") + "00FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF" + lineChecksum());
            segment_off += 0x10;
        }

        private static void fillInfoLine()
        {
            int infoPt = 0;
            checksum_line = 0;
            FileInfo infoFile = null;
            try
            {
                infoFile = new FileInfo(INFO_FILE_PATH);
                using (StreamReader reader = new StreamReader(infoFile.OpenRead()))
                {
                    /// line of version info 
                    for (infoPt = 0; infoPt < reader.BaseStream.Length && infoPt < 16; infoPt++)
                    {
                        b = reader.BaseStream.ReadByte();
                        lineBuff += b.ToString("X2");
                        checksum_line += b;
                        crc = (uint)crcComputer.Update(crc, (byte)b);
                    }
                    while (infoPt < 16)
                    {
                        checksum_line += ' ';
                        crc = (uint)crcComputer.Update(crc, (byte)' ');
                        lineBuff += ((byte)' ').ToString("X2");
                        infoPt++;
                    }
                    checksum_line += (segment_off >> 8) + (segment_off & 0xFF) + 0x10;
                    lineBuff = ":10" + segment_off.ToString("X4") + "00" + lineBuff + lineChecksum();
                    sb.AppendLine(lineBuff);
                    lineBuff = string.Empty;
                    checksum_line = 0;
                    segment_off += 0x10;

                    /// line of system time and crc
                    DateTime now = DateTime.Now;
                    int dbcYear = Decimal2DBC(now.Year) & 0xFF;
                    int dbcMonth = Decimal2DBC(now.Month) & 0xFF;
                    int dbcDay = Decimal2DBC(now.Day) & 0xFF;
                    checksum_line += dbcYear + dbcMonth + dbcDay;
                    crc = (uint)crcComputer.Update(crc, (byte)dbcYear);
                    crc = (uint)crcComputer.Update(crc, (byte)dbcMonth);
                    crc = (uint)crcComputer.Update(crc, (byte)dbcDay);
                    lineBuff += dbcYear.ToString("X2") + dbcMonth.ToString("X2") + dbcDay.ToString("X2");

                    for (int k = 0; k < 9; ++k)
                    {
                        checksum_line += ' ';
                        crc = (uint)crcComputer.Update(crc, (byte)' ');
                        lineBuff += ((byte)' ').ToString("X2");
                    }

                    crc = crc ^ 0xFFFFFFFF;
                    checksum_line += (segment_off >> 8) + (segment_off & 0x00FF) + 0x10;
                    checksum_line += (int)((crc >> 24) + ((crc >> 16) & 0xFF) + ((crc >> 8) & 0xFF) + (crc & 0xFF)); 
                    lineBuff = ":10" + (segment_off).ToString("X4") + "00" + lineBuff + crc.ToString("X8") + lineChecksum();
                    sb.AppendLine(lineBuff);
                    lineBuff = string.Empty;
                    checksum_line = 0;
                    segment_off += 0x10;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("VerInfo.txt not found! "+e.Message);
                Environment.Exit(0);
                return;
            }
        }

        private static int Decimal2DBC(int dec){
            int res = 0;
            int count = 0;
            while(dec >0){
                res += (dec % 10) << (4 * count++);
                dec /= 10;
            }
            return res;
        }

        private static string lineChecksum()
        {
            return ((0x100 - (byte)checksum_line) % 256).ToString("X2");
        }
    }
}
