using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HawkEye_PTZInterface
{
    class Pelco_D
    {
        #region Define
        //PELCO-D Code Define
        static public readonly int PELCOD_RECV_LEN = 7;

        static private readonly int PELCOD_RECV_BYTE_START = 0xFF;
        //private readonly int  0x00		0x00

        static private readonly int PELCOD_RECV_IDX_START = 0;
        static private readonly int PELCOD_RECV_IDX_ID = 1;
        static private readonly int PELCOD_RECV_IDX_CMD1 = 2;
        static private readonly int PELCOD_RECV_IDX_CMD2 = 3;
        static private readonly int PELCOD_RECV_IDX_DATA1 = 4;
        static private readonly int PELCOD_RECV_IDX_DATA2 = 5;
        static private readonly int PELCOD_RECV_IDX_CHK = 6;

        static public readonly int PELCOD_SEND_LEN = 7;

        static private readonly byte PELCOD_SEND_BYTE_START = 0xFF;
        static private readonly byte PELCOD_SEND_IDX_START = 0;
        static private readonly byte PELCOD_SEND_IDX_ID = 1;
        static private readonly byte PELCOD_SEND_IDX_CMD1 = 2;
        static private readonly byte PELCOD_SEND_IDX_CMD2 = 3;
        static private readonly byte PELCOD_SEND_IDX_DATA1 = 4;
        static private readonly byte PELCOD_SEND_IDX_DATA2 = 5;
        static private readonly byte PELCOD_SEND_IDX_CHK = 6;

        static private readonly int PELCOD_SPD_MAX = 63;

        //본 소스에서 PTZF는 팬틸트/줌/포커스 제어 기능을 가지고 있는 프로토콜을 의미
        //PTZF 프로토콜
        public enum TypeOfProtocol { PROTO_PTZF_NULL, PROTO_PTZF_PELCOD_U2SR, PROTO_PTZF_PELCOD_YUJIN }
        //2 //PELCO-D 기반의 유진시스템에서 프로토콜 확장한 버전
        #endregion

        #region Var
        static private TypeOfProtocol typeOfProtocol = TypeOfProtocol.PROTO_PTZF_PELCOD_YUJIN;
        static private byte byPelcoID = 0x01;
        #endregion

        Pelco_D()
        {
#if false
    m_uPTSendDataSize=0;
	m_uLensSendDataSize=0;
	m_uCameraSendDataSize=0;

	m_uPTRecvDataSize=0;
	m_uLensRecvDataSize=0;
	m_uCameraRecvDataSize=0;

	m_byPTRecvDataStartPattern=0;
	m_byLensRecvDataStartPattern=0;
	m_byCameraRecvDataStartPattern=0;	

	m_byPTRecvDataFinishPattern=0;
	m_byLensRecvDataFinishPattern=0;
	m_byCameraRecvDataFinishPattern=0;	
	

	m_iLastCmd=0;
	m_iMyChIdx = -1;
#endif
        }

        static public void SetID(byte id)
        {
            byPelcoID = id;
        }
        static public void SetProtoTypePTZF(TypeOfProtocol type)//PTZF 프로토콜 설정
        {
            //this.typeOfProtocol = type;
#if false
            switch (uProtoType)
    {
        case IDX_PROTO_PTZF_PELCOD:
            {
                m_uPTSendDataSize = PELCOD_SEND_LEN;
                m_uLensSendDataSize = PELCOD_SEND_LEN;
                m_uCameraSendDataSize = PELCOD_SEND_LEN;

                m_uPTRecvDataSize = PELCOD_RECV_LEN;
                m_uLensRecvDataSize = PELCOD_RECV_LEN;
                m_uCameraRecvDataSize = PELCOD_RECV_LEN;

                m_byPTRecvDataStartPattern = PELCOD_RECV_BYTE_START;
                m_byLensRecvDataStartPattern = PELCOD_RECV_BYTE_START;
                m_byCameraRecvDataStartPattern = PELCOD_RECV_BYTE_START;

                //PELCO-D는 마지막 바이트를 첵섬을 사용한다. 가변적임
                m_byPTRecvDataFinishPattern = 0x00;
                m_byLensRecvDataFinishPattern = 0x00;
                m_byCameraRecvDataFinishPattern = 0x00;
            }
            break;
        case IDX_PROTO_PTZF_PELCOD_YUJIN:
            {
                m_uPTSendDataSize = PELCOD_SEND_LEN;
                m_uLensSendDataSize = PELCOD_SEND_LEN;
                m_uCameraSendDataSize = PELCOD_SEND_LEN;

                m_uPTRecvDataSize = PELCOD_RECV_LEN;
                m_uLensRecvDataSize = PELCOD_RECV_LEN;
                m_uCameraRecvDataSize = PELCOD_RECV_LEN;

                m_byPTRecvDataStartPattern = PELCOD_RECV_BYTE_START;
                m_byLensRecvDataStartPattern = PELCOD_RECV_BYTE_START;
                m_byCameraRecvDataStartPattern = PELCOD_RECV_BYTE_START;

                m_byPTRecvDataFinishPattern = 0x00;
                m_byLensRecvDataFinishPattern = 0x00;
                m_byCameraRecvDataFinishPattern = 0x00;
            }
            break;
        default:
            {
                m_uPTSendDataSize = 0;
                m_uLensSendDataSize = 0;
                m_uCameraSendDataSize = 0;

                m_uPTRecvDataSize = 0;
                m_uLensRecvDataSize = 0;
                m_uCameraRecvDataSize = 0;

                m_byPTRecvDataStartPattern = 0x00;
                m_byLensRecvDataStartPattern = 0x00;
                m_byCameraRecvDataStartPattern = 0x00;

                m_byPTRecvDataFinishPattern = 0x00;
                m_byLensRecvDataFinishPattern = 0x00;
                m_byCameraRecvDataFinishPattern = 0x00;
            }

            ASSERT(FALSE);
            break;
    }
    m_uProtoTypePT = uProtoType;
    m_uProtoTypeLens = uProtoType;
#endif
        }

        //**********Set**********//
        //PanTilt
        static public void SetPanTiltLeft(int nSpeed, byte[] pByBuffer)
        {
            switch (typeOfProtocol)
            {
                case TypeOfProtocol.PROTO_PTZF_PELCOD_YUJIN:
                    //ZeroMemory(pByBuffer, PELCOD_SEND_LEN);

                    pByBuffer[PELCOD_SEND_IDX_START] = Pelco_D.PELCOD_SEND_BYTE_START;          // STX
                    pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                            // pantilt receiver Cam id 
                    pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1
                    pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x04;                                 // Command 2
                    pByBuffer[PELCOD_SEND_IDX_DATA1] = (byte)nSpeed;                              // data 1
                    pByBuffer[PELCOD_SEND_IDX_DATA2] = 0x00;                                    // data	2

                    // BCC
                    pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
                    break;
            }
        }
        static public void SetPanTiltRight(int nSpeed, byte[] pByBuffer)
        {
            switch (typeOfProtocol)
            {
                case TypeOfProtocol.PROTO_PTZF_PELCOD_YUJIN:
                    //ZeroMemory(pByBuffer, PELCOD_SEND_LEN);

                    pByBuffer[PELCOD_SEND_IDX_START] = PELCOD_SEND_BYTE_START;          // STX
                    pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                                    // pantilt receiver Cam id 
                    pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1
                    pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x02;                                 // Command 2
                    pByBuffer[PELCOD_SEND_IDX_DATA1] = (byte)nSpeed;                              // data 1
                    pByBuffer[PELCOD_SEND_IDX_DATA2] = 0x00;                                    // data	2

                    // BCC
                    pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
                    break;
            }
        }
        static public void SetPanTiltUp(int nSpeed, byte[] pByBuffer)
        {
            switch (typeOfProtocol)
            {
                case TypeOfProtocol.PROTO_PTZF_PELCOD_YUJIN:
                    //ZeroMemory(pByBuffer, PELCOD_SEND_LEN);

                    pByBuffer[PELCOD_SEND_IDX_START] = PELCOD_SEND_BYTE_START;          // STX
                    pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                            // pantilt receiver Cam id 
                    pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1
                    pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x08;                                 // Command 2
                    pByBuffer[PELCOD_SEND_IDX_DATA1] = 0x00;                                // data 1
                    pByBuffer[PELCOD_SEND_IDX_DATA2] = (byte)nSpeed;                                  // data	2

                    // BCC
                    pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
                    break;
            }

        }

        static public void SetPanTiltDown(int nSpeed, byte[] pByBuffer)
        {
            switch (typeOfProtocol)
            {
                case TypeOfProtocol.PROTO_PTZF_PELCOD_YUJIN:
                    //ZeroMemory(pByBuffer, PELCOD_SEND_LEN);

                    pByBuffer[PELCOD_SEND_IDX_START] = PELCOD_SEND_BYTE_START;          // STX
                    pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                        // pantilt receiver Cam id 
                    pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1
                    pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x10;                                 // Command 2
                    pByBuffer[PELCOD_SEND_IDX_DATA1] = 0x00;                                    // data 1
                    pByBuffer[PELCOD_SEND_IDX_DATA2] = (byte)nSpeed;                              // data	2

                    // BCC
                    pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
                    break;
            }

        }

        static public void SetPanTiltLeftUp(int nPanSpeed, int nTiltSpeed, byte[] pByBuffer)
        {
            switch (typeOfProtocol)
            {
                case TypeOfProtocol.PROTO_PTZF_PELCOD_YUJIN:
                    //ZeroMemory(pByBuffer, PELCOD_SEND_LEN);

                    pByBuffer[PELCOD_SEND_IDX_START] = PELCOD_SEND_BYTE_START;          // STX
                    pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                        // pantilt receiver Cam id 
                    pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1
                    pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x0C;                                 // Command 2
                    pByBuffer[PELCOD_SEND_IDX_DATA1] = (byte)nPanSpeed;                           // data 1
                    pByBuffer[PELCOD_SEND_IDX_DATA2] = (byte)nTiltSpeed;                          // data	2

                    // BCC
                    pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
                    break;
            }

        }
        static public void SetPanTiltLeftDown(int nPanSpeed, int nTiltSpeed, byte[] pByBuffer)
        {
            switch (typeOfProtocol)
            {
                case TypeOfProtocol.PROTO_PTZF_PELCOD_YUJIN:
                    //ZeroMemory(pByBuffer, PELCOD_SEND_LEN);

                    pByBuffer[PELCOD_SEND_IDX_START] = PELCOD_SEND_BYTE_START;          // STX
                    pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                        // pantilt receiver Cam id 
                    pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1
                    pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x14;                                 // Command 2
                    pByBuffer[PELCOD_SEND_IDX_DATA1] = (byte)nPanSpeed;                           // data 1
                    pByBuffer[PELCOD_SEND_IDX_DATA2] = (byte)nTiltSpeed;                          // data	2

                    // BCC
                    pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
                    break;
            }

        }

        static public void SetFocusAutoDayCam(bool on, byte[] pByBuffer)
        {
            pByBuffer[PELCOD_SEND_IDX_START] = PELCOD_SEND_BYTE_START;          // STX
            pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                        // pantilt receiver Cam id 
            pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1

            if(on)
                pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x09;                                 // Command 2
            else
                pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x0b;                                 // Command 2


            pByBuffer[PELCOD_SEND_IDX_DATA1] = 0x00;                                    // data 1
            pByBuffer[PELCOD_SEND_IDX_DATA2] = 0x02;                                    // data	2

            // BCC
            pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
        }

        static public void SetFocusAutoThermCam(bool on, byte[] pByBuffer)
        {
            pByBuffer[PELCOD_SEND_IDX_START] = PELCOD_SEND_BYTE_START;          // STX
            pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                        // pantilt receiver Cam id 
            pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1
            pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x2B;                                 // Command 2
            pByBuffer[PELCOD_SEND_IDX_DATA1] = 0x00;                                    // data 1
            if(on)
                pByBuffer[PELCOD_SEND_IDX_DATA2] = 0x00;                                    // data	2
            else
                pByBuffer[PELCOD_SEND_IDX_DATA2] = 0x01;                                    // data	2

            // BCC
            pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
        }

        static public void SetPanTiltRightUp(int nPanSpeed, int nTiltSpeed, byte[] pByBuffer)
        {
            switch (typeOfProtocol)
            {
                case TypeOfProtocol.PROTO_PTZF_PELCOD_YUJIN:
                    //ZeroMemory(pByBuffer, PELCOD_SEND_LEN);

                    pByBuffer[PELCOD_SEND_IDX_START] = PELCOD_SEND_BYTE_START;          // STX
                    pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                        // pantilt receiver Cam id 
                    pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1
                    pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x0A;                                 // Command 2
                    pByBuffer[PELCOD_SEND_IDX_DATA1] = (byte)nPanSpeed;                           // data 1
                    pByBuffer[PELCOD_SEND_IDX_DATA2] = (byte)nTiltSpeed;                          // data	2

                    // BCC
                    pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
                    break;
            }

        }
        static public void SetPanTiltRightDown(int nPanSpeed, int nTiltSpeed, byte[] pByBuffer)
        {
            switch (typeOfProtocol)
            {
                case TypeOfProtocol.PROTO_PTZF_PELCOD_YUJIN:
                    //ZeroMemory(pByBuffer, PELCOD_SEND_LEN);

                    pByBuffer[PELCOD_SEND_IDX_START] = PELCOD_SEND_BYTE_START;          // STX
                    pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                        // pantilt receiver Cam id 
                    pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1
                    pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x12;                                 // Command 2
                    pByBuffer[PELCOD_SEND_IDX_DATA1] = (byte)nPanSpeed;                           // data 1
                    pByBuffer[PELCOD_SEND_IDX_DATA2] = (byte)nTiltSpeed;                          // data	2

                    // BCC
                    pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
                    break;
            }

        }
        static public void SetPanTiltStop(byte[] pByBuffer)
        {
            pByBuffer[PELCOD_SEND_IDX_START] = PELCOD_SEND_BYTE_START;          // STX
            pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                        // pantilt receiver Cam id 
            pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1
            pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x00;                                 // Command 2
            pByBuffer[PELCOD_SEND_IDX_DATA1] = 0x00;                                    // data 1
            pByBuffer[PELCOD_SEND_IDX_DATA2] = 0x00;                                    // data	2

            // BCC
            pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);

        }

        static public void SetZoomTele(byte[] pByBuffer)
        {
            //Zoom Wide 동작 FF 01 00 40 00 00 41
            pByBuffer[PELCOD_SEND_IDX_START] = PELCOD_SEND_BYTE_START;          // STX
            pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                      // pantilt receiver Cam id 
            pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1
            pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x20;                                 // Command 2
            pByBuffer[PELCOD_SEND_IDX_DATA1] = 0x00;                                    // data 1
            pByBuffer[PELCOD_SEND_IDX_DATA2] = 0x00;                                    // data	2

            // BCC
            pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
        }
        static public void SetZoomWide(byte[] pByBuffer)
        {            
            //Zoom Tele 동작 FF 01 00 20 00 00 21
            pByBuffer[PELCOD_SEND_IDX_START] = PELCOD_SEND_BYTE_START;          // STX
            pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                          // pantilt receiver Cam id 
            pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                             // Command 1
            pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x40;                             // Command 2
            pByBuffer[PELCOD_SEND_IDX_DATA1] = 0x00;                            // data 1
            pByBuffer[PELCOD_SEND_IDX_DATA2] = 0x00;                            // data	2

            // BCC
            pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
        }
        static public void SetZoomStop(byte[] pByBuffer)
        {
            pByBuffer[PELCOD_SEND_IDX_START] = PELCOD_SEND_BYTE_START;          // STX
            pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                        // pantilt receiver Cam id 
            pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1
            pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x00;                                 // Command 2
            pByBuffer[PELCOD_SEND_IDX_DATA1] = 0x00;                                    // data 1
            pByBuffer[PELCOD_SEND_IDX_DATA2] = 0x00;                                    // data	2

            // BCC
            pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
        }
        static public void SetFocusNear(byte[] pByBuffer)
        {
            pByBuffer[PELCOD_SEND_IDX_START] = PELCOD_SEND_BYTE_START;          // STX
            pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                      // pantilt receiver Cam id 
            pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x01;                                 // Command 1
            pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x00;                                 // Command 2
            pByBuffer[PELCOD_SEND_IDX_DATA1] = 0x00;                                    // data 1
            pByBuffer[PELCOD_SEND_IDX_DATA2] = 0x00;                                    // data	2

            // BCC
            pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
        }
        static public void SetFocusFar(byte[] pByBuffer)
        {
            pByBuffer[PELCOD_SEND_IDX_START] = PELCOD_SEND_BYTE_START;          // STX
            pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                          // pantilt receiver Cam id 
            pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1
            pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x80;                                 // Command 2
            pByBuffer[PELCOD_SEND_IDX_DATA1] = 0x00;                                    // data 1
            pByBuffer[PELCOD_SEND_IDX_DATA2] = 0x00;                                    // data	2

            // BCC
            pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
        }
        static public void SetFocusStop(byte[] pByBuffer)
        {
            pByBuffer[PELCOD_SEND_IDX_START] = PELCOD_SEND_BYTE_START;          // STX
            pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                        // pantilt receiver Cam id 
            pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1
            pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x00;                                 // Command 2
            pByBuffer[PELCOD_SEND_IDX_DATA1] = 0x00;                                    // data 1
            pByBuffer[PELCOD_SEND_IDX_DATA2] = 0x00;                                    // data	2

            // BCC
            pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
        }

        static public void SetAbsPan(int iPan, byte[] pByBuffer)
        {
            pByBuffer[PELCOD_SEND_IDX_START] = PELCOD_SEND_BYTE_START;          // STX
            pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                        // pantilt receiver Cam id 
            pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1
            pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x4B;                                 // Command 2

            byte MSB = 0x00, LSB = 0x00;
            MSB = (byte)(0x0FF & (iPan >> 8));  // MSB
            LSB = (byte)(0x0FF & iPan);       // LSB

            pByBuffer[PELCOD_SEND_IDX_DATA1] = MSB;                                 // data 1
            pByBuffer[PELCOD_SEND_IDX_DATA2] = LSB;                                 // data	2

            // BCC
            pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
          
        }
        static public void SetAbsTilt(int iTilt, byte[] pByBuffer)
        {
            pByBuffer[PELCOD_SEND_IDX_START] = PELCOD_SEND_BYTE_START;          // STX
            pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                        // pantilt receiver Cam id 
            pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1
            pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x4D;                                 // Command 2

            byte MSB = 0x00, LSB = 0x00;
            MSB = (byte)(0x0FF & (iTilt >> 8));// MSB
            LSB = (byte)(0x0FF & iTilt);      // LSB

            pByBuffer[PELCOD_SEND_IDX_DATA1] = MSB;                                 // data 1
            pByBuffer[PELCOD_SEND_IDX_DATA2] = LSB;                                 // data	2

            // BCC
            pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
        }
        static public void SetAbsZoom(int iZoom, byte[] pByBuffer)
        {
            pByBuffer[PELCOD_SEND_IDX_START] = PELCOD_SEND_BYTE_START;          // STX
            pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                      // pantilt receiver Cam id 
            pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1
            pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x4f;                                 // Command 2

            byte MSB = 0x00, LSB = 0x00;
            MSB = (byte)(0x0FF & (iZoom >> 8));// MSB
            LSB = (byte)(0x0FF & iZoom);      // LSB

            pByBuffer[PELCOD_SEND_IDX_DATA1] = MSB;                                 // data 1
            pByBuffer[PELCOD_SEND_IDX_DATA2] = LSB;                                 // data	2

            // BCC
            pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
        }
        static public void SetAbsFocus(int iFocus, byte[] pByBuffer)
        {
            pByBuffer[PELCOD_SEND_IDX_START] = PELCOD_SEND_BYTE_START;          // STX
            pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                      // pantilt receiver Cam id 
            pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1
            pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x5f;                                 // Command 2

            byte MSB = 0x00, LSB = 0x00;
            MSB = (byte)(0x0FF & (iFocus >> 8));// MSB
            LSB = (byte)(0x0FF & iFocus);     // LSB

            pByBuffer[PELCOD_SEND_IDX_DATA1] = MSB;                                 // data 1
            pByBuffer[PELCOD_SEND_IDX_DATA2] = LSB;                                 // data	2

            // BCC
            pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
        }

        //정보요청
        static public void ReqAbsPan(byte[] pByBuffer)
        {
            pByBuffer[PELCOD_SEND_IDX_START] = PELCOD_SEND_BYTE_START;          // STX
            pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                        // pantilt receiver Cam id 
            pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1
            pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x51;                                 // Command 2
            pByBuffer[PELCOD_SEND_IDX_DATA1] = 0x00;                                 // data 1
            pByBuffer[PELCOD_SEND_IDX_DATA2] = 0x00;                                 // data	2

            // BCC
            pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);

        }
        static public void ReqAbsTilt(byte[] pByBuffer)
        {
            pByBuffer[PELCOD_SEND_IDX_START] = PELCOD_SEND_BYTE_START;          // STX
            pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                        // pantilt receiver Cam id 
            pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1
            pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x53;                                 // Command 2
            pByBuffer[PELCOD_SEND_IDX_DATA1] = 0x00;                                 // data 1
            pByBuffer[PELCOD_SEND_IDX_DATA2] = 0x00;                                 // data	2

            // BCC
            pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
        }
        static public void ReqAbsZoom(byte[] pByBuffer)
        {
            pByBuffer[PELCOD_SEND_IDX_START] = PELCOD_SEND_BYTE_START;          // STX
            pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                        // pantilt receiver Cam id 
            pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1
            pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x55;                                 // Command 2
            pByBuffer[PELCOD_SEND_IDX_DATA1] = 0x00;                                 // data 1
            pByBuffer[PELCOD_SEND_IDX_DATA2] = 0x00;                                 // data	2

            // BCC
            pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
        }
        static public void ReqAbsFocus(byte[] pByBuffer)
        {
            pByBuffer[PELCOD_SEND_IDX_START] = PELCOD_SEND_BYTE_START;          // STX
            pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                        // pantilt receiver Cam id 
            pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1
            pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x61;                                 // Command 2
            pByBuffer[PELCOD_SEND_IDX_DATA1] = 0x00;                                 // data 1
            pByBuffer[PELCOD_SEND_IDX_DATA2] = 0x00;                                 // data	2

            // BCC
            pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
        }


        //Prset
        static public void SetPreset(int nNo, byte[] pByBuffer)
        {
            switch (typeOfProtocol)
            {
                case TypeOfProtocol.PROTO_PTZF_PELCOD_YUJIN:
                    //ZeroMemory(pByBuffer, PELCOD_SEND_LEN);

                    pByBuffer[PELCOD_SEND_IDX_START] = Pelco_D.PELCOD_SEND_BYTE_START;          // STX
                    pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                            // pantilt receiver Cam id 
                    pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1
                    pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x07;                                 // Command 2
                    pByBuffer[PELCOD_SEND_IDX_DATA1] = 0x00;                              // data 1
                    pByBuffer[PELCOD_SEND_IDX_DATA2] = (byte)nNo;                                    // data	2

                    // BCC
                    pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
                    break;
            }
        }
        static public void ClearPreset(int nNo, byte[] pByBuffer)
        {
            switch (typeOfProtocol)
            {
                case TypeOfProtocol.PROTO_PTZF_PELCOD_YUJIN:
                    //ZeroMemory(pByBuffer, PELCOD_SEND_LEN);

                    pByBuffer[PELCOD_SEND_IDX_START] = Pelco_D.PELCOD_SEND_BYTE_START;          // STX
                    pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                            // pantilt receiver Cam id 
                    pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1
                    pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x05;                                 // Command 2
                    pByBuffer[PELCOD_SEND_IDX_DATA1] = 0x00;                              // data 1
                    pByBuffer[PELCOD_SEND_IDX_DATA2] = (byte)nNo;                                    // data	2

                    // BCC
                    pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
                    break;
            }
        }
        static public void MovePreset(int nNo, byte[] pByBuffer)
        {
            switch (typeOfProtocol)
            {
                case TypeOfProtocol.PROTO_PTZF_PELCOD_YUJIN:
                    //ZeroMemory(pByBuffer, PELCOD_SEND_LEN);

                    pByBuffer[PELCOD_SEND_IDX_START] = Pelco_D.PELCOD_SEND_BYTE_START;          // STX
                    pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                            // pantilt receiver Cam id 
                    pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1
                    pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x07;                                 // Command 2
                    pByBuffer[PELCOD_SEND_IDX_DATA1] = 0x00;                              // data 1
                    pByBuffer[PELCOD_SEND_IDX_DATA2] = (byte)nNo;                                    // data	2

                    // BCC
                    pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
                    break;
            }
        }

        static public void RemoteReset(byte[] pByBuffer)
        {
            switch (typeOfProtocol)
            {
                case TypeOfProtocol.PROTO_PTZF_PELCOD_YUJIN:
                    //ZeroMemory(pByBuffer, PELCOD_SEND_LEN);

                    pByBuffer[PELCOD_SEND_IDX_START] = Pelco_D.PELCOD_SEND_BYTE_START;          // STX
                    pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                            // pantilt receiver Cam id 
                    pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1
                    pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x0F;                                 // Command 2
                    pByBuffer[PELCOD_SEND_IDX_DATA1] = 0x00;                              // data 1
                    pByBuffer[PELCOD_SEND_IDX_DATA2] = 0x00;                                    // data	2

                    // BCC
                    pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
                    break;
                case TypeOfProtocol.PROTO_PTZF_PELCOD_U2SR:
                    {
                        pByBuffer[0] = (byte)'H';
                        pByBuffer[1] = (byte)'M';
                        pByBuffer[2] = (byte)'=';
                        pByBuffer[3] = (byte)'O';
                        pByBuffer[4] = 0x0d;     
                    }
                    break;
            }
        }

        //Aux
        static public void SetAux(int nAuxNo, bool on, byte[] pByBuffer)
        {
            pByBuffer[PELCOD_SEND_IDX_START] = Pelco_D.PELCOD_SEND_BYTE_START;    // STX
            pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                            // pantilt receiver Cam id 
            pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                               // Command 1

            if(on)
                pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x09;                           // Command 2
            else
                pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x0b;                           // Command 2

            pByBuffer[PELCOD_SEND_IDX_DATA1] = 0x00;                              // data 1
            pByBuffer[PELCOD_SEND_IDX_DATA2] = (byte)nAuxNo;                      // data 2

            // BCC
            pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
        }
        static public void ClearAux(int nAuxNo, byte[] pByBuffer)
        {
            switch (typeOfProtocol)
            {
                case TypeOfProtocol.PROTO_PTZF_PELCOD_YUJIN:
                    //ZeroMemory(pByBuffer, PELCOD_SEND_LEN);

                    pByBuffer[PELCOD_SEND_IDX_START] = Pelco_D.PELCOD_SEND_BYTE_START;          // STX
                    pByBuffer[PELCOD_SEND_IDX_ID] = byPelcoID;                            // pantilt receiver Cam id 
                    pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x00;                                 // Command 1
                    pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x0B;                                 // Command 2
                    pByBuffer[PELCOD_SEND_IDX_DATA1] = 0x00;                              // data 1
                    pByBuffer[PELCOD_SEND_IDX_DATA2] = (byte)nAuxNo;                                    // data	2

                    // BCC
                    pByBuffer[PELCOD_SEND_IDX_CHK] = (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);
                    break;
            }
        }

        static public void SetThermCamPower(bool on, byte[] pByBuffer)
        {
            switch (typeOfProtocol)
            {
                case TypeOfProtocol.PROTO_PTZF_PELCOD_YUJIN:
                    //ZeroMemory(pByBuffer, PELCOD_SEND_LEN);

                    pByBuffer[PELCOD_SEND_IDX_START] = Pelco_D.PELCOD_SEND_BYTE_START;          // STX
                    pByBuffer[PELCOD_SEND_IDX_ID] = 0x0B;//11                                       // pantilt receiver Cam id 
                    if (on == true)
                    {
                        pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x88;                                     // Command 1
                     
                    }
                    else
                    {
                        pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x08;                                     // Command 2
                      
                    }

                    pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x00;
                    pByBuffer[PELCOD_SEND_IDX_DATA1] = 0x00;                                    // data 1
                    pByBuffer[PELCOD_SEND_IDX_DATA2] = 0x00;                                    // data	2

                    // BCC
                    pByBuffer[PELCOD_SEND_IDX_CHK] = (byte) (pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);

                    break;
            }
        }

        static public void SetMainPower(bool on, byte[] pByBuffer)
        {
            switch (typeOfProtocol)
            {
                case TypeOfProtocol.PROTO_PTZF_PELCOD_YUJIN:
                    //ZeroMemory(pByBuffer, PELCOD_SEND_LEN);

                    pByBuffer[PELCOD_SEND_IDX_START] = Pelco_D.PELCOD_SEND_BYTE_START;          // STX
                    pByBuffer[PELCOD_SEND_IDX_ID] = 0x0A;//10                                       // pantilt receiver Cam id 
                    if (on == true)
                    {
                        pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x88;                                     // Command 1
                       
                    }
                    else
                    {
                        pByBuffer[PELCOD_SEND_IDX_CMD1] = 0x08;                                     // Command 2
                      
                    }
                    pByBuffer[PELCOD_SEND_IDX_CMD2] = 0x00;
                    pByBuffer[PELCOD_SEND_IDX_DATA1] = 0x00;                                    // data 1
                    pByBuffer[PELCOD_SEND_IDX_DATA2] = 0x00;                                    // data	2

                    // BCC
                    pByBuffer[PELCOD_SEND_IDX_CHK] = (byte) (pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5]);

                    break;
            }
        }

        static public bool ParsePanPacket(byte[] pByBuffer, out int panAbsVal)
        {
            if (
                pByBuffer[PELCOD_SEND_IDX_START] == PELCOD_SEND_BYTE_START &&
                pByBuffer[PELCOD_SEND_IDX_ID] == byPelcoID &&
                pByBuffer[PELCOD_SEND_IDX_CMD1] == 0x00 &&
                pByBuffer[PELCOD_SEND_IDX_CMD2] == 0x59 &&
                pByBuffer[PELCOD_SEND_IDX_CHK] == (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5])
                )
            {
                int iVal = 0;

                iVal = (int)pByBuffer[4] * 256 + pByBuffer[5];

                panAbsVal = iVal;


                return true;
            }
            panAbsVal = -1;
            return false;
        }

        static public bool ParseTiltPacket(byte[] pByBuffer, out int tiltAbsVal)
        {
            if (
                pByBuffer[PELCOD_SEND_IDX_START] == PELCOD_SEND_BYTE_START &&
                pByBuffer[PELCOD_SEND_IDX_ID] == byPelcoID &&
                pByBuffer[PELCOD_SEND_IDX_CMD1] == 0x00 &&
                pByBuffer[PELCOD_SEND_IDX_CMD2] == 0x5b &&
                pByBuffer[PELCOD_SEND_IDX_CHK] == (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5])
                )
            {
                int iVal = 0;

                iVal = (int)pByBuffer[4] * 256 + pByBuffer[5];
                tiltAbsVal = iVal;

                return true;
            }
            tiltAbsVal = -1;
            return false;
        }

        static public bool ParseZoomPacket(byte[] pByBuffer, out int zoomAbsVal)
        {
            if (
                 pByBuffer[PELCOD_SEND_IDX_START] == PELCOD_SEND_BYTE_START &&
                 pByBuffer[PELCOD_SEND_IDX_ID] == byPelcoID &&
                 pByBuffer[PELCOD_SEND_IDX_CMD1] == 0x00 &&
                 pByBuffer[PELCOD_SEND_IDX_CMD2] == 0x5d &&
                 pByBuffer[PELCOD_SEND_IDX_CHK] == (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5])
             )
            {
                int iVal = 0;
                //iVal = pByBuffer[PELCOD_SEND_IDX_DATA1];
                //iVal = (iVal<<8)+pByBuffer[PELCOD_SEND_IDX_DATA1];

                iVal = (int)pByBuffer[4] * 256 + pByBuffer[5];

                zoomAbsVal = iVal;
              
                return true;
            }
            zoomAbsVal = -1;
            return false;
        }
        static public bool ParseFocusPacket(byte[] pByBuffer, out int focusAbsVal)
        {
            if (
            pByBuffer[PELCOD_SEND_IDX_START] == PELCOD_SEND_BYTE_START &&
            pByBuffer[PELCOD_SEND_IDX_ID] == byPelcoID &&
            pByBuffer[PELCOD_SEND_IDX_CMD1] == 0x00 &&
            pByBuffer[PELCOD_SEND_IDX_CMD2] == 0x63 &&
            pByBuffer[PELCOD_SEND_IDX_CHK] == (byte)(pByBuffer[1] + pByBuffer[2] + pByBuffer[3] + pByBuffer[4] + pByBuffer[5])
        )
            {
                int iVal = 0;
                //iVal = pByBuffer[PELCOD_SEND_IDX_DATA1];
                //iVal = (iVal<<8)+pByBuffer[PELCOD_SEND_IDX_DATA1];

                iVal = (int)pByBuffer[4] * 256 + pByBuffer[5];

                focusAbsVal = iVal;
                
                return true;
            }
            focusAbsVal = -1;
            return false;
        }

        static public string PacketToName(byte[] pByBuffer, out int data)
        {
            string packetName = "미정의패킷";
            data = 0;
            if (pByBuffer[PELCOD_SEND_IDX_START] == PELCOD_SEND_BYTE_START)
            {
                data = (int)pByBuffer[4] * 256 + pByBuffer[5];

                //SetPan
                if (pByBuffer[PELCOD_SEND_IDX_CMD1] == 0x00 && pByBuffer[PELCOD_SEND_IDX_CMD2] == 0x4B)
                {
                    packetName = "팬 절대값 이동";               
                }
                //SetTilt
                else if (pByBuffer[PELCOD_SEND_IDX_CMD1] == 0x00 && pByBuffer[PELCOD_SEND_IDX_CMD2] == 0x4D)
                {
                    packetName = "틸트 절대값 이동";
                }
                //SetZoom
                else if (pByBuffer[PELCOD_SEND_IDX_CMD1] == 0x00 && pByBuffer[PELCOD_SEND_IDX_CMD2] == 0x4f)
                {
                    packetName = "줌 절대값 이동";
                }
                //SetFocus
                else if (pByBuffer[PELCOD_SEND_IDX_CMD1] == 0x00 && pByBuffer[PELCOD_SEND_IDX_CMD2] == 0x5f)
                {
                    packetName = "포커스 절대값 이동";
                }
                //ReqPan
                else if (pByBuffer[PELCOD_SEND_IDX_CMD1] == 0x00 && pByBuffer[PELCOD_SEND_IDX_CMD2] == 0x51)
                {
                    packetName = "팬 값 요청";
                }
                //ReqTilt
                else if (pByBuffer[PELCOD_SEND_IDX_CMD1] == 0x00 && pByBuffer[PELCOD_SEND_IDX_CMD2] == 0x53)
                {
                    packetName = "틸트 값 요청";
                }
                //ReqZoom
                else if (pByBuffer[PELCOD_SEND_IDX_CMD1] == 0x00 && pByBuffer[PELCOD_SEND_IDX_CMD2] == 0x55)
                {
                    packetName = "줌 값 요청";
                }
                //ReqFocus
                else if (pByBuffer[PELCOD_SEND_IDX_CMD1] == 0x00 && pByBuffer[PELCOD_SEND_IDX_CMD2] == 0x61)
                {
                    packetName = "포커스 값 요청";
                }
                //RecvPan
                else if (pByBuffer[PELCOD_SEND_IDX_CMD1] == 0x00 && pByBuffer[PELCOD_SEND_IDX_CMD2] == 0x59)
                {
                    packetName = "팬 값 수신";
                }
                //RecvTilt
                else if (pByBuffer[PELCOD_SEND_IDX_CMD1] == 0x00 && pByBuffer[PELCOD_SEND_IDX_CMD2] == 0x5b )
                {
                    packetName = "틸트 값 수신";
                }
                //RecvZoom
                else if (pByBuffer[PELCOD_SEND_IDX_CMD1] == 0x00 && pByBuffer[PELCOD_SEND_IDX_CMD2] == 0x5d )
                {
                    packetName = "줌 값 수신";
                }
                //RecvFcous
                else if (pByBuffer[PELCOD_SEND_IDX_CMD1] == 0x00 && pByBuffer[PELCOD_SEND_IDX_CMD2] == 0x63 )
                {
                    packetName = "포커스 값 수신";
                }
                //Stop
                else if (pByBuffer[PELCOD_SEND_IDX_CMD1] == 0x00 && pByBuffer[PELCOD_SEND_IDX_CMD2] == 0x00)
                {
                    packetName = "정지";
                }
            }
            else
                packetName = "미정의패킷";

            return packetName;
        }
    }
}
