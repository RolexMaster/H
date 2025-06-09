using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HawkEye_PTZInterface
{
    public class U2SR
    {
        #region 전원제어
        //$00004PW=O4B + 0x0d len=13
        //$ len=2
        //$00131U2SR HawkEye4-HS60 V1.191011(E)0D + 0x0d len = 40
        //$00129U2SR HawkEye-30D V6.200205(E)87 + 0x0D

        static public int LEN_MAIN_POWER = 13 + 2 + 41;
        static public int LEN_POWER = 13;
        static public byte[] SetThermCamPower(bool on)
        {
            int i = 0;
            byte[] buffer = new byte[5];
            buffer[i++] = (byte)'T';
            buffer[i++] = (byte)'P';
            buffer[i++] = (byte)'=';

            if (on == true)
            {
                buffer[i++] = (byte)'O';
            }
            else
            {
                buffer[i++] = (byte)'F';
            }
            buffer[i++] = 0x0d;

            return buffer;
        }
        static public byte[] SetDayCamPower(bool on)
        {
            int i = 0;
            byte[] buffer = new byte[5];
            buffer[i++] = (byte)'V';
            buffer[i++] = (byte)'P';
            buffer[i++] = (byte)'=';

            if (on == true)
            {
                buffer[i++] = (byte)'O';
            }
            else
            {
                buffer[i++] = (byte)'F';
            }
            buffer[i++] = 0x0d;

            return buffer;
        }
        static public byte[] SetMainPower(bool on)
        {
            int i = 0;
            byte[] buffer = new byte[5];
            buffer[i++] = (byte)'P';
            buffer[i++] = (byte)'W';
            buffer[i++] = (byte)'=';

            if (on == true)
            {
                buffer[i++] = (byte)'O';
            }
            else
            {
                buffer[i++] = (byte)'F';
            }
            buffer[i++] = 0x0d;

            return buffer;
        }
        static public byte[] ReqBatteryInfo()
        {
            int i = 0;
            byte[] buffer = new byte[5];
            buffer[i++] = (byte)'P';
            buffer[i++] = (byte)'W';
            buffer[i++] = (byte)'=';


            buffer[i++] = (byte)'G';

            buffer[i++] = 0x0d;

            return buffer;
        }
        static public byte[] ReqThermCamPower()
        {
            int i = 0;
            byte[] buffer = new byte[5];
            buffer[i++] = (byte)'T';
            buffer[i++] = (byte)'P';
            buffer[i++] = (byte)'=';


            buffer[i++] = (byte)'?';

            buffer[i++] = 0x0d;

            return buffer;
        }
        static public byte[] ReqDayCamPower()
        {
            int i = 0;
            byte[] buffer = new byte[5];
            buffer[i++] = (byte)'V';
            buffer[i++] = (byte)'P';
            buffer[i++] = (byte)'=';

            buffer[i++] = (byte)'?';

            buffer[i++] = 0x0d;

            return buffer;
        }
        static public byte[] ReqMainPower()
        {
            int i = 0;
            byte[] buffer = new byte[5];
            buffer[i++] = (byte)'P';
            buffer[i++] = (byte)'W';
            buffer[i++] = (byte)'=';

            buffer[i++] = (byte)'?';
          
            buffer[i++] = 0x0d;

            return buffer;
        }
        #endregion

        //Wiper
        //[V2=option] EO 하우징 와이퍼를 동작 시킨다 Option=1~6(10~60초동작)
        //O(Off 하지 않을 경우 10분 후 자동으로 Off 됩다
        static public int LEN_WIPER_POS = 13;
        static public byte[] SetDayCamWiper(int n)
        {
            //$00404V2=112 + 0x0d
            string str = String.Format("V2={0}\r", n);
            byte[] strByte = Encoding.UTF8.GetBytes(str);
            return strByte;

            /*
            int i = 0;
            byte[] buffer = new byte[5];
            buffer[i++] = (byte)'V';
            buffer[i++] = (byte)'2';
            buffer[i++] = (byte)'=';

        
            buffer[i++] = (byte)n;
         
            buffer[i++] = 0x0d;

            return buffer;*/
        }
        #region 히터컨트롤
        static public byte[] SetDayCamHeater(bool on)
        {
            //[T1=O] IR 하우징 내 히터를 ON한다
            //[T1=F] IR 하우징 내 히터를 OFF한다
            int i = 0;
            byte[] buffer = new byte[5];
            buffer[i++] = (byte)'V';
            buffer[i++] = (byte)'1';
            buffer[i++] = (byte)'=';

            if (on == true)
            {
                buffer[i++] = (byte)'O';
            }
            else
            {
                buffer[i++] = (byte)'F';
            }
            buffer[i++] = 0x0d;

            return buffer;
        }
        static public byte[] SetThermCamHeater(bool on)
        {
            //[T1=O] IR 하우징 내 히터를 ON한다
            //[T1=F] IR 하우징 내 히터를 OFF한다
            int i = 0;
            byte[] buffer = new byte[5];
            buffer[i++] = (byte)'T';
            buffer[i++] = (byte)'1';
            buffer[i++] = (byte)'=';

            if (on == true)
            {
                buffer[i++] = (byte)'O';
            }
            else
            {
                buffer[i++] = (byte)'F';
            }
            buffer[i++] = 0x0d;

            return buffer;
        }

        static public byte[] ReqDayCamTemp()
        {
            //[TW=?] IR 하우징 내 온도를 읽는다
            int i = 0;
            byte[] buffer = new byte[5];
            buffer[i++] = (byte)'V';
            buffer[i++] = (byte)'W';
            buffer[i++] = (byte)'=';
            buffer[i++] = (byte)'?';
            buffer[i++] = 0x0d;

            return buffer;
        }
        static public byte[] ReqThermCamTemp()
        {
            //[TW=?] IR 하우징 내 온도를 읽는다
            int i = 0;
            byte[] buffer = new byte[5];
            buffer[i++] = (byte)'T';
            buffer[i++] = (byte)'W';
            buffer[i++] = (byte)'=';
            buffer[i++] = (byte)'?';
            buffer[i++] = 0x0d;

            return buffer;
        }

        static public byte[] ReqDayCamHeaterStatus()
        {
            //[V1=?] EO 하우징 내 히터의 동작상태를 읽는다 return=[V1=O],[V1=F]
            int i = 0;
            byte[] buffer = new byte[5];
            buffer[i++] = (byte)'V';
            buffer[i++] = (byte)'1';
            buffer[i++] = (byte)'=';
            buffer[i++] = (byte)'?';
            buffer[i++] = 0x0d;

            return buffer;
        }
        static public byte[] ReqThermCamHeaterStatus()
        {
            //[T1=?] IR 하우징 내 히터의 동작상태를 읽는다 return=[T1=O],[T1=F]
            int i = 0;
            byte[] buffer = new byte[5];
            buffer[i++] = (byte)'T';
            buffer[i++] = (byte)'1';
            buffer[i++] = (byte)'=';
            buffer[i++] = (byte)'?';
            buffer[i++] = 0x0d;

            return buffer;
        }
        #endregion
        //$00002TqDB
        static public int LEN_FOCUS = 11;
        static public byte[] SetThermFocusIn()
        {
            int i = 0;
            byte[] buffer = new byte[3];
            buffer[i++] = (byte)'T';
            buffer[i++] = (byte)'i';
            buffer[i++] = 0x0d;

            return buffer;
        }
        static public byte[] SetThermFocusOut()
        {
            int i = 0;
            byte[] buffer = new byte[3];
            buffer[i++] = (byte)'T';
            buffer[i++] = (byte)'o';
            buffer[i++] = 0x0d;

            return buffer;
        }
        static public byte[] SetThermFocusStop()
        {
            int i = 0;
            byte[] buffer = new byte[3];
            buffer[i++] = (byte)'T';
            buffer[i++] = (byte)'q';
            buffer[i++] = 0x0d;

            return buffer;
        }

        static public int LEN_THERM_FOCUS_SPEED = 15;
        static public byte[] SetThermFocusSpeed(int speed)
        {
            string str = String.Format("TS={0:D3}\r", speed);
            byte[] strByte = Encoding.UTF8.GetBytes(str);
            return strByte;
        }
        static public int LEN_MOVE_POS = 11;
        static public byte[] SetPanLeft()
        {
            //11
            //$00002MLAF + 0x0d 

            int i = 0;
            byte[] buffer = new byte[3];
            buffer[i++] = (byte)'M';
            buffer[i++] = (byte)'L';
            buffer[i++] = 0x0d;

            return buffer;
        }
        static public byte[] SetPanRight()
        {
            int i = 0;
            byte[] buffer = new byte[3];
            buffer[i++] = (byte)'M';
            buffer[i++] = (byte)'R';
            buffer[i++] = 0x0d;

            return buffer;
        }
        static public byte[] SetTiltUp()
        {
            int i = 0;
            byte[] buffer = new byte[3];
            buffer[i++] = (byte)'M';
            buffer[i++] = (byte)'U';
            buffer[i++] = 0x0d;

            return buffer;
        }
        static public byte[] SetTiltDown()
        {
            int i = 0;
            byte[] buffer = new byte[3];
            buffer[i++] = (byte)'M';
            buffer[i++] = (byte)'D';
            buffer[i++] = 0x0d;

            return buffer;
        }
        static public byte[] SetStopPan()
        {
            int i = 0;
            byte[] buffer = new byte[3];
            buffer[i++] = (byte)'Q';
            buffer[i++] = (byte)'R';
            buffer[i++] = 0x0d;

            return buffer;
        }
        static public byte[] SetStopTilt()
        {
            int i = 0;
            byte[] buffer = new byte[3];
            buffer[i++] = (byte)'Q';
            buffer[i++] = (byte)'U';
            buffer[i++] = 0x0d;

            return buffer;
        }

        static public byte[] SetPanTiltSpeed(int speed)
        {
            //RP=nnnn
            //$00007RS=0486CF
            string str = String.Format("RS={0:D4}\r", speed);
            byte[] strByte = Encoding.UTF8.GetBytes(str);
            return strByte;
        }
        #region 절대값 이동 관련

        static public int LEN_SET_SPEED = 16;
        static public byte[] SetPanTiltPresetSpeed(int speed)
        {
            //RP=nnnn
            //$00007RP=0541C4
            string str = String.Format("RP={0:D4}\r", speed);
            byte[] strByte = Encoding.UTF8.GetBytes(str);
            return strByte;
        }

        //$00020PD=350.0000,-19.98002F + 0x0d
        static public int LEN_SET_PANTILT_ABSV = 29;
        static public byte[] SetPanTiltAbsV(float pan, float tilt)
        {
            //PD=100.0000,-20.0000
           
            string str = String.Format("PD={0:000.0000},{1:00.0000}\r", pan, tilt);
            byte[] strByte = Encoding.UTF8.GetBytes(str);
            return strByte;
        }

        static public int LEN_SET_PTSPEED = 21;
        static public byte[] SetPanTiltPresetSpeed(int pan, int tilt)
        {
            tilt = (tilt < 60) ? 60 : tilt;
            tilt = (tilt > 800) ? 800 : tilt;

            string str = String.Format("RP={0:D4},{1:D4}\r", pan, tilt);
            byte[] strByte = Encoding.UTF8.GetBytes(str);
            return strByte;
        }
        static public byte[] SetMemPanAbsV(float pan)
        {
            string str = String.Format("Sp={0:000.0000}\r", pan);
            byte[] strByte = Encoding.UTF8.GetBytes(str);
            return strByte;
        }
        static public byte[] SetMemTiltAbsV(float tilt)
        {
            string str = String.Format("St={0:00.0000}\r", tilt);
            byte[] strByte = Encoding.UTF8.GetBytes(str);
            return strByte;
        }
        static public byte[] SetMoveCurrentPosition()
        {
            string str = String.Format("PM\r");
            byte[] strByte = Encoding.UTF8.GetBytes(str);
            return strByte;
        }
        #endregion

        #region 패킷 요청
        //$00004HM=CD3
        static public int LEN_PANTILT_HOME = 13;
        static public byte[] SetPanTiltHome()
        {
            int i = 0;
            byte[] buffer = new byte[5];
            buffer[i++] = (byte)'H';
            buffer[i++] = (byte)'M';
            buffer[i++] = (byte)'=';
            buffer[i++] = (byte)'C';
            buffer[i++] = 0x0d;

            return buffer;
        }
        static public byte[] ReqPanTiltHome()
        {

            int i = 0;
            byte[] buffer = new byte[5];

            buffer[i++] = (byte)'H';
            buffer[i++] = (byte)'M';
            buffer[i++] = (byte)'=';
            buffer[i++] = (byte)'?';
            buffer[i++] = 0x0d;

            return buffer;
        }

        static public byte[] ReqPanTiltPos()
        {
            int i = 0;
            byte[] buffer = new byte[5];

            buffer[i++] = (byte)'G';
            buffer[i++] = (byte)'C';
            buffer[i++] = (byte)'=';
            buffer[i++] = (byte)'L';
            buffer[i++] = 0x0d;

            return buffer;
        }
        static public byte[] ReqPanContPanTilt()
        {
            int i = 0;
            byte[] buffer = new byte[3];

            buffer[i++] = (byte)'M';
            buffer[i++] = (byte)'B';
            buffer[i++] = 0x0d;

            return buffer;
        }
        //static public int LEN_GPS = 79;
        static public int LEN_GPS = 74;
        static public byte[] ReqGPS()
        {
            int i = 0;
            byte[] buffer = new byte[5];
            buffer[i++] = (byte)'G';
            buffer[i++] = (byte)'C';
            buffer[i++] = (byte)'=';
            buffer[i++] = (byte)'G';
            buffer[i++] = 0x0d;

            return buffer;
        }


        //$00105033.10F
        static public int LEN_DMC = 14;
        static public byte[] ReqDMC()
        {
            int i = 0;
            byte[] buffer = new byte[5];
            buffer[i++] = (byte)'G';
            buffer[i++] = (byte)'C';
            buffer[i++] = (byte)'=';
            buffer[i++] = (byte)'C';
            buffer[i++] = 0x0d;

            return buffer;
        }
        #endregion

        static public byte[] SetPanTiltInitStep1()
        {
            string str = "PP=000:180.0000,+00.0000,50000,50000\r";
            byte[] strByte = Encoding.UTF8.GetBytes(str);
            return strByte;
        }
        static public byte[] SetPanTiltInitStep2()
        {
            string str = "PL=000\r";
            byte[] strByte = Encoding.UTF8.GetBytes(str);
            return strByte;
        }
        static public byte[] SetPanTiltInitStep3()
        {
            string str = "PZ\r";
            byte[] strByte = Encoding.UTF8.GetBytes(str);
            return strByte;
        }

        #region 패킷 파싱


        static public bool ParseGPS(byte[] recvBuffer,
          out int gpsLatDeg, out float gpsLatMin,
          out int gpsLongDeg, out float gpsLongMin)
        {

            //$00170,062116.00,3740.71225,N,12646.78506,E,2,10,1.59,74.2,M,18.0,M,,0000*7F00                     
            //79

            //20201121
            //$00170184622.00,3740.65770,N,12646.76439,E,2,09,1.03,133.1,M,18.0,M,,0000*43F8


            //$00169190209.00,3740.68229,N,12646.78136,E,2,10,1.48,73.8,M,18.0,M,,0000*7
            //$00165080931.00,3740.71974,N,12646.79119,E,1,07,2.86,62.1,M,18.0,M,,*7A26
            gpsLatDeg = 0;
            gpsLatMin = 0.0f;
            gpsLongDeg = 0;
            gpsLongMin = 0.0f;
             
            string str = Encoding.UTF8.GetString(recvBuffer);
            if (str.IndexOf('N') >= 0 && str.IndexOf('E') >= 0)
            {
                try
                {
                    int ret = str.IndexOf("N");
                    string strLat = str.Substring(ret - 11, 10);
                    ret = str.IndexOf("E");
                    string strLong = str.Substring(ret - 12, 11);

                    float tempLat = float.Parse(strLat);    //3740.71225
                    float tempLong = float.Parse(strLong);  //12646.78506
                    gpsLatDeg = (int)tempLat / 100;
                    gpsLongDeg = (int)tempLong / 100;

                    gpsLatMin = tempLat - gpsLatDeg * 100;
                    gpsLongMin = tempLong - gpsLongDeg * 100;

                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("ParseGPS " + e.Message);
                    return false;
                }
            }
            return false;
        }
        static public bool ParseDMC(byte[] recvBuffer, out float DMC)
        {
            //$00105033.10F
            //$00106#019.137
            DMC = 0.0f;
            string str = Encoding.UTF8.GetString(recvBuffer);
            if (str.Length == 15)
            {
                int ret = str.IndexOf("$00106#");
                try
                {
                    string dmc = str.Substring(7, 6);

                    DMC = float.Parse(dmc);

                    return true;
                }
                catch (Exception e)
                {

                }
            }
            if (str.Length == 14)
            {
                int ret = str.IndexOf("$00105");
                try
                {
                    string dmc = str.Substring(6, 6);

                    DMC = float.Parse(dmc);

                    return true;
                }
                catch (Exception e)
                {

                }
            }
            return false;
        }
        static public bool ParseHomePos(byte[] recvBuffer, out bool isHome)
        {
            isHome = false;
            //$00104HM=CD2 + 0x0d
            string str = Encoding.UTF8.GetString(recvBuffer);
            //Console.WriteLine(str);
            if (str.Length == 13)
            {
                int ret1 = str.IndexOf("$00104HM=C");
                if (ret1 > -1)
                {
                    isHome = true;
                    return true;
                }

                int ret2 = str.IndexOf("$00104HM=");
                if (ret2 > -1)
                {
                    return true;
                }

            }
            return false;
        }

        static public bool ParseMainPower(byte[] recvBuffer, out bool isOn)
        {
            string str = Encoding.UTF8.GetString(recvBuffer);
            //PW=O
            //$00004TP=F3F + 0x0d
            isOn = false;
            if (str.Length == 6+4+3)
            {
                if(str.IndexOf("PW=") > -1)//전체전원 명령이면
                {
                    if (str[3] == '0' || str[3] == '1' || str[3] == '6')//set(0) 성공,  get(1) 성공 , 전원꺼짐(6)
                    {
                        if (str.IndexOf("PW=O") > -1)//전체전원 명령이면
                        {
                            isOn = true;
                        }
                        else if(str.IndexOf("PW=F") > -1)
                        {
                            isOn = false;
                        }
                        return true;
                    }
                }
            }
            return false;
        }
        static public bool ParseBatteryInfo(byte[] recvBuffer, out float capacity, out bool state)
        {
            string str = Encoding.UTF8.GetString(recvBuffer);
            //PW=G
            //$000015PW=24.3,13.7,0148 + 0x0d
            capacity = 0;
            state = false;
            if(str.Length == 6+15+3)
            {
                if (str.IndexOf("PW=") > -1)//전체전원 명령이면
                {
                    if (str[3] == '0' || str[3] == '1' || str[3] == '6')//set(0) 성공,  get(1) 성공 , 전원꺼짐(6)
                    {
                        Console.WriteLine("@@@@@@@@@@@@" + str);
                        if (str.IndexOf(",01") > -1)//입전 
                        {
                            int ret = str.IndexOf(",");
                            string dc = str.Substring(ret - 4, 4);
                            string battery = str.Substring(ret + 1, 4);

                            float volt = float.Parse(battery);
                            if(volt > 13.5)
                            {
                                capacity = 100;
                            }
                            else if (volt == 13.5)
                            {
                                capacity = 90;
                            }
                            else if (volt == 13.4)
                            {
                                capacity = 80;
                            }
                            else if (volt == 13.3)
                            {
                                capacity = 70;
                            }
                            else if (volt == 13.2)
                            {
                                capacity = 60;
                            }
                            else if (volt == 13.0)
                            {
                                capacity = 50;
                            }
                            else if (volt == 12.8)
                            {
                                capacity = 40;
                            }
                            else if (volt == 12.6)
                            {
                                capacity = 30;
                            }
                            else if (volt == 12.3)
                            {
                                capacity = 20;
                            }
                            else if (volt == 12.1)
                            {
                                capacity = 10;
                            }
                            else if (volt == 12.0)
                            {
                                capacity = 5;
                            }
                            else if (volt == 11.7)
                            {
                                capacity = 0;
                            }
                            state = true;
                        }
                        else if (str.IndexOf(",02") > -1)//방전
                        {
                            int ret = str.IndexOf(",");
                            string dc = str.Substring(ret - 4, 4);
                            string battery = str.Substring(ret + 1, 4);

                            float volt = float.Parse(battery);
                            if (volt >= 12.6)
                            {
                                capacity = 100;
                            }
                            else if (volt == 12.5)
                            {
                                capacity = 90;
                            }
                            else if (volt == 12.4)
                            {
                                capacity = 80;
                            }
                            else if (volt == 12.3)
                            {
                                capacity = 70;
                            }
                            else if (volt == 12.2)
                            {
                                capacity = 60;
                            }
                            else if (volt == 12.0)
                            {
                                capacity = 50;
                            }
                            else if (volt == 11.8)
                            {
                                capacity = 40;
                            }
                            else if (volt == 11.6)
                            {
                                capacity = 30;
                            }
                            else if (volt == 11.3)
                            {
                                capacity = 20;
                            }
                            else if (volt == 11.0)
                            {
                                capacity = 10;
                            }
                            else if (volt == 10.8)
                            {
                                capacity = 5;
                            }
                            else if (volt == 10.5)
                            {
                                capacity = 0;
                            }
                            state = false;
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        static public bool ParseDayPower(byte[] recvBuffer, out bool isOn)
        {
            string str = Encoding.UTF8.GetString(recvBuffer);
            //PW=O
            //$00004TP=F3F + 0x0d
            isOn = false;
            if (str.Length == 6 + 4 + 3)
            {
                if (str.IndexOf("VP=") > -1)//전체전원 명령이면
                {
                    if (str[3] == '0' || str[3] == '1' || str[3] == '6')//set(0) 성공,  get(1) 성공 , 전원꺼짐(6)
                    {
                        if (str.IndexOf("VP=O") > -1)//전체전원 명령이면
                        {
                            isOn = true;
                        }
                        else if (str.IndexOf("VP=F") > -1)
                        {
                            isOn = false;
                        }
                        return true;
                    }
                }
            }
            return false;
        }
        static public bool ParseThermPower(byte[] recvBuffer, out bool isOn)
        {
            string str = Encoding.UTF8.GetString(recvBuffer);
            //PW=O
            //$00004TP=F3F + 0x0d
            isOn = false;
            if (str.Length == 6 + 4 + 3)
            {
                if (str.IndexOf("TP=") > -1)//전체전원 명령이면
                {
                    if (str[3] == '0' || str[3] == '1' || str[3] == '6')//set(0) 성공,  get(1) 성공 , 전원꺼짐(6)
                    {
                        if (str.IndexOf("TP=O") > -1)//전체전원 명령이면
                        {
                            isOn = true;
                        }
                        else if (str.IndexOf("TP=F") > -1)
                        {
                            isOn = false;
                        }
                        return true;
                    }
                }
            }
            return false;
        }
        static public int LEN_PANTILT_POS = 26;
        static public bool ParsePanTiltPos(byte[] recvBuffer, out float absPan, out float absTilt)
        {
            absPan = 0;
            absTilt = 0;
            //$00117282.3403,+04.51916A + 0x0d
            //$00117008.3452,-06.984376
            string str = Encoding.UTF8.GetString(recvBuffer);
            //Console.WriteLine(str);
            if (str.Length == 26)
            {
                try
                {
                    int ret = str.IndexOf(",");
                    string pan = str.Substring(ret - 8, 8);
                    string tilt = str.Substring(ret + 1, 8);

                    absPan = float.Parse(pan);
                    absTilt = float.Parse(tilt);
                }
                catch (Exception e)
                {

                }


                //Console.WriteLine("{0}{1}", pan, tilt);

                return true;
            }
            return false;
        }


        static public bool ParseDayCamHeaterTemp(byte[] recvBuffer, out int temp)
        {
            //[VW=?] EO 하우징 내 온도를 읽는다 return= -55.0 ~ +125.0
            string str = Encoding.UTF8.GetString(recvBuffer);
            //VW=?
            //$00404VW=?45 + 0x0d
            temp = -999;//파싱이 안된경우 -999 전시되게
            if (str.IndexOf("VW=?") > -1)//주간 카메라 온도 명령이면
            {                
                if (str[3] == '0' || str[3] == '1' || str[3] == '6')//set(0) 성공,  get(1) 성공 , 전원꺼짐(6)
                {
                    int start_index = str.IndexOf("VW=?");
                    if (start_index != -1)
                    {
                        start_index += 4; // "TW=?" 다음 위치로 이동 (온도 값 시작)

                        // 마지막 개행문자 제외하고 온도 값만 추출
                        string _temp = str.Substring(start_index, str.Length - start_index - 1);

                        temp = int.Parse(_temp);
                    }
                    return true;
                }
            }
            return false;
        }
        static public bool ParseDayCamHeaterOnOff(byte[] recvBuffer, out bool isOn)
        {
            //EO 하우징 내 히터의 동작상태를 읽는다 return=[V1=O],[V1=F]
           
            string str = Encoding.UTF8.GetString(recvBuffer);
            //VW=?
            //$00404V1=O2F + 0x0d            
            isOn = false;//파싱이 안된경우 OFF
            //Console.WriteLine("ParseDayCamHeaterOnOff " + str);
            if (str.IndexOf("V1=") > -1)//주간 카메라 온도 명령이면
            {   
                if (str[3] == '0' || str[3] == '1' || str[3] == '6')//set(0) 성공,  get(1) 성공 , 전원꺼짐(6)
                {
                    //Console.WriteLine("ParseDayCamHeaterOnOff " + str);
                    if (str.IndexOf("V1=O") > -1)//전체전원 명령이면
                    {
                        isOn = true;
                    }
                    else if (str.IndexOf("V1=F") > -1)
                    {
                        isOn = false;
                    }
                    return true;
                }
            }
            return false;
        }

        static public bool ParseThermCamHeaterTemp(byte[] recvBuffer, out int temp)
        {
            //[TW=?] IR 하우징 내 온도를 읽는다 return= -55.0 ~ +125.0
            string str = Encoding.UTF8.GetString(recvBuffer);
            //TW=?            
            //$00504TW=?44 + 0x0d
            temp = -999;//파싱이 안된경우 -999 전시되게
            if (str.IndexOf("TW=?") > -1)//Thermal 카메라 온도 명령이면
            {
                if (str[3] == '0' || str[3] == '1' || str[3] == '6')//set(0) 성공,  get(1) 성공 , 전원꺼짐(6)
                {
                    try
                    {
                        int start_index = str.IndexOf("TW=?");
                        if (start_index != -1)
                        {
                            start_index += 4; // "TW=?" 다음 위치로 이동 (온도 값 시작)

                            // 마지막 개행문자 제외하고 온도 값만 추출
                            string _temp = str.Substring(start_index, str.Length - start_index - 1);

                            temp = int.Parse(_temp);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("ParseThermCamHeaterTemp" + e.Message);
                        return false;
                    }

                    return true;
                }
            }
            return false;
        }
        static public bool ParseThermCamHeaterOnOff(byte[] recvBuffer, out bool isOn)
        {
            string str = Encoding.UTF8.GetString(recvBuffer);
            isOn = false;//파싱이 안된경우 OFF
            if (str.IndexOf("T1=") > -1)
            {
                if (str[3] == '0' || str[3] == '1' || str[3] == '6')//set(0) 성공,  get(1) 성공 , 전원꺼짐(6)
                {
                    if (str.IndexOf("T1=O") > -1)
                    {
                        isOn = true;
                    }
                    else if (str.IndexOf("T1=F") > -1)
                    {
                        isOn = false;
                    }
                    return true;
                }
            }
            return false;
        }
        #endregion


        #region 열상카메라
        static public byte[] SetDigitalZoom(double zoom)
        {
            FLIRTau2Protocol.SetDigitalZoom(out byte[] packet, zoom);

            return MakeU2SRThermalProto(packet);
        }
        static public byte[] SetColor(int color)
        {
            FLIRTau2Protocol.SetVideoPalette(out byte[] packet, (ushort)color);

            return MakeU2SRThermalProto(packet);
        }
        static public byte[] SetDDE(int value)
        {
            FLIRTau2Protocol.SetDDESpatialThreshold(out byte[] packet, (ushort)value);

            return MakeU2SRThermalProto(packet);
        }
        static public byte[] SetACE(int value)
        {
            FLIRTau2Protocol.SetACECorrect(out byte[] packet, (ushort)value);

            return MakeU2SRThermalProto(packet);
        }
        static public byte[] SetSSO(int value)
        {
            FLIRTau2Protocol.SetSSO(out byte[] packet, (ushort)value);

            return MakeU2SRThermalProto(packet);
        }
        static private byte[] MakeU2SRThermalProto(byte[] packet)
        {
            byte[] header = Encoding.UTF8.GetBytes("TC=S:");

            string hexString = BitConverter.ToString(packet).Replace("-", string.Empty);
            byte[] body = Encoding.UTF8.GetBytes(hexString);

            IEnumerable<byte> rv = header.Concat(body);
            List<byte> temp = rv.ToList();
            temp.Add(0x0d);

            return temp.ToArray();
        }
        #endregion

    }
}
