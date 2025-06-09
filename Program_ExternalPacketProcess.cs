using CoordConv;
using ObservationInfoWithJSON;
using SocketLib_Multicast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HawkEye_PTZInterface
{
    partial class Program
    {
        /// <summary>
        /// 콜백
        /// </summary>
        /// <param name="recvBuffer"></param>
        static private bool PacketProcess_U2SR(byte[] recvBuffer)
        {
            if(recvBuffer == null)//리시브 에러인경우
            {         
                byte[] buffer = U2SR.SetStopPan();
                Program.tcpClientAppPTZ.ClearQueue(buffer, true, U2SR.LEN_MOVE_POS);
                return false;
            }
            if (recvBuffer[0] == '$')
            {
                if (U2SR.ParseHomePos(recvBuffer, out bool isHome))
                {
                    IsPanTiltHome = isHome;
                    Console.WriteLine("IsPanTiltHome={0}", IsPanTiltHome);
                }
                else if (U2SR.ParsePanTiltPos(recvBuffer, out float pan, out float tilt))
                {
                    PTZInfoMgr.CamInfo.AbsPan = pan;
                    PTZInfoMgr.CamInfo.AbsTilt = tilt;

                    PTZInfoMgr.TempCamInfo.AbsPan = pan;
                    PTZInfoMgr.TempCamInfo.AbsTilt = tilt;

                    //보정 방위각 계산
                    CoordConverter.OffsetPanDeg(PTZInfoMgr.CamInfo.AbsPan, PTZInfoMgr.CamInfo.OffsetPan, out float tempPan);
                    PTZInfoMgr.CamInfo.RelPan = tempPan;
                    PTZInfoMgr.TempCamInfo.RelPan = tempPan;

                    //보정 방위각 계산
                    CoordConverter.OffsetTiltDeg(PTZInfoMgr.CamInfo.AbsTilt, PTZInfoMgr.CamInfo.OffsetTilt, out float tempTilt);
                    PTZInfoMgr.CamInfo.RelTilt = tempTilt;
                    PTZInfoMgr.TempCamInfo.RelTilt = tempTilt;

                    //360 : 6400 =  tempPan : x
                    PTZInfoMgr.CamInfo.RelMilPan = (uint)(tempPan * 6400 / 360);
                    PTZInfoMgr.CamInfo.RelMilTilt = (int)(tempTilt * 6400 / 360);
                }

                else if (U2SR.ParseGPS(recvBuffer, out int gpsLatDeg, out float gpsLatMin, out int gpsLongDeg, out float gpsLongMin))
                {
                    Console.WriteLine("GPS : {0}d{1} {2}d{3}", gpsLatDeg, gpsLatMin, gpsLongDeg, gpsLongMin);
                    PTZInfoMgr.SetGPSInfo(gpsLatDeg, gpsLatMin, gpsLongDeg, gpsLongMin);
                    PTZInfoMgr.SendPTZEnvInfo();
                }
                else if (U2SR.ParseDayCamHeaterTemp(recvBuffer, out int dayCamTemp))
                {
                    //Console.WriteLine("dayCamTemp " + dayCamTemp.ToString());
                    PTZInfoMgr.CamInfo.DayCamTemp = dayCamTemp;
                }
                else if (U2SR.ParseDayCamHeaterOnOff(recvBuffer, out bool dayCamOnOff))
                {
                    //Console.WriteLine("dayCamOnOff " + dayCamOnOff.ToString());
                    PTZInfoMgr.CamInfo.DayCamHeaterOnOff = dayCamOnOff;
                }

                else if (U2SR.ParseThermCamHeaterTemp(recvBuffer, out int thermCamTemp))
                {
                    PTZInfoMgr.CamInfo.ThermCamTemp = thermCamTemp;
                }
                else if (U2SR.ParseThermCamHeaterOnOff(recvBuffer, out bool thermCamOnOff))
                {
                    PTZInfoMgr.CamInfo.ThermCamHeaterOnOff = thermCamOnOff;
                }
                else if (U2SR.ParseDMC(recvBuffer, out float DMC))
                {
                    Console.WriteLine("DMC : {0}", DMC);
                    PTZInfoMgr.CamInfo.DMC = DMC;
                    PTZInfoMgr.SendPTZEnvInfo();
                }
                else if(U2SR.ParseMainPower(recvBuffer, out bool isMainOn))
                {
                    if(isMainOn)
                        PTZInfoMgr.CamInfo.MainPower = POWER_STATUS.POWER_ON;
                    else
                        PTZInfoMgr.CamInfo.MainPower = POWER_STATUS.POWER_OFF;
                }
                else if(U2SR.ParseBatteryInfo(recvBuffer, out float capacity, out bool state))
                {
                    PTZInfoMgr.CamInfo.BatteryPower = capacity;
                    PTZInfoMgr.CamInfo.BatteryState = state;
                }
                else if (U2SR.ParseDayPower(recvBuffer, out bool isDayOn))
                {
                    if (isDayOn)
                        PTZInfoMgr.CamInfo.DayCamPower = POWER_STATUS.POWER_ON;
                    else
                        PTZInfoMgr.CamInfo.DayCamPower = POWER_STATUS.POWER_OFF;
                }
                else if (U2SR.ParseThermPower(recvBuffer, out bool isThermOn))
                {
                    if (isThermOn)
                        PTZInfoMgr.CamInfo.ThermCamPower = POWER_STATUS.POWER_ON;
                    else
                        PTZInfoMgr.CamInfo.ThermCamPower = POWER_STATUS.POWER_OFF;
                }
             
                else
                {
                

                }
            }
            else
                return false;

            return true;
#if false
            if (Pelco_D.ParsePanPacket(recvBuffer, out int panVal))
            {
                //팬값저장
                PTZInfoMgr.CamInfo.PelcoDPan = (uint)panVal;

                switch (commonIni.nProtocol)
                {
                    case (int)Pelco_D.TypeOfProtocol.PROTO_PTZF_PELCOD_U2SR:
                        {
                            //PelcoD 값을 360기준 Deg값으로 변경
                            CoordConverter.PelcoDValToDeg360_U2SRPelcoD(
                                PTZInfoMgr.CamInfo.PelcoDPan, out float fPanDeg360);

                            PTZInfoMgr.CamInfo.AbsPan = fPanDeg360;
                            PTZInfoMgr.TempCamInfo.AbsPan = fPanDeg360;
                        }
                        break;
                    case (int)Pelco_D.TypeOfProtocol.PROTO_PTZF_PELCOD_YUJIN:
                        {
                            //PelcoD 값을 360기준 Deg값으로 변경
                            CoordConverter.PelcoDValToDeg360(
                                PTZInfoMgr.CamInfo.PelcoDPan, (int)PTZInfoMgr.PtzEnvInfo.PanValMin, (int)PTZInfoMgr.PtzEnvInfo.PanValMax, out float fPanDeg360);

                            PTZInfoMgr.CamInfo.AbsPan = fPanDeg360;
                        }
                        break;
                }

             

                //보정 방위각 계산
                CoordConverter.OffsetPanDeg(PTZInfoMgr.CamInfo.AbsPan, PTZInfoMgr.CamInfo.OffsetPan, out float tempPan);
                PTZInfoMgr.CamInfo.RelPan = tempPan;

            }
            else if (Pelco_D.ParseTiltPacket(recvBuffer, out int tiltVal) )
            {
                //틸트저장
                PTZInfoMgr.CamInfo.PelcoDTilt = (uint)tiltVal;

                switch (commonIni.nProtocol)
                {
                    case (int)Pelco_D.TypeOfProtocol.PROTO_PTZF_PELCOD_U2SR:
                        {
                            //PelcoD 값을 360기준 Deg값으로 변경
                            CoordConverter.PelcoDValToDeg180_U2SRPelcoD(
                                PTZInfoMgr.CamInfo.PelcoDTilt, out float fTiltDeg180);

                            PTZInfoMgr.CamInfo.AbsTilt = fTiltDeg180;
                            PTZInfoMgr.CamInfo.AbsTilt = fTiltDeg180;
                        }
                        break;
                    case (int)Pelco_D.TypeOfProtocol.PROTO_PTZF_PELCOD_YUJIN:
                        {
                            //PelcoD 값을 360기준 Deg값으로 변경
                            CoordConverter.PelcoDValToDeg360(
                                PTZInfoMgr.CamInfo.PelcoDTilt, (int)PTZInfoMgr.PtzEnvInfo.TiltValMin, (int)PTZInfoMgr.PtzEnvInfo.TiltValMax, out float fTiltDeg360);

                            PTZInfoMgr.CamInfo.AbsTilt = fTiltDeg360;
                        }
                        break;

                }


                

                //보정 방위각 계산
                CoordConverter.OffsetPanDeg(PTZInfoMgr.CamInfo.AbsTilt, PTZInfoMgr.CamInfo.OffsetTilt, out float tempTilt);
                PTZInfoMgr.CamInfo.RelTilt = tempTilt;
            }
            else if(Pelco_D.ParseZoomPacket(recvBuffer, out int zoomVal))
            {
                //줌값저장
                PTZInfoMgr.CamInfo.PelcoDZoomCam1 = (uint)zoomVal;

                Program.HighMagPelcoDToZoomInfo(PTZInfoMgr.CamInfo.PelcoDZoomCam1);
            }
            else if (Pelco_D.ParseFocusPacket(recvBuffer, out int focusVal))
            {
                //포커스저장
                PTZInfoMgr.CamInfo.PelcoDFocusCam1 = (uint)focusVal;
            }
#endif

        }

        //250210 열상 줌렌즈로 변경으로 창동 대공 감시 프로젝트에 사용 했던 코드로 추가
        static private bool PacketProcess_ThermCamPelcoD(byte[] recvBuffer)
        {
            if (recvBuffer == null)
                return false;

            Pelco_D.SetID(1);

            if (Pelco_D.ParseZoomPacket(recvBuffer, out int zoomVal))
            {
                //줌값저장
                PTZInfoMgr.CamInfo.PelcoDZoomCam2 = (uint)zoomVal;


                PTZInfoMgr.CamInfo.ZoomMag2 = CameraInfoConverter.LensPosToMag_OPHIR225mm(zoomVal);

                //Console.WriteLine("Zoom{0} {1}", PTZInfoMgr.CamInfo.PelcoDZoomCam3, PTZInfoMgr.CamInfo.ZoomMag3);

                CameraInfoConverter.ZoomMagToFovFor_OPHIR225mm(PTZInfoMgr.CamInfo.ZoomMag2, out float w, out float h);

                PTZInfoMgr.CamInfo.FOVWidth2 = w;
                PTZInfoMgr.CamInfo.FOVHeight2 = h;

            }
            else if (Pelco_D.ParseFocusPacket(recvBuffer, out int focusVal))
            {
                //포커스저장
                PTZInfoMgr.CamInfo.PelcoDFocusCam2 = (uint)focusVal;
                //Console.WriteLine("Focus {0}", focusVal);
            }

            return true;
        }
        /*
        static private bool PacketProcess_ThermCamPelcoD(byte[] recvBuffer)
        {
            if (recvBuffer == null)
                return false;

            Pelco_D.SetID(1);

            if (Pelco_D.ParseZoomPacket(recvBuffer, out int zoomVal))
            {
                //줌값저장
                PTZInfoMgr.CamInfo.PelcoDZoomCam2 = (uint)zoomVal;

                Program.ThermLensDZoomToZoomInfo(PTZInfoMgr.CamInfo.PelcoDZoomCam2);

                return true;
            }
            else if (Pelco_D.ParseFocusPacket(recvBuffer, out int focusVal))
            {
                //포커스저장
                PTZInfoMgr.CamInfo.PelcoDFocusCam2 = (uint)focusVal;

                return true;
            }

            return false;
        }
        */
        static private void PacketProcess_LaserIllum(byte[] recvBuffer)
        {

        }
        //250210 대공 감시 프로젝트에 사용 했던 코드로 추가
        static private void PacketProcess_FujiFilm(int value)
        {
            //Console.WriteLine("PacketProcess_FujiFilm={0}", value);
            PTZInfoMgr.CamInfo.PelcoDZoomCam1 = (uint)value;
            PTZInfoMgr.TempCamInfo.PelcoDZoomCam1 = (uint)value;

            CameraInfoConverter.ZoomValtoMag_SX801(value, out double zoomMag);
            PTZInfoMgr.CamInfo.ZoomMag1 = (float)zoomMag;
            CameraInfoConverter.ZoomMagToFovFor_SX801((int)PTZInfoMgr.CamInfo.ZoomMag1, out float w, out float h);
            PTZInfoMgr.CamInfo.FOVWidth1 = w;
            PTZInfoMgr.CamInfo.FOVHeight1 = h;
        }
    }
}
