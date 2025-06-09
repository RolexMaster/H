using System;
using System.Text;
using System.IO;
using System.Net;
using ObservationInfoWithJSON;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace HawkEye_PTZInterface
{
  

    public class IRCanThermalCameraController : IDisposable
    {
        //private string strIP;
        private bool mIsDisposed = false;

        private string strIP;
        private string strID;
        private string strPW;
        
        private int digitalMag=0;
      
        private LogWriter LogWriter { get; set; } = new LogWriter("HawkEye_PTZInterface.log", "StartLog");

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="id"></param>
        /// <param name="pw"></param>
        public IRCanThermalCameraController(string ip, string id, string pw)
        {        
            strIP = ip;
            strID = id;
            strPW = pw;

            InitProcess();
        }

        ~IRCanThermalCameraController()
        {
            Dispose(false);
        }

        private bool isLogin=false;
        private void InitProcess()
        {
            new Thread(() =>
            {
                while (true)
                {
                    bool ret1 = GetDDE(out int dde);
                    bool ret2 = GetACEandSSO(out int ace, out int sso);

                    if (ret1 && ret2)
                    {
                        if (this.isLogin == false)
                        {
                            //초기화
                            Console.WriteLine("열상 초기값 설정");
                            SetDDE(25);
                            SetACE(0);
                            SetSSO(25);
                        }
                        this.isLogin = true;
                    }
                    else
                        this.isLogin = false;

                    Thread.Sleep(20000);
                }
            }).Start();

          
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(true);
        }

        protected virtual void Dispose(bool b)
        {
            if (mIsDisposed)
            {
                return;
            }
            if (b)
            {
            }
            // Free any unmanaged resources in this section
            mIsDisposed = true;
        }

        public void SetColor(THERMCAM_COLOR color)
        {
            StreamReader d;
            HttpWebResponse p;
            HttpWebRequest r = null;
            Stream t;
            string z = string.Empty;

            try
            {
                switch (color)
                {
                    case THERMCAM_COLOR.BLACK_HOT:
                        {
                            //http://192.168.0.109/setup/video/image.php?group=basic&app=set&palette=blackhot
                            r = (HttpWebRequest)WebRequest.Create("https://" + strIP + "/setup/video/image.php?group=basic&app=set&palette=blackhot");
                        }
                        break;
                    case THERMCAM_COLOR.WHITE_HOT:
                        {
                            //http://192.168.0.109/setup/video/image.php?group=basic&app=set&palette=whitehot
                            r = (HttpWebRequest)WebRequest.Create("https://" + strIP + "/setup/video/image.php?group=basic&app=set&palette=whitehot");
                        }
                        break;
                    case THERMCAM_COLOR.RAINBOW:
                        {
                            //http://192.168.0.109/setup/video/image.php?group=basic&app=set&palette=rainbow
                            r = (HttpWebRequest)WebRequest.Create("https://" + strIP + "/setup/video/image.php?group=basic&app=set&palette=rainbow");
                        }
                        break;
                }

                //https://m.blog.naver.com/PostView.nhn?blogId=jaewonman&logNo=220343892452&proxyReferer=https%3A%2F%2Fwww.google.com%2F
                System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;


                r.Method = "GET";            
                r.Timeout = 1000;
                r.Credentials = new NetworkCredential(strID, strPW);
                p = (HttpWebResponse)r.GetResponse();
                t = p.GetResponseStream();
                d = new StreamReader(t, Encoding.Default);
                z = d.ReadToEnd();
            }
            catch (Exception ex)
            {
                //Common.WriteLog("HanwhaCamera.ZoomIn : " + ex.Message);
            }
        }

        public void SetDDE(int value)
        {
            StreamReader d;
            HttpWebResponse p;
            HttpWebRequest r;
            Stream t;
            string z = string.Empty;

            try
            {
                //http://192.168.0.109/setup/video/image.php?group=basic&app=set&sharpness=100
                r = (HttpWebRequest)WebRequest.Create("https://" + strIP + "/setup/video/image.php?group=basic&app=set&sharpness="+value.ToString());

                System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;

                r.Method = "GET";
                r.Timeout = 3000;
                r.Credentials = new NetworkCredential(strID, strPW);
                p = (HttpWebResponse)r.GetResponse();
                t = p.GetResponseStream();
                d = new StreamReader(t, Encoding.Default);
                z = d.ReadToEnd();
                Console.WriteLine(z);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SetDDE : " + ex.Message);
            }
        }
        public void SetACE(int value)
        {
            StreamReader d;
            HttpWebResponse p;
            HttpWebRequest r;
            Stream t;
            string z = string.Empty;

            try
            {
                //http://192.168.0.109/setup/video/image.php?group=basic&app=set&sharpness=100
                r = (HttpWebRequest)WebRequest.Create("https://" + strIP + "/setup/video/image.php?group=ae&app=set&agc_ace=" + value.ToString());

                System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;

                r.Method = "GET";
                r.Timeout = 3000;
                r.Credentials = new NetworkCredential(strID, strPW);
                p = (HttpWebResponse)r.GetResponse();
                t = p.GetResponseStream();
                d = new StreamReader(t, Encoding.Default);
                z = d.ReadToEnd();
                Console.WriteLine(z);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SetACE : " + ex.Message);
            }
        }
        public void SetSSO(int value)
        {
            StreamReader d;
            HttpWebResponse p;
            HttpWebRequest r;
            Stream t;
            string z = string.Empty;

            try
            {
                //http://192.168.0.109/setup/video/image.php?group=basic&app=set&sharpness=100
                r = (HttpWebRequest)WebRequest.Create("https://" + strIP + "/setup/video/image.php?group=ae&app=set&agc_sso=" + value.ToString());

                System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;

                r.Method = "GET";
                r.Timeout = 3000;
                r.Credentials = new NetworkCredential(strID, strPW);
                p = (HttpWebResponse)r.GetResponse();
                t = p.GetResponseStream();
                d = new StreamReader(t, Encoding.Default);
                z = d.ReadToEnd();
                Console.WriteLine(z);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SetSSO : " + ex.Message);
            }
        }
        public int GetDigitalZoom()
        {
            StreamReader d;
            HttpWebResponse p;
            HttpWebRequest r;
            Stream t;
            string z = string.Empty;

            int digitalZoom = 0;
            try
            {
                //http://192.168.0.10/setup/video/digital_zoom.php?app=get
                r = (HttpWebRequest)WebRequest.Create("https://" + strIP + "/setup/video/digital_zoom.php?app=get");

                //https://m.blog.naver.com/PostView.nhn?blogId=jaewonman&logNo=220343892452&proxyReferer=https%3A%2F%2Fwww.google.com%2F
                System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;
                r.Method = "GET";
                r.Timeout = 1000;
                r.Credentials = new NetworkCredential(strID, strPW);
                p = (HttpWebResponse)r.GetResponse();
                t = p.GetResponseStream();
                d = new StreamReader(t, Encoding.Default);
                z = d.ReadToEnd();

                digitalZoom = int.Parse(getBetween(z, "dz_level=", true));
                //LogWriter.LogWrite(String.Format("IRCanThermalCameraController Res = {0}", z));
                //"res=200&rtsp_port=554&http_port=80&https_port=443&https_mode=2&viewer_video=stream3&viewer_codec=JPEG&viewer_fps=30&dz_mode=seperate&dz_enable=1&dz_level=20"
                System.Console.WriteLine("GetDigitalZoom={0}", digitalZoom);
            }
            catch (Exception ex)
            {
                //Common.WriteLog("HanwhaCamera.ZoomIn : " + ex.Message);
                
                digitalZoom = 0;
            }

            return digitalZoom;
        }
        public void DigitalZoomIn()
        {
            digitalMag = GetDigitalZoom();

            if (digitalMag > 0 && digitalMag < 1600)
            {
                if (digitalMag < 200)
                    digitalMag += 20;
                else
                    digitalMag += 100;
            }
            else
                return;

            StreamReader d;
            HttpWebResponse p;
            HttpWebRequest r;
            Stream t;
            string z = string.Empty;

            try
            {
                //http://192.168.0.109/setup/video/digital_zoom.php?app=set&dz_enable=1&dz_level=200
                r = (HttpWebRequest)WebRequest.Create("https://" + strIP + "/setup/video/digital_zoom.php?app=set&dz_enable=1&dz_level=" + ((int)(digitalMag)).ToString());
                //https://m.blog.naver.com/PostView.nhn?blogId=jaewonman&logNo=220343892452&proxyReferer=https%3A%2F%2Fwww.google.com%2F
                System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;
                r.Method = "GET";
                r.Timeout = 1000;
                r.Credentials = new NetworkCredential(strID, strPW);
                p = (HttpWebResponse)r.GetResponse();
                t = p.GetResponseStream();
                d = new StreamReader(t, Encoding.Default);
                z = d.ReadToEnd();

                System.Console.WriteLine("DigitalZoomIn={0}", digitalMag);
            }
            catch (Exception ex)
            {
                //Common.WriteLog("HanwhaCamera.ZoomIn : " + ex.Message);
            }
        }

        public void DigitalZoomOut()
        {
            digitalMag = GetDigitalZoom();

            if (digitalMag > 100)
            {

                if (digitalMag > 180f)
                    digitalMag -= 100;
                else
                    digitalMag -= 20;
            }
            else
                return;
            StreamReader d;
            HttpWebResponse p;
            HttpWebRequest r;
            Stream t;
            string z = string.Empty;

            try
            {                
                r = (HttpWebRequest)WebRequest.Create("https://" + strIP + "/setup/video/digital_zoom.php?app=set&dz_enable=1&dz_level=" + ((int)(digitalMag)).ToString());
                //https://m.blog.naver.com/PostView.nhn?blogId=jaewonman&logNo=220343892452&proxyReferer=https%3A%2F%2Fwww.google.com%2F
                System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;
                r.Method = "GET";
                r.Timeout = 1000;
                r.Credentials = new NetworkCredential(strID, strPW);
                p = (HttpWebResponse)r.GetResponse();
                t = p.GetResponseStream();
                d = new StreamReader(t, Encoding.Default);
                z = d.ReadToEnd();
                System.Console.WriteLine("DigitalZoomOut={0}", digitalMag);
            }
            catch (Exception ex)
            {
                //Common.WriteLog("HanwhaCamera.ZoomOut : " + ex.Message);
            }
        }
        public static string getBetween(string strSource, string strStart, string strEnd)
        {
            int Start, End;
            if (strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                End = strSource.IndexOf(strEnd, Start);
                return strSource.Substring(Start, End - Start);
            }
            else
            {
                return "";
            }
        }
        public static string getBetween(string strSource, string strStart, bool strEnd)
        {
            int Start;
            if (strSource.Contains(strStart))
            {
                Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                return strSource.Substring(Start, strSource.Length - Start);
            }
            else
            {
                return "";
            }
        }

    
        private bool GetDDE(out int dde)
        {
            StreamReader d;
            HttpWebResponse p;
            HttpWebRequest r;
            Stream t;
            string z = string.Empty;

            dde = 0;
            try
            {                
                r = (HttpWebRequest)WebRequest.Create("https://" + strIP + "/setup/video/image.php?group=basic&app=get");                
                System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;
                r.Method = "GET";
                r.Timeout = 3000;
                r.Credentials = new NetworkCredential(strID, strPW);
                p = (HttpWebResponse)r.GetResponse();
                t = p.GetResponseStream();
                d = new StreamReader(t, Encoding.Default);
                z = d.ReadToEnd();

                //res=200&rtsp_port=554&http_port=80&https_mode=2&viewer_video=stream3&viewer_codec=JPEG&viewer_fps=30&min_sharpness=-20&max_sharpness=100&def_sharpness=10&flip=0&mirror=0&sharpness=79&palette=blackhot&ffc_warn=10&
                Console.WriteLine("GetDDE : " + z);
                dde = int.Parse(getBetween(z, "&sharpness=", "&palette"));
                
                return true;

            }
            catch (Exception ex)
            {
                dde = 0;
                Console.WriteLine("GetDDE : " + ex.Message);
            }

            return false;
        }
        private bool GetACEandSSO(out int ace, out int sso)
        {
            StreamReader d;
            HttpWebResponse p;
            HttpWebRequest r;
            Stream t;
            string z = string.Empty;

           //res=200&http_port=80&min_agc_bright=1&max_agc_bright=16383&def_agc_bright=8192&min_agc_contrast=1&max_agc_contrast=256&def_agc_contrast=32&min_agc_filter=0&max_agc_filter=255&def_agc_filter=64&min_agc_sso=0&max_agc_sso=100&def_agc_sso=15&min_agc_ace=-8&max_agc_ace=8&def_agc_ace=3&agc_mode=auto&agc_preset=default&agc_bright=8192&agc_contrast=32&agc_filter=64&agc_sso=0&agc_ace=6&agc_plateau=150&agc_gain=12&
            try
            {
                r = (HttpWebRequest)WebRequest.Create("https://" + strIP + "/setup/video/image.php?group=ae&app=get");
                System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;
                r.Method = "GET";
                r.Timeout = 3000;
                r.Credentials = new NetworkCredential(strID, strPW);
                p = (HttpWebResponse)r.GetResponse();
                t = p.GetResponseStream();
                d = new StreamReader(t, Encoding.Default);
                z = d.ReadToEnd();

                Console.WriteLine("GetACEandSSO : " + z);
                ace = int.Parse(getBetween(z, "&agc_ace=", "&agc_plateau"));
                sso = int.Parse(getBetween(z, "&agc_sso=", "&agc_ace"));
                return true;

            }
            catch (Exception ex)
            {
                ace = 0;
                sso = 0;
                Console.WriteLine("GetACEandSSO : " + ex.Message);
            }

            return false;
        }

    }

    internal class TrustAllCert
    {
    }
}

