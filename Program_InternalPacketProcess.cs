using CoordConv;
using Newtonsoft.Json;
using ObservationInfoWithJSON;
using SocketLib_Multicast;
using System;
using System.Linq;
using System.Text;
using static ObservationInfoWithJSON.PTZCommand;
using System.Threading;
using System.Windows.Forms;

namespace HawkEye_PTZInterface
{
    partial class Program
    {
        private static int lastPresetTiltSpd;

        #region PTZCmd 처리

        private static UDPMulticastSocketWithDomain m_socketPTZCommand = null;

        private static void ReceiveBufferCallback_PTZCommand(byte[] receiveBuffer)
        {
#if false
            string strJSON = Encoding.Unicode.GetString(receiveBuffer);
#else
            string strJSON = Encoding.UTF8.GetString(receiveBuffer);
#endif

            //System.Console.WriteLine("receiveMessage : " + strJSON.Trim('\0'));

            ObservationInfoWithJSON.PTZCommand ptzCommand = new ObservationInfoWithJSON.PTZCommand();
            //bool bRet = ptzCommand.SetJSONString(strJSON);

            try
            {
                ptzCommand = JsonConvert.DeserializeObject<PTZCommand>(strJSON);

                Console.WriteLine("[PTZCommand] " + ptzCommand.command + " Start Processing");

                #region PTZ 제어                
                ProcessPTCommand(ptzCommand);
                ProcessZFCommand(ptzCommand);
                ProcessPTZFAbsCommand(ptzCommand);
                #endregion

                #region 카메라 제어
                ProcessCameraCmd(ptzCommand);
                #endregion

                #region 전원제어
                ProcessPowerCmd(ptzCommand);
                #endregion

                #region 레이저 조사기 제어
                ProcessLaserCmd(ptzCommand);
                #endregion

                #region 기타
                ProcessETCCmd(ptzCommand);

                #endregion

                Console.WriteLine("[PTZCommand] " + ptzCommand.command + " End Processing");
            }
            catch(Exception e)
            {

            }
        }

        private static void ProcessETCCmd(PTZCommand ptzCommand)
        {
            //if (ptzCommand.cameraType == DEVICE_TYPE.DAY_CAM1)//레이저조사기 팬틸트는 별도 제어
            {
                Pelco_D.SetID(1);


                if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_WIPER)
                {
                  
                    byte[] buffer = U2SR.SetDayCamWiper(ptzCommand.cmdValue);
                    tcpClientAppPTZ.EnqueuePacket(buffer);

                    /*byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                    Pelco_D.SetAux(1, true, buffer);
                    tcpClientAppPTZ.EnqueuePacket(buffer);*/
                }
                else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_DAYCAM_HEATER)
                {
                    byte[] buffer = U2SR.SetDayCamHeater(ptzCommand.on);
                    tcpClientAppPTZ.EnqueuePacket(buffer);
                }
                else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_THERMCAM_HEATER)
                {
                    byte[] buffer = U2SR.SetThermCamHeater(ptzCommand.on);
                    tcpClientAppPTZ.EnqueuePacket(buffer);
                }
                else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.REQ_DAYCAM_TEMP)
                {
                    byte[] buffer = U2SR.ReqDayCamTemp();
                    tcpClientAppPTZ.EnqueuePacket(buffer);
                }
                else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.REQ_THERMCAM_TEMP)
                {
                    byte[] buffer = U2SR.ReqThermCamTemp();
                    tcpClientAppPTZ.EnqueuePacket(buffer);
                }
                else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.REQ_DAYCAM_HEATER_STATUS)
                {
                    byte[] buffer = U2SR.ReqDayCamHeaterStatus();
                    tcpClientAppPTZ.EnqueuePacket(buffer);
                }
                else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.REQ_THERMCAM_HEATER_STATUS)
                {
                    byte[] buffer = U2SR.ReqThermCamHeaterStatus();
                    tcpClientAppPTZ.EnqueuePacket(buffer);
                }
                else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_EXTENDER)
                {
                    byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                    Pelco_D.SetAux(3, ptzCommand.on, buffer);
                    tcpClientAppPTZ.EnqueuePacket(buffer);
                }
            }
        }

        private static void ProcessPTCommand(ObservationInfoWithJSON.PTZCommand ptzCommand)
        {
            if (ptzCommand.cameraType != DEVICE_TYPE.LASER_ILLUM)//레이저조사기 팬틸트는 별도 제어
            {
                Pelco_D.SetID(1);

                if (ptzCommand.command == PTZCommand.SET_PANLEFT)
                {

#if false
                    byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                    if (ptzCommand.speed > 0)
                        Pelco_D.SetPanTiltLeft(ptzCommand.speed, buffer);
                    else
                        Pelco_D.SetPanTiltLeft(PTZInfoMgr.CamInfo.PanTiltSpeed, buffer);
#else


                    byte[] buffer = U2SR.SetPanLeft();

#endif
                    tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_MOVE_POS);

                    RunReqPanTimer(true);
                }
                else if (ptzCommand.command == PTZCommand.SET_PANRIGHT)
                {
#if false
                    byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                    if (ptzCommand.speed > 0)
                        Pelco_D.SetPanTiltRight(ptzCommand.speed, buffer);
                    else
                        Pelco_D.SetPanTiltRight(PTZInfoMgr.CamInfo.PanTiltSpeed, buffer);
#else

                    byte[] buffer = U2SR.SetPanRight();
#endif
                    tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_MOVE_POS);

                    RunReqPanTimer(true);
                }
                else if (ptzCommand.command == PTZCommand.SET_TILTUP)
                {
#if false
                    byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                    if (ptzCommand.speed > 0)
                        Pelco_D.SetPanTiltUp(ptzCommand.speed, buffer);
                    else
                        Pelco_D.SetPanTiltUp(PTZInfoMgr.CamInfo.PanTiltSpeed, buffer);
                    tcpClientAppPTZ.EnqueuePacket(buffer);
#else
                    byte[] buffer = U2SR.SetTiltUp();
                    tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_MOVE_POS);
#endif

                    RunReqTiltTimer(true);
                }
                else if (ptzCommand.command == PTZCommand.SET_TILTDOWN)
                {
#if false
                    byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                    if (ptzCommand.speed > 0)
                        Pelco_D.SetPanTiltDown(ptzCommand.speed, buffer);
                    else
                        Pelco_D.SetPanTiltDown(PTZInfoMgr.CamInfo.PanTiltSpeed, buffer);
                    tcpClientAppPTZ.EnqueuePacket(buffer);
#else
                    byte[] buffer = U2SR.SetTiltDown();
                    tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_MOVE_POS);
#endif
                    RunReqTiltTimer(true);
                }
                else if (ptzCommand.command == PTZCommand.SET_PTLU)
                {
#if false
                    byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                    if (ptzCommand.speed > 0)
                        Pelco_D.SetPanTiltLeftUp(ptzCommand.speed, ptzCommand.speed, buffer);
                    else
                        Pelco_D.SetPanTiltLeftUp(PTZInfoMgr.CamInfo.PanTiltSpeed, PTZInfoMgr.CamInfo.PanTiltSpeed, buffer);
                    tcpClientAppPTZ.EnqueuePacket(buffer);

#else
                    {
                        byte[] buffer = U2SR.SetPanLeft();
                        tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_MOVE_POS);
                    }
                    {
                        byte[] buffer = U2SR.SetTiltUp();
                        tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_MOVE_POS);
                    }
#endif
                    RunReqPanTimer(true);
                    RunReqTiltTimer(true);
                }
                else if (ptzCommand.command == PTZCommand.SET_PTRU)
                {
#if false
                    byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                    if (ptzCommand.speed > 0)
                        Pelco_D.SetPanTiltRightUp(ptzCommand.speed, ptzCommand.speed, buffer);
                    else
                        Pelco_D.SetPanTiltRightUp(PTZInfoMgr.CamInfo.PanTiltSpeed, PTZInfoMgr.CamInfo.PanTiltSpeed, buffer);
                    tcpClientAppPTZ.EnqueuePacket(buffer);
#else
                    {
                        byte[] buffer = U2SR.SetPanRight();
                        tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_MOVE_POS);
                    }
                    {
                        byte[] buffer = U2SR.SetTiltUp();
                        tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_MOVE_POS);
                    }
#endif
                    RunReqPanTimer(true);
                    RunReqTiltTimer(true);
                }
                else if (ptzCommand.command == PTZCommand.SET_PTLD)
                {
#if false
                    byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                    if (ptzCommand.speed > 0)
                        Pelco_D.SetPanTiltLeftDown(ptzCommand.speed, ptzCommand.speed, buffer);
                    else
                        Pelco_D.SetPanTiltLeftDown(PTZInfoMgr.CamInfo.PanTiltSpeed, PTZInfoMgr.CamInfo.PanTiltSpeed, buffer);
                    tcpClientAppPTZ.EnqueuePacket(buffer);
#else
                    {
                        byte[] buffer = U2SR.SetPanLeft();
                        tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_MOVE_POS);
                    }
                    {
                        byte[] buffer = U2SR.SetTiltDown();
                        tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_MOVE_POS);
                    }
#endif
                    RunReqPanTimer(true);
                    RunReqTiltTimer(true);
                }
                else if (ptzCommand.command == PTZCommand.SET_PTRD)
                {
#if false
                    byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                    if (ptzCommand.speed > 0)
                        Pelco_D.SetPanTiltRightDown(ptzCommand.speed, ptzCommand.speed, buffer);
                    else
                        Pelco_D.SetPanTiltRightDown(PTZInfoMgr.CamInfo.PanTiltSpeed, PTZInfoMgr.CamInfo.PanTiltSpeed, buffer);
                    tcpClientAppPTZ.EnqueuePacket(buffer);
#else
                    {
                        byte[] buffer = U2SR.SetPanRight();
                        tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_MOVE_POS);
                    }
                    {
                        byte[] buffer = U2SR.SetTiltDown();
                        tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_MOVE_POS);
                    }
#endif
                    RunReqPanTimer(true);
                    RunReqTiltTimer(true);
                }
                //else if (ptzCommand.command == PTZCommand.SET_Q)
                //{
                //    byte[] buffer = new byte[2];
                //    buffer[0] = (byte)'Q';
                //    buffer[1] = 0x0d;
                //    tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_MOVE_POS - 1);
                //}
                else if (ptzCommand.command == PTZCommand.SET_PTSTOP)
                {
                    {
                        byte[] buffer = U2SR.SetStopPan();
                        tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_MOVE_POS);
                    }
                    {
                        byte[] buffer = U2SR.SetStopTilt();
                        tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_MOVE_POS);
                    }

                    RunReqPanTimer(false);
                    RunReqTiltTimer(false);
                }
                else if (ptzCommand.command == PTZCommand.SET_PANSTOP)
                {
                    byte[] buffer = U2SR.SetStopPan();

                    tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_MOVE_POS);

                    RunReqPanTimer(false);
                    //RunReqTiltTimer(false);
                    //EnqueueReqPanTiltPacket();
                }
                else if (ptzCommand.command == PTZCommand.SET_TILTSTOP)
                {
                    byte[] buffer = U2SR.SetStopTilt();
                    tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_MOVE_POS);

                    //RunReqPanTimer(false);
                    RunReqTiltTimer(false);
                    //EnqueueReqPanTiltPacket();
                    //EnqueueReqTiltPacket();

                }
                else if (ptzCommand.command == PTZCommand.SET_PTSPEED)
                {
                    PTZInfoMgr.CamInfo.PanTiltSpeed = ptzCommand.speed;

                    byte[] buffer = U2SR.SetPanTiltSpeed(ptzCommand.speed);
                    tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_SET_SPEED);

                }
                else if (ptzCommand.command == PTZCommand.SET_PTSPEED_UP)
                {
                    //PTZInfoMgr.CamInfo.PanTiltSpeed++;
                    PTZInfoMgr.CamInfo.PanTiltSpeed += 30;

                    byte[] buffer = U2SR.SetPanTiltSpeed(PTZInfoMgr.CamInfo.PanTiltSpeed);
                    tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_SET_SPEED);
                }
                else if (ptzCommand.command == PTZCommand.SET_PTSPEED_DOWN)
                {
                    //PTZInfoMgr.CamInfo.PanTiltSpeed--;
                    //PTZInfoMgr.CamInfo.PanTiltSpeed -= 50;

                    if (PTZInfoMgr.CamInfo.PanTiltSpeed - 30 < 6)
                    {
                        PTZInfoMgr.CamInfo.PanTiltSpeed = 6;
                    }
                    else
                    {
                        PTZInfoMgr.CamInfo.PanTiltSpeed -= 30;
                    }

                    byte[] buffer = U2SR.SetPanTiltSpeed(PTZInfoMgr.CamInfo.PanTiltSpeed);
                    tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_SET_SPEED);
                }
                else if (ptzCommand.command == PTZCommand.SET_PRESET_SPEED)
                {
                    Console.WriteLine("recv lastPresetTiltSpd={0}, PresetSpeed={1} speed={2}",
                        lastPresetTiltSpd, PTZInfoMgr.CamInfo.PresetSpeed, ptzCommand.speed);


                    lastPresetTiltSpd = lastPresetTiltSpd * ptzCommand.speed / PTZInfoMgr.CamInfo.PresetSpeed;

                    PTZInfoMgr.CamInfo.PresetSpeed = ptzCommand.speed;

                    byte[] spdBuffer = U2SR.SetPanTiltPresetSpeed(PTZInfoMgr.CamInfo.PresetSpeed, lastPresetTiltSpd);

                    Console.WriteLine("SetPanTiltPresetSpd PanSpd={0}, TiltSpd={1} str={2}",
                        PTZInfoMgr.CamInfo.PresetSpeed, lastPresetTiltSpd, Encoding.UTF8.GetString(spdBuffer));

                    tcpClientAppPTZ.EnqueuePacket(spdBuffer, false, U2SR.LEN_SET_PTSPEED);

                }
                else if (ptzCommand.command == PTZCommand.SET_TRACKING_SPEED)
                {
                    //byte[] buffer = U2SR.SetPanTiltPresetSpeed(ptzCommand.speed);
                    //tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_SET_SPEED);
                    PTZInfoMgr.CamInfo.TrackingSpeed = ptzCommand.speed;
                }
                else if (ptzCommand.command == PTZCommand.SET_PTINIT)
                {
                    Program.StartInitPanTiltThread();
                }
                else if (ptzCommand.command == PTZCommand.REQ_DMC)
                {
                    byte[] buffer = U2SR.ReqDMC();
                    tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_DMC);
                }
                else if (ptzCommand.command == PTZCommand.REQ_GPS)
                {
                    byte[] buffer = U2SR.ReqGPS();
                    tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_GPS);
                }
                //kyk
                else if (ptzCommand.command == PTZCommand.SET_PTSPEED_LOW)
                {                    
                    PTZInfoMgr.CamInfo.PanTiltSpeed = PTZInfoMgr.CamInfo.LOW;
                    byte[] buffer = U2SR.SetPanTiltSpeed(PTZInfoMgr.CamInfo.PanTiltSpeed);
                    tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_SET_SPEED);
                }
                else if (ptzCommand.command == PTZCommand.SET_PTSPEED_MID)
                {
                    PTZInfoMgr.CamInfo.PanTiltSpeed = PTZInfoMgr.CamInfo.MID;
                    byte[] buffer = U2SR.SetPanTiltSpeed(PTZInfoMgr.CamInfo.MID);                   
                    tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_SET_SPEED);
                }
                else if (ptzCommand.command == PTZCommand.SET_PTSPEED_HIGH)
                {
                    PTZInfoMgr.CamInfo.PanTiltSpeed = PTZInfoMgr.CamInfo.HIGH;
                    byte[] buffer = U2SR.SetPanTiltSpeed(PTZInfoMgr.CamInfo.HIGH);
                    tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_SET_SPEED);
                }

                //
            }
        }

        private static void ProcessPTZFAbsCommand(ObservationInfoWithJSON.PTZCommand ptzCommand)
        {
            if (ptzCommand.cameraType != DEVICE_TYPE.LASER_ILLUM)//레이저조사기 팬틸트는 별도 제어
            {
                Pelco_D.SetID(1);

                if (ptzCommand.command == PTZCommand.SET_PTZF_ORGVAL)
                {
                    Pelco_D.SetID(1);
                    {
                        //PAN
                        byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        Pelco_D.SetAbsPan((int)ptzCommand.pan, buffer);
                        tcpClientAppPTZ.EnqueuePacket(buffer);
                    }
                    {
                        //TILT
                        byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        Pelco_D.SetAbsTilt((int)ptzCommand.tilt, buffer);
                        tcpClientAppPTZ.EnqueuePacket(buffer);
                    }

                    {
                        //ZOOM
                        byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        Pelco_D.SetAbsZoom((int)ptzCommand.zoom, buffer);
                        tcpClientAppPTZ.EnqueuePacket(buffer);
                    }

                    {
                        //Focus
                        byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        Pelco_D.SetAbsFocus((int)ptzCommand.focus, buffer);
                        tcpClientAppPTZ.EnqueuePacket(buffer);
                    }

                    Pelco_D.SetID(11);
                    {
                        byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        Pelco_D.SetAbsZoom((int)ptzCommand.zoom2, buffer);
                        tcpClientAppThermalLens.EnqueuePacket(buffer);
                    }
                    {
                        byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        Pelco_D.SetAbsFocus((int)ptzCommand.focus2, buffer);
                        tcpClientAppThermalLens.EnqueuePacket(buffer);
                    }


                }
                else if (ptzCommand.command == PTZCommand.SET_PT_ORGVAL)
                {
                    Pelco_D.SetID(1);
                    {
                        //PAN
                        byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        Pelco_D.SetAbsPan((int)ptzCommand.pan, buffer);
                        tcpClientAppPTZ.EnqueuePacket(buffer);
                    }
                    {
                        //TILT
                        byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        Pelco_D.SetAbsTilt((int)ptzCommand.tilt, buffer);
                        tcpClientAppPTZ.EnqueuePacket(buffer);
                    }
                }

                else if (ptzCommand.command == SET_ZF_ORGVAL)
                {
                    DayCamCtl.SetMoveToZoomVal((int)ptzCommand.zoom);
                }
                else if (ptzCommand.command == PTZCommand.SET_PANRELVAL)
                {
#if false
                    //상대값 수신
                    CoordConverter.RelPanToAbsPanDeg(ptzCommand.pan, PTZInfoMgr.CamInfo.OffsetPan, out float absPan);
                    //360기준 Deg값을 PelcoD 값으로 변환
                    CoordConverter.Deg360ToPelcoDVal(
                        absPan, PTZInfoMgr.PtzEnvInfo.PanValMin, PTZInfoMgr.PtzEnvInfo.PanValMax, out uint PelcoDVal);

                    byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                    Pelco_D.SetAbsPan((int)PelcoDVal, buffer);
                    tcpClientAppPTZ.EnqueuePacket(buffer);
#else
                    //상대값 수신
                    CoordConverter.RelPanToAbsPanDeg(ptzCommand.pan, PTZInfoMgr.CamInfo.OffsetPan, out float absPan);
                   
             

                    byte[] buffer = U2SR.SetPanTiltAbsV(absPan, PTZInfoMgr.CamInfo.AbsTilt);
                    tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_SET_PANTILT_ABSV);
#endif
                    RunReqPanTimer(true);
                    IsPanStop(absPan);

                }

                else if (ptzCommand.command == PTZCommand.SET_PANABSVAL)
                {
                    //360기준 Deg값을 PelcoD 값으로 변환
                    CoordConverter.Deg360ToPelcoDVal(
                        ptzCommand.pan, PTZInfoMgr.PtzEnvInfo.PanValMin, PTZInfoMgr.PtzEnvInfo.PanValMax, out uint PelcoDVal);

                    byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                    Pelco_D.SetAbsPan((int)PelcoDVal, buffer);
                    tcpClientAppPTZ.EnqueuePacket(buffer);
                }
                //팬틸트 절대값 이동
                else if (ptzCommand.command == PTZCommand.SET_PANTILTABSVAL)
                {
#if false
                    //360기준 Deg값을 PelcoD 값으로 변환

                    CoordConverter.Deg360ToPelcoDVal(
                      ptzCommand.pan, PTZInfoMgr.PtzEnvInfo.PanValMin, PTZInfoMgr.PtzEnvInfo.PanValMax, out uint PelcoDVal1);

                    byte[] buffer1 = new byte[Pelco_D.PELCOD_SEND_LEN];
                    Pelco_D.SetAbsPan((int)PelcoDVal1, buffer1);
                    tcpClientAppPTZ.EnqueuePacket(buffer1);

                    //360기준 Deg값을 PelcoD 값으로 변환

                    CoordConverter.Deg360ToPelcoDVal(
                        ptzCommand.tilt, PTZInfoMgr.PtzEnvInfo.TiltValMin, PTZInfoMgr.PtzEnvInfo.TiltValMax, out uint PelcoDVal2);

                    byte[] buffer2 = new byte[Pelco_D.PELCOD_SEND_LEN];
                    Pelco_D.SetAbsTilt((int)PelcoDVal2, buffer2);
                    tcpClientAppPTZ.EnqueuePacket(buffer2);


                    RunReqPanTimer(true);
                    IsPanStop(PelcoDVal1);
                    RunReqTiltTimer(true);
                    IsTiltStop(PelcoDVal2);
#endif
                    byte[] qBuffer = new byte[2];
                    qBuffer[0] = (byte)'Q';
                    qBuffer[1] = 0x0d;
                    Console.WriteLine("Set Q");
                    tcpClientAppPTZ.EnqueuePacket(qBuffer, false, U2SR.LEN_MOVE_POS - 1);


                    //RP=0100,1000\r
                    //Set Speed   
                    byte[] spdBuffer = null;
                        
                        if(ptzCommand.cmdValue == 2) //tracking
                    {
                        CameraInfoConverter.GetSpeedByAtoB(PTZInfoMgr.CamInfo.AbsPan, PTZInfoMgr.CamInfo.AbsTilt, ptzCommand.pan, ptzCommand.tilt,
                            PTZInfoMgr.CamInfo.TrackingSpeed, out int tiltSpeed);
                        spdBuffer = U2SR.SetPanTiltPresetSpeed(PTZInfoMgr.CamInfo.TrackingSpeed, tiltSpeed);
                        

                        Console.WriteLine("SetPanTiltPresetSpd pan1={0}, tilt1={1}, pan2={2}, tilt2={3} " +
                "PanSpd={4}, TiltSpd={5} str={6}", PTZInfoMgr.CamInfo.AbsPan, PTZInfoMgr.CamInfo.AbsTilt, ptzCommand.pan, ptzCommand.tilt,
                PTZInfoMgr.CamInfo.TrackingSpeed, tiltSpeed, Encoding.UTF8.GetString(spdBuffer));
                    }
                    else
                    {

                        CameraInfoConverter.GetSpeedByAtoB(PTZInfoMgr.CamInfo.AbsPan, PTZInfoMgr.CamInfo.AbsTilt, ptzCommand.pan, ptzCommand.tilt,
                            PTZInfoMgr.CamInfo.PresetSpeed, out int tiltSpeed);
                        
                        lastPresetTiltSpd = tiltSpeed;

                        spdBuffer = U2SR.SetPanTiltPresetSpeed(PTZInfoMgr.CamInfo.PresetSpeed, tiltSpeed);

                        Console.WriteLine("SetPanTiltPresetSpd pan1={0}, tilt1={1}, pan2={2}, tilt2={3} " +
                   "PanSpd={4}, TiltSpd={5} str={6}", PTZInfoMgr.CamInfo.AbsPan, PTZInfoMgr.CamInfo.AbsTilt, ptzCommand.pan, ptzCommand.tilt,
                   PTZInfoMgr.CamInfo.PresetSpeed, tiltSpeed, Encoding.UTF8.GetString(spdBuffer));
                    }

                    tcpClientAppPTZ.EnqueuePacket(spdBuffer, false, U2SR.LEN_SET_PTSPEED);

                    //Set PanTilT
                    Console.WriteLine("SetPanTiltAbsV {0} {1}", ptzCommand.pan, ptzCommand.tilt);
                    byte[] buffer = U2SR.SetPanTiltAbsV(ptzCommand.pan, ptzCommand.tilt);
                    tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_SET_PANTILT_ABSV);
                }
                else if(ptzCommand.command == SET_PANTILTABSVAL_STOPCHECK)
                {
                    byte[] buffer = U2SR.SetPanTiltAbsV(ptzCommand.pan, ptzCommand.tilt);
                    tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_SET_PANTILT_ABSV);

                    RunReqPanTimer(true);
                    IsPanStop(ptzCommand.pan);
                }
                //틸트 절대값 이동
                else if (ptzCommand.command == PTZCommand.SET_TILTABSVAL)
                {
                    //360기준 Deg값을 PelcoD 값으로 변환
                    CoordConverter.Deg360ToPelcoDVal(
                        ptzCommand.pan, PTZInfoMgr.PtzEnvInfo.TiltValMin, PTZInfoMgr.PtzEnvInfo.TiltValMax, out uint PelcoDVal);

                    byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                    Pelco_D.SetAbsTilt((int)PelcoDVal, buffer);
                    tcpClientAppPTZ.EnqueuePacket(buffer);
                }

                else if (ptzCommand.command == PTZCommand.SET_PANTILTZOOMABSVAL)
                {
                    //Pan
                    CoordConverter.Deg360ToPelcoDVal(
                        ptzCommand.pan, PTZInfoMgr.PtzEnvInfo.PanValMin, PTZInfoMgr.PtzEnvInfo.PanValMax, out uint PelcoDVal1);

                    byte[] buffer1 = new byte[Pelco_D.PELCOD_SEND_LEN];
                    Pelco_D.SetAbsPan((int)PelcoDVal1, buffer1);
                    tcpClientAppPTZ.EnqueuePacket(buffer1);

                    //Tilt                    
                    CoordConverter.Deg360ToPelcoDVal(
                        ptzCommand.tilt, PTZInfoMgr.PtzEnvInfo.TiltValMin, PTZInfoMgr.PtzEnvInfo.TiltValMax, out uint PelcoDVal2);

                    byte[] buffer2 = new byte[Pelco_D.PELCOD_SEND_LEN];
                    Pelco_D.SetAbsTilt((int)PelcoDVal2, buffer2);
                    tcpClientAppPTZ.EnqueuePacket(buffer2);


                    //Zoom
                    byte[] buffer3 = new byte[Pelco_D.PELCOD_SEND_LEN];
                    Pelco_D.SetAbsZoom((int)ptzCommand.zoom, buffer3);
                    tcpClientAppPTZ.EnqueuePacket(buffer3);
                }

                else if (ptzCommand.command == PTZCommand.SET_PANZOOMABSVAL)
                {
                    //Pan
                    CameraInfoConverter.GetSpeedByAtoB(PTZInfoMgr.CamInfo.AbsPan, PTZInfoMgr.CamInfo.AbsTilt, ptzCommand.pan, ptzCommand.tilt,
                      PTZInfoMgr.CamInfo.TrackingSpeed, out int tiltSpeed);
                    byte [] spdBuffer = U2SR.SetPanTiltPresetSpeed(PTZInfoMgr.CamInfo.TrackingSpeed, tiltSpeed);
                    tcpClientAppPTZ.EnqueuePacket(spdBuffer, false, U2SR.LEN_SET_PTSPEED);

                    byte[] buffer = U2SR.SetPanTiltAbsV(ptzCommand.pan, PTZInfoMgr.CamInfo.AbsTilt);
                    tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_SET_PANTILT_ABSV);

                    DayCamCtl.SetMoveToZoomVal((int)ptzCommand.zoom);

                    //RunReqPanTimer(true);
                    //IsPanStop(PelcoDPanVal);
                }

                else if (ptzCommand.command == PTZCommand.QUERY_PANTILTABSVAL)
                {
#if false
                    Pelco_D.SetID(1);
                    byte[] buffer1 = new byte[Pelco_D.PELCOD_SEND_LEN];
                    Pelco_D.ReqAbsPan(buffer1);
                    tcpClientAppPTZ.EnqueuePacket(buffer1, true);

                    Pelco_D.SetID(1);
                    byte[] buffer2 = new byte[Pelco_D.PELCOD_SEND_LEN];
                    Pelco_D.ReqAbsTilt(buffer2);
                    tcpClientAppPTZ.EnqueuePacket(buffer2, true);
#else
                    //byte[] buffer = U2SR.ReqPanTiltPos();
                    //tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_PANTILT_POS);
#endif
                }

                else if (ptzCommand.command == PTZCommand.SET_PIXEL_TO_MOVE)
                {
                    PTZInfoMgr.ClearPTZ(ptzCommand.cameraType);
                    //EnqueueReqPanTiltPacket();
                    //EnqueueReqTiltPacket();
                    if (ptzCommand.cameraType == DEVICE_TYPE.DAY_CAM1)
                        EnqueueReqDayCamZoomPacket();
                    else if (ptzCommand.cameraType == DEVICE_TYPE.THERM_CAM)
                        EnqueueReqThermCamZoomPacket();
                    else
                        return;

                    WaitingPTZandPTZFMove(ptzCommand);


                }
                else if (ptzCommand.command == PTZCommand.REQ_PTZF_FOR_MSG)
                {
                    PTZInfoMgr.ClearPTZF();
                    //EnqueueReqPanTiltPacket();
                    DayCamCtl.ReqZoom();
                    //EnqueueReqTiltPacket();
                   // PTZInfoMgr.TempCamInfo.PelcoDZoomCam1 = (uint)DayCamCtl.GetZoom();
                    
                    WaitingPTZFandSendMsg();

                }
                else if (ptzCommand.command == PTZCommand.REFRESH_PTZF_VALUE)
                {
                    //PTZF 값 클리어
                    PTZInfoMgr.ClearPTZF();
                    //EnqueueReqPanTiltPacket();
                   // EnqueueReqTiltPacket();
                    EnqueueReqDayCamZoomPacket();
                    EnqueueReqDayCamFocusPacket();
                    EnqueueReqThermCamZoomPacket();
                    EnqueueReqThermCamFocusPacket();
                    return;
                }
                else if (ptzCommand.command == PTZCommand.REFRESH_PT_VALUE)
                {
                    //PTZInfoMgr.ClearPT();
                    //EnqueueReqPanTiltPacket();
                    //EnqueueReqTiltPacket();
                    return;
                }
            }
        }

        private static void WaitingPTZandPTZFMove(PTZCommand ptzCommand)
        {
            var timer = new System.Timers.Timer();
            timer.Interval = 200;
            timer.Start();
            int timeoutCnt = 0;
            timer.Elapsed += (sender, args) =>
            {
                timer.Stop();
                timeoutCnt++;

                if (ptzCommand.cameraType == DEVICE_TYPE.DAY_CAM1)
                {
                    if (PTZInfoMgr.CamInfo.AbsPan != float.MinValue && PTZInfoMgr.CamInfo.AbsTilt != float.MinValue &&
                        PTZInfoMgr.CamInfo.FOVWidth1 > 0 && PTZInfoMgr.CamInfo.FOVHeight1 > 0 || timeoutCnt > 10
                        )
                    {
                        PixelInfoToPTZMove(ptzCommand.TargetX, ptzCommand.TargetY,
                    ptzCommand.VideoWidth, ptzCommand.VideoHeight,
                    PTZInfoMgr.CamInfo.FOVWidth1, PTZInfoMgr.CamInfo.FOVHeight1,
                    PTZInfoMgr.CamInfo.AbsPan, PTZInfoMgr.CamInfo.AbsTilt);
                        return;
                    }
                }
                else if (ptzCommand.cameraType == DEVICE_TYPE.THERM_CAM)
                {
                    if (PTZInfoMgr.CamInfo.AbsPan != float.MinValue && PTZInfoMgr.CamInfo.AbsTilt != float.MinValue &&
                      PTZInfoMgr.CamInfo.FOVWidth2 > 0 && PTZInfoMgr.CamInfo.FOVHeight2 > 0 || timeoutCnt > 10
                      )
                    {
                        PixelInfoToPTZMove(ptzCommand.TargetX, ptzCommand.TargetY,
                    ptzCommand.VideoWidth, ptzCommand.VideoHeight,
                    PTZInfoMgr.CamInfo.FOVWidth2, PTZInfoMgr.CamInfo.FOVHeight2,
                    PTZInfoMgr.CamInfo.AbsPan, PTZInfoMgr.CamInfo.AbsTilt);
                        return;
                    }
                }
                timer.Start();
            };
        }



        private static void PixelInfoToPTZMove(
            int targetX, int targetY, int videoWidth, int videoHeight, float fovWidth, float fovHeight, float absPan, float absTilt)
        {          
            bool ret = CoordConverter.VideoObjectInfoToPTZInfo(
                     targetX, targetY,
                     videoWidth, videoHeight,
                     fovWidth, fovHeight,
                     absPan, absTilt,
                     out float targetPan, out float targetTilt);


             
            if (ret)
            {
                //Pan
                CoordConverter.Deg360ToPelcoDVal(
                   targetPan, PTZInfoMgr.PtzEnvInfo.PanValMin, PTZInfoMgr.PtzEnvInfo.PanValMax, out uint PelcoDVal1);

                byte[] buffer1 = new byte[Pelco_D.PELCOD_SEND_LEN];
                Pelco_D.SetAbsPan((int)PelcoDVal1, buffer1);
                tcpClientAppPTZ.EnqueuePacket(buffer1);

                //Tilt                    
                CoordConverter.Deg360ToPelcoDVal(
                    targetTilt, PTZInfoMgr.PtzEnvInfo.TiltValMin, PTZInfoMgr.PtzEnvInfo.TiltValMax, out uint PelcoDVal2);

                byte[] buffer2 = new byte[Pelco_D.PELCOD_SEND_LEN];
                Pelco_D.SetAbsTilt((int)PelcoDVal2, buffer2);
                tcpClientAppPTZ.EnqueuePacket(buffer2);
            }
            else
                Console.WriteLine("SET_PIXEL_TO_MOVE Failure");
        }

        private static void WaitingPTZFandSendMsg()
        {
            var timer = new System.Timers.Timer();
            timer.Interval = 200;
            timer.Start();
            int timeoutCnt = 0;
            timer.Elapsed += (sender, args) =>
            {
                timer.Stop();
                timeoutCnt++;

                //
                if (PTZInfoMgr.TempCamInfo.AbsPan != float.MinValue && PTZInfoMgr.TempCamInfo.AbsTilt != float.MinValue &&
            PTZInfoMgr.TempCamInfo.PelcoDZoomCam1 != uint.MinValue || timeoutCnt > 10
            )
                {
                    //참조점 추가
                    PTZCommand cmd = new PTZCommand();
                    cmd.command = PTZCommand.RESP_PTZF_FOR_MSG;

                    cmd.AbsPan = PTZInfoMgr.TempCamInfo.AbsPan;
                    cmd.AbsTilt = PTZInfoMgr.TempCamInfo.AbsTilt;

                    cmd.RelPan = PTZInfoMgr.TempCamInfo.RelPan;
                    cmd.RelTilt = PTZInfoMgr.TempCamInfo.RelTilt;

                    cmd.PelcoDZoomCam1 = (int)PTZInfoMgr.TempCamInfo.PelcoDZoomCam1;

                    ObservationInfoWithJSON.JSONConverter.ObjToJSONString(cmd, out string json);
                    m_socketPTZCommand.SendPacket(json);

                    return;
                }

                timer.Start();
            };
        }

        private static void IsPanStop(float absVal)
        {
            var timer = new System.Timers.Timer();
            timer.Interval = 600;
            timer.Start();
            int timeoutCnt = 0;
            timer.Elapsed += (sender, args) =>
            {
                timer.Stop();

                timeoutCnt++;
                int retPan = Math.Abs((int)(PTZInfoMgr.CamInfo.AbsPan - absVal));

                if (Math.Abs(PTZInfoMgr.CamInfo.AbsPan) == Math.Abs(absVal))
                {
                    //Thread.Sleep(1000);
                    RunReqPanTimer(false);
                    return;
                }
                if (timeoutCnt > 40)
                {
                    RunReqPanTimer(false);
                    return;
                }
                timer.Start();
            };
        }
        private static void IsTiltStop(uint PelcoDVal)
        {
            var timer = new System.Timers.Timer();
            timer.Interval = 600;
            timer.Start();
            int timeoutCnt = 0;
            timer.Elapsed += (sender, args) =>
            {
                timer.Stop();

                timeoutCnt++;
                int retPan = Math.Abs((int)(PTZInfoMgr.CamInfo.PelcoDPan - PelcoDVal));

                if ((PTZInfoMgr.CamInfo.PelcoDTilt == PelcoDVal) || (retPan < 10))
                {
                //Thread.Sleep(1000);
                RunReqTiltTimer(false);
                    return;
                }
                if (timeoutCnt > 20)
                {
                    RunReqTiltTimer(false);
                    return;
                }
                timer.Start();
            };
        }

        private static void ProcessZFCommand(ObservationInfoWithJSON.PTZCommand ptzCommand)
        {
            if (ptzCommand.cameraType != DEVICE_TYPE.LASER_ILLUM)//레이저조사기 팬틸트는 별도 제어
            {
                //줌 절대값 이동
                if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_ZOOMABSVAL)
                {
                    if (ptzCommand.cameraType == DEVICE_TYPE.DAY_CAM1)
                    {
                        //Pelco_D.SetID(1);
                        //byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        //Pelco_D.SetAbsZoom((int)ptzCommand.zoom, buffer);
                        //tcpClientAppPTZ.EnqueuePacket(buffer);
                    }
                    else if (ptzCommand.cameraType == DEVICE_TYPE.THERM_CAM)
                    {
                        //Pelco_D.SetID(11);
                        //byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        //Pelco_D.SetAbsZoom((int)ptzCommand.zoom, buffer);
                        //tcpClientAppThermalLens.EnqueuePacket(buffer);
                    }

                }
                //포커스 절대값 이동
                else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_FOCUSABSVAL)
                {
                    if (ptzCommand.cameraType == DEVICE_TYPE.DAY_CAM1)
                    {
                        //Pelco_D.SetID(1);
                        //byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        //Pelco_D.SetAbsFocus((int)ptzCommand.focus, buffer);
                        //tcpClientAppPTZ.EnqueuePacket(buffer);
                    }
                    else if (ptzCommand.cameraType == DEVICE_TYPE.THERM_CAM)
                    {
                        //Pelco_D.SetID(11);
                        //byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        //Pelco_D.SetAbsFocus((int)ptzCommand.focus, buffer);
                        //tcpClientAppThermalLens.EnqueuePacket(buffer);
                    }

                }
                else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_ZOOMTELE)
                {
                    if (ptzCommand.cameraType == DEVICE_TYPE.DAY_CAM1)
                    {
                        //Pelco_D.SetID(1);
                        //byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        //Pelco_D.SetZoomTele(buffer);
                        //tcpClientAppPTZ.EnqueuePacket(buffer);

                        DayCamCtl.ZoomIn();

                        //RunDayCamZoomTimer(true);
                    }
                    else if (ptzCommand.cameraType == DEVICE_TYPE.THERM_CAM)
                    {
                        //250210 열상 줌렌즈로 변경으로 프로토콜 변경                        
                        Pelco_D.SetID(1);
                        byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        Pelco_D.SetZoomTele(buffer);
                        tcpClientAppThermalLens.EnqueuePacket(buffer);
                        RunThermCamZoomTimer(true);

                        //httpClientIRCanController.DigitalZoomIn();
                        //ThermDZoomTele();
                        //DigitalZoomStatus = DIGITALZOOM_STATUS.ZOOM_TELE;
                    }
                }
                else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_ZOOMWIDE)
                {
                    if (ptzCommand.cameraType == DEVICE_TYPE.DAY_CAM1)
                    {
                        //Pelco_D.SetID(1);
                        //byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        //Pelco_D.SetZoomWide(buffer);
                        //tcpClientAppPTZ.EnqueuePacket(buffer);

                        DayCamCtl.ZoomOut();
                      
                        //RunDayCamZoomTimer(true);
                    }
                    else if (ptzCommand.cameraType == DEVICE_TYPE.THERM_CAM)
                    {
                        //250210 열상 줌렌즈로 변경으로 프로토콜 변경
                        byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        Pelco_D.SetZoomWide(buffer);
                        tcpClientAppThermalLens.EnqueuePacket(buffer);
                        RunThermCamZoomTimer(true);

                        //ThermDZoomWide();
                        //DigitalZoomStatus = DIGITALZOOM_STATUS.ZOOM_WIDE;


                    }
                }
                else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_ZOOMSTOP)
                {
                    if (ptzCommand.cameraType == DEVICE_TYPE.DAY_CAM1)
                    {
                        //Pelco_D.SetID(1);
                        //byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        //Pelco_D.SetZoomStop(buffer);
                        //tcpClientAppPTZ.EnqueuePacket(buffer);

                        DayCamCtl.ZoomStop();

                        //RunDayCamZoomTimer(false);
                        //EnqueueReqDayCamZoomPacket();
                    }
                    else if (ptzCommand.cameraType == DEVICE_TYPE.THERM_CAM)
                    {
                        //250210 열상 줌렌즈로 변경으로 프로토콜 변경
                        Pelco_D.SetID(1);
                        byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        Pelco_D.SetZoomStop(buffer);
                        tcpClientAppThermalLens.EnqueuePacket(buffer);
                        RunThermCamZoomTimer(false);
                        
                        //DigitalZoomStatus = DIGITALZOOM_STATUS.ZOOM_STOP;
                    }
                }
                else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_FOCUSNEAR)
                {
                    if (ptzCommand.cameraType == DEVICE_TYPE.DAY_CAM1)
                    {
                        //Pelco_D.SetID(1);
                        //byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        //Pelco_D.SetFocusNear(buffer);
                        //tcpClientAppPTZ.EnqueuePacket(buffer);

                        //RunDayCamFocusTimer(true);
                        DayCamCtl.FocusNear();


                    }
                    else if (ptzCommand.cameraType == DEVICE_TYPE.THERM_CAM)
                    {
                        //byte[] buffer = U2SR.SetThermFocusIn();
                        //tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_FOCUS);

                        //250210 열상 줌렌즈로 변경으로 프로토콜 변경
                        Pelco_D.SetID(1);
                        byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        Pelco_D.SetFocusNear(buffer);
                        tcpClientAppThermalLens.EnqueuePacket(buffer);
                        RunThermCamFocusTimer(true);
                    }
                }
                else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_FOCUSFAR)
                {
                    if (ptzCommand.cameraType == DEVICE_TYPE.DAY_CAM1)
                    {
                        DayCamCtl.FocusFar();
                        //Pelco_D.SetID(1);
                        //byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        //Pelco_D.SetFocusFar(buffer);
                        //tcpClientAppPTZ.EnqueuePacket(buffer);

                        //RunDayCamFocusTimer(true);

                    }
                    else if (ptzCommand.cameraType == DEVICE_TYPE.THERM_CAM)
                    {
                        //byte[] buffer = U2SR.SetThermFocusOut();
                        //tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_FOCUS);

                        //250210 열상 줌렌즈로 변경으로 프로토콜 변경
                        Pelco_D.SetID(1);
                        byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        Pelco_D.SetFocusFar(buffer);
                        tcpClientAppThermalLens.EnqueuePacket(buffer);
                        RunThermCamFocusTimer(true);
                    }
                }
                else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_ZFSTOP)
                {
                    if (ptzCommand.cameraType == DEVICE_TYPE.DAY_CAM1)
                    {
                        DayCamCtl.ZoomStop();
                        DayCamCtl.FocusStop();                        
                    }
                    else if (ptzCommand.cameraType == DEVICE_TYPE.THERM_CAM)
                    {
                        byte[] buffer = U2SR.SetThermFocusStop();
                        tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_FOCUS);
                        DigitalZoomStatus = DIGITALZOOM_STATUS.ZOOM_STOP;
                    }
                }
                else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_FOCUSAUTO)
                {
                    if (ptzCommand.cameraType == DEVICE_TYPE.DAY_CAM1)
                    {
                        //Pelco_D.SetID(1);
                        //byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        //Pelco_D.SetFocusAutoDayCam(ptzCommand.on, buffer);
                        //tcpClientAppPTZ.EnqueuePacket(buffer);
                        DayCamCtl.AutoFocus();                        
                    }
                    else if (ptzCommand.cameraType == DEVICE_TYPE.THERM_CAM)
                    {
                        Pelco_D.SetID(1);
                        byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        Pelco_D.SetFocusAutoThermCam(ptzCommand.on, buffer);
                        tcpClientAppThermalLens.EnqueuePacket(buffer);
                    }
                }
                else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_FOCUSMANUAL)
                {
                    if (ptzCommand.cameraType == DEVICE_TYPE.DAY_CAM1)
                    {
                        //Pelco_D.SetID(1);
                        //byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        //Pelco_D.SetFocusAutoDayCam(ptzCommand.on, buffer);
                        //tcpClientAppPTZ.EnqueuePacket(buffer);
                        DayCamCtl.ManualFocus();
                    }
                    else if (ptzCommand.cameraType == DEVICE_TYPE.THERM_CAM)
                    {
                        //Pelco_D.SetID(11);
                        //byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        //Pelco_D.SetFocusAutoThermCam(ptzCommand.on, buffer);
                        //tcpClientAppThermalLens.EnqueuePacket(buffer);
                    }
                }
                else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_FOCUSSTOP)
                {
                    if (ptzCommand.cameraType == DEVICE_TYPE.DAY_CAM1)
                    {
                        DayCamCtl.FocusStop();
                        //Pelco_D.SetID(1);
                        //byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        //Pelco_D.SetFocusStop(buffer);
                        //tcpClientAppPTZ.EnqueuePacket(buffer);

                        //RunDayCamFocusTimer(false);
                        //EnqueueReqDayCamFocusPacket();
                    }
                    else if (ptzCommand.cameraType == DEVICE_TYPE.THERM_CAM)
                    {
                        //byte[] buffer = U2SR.SetThermFocusStop();
                        //tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_FOCUS);

                        //250210 열상 줌렌즈로 변경으로 프로토콜 변경
                        Pelco_D.SetID(1);
                        byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                        Pelco_D.SetFocusStop(buffer);
                        tcpClientAppThermalLens.EnqueuePacket(buffer);
                        RunThermCamFocusTimer(false);

                        //EnqueueReqThermCamFocusPacket();
                    }
                }
                else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_DIGITAL_ZOOMTELE)
                {
                    if (ptzCommand.cameraType == DEVICE_TYPE.THERM_CAM)
                    {
                        ThermDZoomTele();
                    }
                }
                else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_DIGITAL_ZOOMWIDE)
                {
                    if (ptzCommand.cameraType == DEVICE_TYPE.THERM_CAM)
                    {
                        ThermDZoomWide();
                    }
                }
                else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_FOCUSSPEED)
                {
                    if (ptzCommand.cameraType == DEVICE_TYPE.THERM_CAM)
                    {
                        SetThermFocusSpeed(ptzCommand.speed);
                    }
                }
            }
        }

        private static void ThermDZoomWide()
        {
            PTZInfoMgr.CamInfo.ThermDigitalZoom -= 0.1;
            if (PTZInfoMgr.CamInfo.ThermDigitalZoom < 1)
            {
                PTZInfoMgr.CamInfo.ThermDigitalZoom = 1;
            }
            
            byte[] buffer = U2SR.SetDigitalZoom(PTZInfoMgr.CamInfo.ThermDigitalZoom);

            tcpClientAppPTZ.EnqueuePacket(buffer, true, buffer.Length + 6 + 2);
        }

        private static void ThermDZoomTele()
        {
            PTZInfoMgr.CamInfo.ThermDigitalZoom+=0.1;
            if (PTZInfoMgr.CamInfo.ThermDigitalZoom > 8)
            {
                PTZInfoMgr.CamInfo.ThermDigitalZoom = 8;
            }            
            byte[] buffer = U2SR.SetDigitalZoom(PTZInfoMgr.CamInfo.ThermDigitalZoom);
        
            tcpClientAppPTZ.EnqueuePacket(buffer, true, buffer.Length + 6 + 2);
        }

        private static void ProcessCameraCmd(PTZCommand ptzCommand)
        {
            if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_DAY)
            {
                //httpClientHanwhaController.SetDay(true);
                DayCamCtl.SetDay(true);
            }
            else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_NIGHT)
            {
                //httpClientHanwhaController.SetDay(false);
                DayCamCtl.SetDay(false);
            }
            else if(ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_GAIN)
            {
                DayCamCtl.SetManualGain(ptzCommand.cmdValue);
            }
            else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_ENHANCEMENT)
            {
                //httpClientHanwhaController.SetDeFog(ptzCommand.on);
                //new Thread(() =>
                //{
                    DayCamCtl.SetDeFog(ptzCommand.on);

                //}).Start();
                
            }
            else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_STABILIZER)
            {
                //httpClientHanwhaController.SetDIS(ptzCommand.on);
                DayCamCtl.SetDIS(ptzCommand.on);
            }
            else if (ptzCommand.command == PTZCommand.SET_FILTER1)
            {
                DayCamCtl.SetVLC(ptzCommand.on);
            }
            else if (ptzCommand.command == PTZCommand.SET_FILTER2)
            {
                DayCamCtl.SetDeNoise(ptzCommand.cmdValue);
            }

            else if (ptzCommand.command == PTZCommand.SET_BRIGHTNESS)
            {
                DayCamCtl.SetBrightness(ptzCommand.cmdValue);
            }
            else if (ptzCommand.command == PTZCommand.SET_CONTRAST)
            {
                DayCamCtl.SetContrast(ptzCommand.cmdValue);
            }            
            else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_BLACKHOT)
            {
                //httpClientIRCanController.SetColor(THERMCAM_COLOR.BLACK_HOT);
            }
            else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_WHITEHOT)
            {
                //httpClientIRCanController.SetColor(THERMCAM_COLOR.WHITE_HOT);
            }
            else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_RAINBOW)
            {
                //httpClientIRCanController.SetColor(THERMCAM_COLOR.RAINBOW);
            }
            else if(ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_THERM_COLOR_NEXT)
            {
                PTZInfoMgr.CamInfo.ThermColor++;
                if (PTZInfoMgr.CamInfo.ThermColor == THERMCAM_COLOR.UPPER_LIMIT)
                {
                    PTZInfoMgr.CamInfo.ThermColor = THERMCAM_COLOR.WHITE_HOT;                    
                }
                
                byte[] buffer = U2SR.SetColor((int)PTZInfoMgr.CamInfo.ThermColor);                
                tcpClientAppPTZ.EnqueuePacket(buffer, true, buffer.Length + 6 + 2);                
            }
            else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_THERM_COLOR_PREV)
            {
                PTZInfoMgr.CamInfo.ThermColor--;
                if (PTZInfoMgr.CamInfo.ThermColor == THERMCAM_COLOR.LOWER_LIMIT)
                {
                    PTZInfoMgr.CamInfo.ThermColor = THERMCAM_COLOR.COLOR4;
                }
              
                byte[] buffer = U2SR.SetColor((int)PTZInfoMgr.CamInfo.ThermColor);
                tcpClientAppPTZ.EnqueuePacket(buffer, true, buffer.Length + 6 + 2);
            }
            else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_DDE)
            {
                byte[] buffer = U2SR.SetDDE(ptzCommand.cmdValue);
                tcpClientAppPTZ.EnqueuePacket(buffer, true, buffer.Length + 6 + 2);

                PTZInfoMgr.CamInfo.DDE = ptzCommand.cmdValue;
            
            }
            else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_ACE)
            {
                byte[] buffer = U2SR.SetACE(ptzCommand.cmdValue);
                tcpClientAppPTZ.EnqueuePacket(buffer, true, buffer.Length + 6 + 2);
                PTZInfoMgr.CamInfo.ACECorrect = ptzCommand.cmdValue;
              
            }
            else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_SSO)
            {
                byte[] buffer = U2SR.SetSSO(ptzCommand.cmdValue);
                tcpClientAppPTZ.EnqueuePacket(buffer, true, buffer.Length + 6 + 2);
                PTZInfoMgr.CamInfo.SSO = ptzCommand.cmdValue;
            }
        }
        private static void ProcessPowerCmd(ObservationInfoWithJSON.PTZCommand ptzCommand)
        {
            if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_MAINPOWER)
            {
                PTZInfoMgr.CamInfo.MainPower = POWER_STATUS.POWER_NOT_RESP;
                byte[] buffer1 = U2SR.SetMainPower(ptzCommand.on);
                if (ptzCommand.on)
                {
                    tcpClientAppPTZ.EnqueuePacket(buffer1, true, U2SR.LEN_POWER);
                    tcpClientAppPTZ.EnqueuePacket(buffer1, true, 100);

                    PTZInfoMgr.CamInfo.DayCamPower = POWER_STATUS.POWER_NOT_RESP;
                    byte[] buffer2 = U2SR.SetDayCamPower(ptzCommand.on);
                    tcpClientAppPTZ.EnqueuePacket(buffer2, true, U2SR.LEN_POWER);

                    byte[] buffer3 = U2SR.ReqMainPower();
                    tcpClientAppPTZ.EnqueuePacket(buffer3, true, U2SR.LEN_POWER);

                    byte[] buffer4 = U2SR.ReqDayCamPower();
                    tcpClientAppPTZ.EnqueuePacket(buffer4, true, U2SR.LEN_POWER);

                    byte[] buffer = U2SR.ReqPanContPanTilt();
                    tcpClientAppPTZ.EnqueuePacket(buffer, false);
                }
                else
                {
                    //전체 종료
                    tcpClientAppPTZ.EnqueuePacket(buffer1, true, U2SR.LEN_POWER);

                    byte[] reqDayPwr = U2SR.ReqDayCamPower();
                    tcpClientAppPTZ.EnqueuePacket(reqDayPwr, true, U2SR.LEN_POWER);

                    byte[] reqThermPwr = U2SR.ReqThermCamPower();
                    tcpClientAppPTZ.EnqueuePacket(reqThermPwr, true, U2SR.LEN_POWER);
                }

              
            }
            else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_DAYPOWER)
            {
                PTZInfoMgr.CamInfo.DayCamPower = POWER_STATUS.POWER_NOT_RESP;

                byte[] buffer2 = U2SR.SetDayCamPower(ptzCommand.on);
                tcpClientAppPTZ.EnqueuePacket(buffer2, true, U2SR.LEN_POWER);
            }
            else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_THERMCAMPOWER)
            {
                PTZInfoMgr.CamInfo.ThermCamPower = POWER_STATUS.POWER_NOT_RESP;

                byte[] buffer = U2SR.SetThermCamPower(ptzCommand.on);
                tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_POWER);
            }
            else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.QUERY_MAINPOWER)
            {
                PTZInfoMgr.CamInfo.MainPower = POWER_STATUS.POWER_NOT_RESP;
                byte[] buffer = U2SR.ReqMainPower();
                tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_POWER);
            }
          
            else if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.QUERY_DAYPOWER)
            {
                PTZInfoMgr.CamInfo.DayCamPower = POWER_STATUS.POWER_NOT_RESP;
                byte[] buffer = U2SR.ReqDayCamPower();
                tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_POWER);
            }
            else if(ptzCommand.command == ObservationInfoWithJSON.PTZCommand.QUERY_THERMCAMPOWER)
            {
                PTZInfoMgr.CamInfo.ThermCamPower = POWER_STATUS.POWER_NOT_RESP;
                byte[] buffer = U2SR.ReqThermCamPower();
                tcpClientAppPTZ.EnqueuePacket(buffer, true, U2SR.LEN_POWER);
            }
        }

        //레이저 조사기 명령
        private static void ProcessLaserCmd(PTZCommand ptzCommand)
        {
            if (ptzCommand.cameraType == DEVICE_TYPE.LASER_ILLUM)
            {
                //if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_LASERILLUM_POWER)
                //{
                //    byte[] buffer = new byte[Pelco_D.PELCOD_SEND_LEN];
                //    Pelco_D.SetMainPower(true, buffer);
                //    tcpClientAppLaserIllum.EnqueuePacket(buffer);
                //}

                if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_ZOOMTELE)
                {
                    byte[] buffer = new byte[Visca.VISCA_SEND_LEN];
                    Visca.SetLaserIllumTele(buffer);
                    tcpClientAppLaserIllum.EnqueuePacket(buffer);
                }

                if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_ZOOMWIDE)
                {

                    byte[] buffer = new byte[Visca.VISCA_SEND_LEN];
                    Visca.SetLaserIllumWide(buffer);
                    tcpClientAppLaserIllum.EnqueuePacket(buffer);
                }


                if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_TILTUP)
                {
                    byte[] buffer = new byte[Visca.VISCA_SEND_LEN];
                    Visca.SetPanTiltUP(buffer);
                    tcpClientAppLaserIllum.EnqueuePacket(buffer);
                }

                if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_TILTDOWN)
                {
                    byte[] buffer = new byte[Visca.VISCA_SEND_LEN];
                    Visca.SetPanTiltDOWN(buffer);
                    tcpClientAppLaserIllum.EnqueuePacket(buffer);
                }

                if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_PANLEFT)
                {
                    byte[] buffer = new byte[Visca.VISCA_SEND_LEN];
                    Visca.SetPanTiltLEFT(buffer);
                    tcpClientAppLaserIllum.EnqueuePacket(buffer);
                }

                if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_PANRIGHT)
                {
                    byte[] buffer = new byte[Visca.VISCA_SEND_LEN];
                    Visca.SetPanTiltRIGHT(buffer);
                    tcpClientAppLaserIllum.EnqueuePacket(buffer);
                }

                if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_PANSTOP)
                {
                    byte[] buffer = new byte[Visca.VISCA_SEND_LEN];
                    Visca.SetPanTiltSTOP(buffer);
                    tcpClientAppLaserIllum.EnqueuePacket(buffer);
                }

                if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_PANSTOP)
                {
                    byte[] buffer = new byte[Visca.VISCA_STOP_LEN];
                    Visca.SetPanTiltSTOP(buffer);
                    tcpClientAppLaserIllum.EnqueuePacket(buffer);
                }

                if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_LASERILLUM_INTENSITY20)
                {
                    byte[] buffer = new byte[Visca.VISCA_SEND_LEN];
                    Visca.SetLaserIllumIntensity20(buffer);
                    tcpClientAppLaserIllum.EnqueuePacket(buffer);
                }

                if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_LASERILLUM_INTENSITY40)
                {
                    byte[] buffer = new byte[Visca.VISCA_SEND_LEN];
                    Visca.SetLaserIllumIntensity40(buffer);
                    tcpClientAppLaserIllum.EnqueuePacket(buffer);
                }

                if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_LASERILLUM_INTENSITY60)
                {
                    byte[] buffer = new byte[Visca.VISCA_SEND_LEN];
                    Visca.SetLaserIllumIntensity60(buffer);
                    tcpClientAppLaserIllum.EnqueuePacket(buffer);
                }

                if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_LASERILLUM_INTENSITY80)
                {
                    byte[] buffer = new byte[Visca.VISCA_SEND_LEN];
                    Visca.SetLaserIllumIntensity80(buffer);
                    tcpClientAppLaserIllum.EnqueuePacket(buffer);
                }

                if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_LASERILLUM_INTENSITY100)
                {
                    byte[] buffer = new byte[Visca.VISCA_SEND_LEN];
                    Visca.SetLaserIllumIntensity100(buffer);
                    tcpClientAppLaserIllum.EnqueuePacket(buffer);
                }
                //수정
                if (ptzCommand.command == ObservationInfoWithJSON.PTZCommand.SET_LASERILLUM_POWER)
                {
                    if (ptzCommand.on)
                    {
                        byte[] buffer = new byte[Visca.VISCA_POWER_LEN];
                        Visca.LaserIllumPowerOn(buffer);
                        tcpClientAppLaserIllum.EnqueuePacket(buffer);
                    }
                    else
                    {
                        byte[] buffer = new byte[Visca.VISCA_POWER_LEN];
                        Visca.LaserIllumPowerOff(buffer);
                        tcpClientAppLaserIllum.EnqueuePacket(buffer);
                    }
                }
            }
        }

        private static UDPMulticastSocketWithDomain m_socketCameraInfoCommonCh = null;
        private static void ReceiveBufferCallback_CameraInfoCommonCh(byte[] receiveBuffer)
        {
            string strJSON = Encoding.UTF8.GetString(receiveBuffer);

            ObservationInfoWithJSON.CameraInfo camInfo = new ObservationInfoWithJSON.CameraInfo();
            bool bRet = camInfo.SetJSONString(strJSON);

            if (bRet == true)
            {

            }
        }
#endregion

#region 탐지 표적 수신 소켓
        private static int TargetDetectionCountCam1 { get; set; } = 0;
        private static int TargetDetectionCountCam2 { get; set; } = 0;
        public static UDPMulticastSocketWithDomain m_socketTrackInfo = null;
        private static void ReceiveBufferCallback_TrackInfo(byte[] receiveBuffer)
        {
            string strJSON = Encoding.UTF8.GetString(receiveBuffer);
            //System.Console.WriteLine("receiveMessage : " + strJSON.Trim('\0'));                      

            TargetDetectionMsg tempList = new TargetDetectionMsg();
            bool bRet = tempList.SetJSONString(strJSON);

            if (bRet == true)
            {
                if (tempList.MainChannel == Program.MainChannel)
                {
                    switch (tempList.SubChannel)
                    {
                        case 1:
                            if (tempList.TargetDetecInfoList.Count() != 0)
                            {
                                Program.TargetDetectionCountCam1++;
                            }
                            break;
                        case 2:
                            if (tempList.TargetDetecInfoList.Count() != 0)
                            {
                                Program.TargetDetectionCountCam2++;
                            }
                            break;

                    }


                }

            }
        }
        private static void StartTargetFPSTimer()
        {
            //탐지표적 FPS 계산 타이머
            var timer = new System.Timers.Timer();
            timer.Interval = 1000;
            timer.Start();
            timer.Elapsed += (sender, args) =>
            {
                timer.Stop();

                if (Program.TargetDetectionCountCam1 > 0)
                    PTZInfoMgr.CamInfo.TargetDetectionCam1 = true;
                else
                    PTZInfoMgr.CamInfo.TargetDetectionCam1 = false;

                if (Program.TargetDetectionCountCam2 > 0)
                    PTZInfoMgr.CamInfo.TargetDetectionCam2 = true;
                else
                    PTZInfoMgr.CamInfo.TargetDetectionCam2 = false;

                Program.TargetDetectionCountCam1 = 0;
                Program.TargetDetectionCountCam2 = 0;
                timer.Start();
            };
        }

#endregion
    }
}
