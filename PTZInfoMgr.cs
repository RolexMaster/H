using CoordConv;
using ObservationInfoWithJSON;
using ReadWriteIni;
using SocketLib_Multicast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ObservationInfoWithJSON.PTZCommand;

namespace HawkEye_PTZInterface
{
    /// <summary>
    /// PTZ 정보를 수신/송신/저장
    /// </summary>
    public static class PTZInfoMgr
    {
        /// <summary>
        /// Common 메세지 송/수신 소켓
        /// </summary>
        private static UDPMulticastSocketWithDomain m_socketCommonMsg = null;

        /// <summary>
        /// 카메라 정보 저장 변수
        /// </summary>
        public static CameraInfo CamInfo { get; set; } = new CameraInfo();
        public static CameraInfo TempCamInfo { get; set; } = new CameraInfo();

        public static void ClearPTZF()
        {
            PTZInfoMgr.TempCamInfo.PelcoDPan = uint.MinValue;
            PTZInfoMgr.TempCamInfo.PelcoDTilt = uint.MinValue;
            PTZInfoMgr.TempCamInfo.AbsPan = float.MinValue;
            PTZInfoMgr.TempCamInfo.AbsTilt = float.MinValue;
            PTZInfoMgr.TempCamInfo.RelPan = float.MinValue;
            PTZInfoMgr.TempCamInfo.RelTilt = float.MinValue;

            PTZInfoMgr.TempCamInfo.PelcoDZoomCam1 = uint.MinValue;
        }
        public static void ClearPTZ(DEVICE_TYPE type)
        {
            PTZInfoMgr.TempCamInfo.PelcoDPan = uint.MinValue;
            PTZInfoMgr.TempCamInfo.PelcoDTilt = uint.MinValue;
            PTZInfoMgr.TempCamInfo.AbsPan = float.MinValue;
            PTZInfoMgr.TempCamInfo.AbsTilt = float.MinValue;
            PTZInfoMgr.TempCamInfo.RelPan = float.MinValue;
            PTZInfoMgr.TempCamInfo.RelTilt = float.MinValue;

            if (type == DEVICE_TYPE.DAY_CAM1)
            {
                PTZInfoMgr.TempCamInfo.PelcoDZoomCam1 = uint.MinValue;
                //PTZInfoMgr.CamInfo.PelcoDZoomCam1 = uint.MinValue;                
                //PTZInfoMgr.CamInfo.FOVWidth1 = 0;
                //PTZInfoMgr.CamInfo.FOVHeight1 = 0;
                //PTZInfoMgr.CamInfo.ZoomMag1 = 0;
            }
            else if(type == DEVICE_TYPE.THERM_CAM)
            {
                //PTZInfoMgr.CamInfo.PelcoDZoomCam2 = uint.MinValue;                
                //PTZInfoMgr.CamInfo.FOVWidth2 = 0;
                //PTZInfoMgr.CamInfo.FOVHeight2 = 0;
                //PTZInfoMgr.CamInfo.ZoomMag2 = 0;
            }         
        }

        public static void SetGPSInfo(int gpsLatDeg, float gpsLatMin, int gpsLongDeg, float gpsLongMin)
        {
            LatLong _lat = CoordConv.CoordConverter.DEGtoDMS(gpsLatDeg, gpsLatMin);
            LatLong _long = CoordConv.CoordConverter.DEGtoDMS(gpsLongDeg, gpsLongMin);

            systemPTZEnvSetup.Lat.degrees   = _lat.degrees.ToString();
            systemPTZEnvSetup.Lat.minute    = _lat.minute.ToString();     
            systemPTZEnvSetup.Lat.second    = _lat.second.ToString();
            systemPTZEnvSetup.Lat.secondDot = _lat.secondDot.ToString();

            systemPTZEnvSetup.Long.degrees      = _long.degrees.ToString();
            systemPTZEnvSetup.Long.minute       = _long.minute.ToString();  
            systemPTZEnvSetup.Long.second       = _long.second.ToString();
            systemPTZEnvSetup.Long.secondDot = _long.secondDot.ToString();
        }

        public static void ClearPT()
        {
            PTZInfoMgr.TempCamInfo.AbsPan = float.MinValue;
            PTZInfoMgr.TempCamInfo.AbsTilt = float.MinValue;

        }
        /// <summary>
        /// PTZ 접속정보(디바이스서버)
        /// </summary>
        public static PTZEnvInfo PtzEnvInfo { get; set; } = new PTZEnvInfo();

        public static void CreatePTZInfoMgr(int mainChannel)
        {
            m_socketCommonMsg = new UDPMulticastSocketWithDomain(
           MULTICAST_DOMAIN.UI_STATE_INFO, MULTICAST_CHANNEL.COMMON, ReceiveBufferCallback_CommonMsg);

            PTZInfoMgr.CamInfo.PanTiltSpeed = 150;
            PTZInfoMgr.CamInfo.PresetSpeed = 100;
            PTZInfoMgr.CamInfo.TrackingSpeed = 800;

            ReadPTZEnvSetupInfo(mainChannel);
            SendPTZEnvInfo();
        }
    

        /// <summary>
        /// common 패킷 수신
        /// </summary>
        /// <param name="receiveBuffer"></param>
        private static void ReceiveBufferCallback_CommonMsg(byte[] receiveBuffer)
        {
            string strJSON = Encoding.UTF8.GetString(receiveBuffer);

            ObservationInfoWithJSON.CommonMsg commonMsg = new ObservationInfoWithJSON.CommonMsg(0);
            bool bRet = commonMsg.SetJSONString(strJSON);

            if (bRet == true)
            {
                if (commonMsg.SelMainChannel == Program.MainChannel)
                {
                    //PTZInfo 응답처리
                    if (commonMsg.InfoName == CommonMsg.REQ_PTZINFO)
                    {
                        System.Console.WriteLine("REQ_PTZINFO");
                        SendPTZEnvInfo();
                    }
                    else if (commonMsg.InfoName == CommonMsg.SET_PTZINFO)
                    {
                        System.Console.WriteLine("ReceiveMessage : " + strJSON.Trim('\0'));

                        WritePTZEnvInfo(commonMsg.PTZEnvInfo);
                        SendPTZEnvInfo();
                        ReadPTZEnvSetupInfo(Program.MainChannel, false);

                        //보정 방위각 계산
                        CoordConverter.OffsetPanDeg(PTZInfoMgr.CamInfo.AbsPan, PTZInfoMgr.CamInfo.OffsetPan, out float tempPan);
                        PTZInfoMgr.CamInfo.RelPan = tempPan;

                        //보정 고저각 계산
                        CoordConverter.OffsetTiltDeg(PTZInfoMgr.CamInfo.AbsTilt, PTZInfoMgr.CamInfo.OffsetTilt, out float tempTilt);
                        PTZInfoMgr.CamInfo.RelTilt = tempTilt;

                        //Program.PanPelcoDToPan(CamInfo.PelcoDPan);
                        //Program.TiltPelcoDToTilt(CamInfo.PelcoDTilt);
                        //Program.HighMagPelcoDToZoomInfo(CamInfo.PelcoDZoomCam1);
                        //Program.ThermLensDZoomToZoomInfo(CamInfo.PelcoDZoomCam2);
                    }
                    else if(commonMsg.InfoName == CommonMsg.SET_CAM_LATLONG)
                    {
                        LatLong camLat = commonMsg.Lat;
                        LatLong camLong = commonMsg.Long;

                        systemPTZEnvSetup.Lat.degrees =     camLat.degrees.ToString();
                        systemPTZEnvSetup.Lat.minute =      camLat.minute.ToString();
                        systemPTZEnvSetup.Lat.second =      camLat.second.ToString();
                        systemPTZEnvSetup.Lat.secondDot =   camLat.secondDot.ToString();

                        systemPTZEnvSetup.Long.degrees =    camLong.degrees.ToString();
                        systemPTZEnvSetup.Long.minute =     camLong.minute.ToString();
                        systemPTZEnvSetup.Long.second =     camLong.second.ToString();
                        systemPTZEnvSetup.Long.secondDot =  camLong.secondDot.ToString();

                        systemPTZEnvSetup.WriteIni(Program.MainChannel);

                        SendPTZEnvInfo();
                        ReadPTZEnvSetupInfo(Program.MainChannel, false);
                    }
                }
            }


        }
        //PTZ정보 멤버 변수를 전송
        public static void SendPTZEnvInfo()
        {
            PTZEnvInfo info = new PTZEnvInfo();

            info.DMC = CamInfo.DMC;

            try
            {
                //PTZ에서 제공하지 않는 정보는 파일에서 읽어온다.
                info.Area = systemPTZEnvSetup.area;
                info.Range = int.Parse(systemPTZEnvSetup.rangeSurveillance);
                info.Lat.degrees = int.Parse(systemPTZEnvSetup.Lat.degrees);
                info.Lat.minute = int.Parse(systemPTZEnvSetup.Lat.minute);
                info.Lat.second = int.Parse(systemPTZEnvSetup.Lat.second);
                info.Lat.secondDot = int.Parse(systemPTZEnvSetup.Lat.secondDot);

                info.Long.degrees = int.Parse(systemPTZEnvSetup.Long.degrees);
                info.Long.minute = int.Parse(systemPTZEnvSetup.Long.minute);
                info.Long.second = int.Parse(systemPTZEnvSetup.Long.second);
                info.Long.secondDot = int.Parse(systemPTZEnvSetup.Long.secondDot);                

                info.PanTiltOffset = new System.Windows.Point(
                    double.Parse(systemPTZEnvSetup.azimuth),
                    double.Parse(systemPTZEnvSetup.angleOfElevation)
                    );

                info.PanValMax = uint.Parse(systemPTZEnvSetup.PanValMax);
                info.PanValMin = uint.Parse(systemPTZEnvSetup.PanValMin);

                info.TiltValMax = uint.Parse(systemPTZEnvSetup.TiltValMax);
                info.TiltValMin = uint.Parse(systemPTZEnvSetup.TiltValMin);

                info.ZoomValMin1 = uint.Parse(systemPTZEnvSetup.cam1MinZoom);
                info.ZoomValMax1 = uint.Parse(systemPTZEnvSetup.cam1MaxZoom);
                info.ZoomValMin2 = uint.Parse(systemPTZEnvSetup.cam2MinZoom);
                info.ZoomValMax2 = uint.Parse(systemPTZEnvSetup.cam2MaxZoom);


                info.ZoomFovWidthMin1 = float.Parse(systemPTZEnvSetup.cam1MinFovWidth);
                info.ZoomFovWidthMax1 = float.Parse(systemPTZEnvSetup.cam1MaxFovWidth);
                info.ZoomFovHeightMin1 = float.Parse(systemPTZEnvSetup.cam1MinFovHeight);
                info.ZoomFovHeightMax1 = float.Parse(systemPTZEnvSetup.cam1MaxFovHeight);

                info.ZoomFovWidthMin2 = float.Parse(systemPTZEnvSetup.cam2MinFovWidth);
                info.ZoomFovWidthMax2 = float.Parse(systemPTZEnvSetup.cam2MaxFovWidth);
                info.ZoomFovHeightMin2 = float.Parse(systemPTZEnvSetup.cam2MinFovHeight);
                info.ZoomFovHeightMax2 = float.Parse(systemPTZEnvSetup.cam2MaxFovHeight);

                //kyk
                info.SpeedLow = int.Parse(systemPTZEnvSetup.PanTiltSpeed.Low);
                info.SpeedMid = int.Parse(systemPTZEnvSetup.PanTiltSpeed.Mid);
                info.SpeedHigh = int.Parse(systemPTZEnvSetup.PanTiltSpeed.High);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            try
            {
                info.CamAltitude = int.Parse(systemPTZEnvSetup.camAltitude);
                
            }
            catch(Exception e)
            {

            }

            try
            {
                info.IPAddr1 = systemPTZEnvSetup.ipAddr1;
                info.Port1 = uint.Parse(systemPTZEnvSetup.port1);
                info.ID1 = systemPTZEnvSetup.id1;

                if (systemPTZEnvSetup.pw1 != null && systemPTZEnvSetup.pw1 != "")
                    info.PW1 = systemPTZEnvSetup.pw1;

                info.IPAddr2 = systemPTZEnvSetup.ipAddr2;
                info.Port2 = uint.Parse(systemPTZEnvSetup.port2);
                info.ID2 = systemPTZEnvSetup.id2;

                if (systemPTZEnvSetup.pw2 != null && systemPTZEnvSetup.pw2 != "")
                    info.PW2 = systemPTZEnvSetup.pw2;

                info.IPAddr3 = systemPTZEnvSetup.ipAddr3;
                info.Port3 = uint.Parse(systemPTZEnvSetup.port3);
                info.ID3 = systemPTZEnvSetup.id3;

                if (systemPTZEnvSetup.pw3 != null && systemPTZEnvSetup.pw3 != "")
                    info.PW3 = systemPTZEnvSetup.pw3;

                info.IPAddr4 = systemPTZEnvSetup.ipAddr4;
                info.Port4 = uint.Parse(systemPTZEnvSetup.port4);
                info.ID4 = systemPTZEnvSetup.id4;

                if (systemPTZEnvSetup.pw4 != null && systemPTZEnvSetup.pw4 != "")
                    info.PW4 = systemPTZEnvSetup.pw4;

                info.IPAddr5 = systemPTZEnvSetup.ipAddr5;
                info.Port5 = uint.Parse(systemPTZEnvSetup.port5);
                info.ID5 = systemPTZEnvSetup.id5;

                if (systemPTZEnvSetup.pw5 != null && systemPTZEnvSetup.pw5 != "")
                    info.PW5 = systemPTZEnvSetup.pw5;

                info.IPAddr6 = systemPTZEnvSetup.ipAddr6;
                info.Port6 = uint.Parse(systemPTZEnvSetup.port6);
                info.ID6 = systemPTZEnvSetup.id6;
                if (systemPTZEnvSetup.pw6 != null && systemPTZEnvSetup.pw6 != "")
                    info.PW6 = systemPTZEnvSetup.pw6;

                info.IPAddr7 = systemPTZEnvSetup.ipAddr7;
                info.Port7 = uint.Parse(systemPTZEnvSetup.port7);
                info.ID7 = systemPTZEnvSetup.id7;

                if (systemPTZEnvSetup.pw7 != null && systemPTZEnvSetup.pw7 != "")
                    info.PW7 = systemPTZEnvSetup.pw7;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            CommonMsg cmd = new CommonMsg(0, CommonMsg.RESP_PTZINFO);
            cmd.SelMainChannel = Program.MainChannel;
            cmd.PTZEnvInfo = info;
            ObservationInfoWithJSON.JSONConverter.ObjToJSONString(cmd, out string json);
            m_socketCommonMsg.SendPacket(json);
            Console.WriteLine("RESP_PTZINFO");
            Thread.Sleep(100);
            m_socketCommonMsg.SendPacket(json);
            Console.WriteLine("RESP_PTZINFO");
        }

        #region INI 관련
        public static SystemIpAddrInfo systemIpAddrInfo = new SystemIpAddrInfo("SystemIpAddrInfo");
        private static PTZEnvSetup systemPTZEnvSetup = new PTZEnvSetup("PTZEnvSetup");

        /// <summary>
        /// ini 파일에 PTZ정보 저장
        /// </summary>
        /// <param name="info"></param>
        private static void WritePTZEnvInfo(PTZEnvInfo info)
        {

            systemPTZEnvSetup.area = info.Area;
            systemPTZEnvSetup.rangeSurveillance = info.Range.ToString();

            systemPTZEnvSetup.camAltitude = info.CamAltitude.ToString();

            systemPTZEnvSetup.Lat.degrees = info.Lat.degrees.ToString();
            systemPTZEnvSetup.Lat.minute = info.Lat.minute.ToString();
            systemPTZEnvSetup.Lat.second = info.Lat.second.ToString();
            systemPTZEnvSetup.Lat.secondDot = info.Lat.secondDot.ToString();

            systemPTZEnvSetup.Long.degrees = info.Long.degrees.ToString();
            systemPTZEnvSetup.Long.minute = info.Long.minute.ToString();
            systemPTZEnvSetup.Long.second = info.Long.second.ToString();
            systemPTZEnvSetup.Long.secondDot = info.Long.secondDot.ToString();


            systemPTZEnvSetup.azimuth = info.PanTiltOffset.X.ToString();
            systemPTZEnvSetup.angleOfElevation = info.PanTiltOffset.Y.ToString();

#if false
            systemPTZEnvSetup.onvifPanMin = info.OnvifPanTiltRangeMin.X.ToString();
            systemPTZEnvSetup.onvifTiltMin = info.OnvifPanTiltRangeMin.Y.ToString();

            systemPTZEnvSetup.onvifPanMax = info.OnvifPanTiltRangeMax.X.ToString();
            systemPTZEnvSetup.onvifTiltMax = info.OnvifPanTiltRangeMax.Y.ToString();
#else
            systemPTZEnvSetup.PanValMax = info.PanValMax.ToString();
            systemPTZEnvSetup.PanValMin = info.PanValMin.ToString();

            systemPTZEnvSetup.TiltValMax = info.TiltValMax.ToString();
            systemPTZEnvSetup.TiltValMin = info.TiltValMin.ToString();

            systemPTZEnvSetup.cam1MinZoom = info.ZoomValMin1.ToString();
            systemPTZEnvSetup.cam1MaxZoom = info.ZoomValMax1.ToString();
            systemPTZEnvSetup.cam2MinZoom = info.ZoomValMin2.ToString();
            systemPTZEnvSetup.cam2MaxZoom = info.ZoomValMax2.ToString();

            systemPTZEnvSetup.cam1MinFovWidth = info.ZoomFovWidthMin1.ToString();
            systemPTZEnvSetup.cam1MaxFovWidth = info.ZoomFovWidthMax1.ToString();
            systemPTZEnvSetup.cam1MinFovHeight = info.ZoomFovHeightMin1.ToString();
            systemPTZEnvSetup.cam1MaxFovHeight = info.ZoomFovHeightMax1.ToString();

            systemPTZEnvSetup.cam2MinFovWidth = info.ZoomFovWidthMin2.ToString();
            systemPTZEnvSetup.cam2MaxFovWidth = info.ZoomFovWidthMax2.ToString();
            systemPTZEnvSetup.cam2MinFovHeight = info.ZoomFovHeightMin2.ToString();
            systemPTZEnvSetup.cam2MaxFovHeight = info.ZoomFovHeightMax2.ToString();

            //kyk
            systemPTZEnvSetup.PanTiltSpeed.Low = info.SpeedLow.ToString();
            systemPTZEnvSetup.PanTiltSpeed.Mid = info.SpeedMid.ToString();
            systemPTZEnvSetup.PanTiltSpeed.High = info.SpeedHigh.ToString();



#endif

            systemPTZEnvSetup.ipAddr1 = info.IPAddr1;
            systemPTZEnvSetup.port1 = info.Port1.ToString();
            systemPTZEnvSetup.id1 = info.ID1;

            if(info.PW1 != null && info.PW1 != "")
                systemPTZEnvSetup.pw1 = info.PW1;

            systemPTZEnvSetup.ipAddr2 = info.IPAddr2;
            systemPTZEnvSetup.port2 = info.Port2.ToString();
            systemPTZEnvSetup.id2 = info.ID2;
            if (info.PW2 != null && info.PW2 != "")
                systemPTZEnvSetup.pw2 = info.PW2;

            systemPTZEnvSetup.ipAddr3 = info.IPAddr3;
            systemPTZEnvSetup.port3 = info.Port3.ToString();
            systemPTZEnvSetup.id3 = info.ID3;

            if (info.PW3 != null && info.PW3 != "")
                systemPTZEnvSetup.pw3 = info.PW3;

            systemPTZEnvSetup.ipAddr4 = info.IPAddr4;
            systemPTZEnvSetup.port4 = info.Port4.ToString();
            systemPTZEnvSetup.id4 = info.ID4;

            if (info.PW4 != null && info.PW4 != "")
                systemPTZEnvSetup.pw4 = info.PW4;

            systemPTZEnvSetup.ipAddr5 = info.IPAddr5;
            systemPTZEnvSetup.port5 = info.Port5.ToString();
            systemPTZEnvSetup.id5 = info.ID5;

            if (info.PW5 != null && info.PW5 != "")
                systemPTZEnvSetup.pw5 = info.PW5;

            systemPTZEnvSetup.ipAddr6 = info.IPAddr6;
            systemPTZEnvSetup.port6 = info.Port6.ToString();
            systemPTZEnvSetup.id6 = info.ID6;

            if (info.PW6 != null && info.PW6 != "")
                systemPTZEnvSetup.pw6 = info.PW6;

            systemPTZEnvSetup.ipAddr7 = info.IPAddr7;
            systemPTZEnvSetup.port7 = info.Port7.ToString();
            systemPTZEnvSetup.id7 = info.ID7;

            if (info.PW7 != null && info.PW7 != "")
                systemPTZEnvSetup.pw7 = info.PW7;

            systemPTZEnvSetup.WriteIni(Program.MainChannel);
        }
      
        //ini 파일에서 PTZ정보 읽기
        private static void ReadPTZEnvSetupInfo(int nCh, bool readFile = true)
        {
            if (readFile)
                systemPTZEnvSetup.ReadIni(nCh);
           
            PTZInfoMgr.CamInfo.Name = systemPTZEnvSetup.area;
            try
            {
                //kyk 저중고 읽어오기 수정
                PTZInfoMgr.CamInfo.LOW = int.Parse(systemPTZEnvSetup.PanTiltSpeed.Low);
                PTZInfoMgr.CamInfo.MID = int.Parse(systemPTZEnvSetup.PanTiltSpeed.Mid);
                PTZInfoMgr.CamInfo.HIGH = int.Parse(systemPTZEnvSetup.PanTiltSpeed.High);

                //PTZ에서 제공하지 않는 정보는 파일에서 읽어온다.

                PTZInfoMgr.CamInfo.Range = int.Parse(systemPTZEnvSetup.rangeSurveillance);
                PTZInfoMgr.CamInfo.Lat.degrees = byte.Parse(systemPTZEnvSetup.Lat.degrees);
                PTZInfoMgr.CamInfo.Lat.minute = byte.Parse(systemPTZEnvSetup.Lat.minute);
                PTZInfoMgr.CamInfo.Lat.second = byte.Parse(systemPTZEnvSetup.Lat.second);
                PTZInfoMgr.CamInfo.Lat.secondDot = byte.Parse(systemPTZEnvSetup.Lat.secondDot);

                PTZInfoMgr.CamInfo.Long.degrees = byte.Parse(systemPTZEnvSetup.Long.degrees);
                PTZInfoMgr.CamInfo.Long.minute = byte.Parse(systemPTZEnvSetup.Long.minute);
                PTZInfoMgr.CamInfo.Long.second = byte.Parse(systemPTZEnvSetup.Long.second);
                PTZInfoMgr.CamInfo.Long.secondDot = byte.Parse(systemPTZEnvSetup.Long.secondDot);

                PTZInfoMgr.CamInfo.OffsetPan = float.Parse(systemPTZEnvSetup.azimuth);
                PTZInfoMgr.CamInfo.OffsetTilt = float.Parse(systemPTZEnvSetup.angleOfElevation);


            }
            catch (Exception e)
            {

            }

            try
            {
                PTZInfoMgr.CamInfo.CamAltitude = int.Parse(systemPTZEnvSetup.camAltitude);
            }
            catch(Exception e)
            {

            }
            
            try
            {
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.PanValMax = uint.Parse(systemPTZEnvSetup.PanValMax);
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.PanValMin = uint.Parse(systemPTZEnvSetup.PanValMin);
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.TiltValMax = uint.Parse(systemPTZEnvSetup.TiltValMax);
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.TiltValMin = uint.Parse(systemPTZEnvSetup.TiltValMin);

                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.ZoomValMin1 = uint.Parse(systemPTZEnvSetup.cam1MinZoom);
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.ZoomValMax1 = uint.Parse(systemPTZEnvSetup.cam1MaxZoom);
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.ZoomValMin2 = uint.Parse(systemPTZEnvSetup.cam2MinZoom);
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.ZoomValMax2 = uint.Parse(systemPTZEnvSetup.cam2MaxZoom);


                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.ZoomFovWidthMin1 = float.Parse(systemPTZEnvSetup.cam1MinFovWidth);
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.ZoomFovWidthMax1 = float.Parse(systemPTZEnvSetup.cam1MaxFovWidth);
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.ZoomFovHeightMin1 = float.Parse(systemPTZEnvSetup.cam1MinFovHeight);
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.ZoomFovHeightMax1 = float.Parse(systemPTZEnvSetup.cam1MaxFovHeight);
                                                                             
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.ZoomFovWidthMin2 = float.Parse(systemPTZEnvSetup.cam2MinFovWidth);
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.ZoomFovWidthMax2 = float.Parse(systemPTZEnvSetup.cam2MaxFovWidth);
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.ZoomFovHeightMin2 = float.Parse(systemPTZEnvSetup.cam2MinFovHeight);
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.ZoomFovHeightMax2 = float.Parse(systemPTZEnvSetup.cam2MaxFovHeight);
            }
            catch (Exception e)
            {

            }

            try
            {
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.IPAddr1 = systemPTZEnvSetup.ipAddr1;
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.Port1 = uint.Parse(systemPTZEnvSetup.port1);
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.ID1 = systemPTZEnvSetup.id1;

                if (systemPTZEnvSetup.pw1 != null && systemPTZEnvSetup.pw1 != "")
                    HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.PW1 = systemPTZEnvSetup.pw1;

                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.IPAddr2 = systemPTZEnvSetup.ipAddr2;
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.Port2 = uint.Parse(systemPTZEnvSetup.port2);
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.ID2 = systemPTZEnvSetup.id2;

                if (systemPTZEnvSetup.pw2 != null && systemPTZEnvSetup.pw2 != "")
                    HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.PW2 = systemPTZEnvSetup.pw2;

                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.IPAddr3 = systemPTZEnvSetup.ipAddr3;
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.Port3 = uint.Parse(systemPTZEnvSetup.port3);
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.ID3 = systemPTZEnvSetup.id3;

                if (systemPTZEnvSetup.pw3 != null && systemPTZEnvSetup.pw3 != "")
                    HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.PW3 = systemPTZEnvSetup.pw3;

                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.IPAddr4 = systemPTZEnvSetup.ipAddr4;
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.Port4 = uint.Parse(systemPTZEnvSetup.port4);
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.ID4 = systemPTZEnvSetup.id4;

                if (systemPTZEnvSetup.pw4 != null && systemPTZEnvSetup.pw4 != "")
                    HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.PW4 = systemPTZEnvSetup.pw4;

                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.IPAddr5 = systemPTZEnvSetup.ipAddr5;
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.Port5 = uint.Parse(systemPTZEnvSetup.port5);
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.ID5 = systemPTZEnvSetup.id5;

                if (systemPTZEnvSetup.pw5 != null && systemPTZEnvSetup.pw5 != "")
                    HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.PW5 = systemPTZEnvSetup.pw5;

                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.IPAddr6 = systemPTZEnvSetup.ipAddr6;
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.Port6 = uint.Parse(systemPTZEnvSetup.port6);
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.ID6 = systemPTZEnvSetup.id6;

                if (systemPTZEnvSetup.pw6 != null && systemPTZEnvSetup.pw6 != "")
                    HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.PW6 = systemPTZEnvSetup.pw6;

                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.IPAddr7 = systemPTZEnvSetup.ipAddr7;
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.Port7 = uint.Parse(systemPTZEnvSetup.port7);
                HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.ID7 = systemPTZEnvSetup.id7;

                if (systemPTZEnvSetup.pw7 != null && systemPTZEnvSetup.pw7 != "")
                    HawkEye_PTZInterface.PTZInfoMgr.PtzEnvInfo.PW7 = systemPTZEnvSetup.pw7;
            }
            catch (Exception e)
            {

            }
        }

  

        #endregion
    }
}
