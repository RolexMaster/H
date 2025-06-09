using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HawkEye_PTZInterface
{
    class FLIRTau2Protocol
    {
        #region Define
        public static readonly int PROTO_IDX_PROCESSCODE = 0;
        public static readonly int PROTO_IDX_STATUS = 1;
        /*
        0x00 CAM_OK Message received
        0x03 CAM _RANGE_ERROR Argument out of range
        0x04 CAM _CHECKSUM_ERROR Header or message-body checksum error
        0x05 CAM _UNDEFINED_PROCESS_ERROR Unknown process code
        0x06 CAM _UNDEFINED_FUNCTION_ERROR Unknown function code
        0x07 CAM _TIMEOUT_ERROR Timeout executing serial command
        0x09 CAM _BYTE_COUNT_ERROR Byte count incorrect for the function code
        0x0A CAM _FEATURE_NOT_ENABLED Function code not enabled in the current configuration
        */
        public static readonly int PROTO_IDX_RESERVED = 2;
        public static readonly int PROTO_IDX_FUNCTION = 3;
        public static readonly int PROTO_IDX_BYTECOUNT_M = 4;
        public static readonly int PROTO_IDX_BYTECOUNT_L = 5;
        public static readonly int PROTO_IDX_CRC1_M = 6;
        public static readonly int PROTO_IDX_CRC1_L = 7;
        public static readonly int PROTO_IDX_ARGUMENT_START = 8;



        public static readonly int CRC1_PART_LEN = 6;// array index 0~5


        //byte code list
        public static readonly int PROCESSCODE = 0x6E;

        //status byte list
        public static readonly int STATUS_CAMOK = 0x00;

        //reseved byte
        public static readonly int RESERVED = 0x00;

        //function byte list

        //digital zoom
        public static readonly int FUNC_DZOOM = 0x32;
        public static readonly int FUNC_DZOOM_ARGU_LEN = 0x0004;
        //public static readonly int FUNC_DZOOM_PROTO_LEN 14
        //

        //setbaud zoom
        public static readonly int FUNC_BAUD = 0x07;

        public static readonly int FUNC_SETBAUD_ARGU_LEN = 0x0002;
        public static readonly int FUNC_GETBAUD_ARGU_LEN = 0x0000;

        //public static readonly int FUNC_SETBAUD_PROTO_LEN 12
        //public static readonly int FUNC_GETBAUD_PROTO_LEN 10
        //

        //video palette
        public static readonly int FUNC_VIDEOPALETTE = 0x10;
        public static readonly int FUNC_VIDEOPALETTE_ARGU_LEN = 0x0002;
        //

        //memory status(save setting)
        public static readonly int FUNC_MEMSTATUS = 0xC4;
        public static readonly int FUNC_MEMSTATUS_ARGU_LEN = 0x0000;
        //public static readonly int FUNC_MEMSTATUS_PROTO_LEN 10


        //setDefault
        public static readonly int FUNC_SETDEFAULT = 0x01;
        public static readonly int FUNC_SETDEFAULT_ARGU_LEN = 0x0000;
        //public static readonly int FUNC_SETDEFAULT_PROTO_LEN 12

        //  [4/7/2014 Yeun]
        //DDE


        /// <summary>
        /// FactoryDefault : 0x010A (10, automatic mode)
        /// SPATIAL_THRESHOLD (AUTO_DDE)
        /// Bytes 0 -1:
        /// 0x0000 – 0x000F = manually
        /// specified threshold
        /// 0x0100 – 0x013F automatic
        /// threshold(0 to 63)   
        /// </summary>
        public static readonly int FUNC_SET_SPATIAL_THRESHOLD = 0xE3;
        public static readonly int FUNC_SET_SPATIAL_THRESHOLD_ARGU_LEN = 0x0002;

        /// <summary>
        /// FactoryDefault : n/a (auto)
        /// Range: 0 – 65535
        /// The range changed from 255 to
        /// 65535 for Tau 2.7 due to the updated
        /// DDE.
        /// </summary>
        public static readonly int FUNC_SETDDEGAIN = 0x2C;/*Note: Set capability has no effect in automatic DDE mode. (See SPATIAL_THRESHOLD, 0xE3.)*/
        public static readonly int FUNC_SETDDEGAIN_ARGU_LEN = 0x0002;

        /// <summary>
        /// FactoryDefault : n/a (default mode is automatic)
        /// Range: 0 to 255
        /// </summary>
        public static readonly int FUNC_SETDDETHRESHOLD = 0xE2;
        public static readonly int FUNC_SETDDETHRESHOLD_ARGU_LEN = 0x0002;

        /// <summary>
        /// FactoryDefault : 0x0003 (3)
        /// Range: -8 to 8
        /// 0 = disabled
        /// </summary>
        public static readonly int FUNC_SET_ACE_CORRECT = 0x1C;
        public static readonly int FUNC_SET_ACE_CORRECT_ARGU_LEN = 0x0002;


        public static readonly int FUNC_SET_AGC_TYPE = 0x13;
        public static readonly int FUNC_SET_AGC_TYPE_ARGU_LEN = 0x0004;

        #endregion

        public static CRC16_ccitt crc = new CRC16_ccitt();
        public static int SetDigitalZoom(out byte[] packet, double dZoom)
        {
            packet = new byte[14];
            //0x0001: Set zoom width to specified value

            //Bytes 2-3: Specified value
            //줌 배율값 commented by yeun
            ushort wWidthPixel = (ushort)Math.Round(640 / dZoom, 0);

            return SetCommand(packet, FUNC_DZOOM, FUNC_DZOOM_ARGU_LEN, 0x0001, wWidthPixel);

            //test 
            /*
      DWORD word;
      word = MAKEWORD(0x80, 0x02);//1x = 640
      word = MAKEWORD(0x46, 0x02);//1.1x = 640/1.1 = 581.81(calc value) = 582(flir sw value by round)
      word = MAKEWORD(0x15, 0x02);//1.2x = 640/1.2 = 533.33(calc value) = 533(flir sw value by round)
      */
        }
        public static int ReqDigitalZoom(byte[] packet)
        {
            return SetCommand(packet, FUNC_DZOOM);
        }
        public static double ParseDigitalZoomPacket(byte[] packet)
        {
            uint dWord;
            //1x = 640
            dWord = ByteAccess.MakeWord(packet[PROTO_IDX_ARGUMENT_START + 1], packet[PROTO_IDX_ARGUMENT_START]);
            return (double)(640 / dWord);
        }

        //0x0000 (Palette 0 = white hot)
        public static int SetVideoPalette(out byte[] packet, ushort wPaletteNumber)
        {
            packet = new byte[12];

            return SetCommand(packet, FUNC_VIDEOPALETTE, FUNC_VIDEOPALETTE_ARGU_LEN, wPaletteNumber);
        }

        public static int SetBaudRate(byte[] packet, int iBaud)
        {
            ushort wArgu = 0x0000;
            if (iBaud == 0)
                wArgu = 0x0000;
            else if (iBaud == 9600)
                wArgu = 0x0001;
            else if (iBaud == 19200)
                wArgu = 0x0002;
            else if (iBaud == 28800)
                wArgu = 0x0003;
            else if (iBaud == 57600)
                wArgu = 0x0004;
            else if (iBaud == 115200)
                wArgu = 0x0005;
            else if (iBaud == 460800)
                wArgu = 0x0006;
            else if (iBaud == 921600)
                wArgu = 0x0007;

            return SetCommand(packet, FUNC_BAUD, FUNC_SETBAUD_ARGU_LEN, wArgu);

            //SetCommand1(pBuf, FUNC_SETBAUD, FUNC_SETBAUD_ARGU_LEN);

            /*
            0x0000: Auto baud
            0x0001: 9600 baud
            0x0002: 19200 baud
            0x0003: 28800 baud
            0x0004: 57600 baud
            0x0005: 115200 baud
            0x0006: 460800 baud
            0x0007: 921600 baud
            */

            /*

            pBuf[PROTO_IDX_ARGUMENT_START]		= ByteAccess.HiByte(wArgu);
            pBuf[PROTO_IDX_ARGUMENT_START+1]	= ByteAccess.LoByte(wArgu);

            Dushort wCRC = 0;
            wCRC = crc16_ccitt(pBuf, PROTO_IDX_ARGUMENT_START + FUNC_SETBAUD_ARGU_LEN );

            pBuf[PROTO_IDX_ARGUMENT_START + FUNC_SETBAUD_ARGU_LEN]	= ByteAccess.HiByte(wCRC);
            pBuf[PROTO_IDX_ARGUMENT_START + FUNC_SETBAUD_ARGU_LEN+1]= ByteAccess.LoByte(wCRC);


            return FUNC_SETBAUD_PROTO_LEN;
            */
        }
        public static int ReqBaudRate(byte[] packet)
        {
            //6E 00 00 07 00 00 5A 2B 00 00
            return SetCommand(packet, FUNC_BAUD, FUNC_GETBAUD_ARGU_LEN);
            //recv format
            //baud 9600 : 6E 00 00 07 00 02 7A 69 00 01 10 21
        }
        public static int SaveSettings(byte[] packet)
        {
            //6E 00 00 C4 00 00 25 8C 00 00 
            return SetCommand(packet, FUNC_MEMSTATUS, FUNC_MEMSTATUS_ARGU_LEN);
        }

        public static int SetDefault(byte[] packet)
        {
            //6E 00 00 01 00 00 E8 8B 00 00 
            return SetCommand(packet, FUNC_SETDEFAULT, FUNC_SETDEFAULT_ARGU_LEN);
        }
              
        public static int SetDDESpatialThreshold(out byte[] packet, ushort word)
        {
            packet = new byte[12];
            ushort temp = 256;
            return SetCommand(packet, FUNC_SET_SPATIAL_THRESHOLD, FUNC_SET_SPATIAL_THRESHOLD_ARGU_LEN, (ushort)((int)word + (int)temp));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="word"> 0 to 255</param>
        /// <returns></returns>
        public static int SetDdeThreshold(out byte[] packet, ushort word)
        {
            packet = new byte[12];
            //return SetCommand(pBuf, FUNC_SETDDETHRESHOLD, FUNC_SETDDETHRESHOLD_ARGU_LEN, ushort);
            return SetCommand(packet, FUNC_SETDDETHRESHOLD, FUNC_SETDDETHRESHOLD_ARGU_LEN, word);
        }

        public static int SetDdeGain(out byte[] packet, ushort word)
        {
            packet = new byte[12];
            return SetCommand(packet, FUNC_SETDDEGAIN, FUNC_SETDDEGAIN_ARGU_LEN, word);
        }

        public static int SetACECorrect(out byte[] packet, ushort word)
        {
            packet = new byte[12];
            return SetCommand(packet, FUNC_SET_ACE_CORRECT, FUNC_SET_ACE_CORRECT_ARGU_LEN, word);
        }

        public static int SetSSO(out byte[] packet, ushort word)
        {
            packet = new byte[14];
            return SetCommand(packet, FUNC_SET_AGC_TYPE, FUNC_SET_AGC_TYPE_ARGU_LEN, 0x0400, word);
        }

        private static int SetCommand(byte[] packet, int iCmd, int iArguLen = 0, ushort wArgu1 = ushort.MaxValue, ushort wArgu2 = ushort.MaxValue)
        {
            SetCommand1(packet, iCmd, iArguLen);
            return SetCommand2(packet, wArgu1, wArgu2);
        }

        //CRC1까지 설정
        private static void SetCommand1(byte[] packet, int iCmd, int iArguLen)
        {
            packet[PROTO_IDX_PROCESSCODE] = (byte)FLIRTau2Protocol.PROCESSCODE;
            packet[PROTO_IDX_STATUS] = (byte)FLIRTau2Protocol.STATUS_CAMOK;
            packet[PROTO_IDX_RESERVED] = (byte)FLIRTau2Protocol.RESERVED;

            packet[PROTO_IDX_FUNCTION] = (byte)iCmd;//command

            packet[PROTO_IDX_BYTECOUNT_M] = ByteAccess.HiByte((byte)iArguLen);
            packet[PROTO_IDX_BYTECOUNT_L] = ByteAccess.LoByte((byte)iArguLen);

            ushort wCRC = crc.crc16_ccitt(packet, CRC1_PART_LEN);

            packet[PROTO_IDX_CRC1_M] = ByteAccess.HiByte(wCRC);
            packet[PROTO_IDX_CRC1_L] = ByteAccess.LoByte(wCRC);
        }

        //CRC2까지 설정
        private static int SetCommand2(byte[] packet, ushort wArgu1, ushort wArgu2)
        {
            int iIdx = 0;

            if (wArgu1 != ushort.MaxValue)
            {
                packet[PROTO_IDX_ARGUMENT_START + iIdx++] = ByteAccess.HiByte(wArgu1);
                packet[PROTO_IDX_ARGUMENT_START + iIdx++] = ByteAccess.LoByte(wArgu1);
            }
            if (wArgu2 != ushort.MaxValue)
            {
                packet[PROTO_IDX_ARGUMENT_START + iIdx++] = ByteAccess.HiByte(wArgu2);
                packet[PROTO_IDX_ARGUMENT_START + iIdx++] = ByteAccess.LoByte(wArgu2);
            }

            ushort wCRC = 0;
            wCRC = crc.crc16_ccitt(packet, FLIRTau2Protocol.PROTO_IDX_ARGUMENT_START + iIdx);

            packet[PROTO_IDX_ARGUMENT_START + iIdx++] = ByteAccess.HiByte(wCRC);
            packet[PROTO_IDX_ARGUMENT_START + iIdx++] = ByteAccess.LoByte(wCRC);

            return PROTO_IDX_ARGUMENT_START + iIdx;
        }


    }



    class ByteAccess
    {
        public static UInt32 MakeLong(UInt16 high, UInt16 low)
        {
            return ((UInt32)low & 0xFFFF) | (((UInt32)high & 0xFFFF) << 16);
        }
        public static UInt16 MakeWord(byte high, byte low)
        {
            return (UInt16)(((UInt32)low & 0xFF) | ((UInt32)high & 0xFF) << 8);
        }
        public static UInt16 LoWord(UInt32 nValue)
        {
            return (UInt16)(nValue & 0xFFFF);
        }
        public static UInt16 HiWord(UInt32 nValue)
        {
            return (UInt16)(nValue >> 16);
        }
        public static Byte LoByte(UInt16 nValue)
        {
            return (Byte)(nValue & 0xFF);
        }
        public static Byte HiByte(UInt16 nValue)
        {
            return (Byte)(nValue >> 8);
        }
    }

    class CRC16_ccitt
    {
        public ushort crc16_ccitt(byte[] buf, int len)
        {
            int counter;
            uint crc = 0;

            int index=0;
            for (counter = 0; counter < len; counter++)
            {
                crc = (crc << 8) ^ crc16tab[((crc >> 8) ^ buf[index++]) & 0xFF];
            }
            return (ushort)crc;
        }

        static readonly ushort[] crc16tab = {
    0x0000,0x1021,0x2042,0x3063,0x4084,0x50a5,0x60c6,0x70e7,
    0x8108,0x9129,0xa14a,0xb16b,0xc18c,0xd1ad,0xe1ce,0xf1ef,
    0x1231,0x0210,0x3273,0x2252,0x52b5,0x4294,0x72f7,0x62d6,
    0x9339,0x8318,0xb37b,0xa35a,0xd3bd,0xc39c,0xf3ff,0xe3de,
    0x2462,0x3443,0x0420,0x1401,0x64e6,0x74c7,0x44a4,0x5485,
    0xa56a,0xb54b,0x8528,0x9509,0xe5ee,0xf5cf,0xc5ac,0xd58d,
    0x3653,0x2672,0x1611,0x0630,0x76d7,0x66f6,0x5695,0x46b4,
    0xb75b,0xa77a,0x9719,0x8738,0xf7df,0xe7fe,0xd79d,0xc7bc,
    0x48c4,0x58e5,0x6886,0x78a7,0x0840,0x1861,0x2802,0x3823,
    0xc9cc,0xd9ed,0xe98e,0xf9af,0x8948,0x9969,0xa90a,0xb92b,
    0x5af5,0x4ad4,0x7ab7,0x6a96,0x1a71,0x0a50,0x3a33,0x2a12,
    0xdbfd,0xcbdc,0xfbbf,0xeb9e,0x9b79,0x8b58,0xbb3b,0xab1a,
    0x6ca6,0x7c87,0x4ce4,0x5cc5,0x2c22,0x3c03,0x0c60,0x1c41,
    0xedae,0xfd8f,0xcdec,0xddcd,0xad2a,0xbd0b,0x8d68,0x9d49,
    0x7e97,0x6eb6,0x5ed5,0x4ef4,0x3e13,0x2e32,0x1e51,0x0e70,
    0xff9f,0xefbe,0xdfdd,0xcffc,0xbf1b,0xaf3a,0x9f59,0x8f78,
    0x9188,0x81a9,0xb1ca,0xa1eb,0xd10c,0xc12d,0xf14e,0xe16f,
    0x1080,0x00a1,0x30c2,0x20e3,0x5004,0x4025,0x7046,0x6067,
    0x83b9,0x9398,0xa3fb,0xb3da,0xc33d,0xd31c,0xe37f,0xf35e,
    0x02b1,0x1290,0x22f3,0x32d2,0x4235,0x5214,0x6277,0x7256,
    0xb5ea,0xa5cb,0x95a8,0x8589,0xf56e,0xe54f,0xd52c,0xc50d,
    0x34e2,0x24c3,0x14a0,0x0481,0x7466,0x6447,0x5424,0x4405,
    0xa7db,0xb7fa,0x8799,0x97b8,0xe75f,0xf77e,0xc71d,0xd73c,
    0x26d3,0x36f2,0x0691,0x16b0,0x6657,0x7676,0x4615,0x5634,
    0xd94c,0xc96d,0xf90e,0xe92f,0x99c8,0x89e9,0xb98a,0xa9ab,
    0x5844,0x4865,0x7806,0x6827,0x18c0,0x08e1,0x3882,0x28a3,
    0xcb7d,0xdb5c,0xeb3f,0xfb1e,0x8bf9,0x9bd8,0xabbb,0xbb9a,
    0x4a75,0x5a54,0x6a37,0x7a16,0x0af1,0x1ad0,0x2ab3,0x3a92,
    0xfd2e,0xed0f,0xdd6c,0xcd4d,0xbdaa,0xad8b,0x9de8,0x8dc9,
    0x7c26,0x6c07,0x5c64,0x4c45,0x3ca2,0x2c83,0x1ce0,0x0cc1,
    0xef1f,0xff3e,0xcf5d,0xdf7c,0xaf9b,0xbfba,0x8fd9,0x9ff8,
    0x6e17,0x7e36,0x4e55,0x5e74,0x2e93,0x3eb2,0x0ed1,0x1ef0
};

}

  



}
