using CoordConv;
using ObservationInfoWithJSON;
using ReadWriteIni;
using SocketLib_Multicast;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HawkEye_PTZInterface
{
    public enum DIGITALZOOM_STATUS
    {
        ZOOM_TELE,
        ZOOM_WIDE,
        ZOOM_STOP,
    }

    partial class Program
    {
        public static int MainChannel { get; set; }
        public static bool IsReqPan { get; set; }
        public static bool IsReqTilt { get; set; }
        public static bool IsReqDayCamZoom { get; set; }
        public static bool IsReqDayCamFocus { get; set; }
        public static bool IsReqThermCamZoom { get; set; }
        public static bool IsReqThermCamFocus { get; set; }
        public static bool IsStartPanTiltInit { get; set; } = false;//팬틸트 초기화 시작 여부
        public static bool IsPanTiltHome { get; set; } = false;//팬틸트 홈 포지션 이동완료 여부
        //public static CameraInfo CamInfo { get; set; } = new CameraInfo();

        public static TCPClientApp tcpClientAppPTZ = new TCPClientApp();
        public static TCPClientApp tcpClientAppThermalLens = new TCPClientApp();
        public static TCPClientApp tcpClientAppLaserIllum = new TCPClientApp();
        public static TCPClientApp tcpClientAppPwrCtlAll = new TCPClientApp();
        public static TCPClientApp tcpClientAppPwrCtlThermCam = new TCPClientApp();
        public static HanwhaCameraController httpClientHanwhaController = null;
        public static DayCameraController DayCamCtl = null;
        //public static IRCanThermalCameraController httpClientIRCanController = null;
        public static LogWriter ConnFailLogWriter { get; set; } = new LogWriter("ConnectionFailure.log", "Start");
        public static LogWriter SendRecvPacketWriter { get; set; } = new LogWriter("SendRecvPacket.log", "Start");
        public static CommonIni commonIni = new CommonIni("Common");

        public static DIGITALZOOM_STATUS DigitalZoomStatus { get; set; } = DIGITALZOOM_STATUS.ZOOM_STOP;
        /// <summary>
        /// 메인함수
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            #region 명령인수처리
            if (args.Length == 0)
            {
                System.Console.WriteLine("Please enter a numeric argument.");
                return;
            }
            try
            {
                long arg = Int64.Parse(args[0]);
                MainChannel = (int)arg;
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Argument parsing error");
            }
            #endregion

          
            AppUtil.WinEnvUtil.MinimizeConsoleWindow();


            commonIni.ReadIni();

            //소켓 초기화
            PTZInfoMgr.CreatePTZInfoMgr(MainChannel);
            PTZInfoMgr.CamInfo.MainChannel = MainChannel;

            InitInternalSockets();
            InitExternalSocket();

            //팬틸트 초기값 설정
            //PanPelcoDToPan(5000);
            //TiltPelcoDToTilt(5000);
            //HighMagPelcoDToZoomInfo(944);
            //ThermLensPelcoDToZoomInfo(305);
            //HighMagPelcoDToZoomInfo(88);
            //ThermLensPelcoDToZoomInfo(3180);
            //HighMagPelcoDToZoomInfo(1);
            ThermLensDZoomToZoomInfo(1);

            //카메라 정보전송 타이머 
            SendTimerCaminfo();
            //PTZ 정보요청 타이머
            //250210줌렌즈로변경으로 값요청 타이머 생성 코드 추가
            SendTimerReqPTZinfo();
            //배터리 정보요청 타이머
            SendTimerBatteryinfo();

            //DigitalZoomProc();
            //
            Console.WriteLine("Press <Enter> to exit... ");
            while (Console.ReadKey().Key != ConsoleKey.Enter)
            {
                Console.Clear();
            }
        }

        private static void DigitalZoomProc()
        {
            new Thread(()=>
            {
                while(true)
                {
                    switch (DigitalZoomStatus)
                    {
                        case DIGITALZOOM_STATUS.ZOOM_STOP:

                            break;
                        case DIGITALZOOM_STATUS.ZOOM_TELE:
                            ThermDZoomTele();
                            break;
                        case DIGITALZOOM_STATUS.ZOOM_WIDE:
                            ThermDZoomWide();
                            break;
                    }
                    ThermLensDZoomToZoomInfo((float)PTZInfoMgr.CamInfo.ThermDigitalZoom);
                    //Console.WriteLine(DigitalZoomStatus + " " + PTZInfoMgr.CamInfo.ThermDigitalZoom);
                    Thread.Sleep(330);
                }
            }).Start();

        }

        /// <summary>
        /// PTZ 정보 요청 타이머
        /// </summary>
        /*
        private static void SendTimerReqPTZinfo()
        {
            var timer = new System.Timers.Timer();
            timer.Interval = 1000;
            timer.Start();
            timer.Elapsed += (sender, args) =>
            {
                timer.Stop();

                if(Program.commonIni.bReqPanTiltPos)
                {
                    if (IsReqPan || IsReqTilt)
                    {
                        EnqueueReqPanTiltPacket();
                    }
                    //if (IsReqTilt)
                    //{
                    //    EnqueueReqTiltPacket();
                    //}

                    if (IsReqDayCamZoom)
                    {
                        //EnqueueReqDayCamZoomPacket();
                    }

                    if (IsReqDayCamFocus)
                    {
                        //EnqueueReqDayCamFocusPacket();
                    }

                    if (IsReqThermCamZoom)
                    {
                        //EnqueueReqThermCamZoomPacket();
                    }
                    if (IsReqThermCamFocus)
                    {
                        //EnqueueReqThermCamFocusPacket();
                    }
                }
                timer.Start();
            };         
        }
        */
        //250210줌렌즈로변경으로 값요청 타이머 생성 코드 추가
        private static void SendTimerReqPTZinfo()
        {
            var timer = new System.Timers.Timer();
            timer.Interval = 333;
            timer.Start();
            timer.Elapsed += (sender, args) =>
            {
                timer.Stop();

                if (Program.commonIni.bReqPanTiltPos)
                {                  
                    if (IsReqDayCamZoom)
                    {
                        //EnqueueReqDayCamZoomPacket();
                    }

                    if (IsReqDayCamFocus)
                    {
                        //EnqueueReqDayCamFocusPacket();
                    }

                    if (IsReqThermCamZoom)
                    {
                        EnqueueReqThermCamZoomPacket();
                    }
                    if (IsReqThermCamFocus)
                    {
                        EnqueueReqThermCamFocusPacket();
                    }
                }
                timer.Start();
            };
        }
        //private static void EnqueueReqTiltPacket()
        //{
        //    byte[] buffer = U2SR.ReqPanTiltPos();
        //    tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_PANTILT_POS);
        //}
        private static void EnqueueReqPanTiltPacket()
        {
            byte[] buffer = U2SR.ReqPanTiltPos();           
            tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_PANTILT_POS);
        }
        private static void EnqueueReqMainDayThermPwrPacket()
        {
            byte[] buffer1 = U2SR.ReqMainPower();
            tcpClientAppPTZ.EnqueuePacket(buffer1, true, U2SR.LEN_POWER);
            byte[] buffer2 = U2SR.ReqDayCamPower();
            tcpClientAppPTZ.EnqueuePacket(buffer2, true, U2SR.LEN_POWER);
            byte[] buffer3 = U2SR.ReqThermCamPower();
            tcpClientAppPTZ.EnqueuePacket(buffer3, true, U2SR.LEN_POWER);
        }
        private static void EnqueueReqDayCamZoomPacket()
        {
            Pelco_D.SetID(1);
            byte[] buffer1 = new byte[Pelco_D.PELCOD_SEND_LEN];
            Pelco_D.ReqAbsZoom(buffer1);
            tcpClientAppPTZ.EnqueuePacket(buffer1, true);
        }
        private static void EnqueueReqDayCamFocusPacket()
        {
            Pelco_D.SetID(1);
            byte[] buffer1 = new byte[Pelco_D.PELCOD_SEND_LEN];
            Pelco_D.ReqAbsFocus(buffer1);
            tcpClientAppPTZ.EnqueuePacket(buffer1, true);
        }
        private static void EnqueueReqThermCamZoomPacket()
        {
            Pelco_D.SetID(1);
            byte[] buffer1 = new byte[Pelco_D.PELCOD_SEND_LEN];
            Pelco_D.ReqAbsZoom(buffer1);
            tcpClientAppThermalLens.EnqueuePacket(buffer1, true);
        }
        private static void EnqueueReqThermCamFocusPacket()
        {
            Pelco_D.SetID(1);
            byte[] buffer1 = new byte[Pelco_D.PELCOD_SEND_LEN];
            Pelco_D.ReqAbsFocus(buffer1);
            tcpClientAppThermalLens.EnqueuePacket(buffer1, true);
        }
        private static void RunReqPanTimer(bool run)
        {
            IsReqPan = run;
        }
        private static void RunReqTiltTimer(bool run)
        {
            IsReqTilt = run;
        }
        private static void RunDayCamZoomTimer(bool run)
        {
            IsReqDayCamZoom = run;
        }
        private static void RunDayCamFocusTimer(bool run)
        {
            IsReqDayCamFocus = run;
        }
        private static void RunThermCamZoomTimer(bool run)
        {
            IsReqThermCamZoom = run;
        }
        private static void RunThermCamFocusTimer(bool run)
        {
            IsReqThermCamFocus = run;
        }

        /*
        public static void HighMagPelcoDToZoomInfo(uint pelcoDZoom)
        {
            PTZInfoMgr.CamInfo.PelcoDZoomCam1 = pelcoDZoom;

            //Onvif 줌값으로 변환
            //float onvifZoom = CameraInfoConverter.PelcoDZoomToOnvifZoom(PTZInfoMgr.CamInfo.PelcoDZoomCam1, PTZInfoMgr.PtzEnvInfo.ZoomValMax1, PTZInfoMgr.PtzEnvInfo.ZoomValMin1);

            //OnvifZoom To FOV
            PTZInfoMgr.CamInfo.ZoomMag1 = pelcoDZoom;// CameraInfoConverter.OnvifZoomToMag(onvifZoom, PTZInfoMgr.PtzEnvInfo.ZoomFovWidthMax1 / PTZInfoMgr.PtzEnvInfo.ZoomFovWidthMin1);
            //PTZInfoMgr.CamInfo.ZoomMag1 = CameraInfoConverter.LinearToNonLinear(PTZInfoMgr.CamInfo.ZoomMag1, PTZInfoMgr.PtzEnvInfo.ZoomFovWidthMax1 / PTZInfoMgr.PtzEnvInfo.ZoomFovWidthMin1, 0.03f);


            //PTZInfoMgr.CamInfo.FOVWidth1 = CameraInfoConverter.ZoomMagToFov(PTZInfoMgr.CamInfo.ZoomMag1, PTZInfoMgr.PtzEnvInfo.ZoomFovWidthMax1);
            //PTZInfoMgr.CamInfo.FOVHeight1 = CameraInfoConverter.ZoomMagToFov(PTZInfoMgr.CamInfo.ZoomMag1, PTZInfoMgr.PtzEnvInfo.ZoomFovHeightMax1);

            CameraInfoConverter.ZoomMagToFovForFuji((int)PTZInfoMgr.CamInfo.ZoomMag1, out float w, out float  h);

            PTZInfoMgr.CamInfo.FOVWidth1 = w;
            PTZInfoMgr.CamInfo.FOVHeight1 = h;


        }
        */
        public static void ThermLensDZoomToZoomInfo(float dZoom)
        {
            //PTZInfoMgr.CamInfo.PelcoDZoomCam2 = pelcoDZoom;

            ////Onvif 줌값으로 변환
            //float onvifZoom = CameraInfoConverter.PelcoDZoomToOnvifZoom(
            //    PTZInfoMgr.CamInfo.PelcoDZoomCam2, PTZInfoMgr.PtzEnvInfo.ZoomValMax2, PTZInfoMgr.PtzEnvInfo.ZoomValMin2);

            //OnvifZoom To FOV
            PTZInfoMgr.CamInfo.PelcoDZoomCam2 = (uint)dZoom;
            PTZInfoMgr.CamInfo.ZoomMag2 = dZoom;

            PTZInfoMgr.CamInfo.FOVWidth2 = CameraInfoConverter.ZoomMagToFov(PTZInfoMgr.CamInfo.ZoomMag2, PTZInfoMgr.PtzEnvInfo.ZoomFovWidthMax2);
            PTZInfoMgr.CamInfo.FOVHeight2 = CameraInfoConverter.ZoomMagToFov(PTZInfoMgr.CamInfo.ZoomMag2, PTZInfoMgr.PtzEnvInfo.ZoomFovHeightMax2);
        }

        public static void PanPelcoDToPan(uint pelcoDPan)
        {
            //팬값저장
            PTZInfoMgr.CamInfo.PelcoDPan = pelcoDPan;

            //PelcoD 값을 360기준 Deg값으로 변경
            CoordConverter.PelcoDValToDeg360(
                PTZInfoMgr.CamInfo.PelcoDPan, (int)PTZInfoMgr.PtzEnvInfo.PanValMin, (int)PTZInfoMgr.PtzEnvInfo.PanValMax, out float fPanDeg360);

            PTZInfoMgr.CamInfo.AbsPan = fPanDeg360;

            //보정 방위각 계산
            CoordConverter.OffsetPanDeg(PTZInfoMgr.CamInfo.AbsPan, PTZInfoMgr.CamInfo.OffsetPan, out float tempPan);
            PTZInfoMgr.CamInfo.RelPan = tempPan;
        }
        public static void TiltPelcoDToTilt(uint pelcoDTilt)
        {
            //틸트저장
            PTZInfoMgr.CamInfo.PelcoDTilt = pelcoDTilt;

            //PelcoD 값을 360기준 Deg값으로 변경
            CoordConverter.PelcoDValToDeg360(
                PTZInfoMgr.CamInfo.PelcoDTilt, (int)PTZInfoMgr.PtzEnvInfo.TiltValMin, (int)PTZInfoMgr.PtzEnvInfo.TiltValMax, out float fTiltDeg360);

            PTZInfoMgr.CamInfo.AbsTilt = fTiltDeg360;

            //보정 방위각 계산
            CoordConverter.OffsetPanDeg(PTZInfoMgr.CamInfo.AbsTilt, PTZInfoMgr.CamInfo.OffsetTilt, out float tempTilt);
            PTZInfoMgr.CamInfo.RelTilt = tempTilt;
        }



        private static void SetPanTiltSpeed(int speed)
        {
            byte[] buffer = U2SR.SetPanTiltSpeed(speed);
            tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_SET_SPEED);
        }
        private static void SetPresetSpeed(int speed)
        {
            byte[] buffer = U2SR.SetPanTiltPresetSpeed(speed);
            tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_SET_SPEED);
            PTZInfoMgr.CamInfo.PresetSpeed = speed;
        }
        private static void SetTrackingSpeed(int speed)
        {
            byte[] buffer = U2SR.SetPanTiltPresetSpeed(speed);
            tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_SET_SPEED);
            PTZInfoMgr.CamInfo.TrackingSpeed = speed;
        }
        private static void SetThermFocusSpeed(int speed)
        {
            PTZInfoMgr.CamInfo.ThermFocusSpeed = speed;
            byte[] buffer = U2SR.SetThermFocusSpeed(speed);
            tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_THERM_FOCUS_SPEED);
        }
        private static void SendTimerBatteryinfo()
        {
            var timer = new System.Timers.Timer();
            timer.Interval = 20000;
            timer.Start();
            timer.Elapsed += (sender, args) =>
            {
                timer.Stop();

                byte[] buffer = U2SR.ReqBatteryInfo();
                tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_SET_SPEED);
                //Console.WriteLine("[ReqBatteryInfo] {0}", buffer);
                timer.Start();
            };
        }
        private static void SendTimerCaminfo()
        {
            var timer = new System.Timers.Timer();
            timer.Interval = 100;
            timer.Start();
            timer.Elapsed += (sender, args) =>
            {
                timer.Stop();
                PTZInfoMgr.CamInfo.IsPTZConnected = Program.tcpClientAppPTZ.IsConnected();
                                             
                if (Program.commonIni.bCamInfoLog)
                {
                    Console.WriteLine("TimerRequestPTZInfo() Start");

                    System.Console.WriteLine("[PTZStatus] Channel : {0}", MainChannel);
                    System.Console.WriteLine("[PTZStatus] PelcoDPan : {0}, PelcoDTilt : {1} PanTiltSpeed : {2}",
                        PTZInfoMgr.CamInfo.PelcoDPan, PTZInfoMgr.CamInfo.PelcoDTilt, PTZInfoMgr.CamInfo.PanTiltSpeed);
                    System.Console.WriteLine("[PTZStatus] AbsPan : {0}, AbsTilt : {1}",
                        PTZInfoMgr.CamInfo.AbsPan, PTZInfoMgr.CamInfo.AbsTilt);
                    System.Console.WriteLine("[PTZStatus] RelPan : {0}, RelTilt : {1}",
                        PTZInfoMgr.CamInfo.RelPan, PTZInfoMgr.CamInfo.RelTilt);
                    System.Console.WriteLine("[PTZStatus] HighMagZoomPelcoD : {0}, HighMagZoomMag : {1}, HighMagFOVWidth : {2}, HighMagFOVHeight : {3}, HighMagFocus : {4}",
                        PTZInfoMgr.CamInfo.PelcoDZoomCam1, PTZInfoMgr.CamInfo.ZoomMag1, PTZInfoMgr.CamInfo.FOVWidth1, PTZInfoMgr.CamInfo.FOVHeight1, PTZInfoMgr.CamInfo.PelcoDFocusCam1);
                    System.Console.WriteLine("[PTZStatus] ThermLensZoomPelcoD : {0}, HighMagZoomMag : {1}, ThermLensFOVWidth : {2}, ThermLensFOVHeight : {3}, ThermLensFocus : {4}",
                        PTZInfoMgr.CamInfo.PelcoDZoomCam2, PTZInfoMgr.CamInfo.ZoomMag2, PTZInfoMgr.CamInfo.FOVWidth2, PTZInfoMgr.CamInfo.FOVHeight2, PTZInfoMgr.CamInfo.PelcoDFocusCam2);

                    //System.Console.WriteLine("[PTZStatus] Pan : {0}, Tilt : {1}, PanR: {2}, TiltR : {3}, Mag : {4}, FOV : {5},{6}",
                    //    fPanDeg360, fTiltDeg180,
                    //    PTZInfoMgr.camInfo.RelPan, PTZInfoMgr.camInfo.RelTilt,
                    //    PTZInfoMgr.camInfo.ZoomMag, PTZInfoMgr.camInfo.FOVWidth, PTZInfoMgr.camInfo.FOVHeight);
                    
                    System.Console.WriteLine("[PTZStatus] Area : {0}, Altitude : {1}, Range : {2}, CalcRange : {3},  Lat : {4}.{5}.{6}.{7}, Long : {8}.{9}.{10}.{11}",
                      PTZInfoMgr.CamInfo.Name, PTZInfoMgr.CamInfo.CamAltitude, PTZInfoMgr.CamInfo.Range, PTZInfoMgr.CamInfo.TarCalcRange,
                        PTZInfoMgr.CamInfo.Lat.degrees, PTZInfoMgr.CamInfo.Lat.minute, PTZInfoMgr.CamInfo.Lat.second, PTZInfoMgr.CamInfo.Lat.secondDot,
                      PTZInfoMgr.CamInfo.Long.degrees, PTZInfoMgr.CamInfo.Long.minute, PTZInfoMgr.CamInfo.Long.second, PTZInfoMgr.CamInfo.Long.secondDot,
                      PTZInfoMgr.CamInfo.Long.degrees, PTZInfoMgr.CamInfo.Long.minute, PTZInfoMgr.CamInfo.Long.second, PTZInfoMgr.CamInfo.Long.secondDot
                      );

                    Console.WriteLine("[PTZStatus] Conn : {0}", PTZInfoMgr.CamInfo.IsPTZConnected);
                }

                ObservationInfoWithJSON.JSONConverter.ObjToJSONString(PTZInfoMgr.CamInfo, out string json);
                //ObservationInfoWithJSON.JSONConverter.ObjToJSONString(camInfo, out string json);
                m_socketCameraInfoCommonCh.SendPacket(json);

                timer.Start();
            };
        }

        private static void StartInitPanTiltThread()
        {
            if (Program.IsStartPanTiltInit == false)
            {
                IsStartPanTiltInit = true;
                Program.IsPanTiltHome = false;
                PTZInfoMgr.CamInfo.PanTiltInit = 0;
                byte[] buffer = U2SR.SetPanTiltHome();

                tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_PANTILT_HOME);
                //Task 시작
                new Thread(() =>
                {
                    for (int i = 0; i < 60; i++)
                    {
                        {
                            byte[] buffer1 = U2SR.ReqPanTiltHome();
                            tcpClientAppPTZ.EnqueuePacket(buffer1, true, U2SR.LEN_PANTILT_HOME);
                        }
                        if (Program.IsPanTiltHome)
                        {
                            //위치값 저장
                            {
                                byte[] buffer1 = U2SR.SetPanTiltInitStep1();
                                tcpClientAppPTZ.EnqueuePacket(buffer1);

                                byte[] buffer2 = U2SR.SetPanTiltInitStep2();
                                tcpClientAppPTZ.EnqueuePacket(buffer2);

                                byte[] buffer3 = U2SR.SetPanTiltInitStep3();
                                tcpClientAppPTZ.EnqueuePacket(buffer3);
                            }
                            // 정상 종료
                            IsStartPanTiltInit = false;
                            Program.IsPanTiltHome = false;
                            PTZInfoMgr.CamInfo.PanTiltInit = CameraInfo.PTINIT_COMPLETED;
                            return;
                        }
                        Thread.Sleep(2000);
                    }
                    IsStartPanTiltInit = false;
                    Program.IsPanTiltHome = false;
                    PTZInfoMgr.CamInfo.PanTiltInit = CameraInfo.PTINIT_FAIL;
                }).Start();
            }



        }


        private static void InitExternalSocket()
        {
            //1. PTZ 제어 통신 소켓 생성        
            tcpClientAppPTZ.CreateTCPClientSocket(PTZInfoMgr.PtzEnvInfo.IPAddr1, PTZInfoMgr.PtzEnvInfo.Port1, PTZInfoMgr.PtzEnvInfo.ID1, PTZInfoMgr.PtzEnvInfo.PW1,
                U2SR.LEN_GPS, Program.PacketProcess_U2SR, false, "PTZSocket");



            //2. 고배율 카메라 제어
            DayCamCtl = new FujiFilmCameraController(PTZInfoMgr.PtzEnvInfo.IPAddr2, PTZInfoMgr.PtzEnvInfo.ID2, PTZInfoMgr.PtzEnvInfo.PW2);
            DayCamCtl.SetRecvCallback(Program.PacketProcess_FujiFilm);
            //httpClientHanwhaController = new HanwhaCameraController(PTZInfoMgr.PtzEnvInfo.IPAddr2, PTZInfoMgr.PtzEnvInfo.ID2, PTZInfoMgr.PtzEnvInfo.PW2);

            //3. 열상 렌즈 제어 통신 소켓 생성         
#if true
            tcpClientAppThermalLens.CreateTCPClientSocket(PTZInfoMgr.PtzEnvInfo.IPAddr3, PTZInfoMgr.PtzEnvInfo.Port3, PTZInfoMgr.PtzEnvInfo.ID3, PTZInfoMgr.PtzEnvInfo.PW3,
                Pelco_D.PELCOD_RECV_LEN, Program.PacketProcess_ThermCamPelcoD, true, "ThermLensSocket");
#endif
            //4. 열상 카메라 제어
            //httpClientIRCanController = new IRCanThermalCameraController(PTZInfoMgr.PtzEnvInfo.IPAddr4, PTZInfoMgr.PtzEnvInfo.ID4, PTZInfoMgr.PtzEnvInfo.PW4);

#if false
            #region 5. 레이저 조사기 
            tcpClientAppLaserIllum.CreateTCPClientSocket(PTZInfoMgr.PtzEnvInfo.IPAddr5, PTZInfoMgr.PtzEnvInfo.Port5, PTZInfoMgr.PtzEnvInfo.ID5, PTZInfoMgr.PtzEnvInfo.ID5,
                Pelco_D.PELCOD_RECV_LEN, Program.PacketProcess_LaserIllum, true, "LaserIllumSocket");

            //최초에 TCP 한번 성공일경우 광도를 초기 값으로 설정한다.
            //Task 시작
            Task.Run(() =>
            {
                if (tcpClientAppLaserIllum.IsConnected())
                {
                    //전원 Off
                    byte[] buffer1 = new byte[Visca.VISCA_POWER_LEN];
                    Visca.LaserIllumPowerOff(buffer1);
                    tcpClientAppLaserIllum.EnqueuePacket(buffer1);

                    //광도 초기값 설정.

                    //수정
                    byte[] buffer2 = new byte[Visca.VISCA_SEND_LEN];
                    Visca.SetLaserIllumIntensity20(buffer2);
                    tcpClientAppLaserIllum.EnqueuePacket(buffer2);

                    return;
                }
                Thread.Sleep(1000);
            });
            #endregion
#endif

#if false
            tcpClientAppPwrCtlAll.CreateTCPClientSocket(PTZInfoMgr.PtzEnvInfo.IPAddr6, PTZInfoMgr.PtzEnvInfo.Port6, PTZInfoMgr.PtzEnvInfo.ID6, PTZInfoMgr.PtzEnvInfo.PW6,
                Pelco_D.PELCOD_RECV_LEN, null, true, "PwrCtlAllSocket");

            tcpClientAppPwrCtlThermCam.CreateTCPClientSocket(PTZInfoMgr.PtzEnvInfo.IPAddr7, PTZInfoMgr.PtzEnvInfo.Port7, PTZInfoMgr.PtzEnvInfo.ID7, PTZInfoMgr.PtzEnvInfo.PW7,
                Pelco_D.PELCOD_RECV_LEN, null, true, "PwrCtlThermCamSocket");
#endif

          
            new Thread(() =>
            {
                POWER_STATUS prevMainPower = POWER_STATUS.POWER_OFF;
                POWER_STATUS prevThermPower = POWER_STATUS.POWER_OFF;
                bool onlyOne = true;
                while (true)
                {
                    if (tcpClientAppPTZ.IsConnected())
                    {
                        if(onlyOne)
                        {
                            onlyOne = false;

                            byte[] buffer = U2SR.ReqPanContPanTilt();
                            tcpClientAppPTZ.EnqueuePacket(buffer, false);

                            QueryAllPower();
                            InitThermValue();

                            //EnqueueReqPanTiltPacket();
                            EnqueueReqMainDayThermPwrPacket();

                            SetPanTiltSpeed(PTZInfoMgr.CamInfo.PanTiltSpeed);
                            SetPresetSpeed(PTZInfoMgr.CamInfo.PresetSpeed);
                        }
                        if ((
                        prevMainPower == POWER_STATUS.POWER_OFF || prevMainPower == POWER_STATUS.POWER_NOT_RESP) &&
                        PTZInfoMgr.CamInfo.MainPower == POWER_STATUS.POWER_ON)
                        {
                            //전원상태 On 이면 1회만 전송
                            Thread.Sleep(2000);
                            //EnqueueReqPanTiltPacket();
                            EnqueueReqMainDayThermPwrPacket();

                            SetPanTiltSpeed(PTZInfoMgr.CamInfo.PanTiltSpeed);
                            SetPresetSpeed(PTZInfoMgr.CamInfo.PresetSpeed);
                        }
                        prevMainPower = PTZInfoMgr.CamInfo.MainPower;
                        //Console.WriteLine("prevMainPower " + prevMainPower);
                        if ((prevThermPower == POWER_STATUS.POWER_OFF || prevThermPower == POWER_STATUS.POWER_NOT_RESP) &&
                        PTZInfoMgr.CamInfo.ThermCamPower == POWER_STATUS.POWER_ON)
                        {
                            //전원상태 On 이면 1회만 전송
                            Thread.Sleep(5000);
                            InitThermValue();
                        }
                        prevThermPower = PTZInfoMgr.CamInfo.ThermCamPower;
                        //Console.WriteLine("prevThermPower " + prevThermPower);
                    }
                    Thread.Sleep(1000);
                }
            }).Start();

            new Thread(() =>
            {
                while (true)
                {
                    if( ((FujiFilmCameraController)DayCamCtl).IsLogin == true)
                    {
                        DayCamCtl.ReqZoom();
                        return;
                    }
                    Thread.Sleep(1000);
                }
            }).Start();
        }

        private static void QueryAllPower()
        {
            PTZInfoMgr.CamInfo.MainPower = POWER_STATUS.POWER_NOT_RESP;
            byte[] buffer1 = U2SR.ReqMainPower();
            tcpClientAppPTZ.EnqueuePacket(buffer1, true, U2SR.LEN_POWER);

            PTZInfoMgr.CamInfo.DayCamPower = POWER_STATUS.POWER_NOT_RESP;
            byte[] buffer2 = U2SR.ReqDayCamPower();
            tcpClientAppPTZ.EnqueuePacket(buffer2, true, U2SR.LEN_POWER);

            PTZInfoMgr.CamInfo.ThermCamPower = POWER_STATUS.POWER_NOT_RESP;
            byte[] buffer3 = U2SR.ReqThermCamPower();
            tcpClientAppPTZ.EnqueuePacket(buffer3, true, U2SR.LEN_POWER);
        }

        private static void InitThermValue()
        {
            byte[] buffer1 = U2SR.SetDDE(PTZInfoMgr.CamInfo.DDE);
            tcpClientAppPTZ.EnqueuePacket(buffer1, true, buffer1.Length + 6 + 2);

            byte[] buffer2 = U2SR.SetACE(PTZInfoMgr.CamInfo.ACECorrect);
            tcpClientAppPTZ.EnqueuePacket(buffer2, true, buffer2.Length + 6 + 2);

            byte[] buffer3 = U2SR.SetSSO(PTZInfoMgr.CamInfo.SSO);
            tcpClientAppPTZ.EnqueuePacket(buffer3, true, buffer3.Length + 6 + 2);

            byte[] buffer4 = U2SR.SetColor((int)PTZInfoMgr.CamInfo.ThermColor);
            tcpClientAppPTZ.EnqueuePacket(buffer4, true, buffer4.Length + 6 + 2);

            byte[] buffer5 = U2SR.SetDigitalZoom((int)PTZInfoMgr.CamInfo.ThermDigitalZoom);
            tcpClientAppPTZ.EnqueuePacket(buffer5, true, buffer5.Length + 6 + 2);


        }



        /// <summary>
        /// 내부 메세지 소켓 초기화
        /// </summary>
        private static void InitInternalSockets()
        {
            Console.WriteLine("InitSockets() Start");
            MULTICAST_CHANNEL nMulticastCh = MULTICAST_CHANNEL.COMMON;
            switch (MainChannel)
            {
                case 1:
                    nMulticastCh = MULTICAST_CHANNEL.CH1;
                    break;
                case 2:
                    nMulticastCh = MULTICAST_CHANNEL.CH2;
                    break;
                case 3:
                    nMulticastCh = MULTICAST_CHANNEL.CH3;
                    break;
                case 4:
                    nMulticastCh = MULTICAST_CHANNEL.CH4;
                    break;
                case 5:
                    nMulticastCh = MULTICAST_CHANNEL.CH5;
                    break;
                case 6:
                    nMulticastCh = MULTICAST_CHANNEL.CH6;
                    break;
                case 7:
                    nMulticastCh = MULTICAST_CHANNEL.CH7;
                    break;
                case 8:
                    nMulticastCh = MULTICAST_CHANNEL.CH8;
                    break;
                case 9:
                    nMulticastCh = MULTICAST_CHANNEL.CH9;
                    break;
                case 10:
                    nMulticastCh = MULTICAST_CHANNEL.CH10;
                    break;
                default:
                    System.Console.WriteLine("Please enter a channel argument.");
                    break;
            }
            m_socketPTZCommand = new UDPMulticastSocketWithDomain(
            MULTICAST_DOMAIN.CONTROL, nMulticastCh, ReceiveBufferCallback_PTZCommand);

            m_socketCameraInfoCommonCh = new UDPMulticastSocketWithDomain(
             MULTICAST_DOMAIN.CAMERA_INFO, MULTICAST_CHANNEL.COMMON, ReceiveBufferCallback_CameraInfoCommonCh);


            m_socketTrackInfo = new UDPMulticastSocketWithDomain(
                 MULTICAST_DOMAIN.TRACK_INFO, MULTICAST_CHANNEL.COMMON, ReceiveBufferCallback_TrackInfo);
            Program.StartTargetFPSTimer();

            Console.WriteLine("InitSockets() End");
        }

    }
}
