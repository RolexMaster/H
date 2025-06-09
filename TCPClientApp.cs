using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ByteQueue = System.Collections.Generic.Queue<byte[]>;


namespace HawkEye_PTZInterface
{
    public class MyPacket
    {
        public ByteQueue Packet { get; set; } = new ByteQueue();
        public bool IsRecv { get; set; } = false;
        public int ReadLen { get; set; } = 7;
    }

    class TCPClientApp
    {
        #region TcpClient 소켓
        private TcpClient tcpClient = null;
        public bool IsConnected()
        {
            if (tcpClient != null)
                return tcpClient.Connected;

            return false;

        }

        #endregion

        private ProcessPacketfuncPtr procPakcetfunc = null;
        private Queue<MyPacket> queueSendToServer = new Queue<MyPacket>();
        private readonly object lockSendToServer = new object();//lock
        private readonly int MAX_QUEUE = 20;
        private string remoteIpAddr;
        private uint remotePort;
        private DateTime previousTimestamp;
        private Timer timer1;
        private Timer timer2;

        public void EnqueuePacket(byte[] packet, bool isRecv = false, int readLen = 7)
        {
            MyPacket myPacket = new MyPacket();
            myPacket.Packet.Enqueue(packet);
            myPacket.IsRecv = isRecv;
            myPacket.ReadLen = readLen;
            lock (lockSendToServer)
            {
                if (queueSendToServer.Count > MAX_QUEUE)
                {
                    if (procPakcetfunc != null)
                        procPakcetfunc(null);
                }
                else
                    queueSendToServer.Enqueue(myPacket);
            }
        }
        public void ClearQueue(byte[] packet, bool isRecv = false, int readLen = 7)
        {
            MyPacket myPacket = new MyPacket();
            myPacket.Packet.Enqueue(packet);
            myPacket.IsRecv = isRecv;
            myPacket.ReadLen = readLen;
            lock (lockSendToServer)
            {
                queueSendToServer.Clear();
                queueSendToServer.Enqueue(myPacket);
            }
        }

        public void ChangeAddr(string remoteIpAddr, uint remotePort)
        {
            this.remoteIpAddr = remoteIpAddr;
            this.remotePort = remotePort;

            if (tcpClient != null)
            {
                tcpClient.Close();
                tcpClient = null;
            }

        }

        public class TimerState
        {
            public string t_socketName { get; set; }
            public NetworkStream t_stream { get; set; }
        }

        // TCP 송신 쓰레드 타이머 콜백 함수
        private ushort count = 0;
        private const ushort TIMER_PERIOD = 100;//ms
        private const ushort TIMER_PERIOD_CNT = TIMER_PERIOD / 10;
        private Stopwatch stopwatch = new Stopwatch();
        //public void TCPSendTimerCallback(object state)
        //{
        //    long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        //    Console.WriteLine($"Callback called at {DateTime.Now.ToString("HH:mm:ss.fff")}, elapsed time: {elapsedMilliseconds}ms");
        //    stopwatch.Restart();

        //    //DateTime now = DateTime.Now;
        //    //TimeSpan interval = now - previousTimestamp;
        //    //previousTimestamp = now;
        //    //Console.WriteLine("SendPacket Elapsed time since last call: " + interval.TotalMilliseconds + "ms");

        //    count++;
        //    if(count == TIMER_PERIOD_CNT)
        //    {
        //        count = 0;
        //        Stopwatch sw = new Stopwatch();
        //        sw.Start();
        //        MyPacket myPacket = null;
        //        TimerState timerState = state as TimerState;
        //        string socketName = timerState.t_socketName;
        //        NetworkStream stream = timerState.t_stream;

        //        lock (lockSendToServer)
        //        {

        //            if (queueSendToServer.Count > 0)
        //            {
        //                if (Program.commonIni.bPakcetLog)
        //                {
        //                    Console.WriteLine("Ch={0} [{1} SendThread] QueueSize = {2}", Program.MainChannel, socketName, queueSendToServer.Count);
        //                }
        //                Console.WriteLine("Ch={0} [{1} SendThread] QueueSize = {2}", Program.MainChannel, socketName, queueSendToServer.Count);
        //                myPacket = queueSendToServer.Dequeue();
        //                //myPacket = queueSendToServer.Last();
        //                //queueSendToServer.Clear();
        //            }

        //        }
        //        try
        //        {
        //            if (myPacket != null)
        //            {
        //                //System.Console.WriteLine("TCP Send RemotePort={0}", arrAuthorityServerRemoteUdpPort[nChIndex]);
        //                Byte[] packet = myPacket.Packet.Dequeue();
        //                stream.Write(packet, 0, packet.Length);
        //                //System.Console.WriteLine("채널={0} TCP 클라이언트 소켓 원격 포트={1}에 패킷 전송함", nCh, nRemotePort);

        //                if (Program.commonIni.bPakcetLog)
        //                {
        //                    Console.WriteLine("Ch={0} [{1} SendThread] RemoteServer={2}, RemotePort={3} SendSuccess Packet={4}, Length={5}",
        //                       Program.MainChannel, socketName, this.remoteIpAddr, this.remotePort, BitConverter.ToString(packet).Replace("-", " "), packet.Length);

        //                    //string text = Encoding.UTF8.GetString(packet);
        //                    //Console.WriteLine(text);
        //                }
        //                //팬틸트가 처리 할 수 있는 명령 주기가 100ms
        //                //추적 좌표(x, y) 전송하면 팬1개 틸트 1개 명령이 큐에 쌓여서 200ms소모됨
        //                //추적 좌표(x, y)를 100ms 주기로 전송하기 불가능함
        //                //추적 좌표를 초당 200ms 보낼 수 있는게 현재로서 최선
        //                //Thread.Sleep(50);
        //            }

        //        }
        //        catch (Exception e)
        //        {
        //            //System.Console.WriteLine("TCP Send Exception RemotePort={0} " + e.Message, arrAuthorityServerRemoteUdpPort[nChIndex]);
        //            //System.Console.WriteLine("채널={0} TCP 클라이언트 소켓 송신 스레드 Exception 원격 포트={1}" + e.Message, nCh, nRemotePort);
        //            if (Program.commonIni.bPakcetLog)
        //                Console.WriteLine("Ch={0} [{1} SendThread] RemoteServer={2}, RemotePort={3} SendFailure Exception{4}", Program.MainChannel, socketName, this.remoteIpAddr, this.remotePort, e.Message);
        //        }
        //        sw.Stop();
        //        if (myPacket != null)
        //        {

        //        }
        //        if (sw.ElapsedMilliseconds > 0)
        //        {
        //            // Console.WriteLine("sendpacket cmd 시간 " + sw.ElapsedMilliseconds);
        //        }
        //    }

        //}
        // 타이머 콜백 함수

        /// <summary>
        /// TCP 소켓 생성
        /// </summary>
        /// <param name="remoteIpAddr">접속할 원격서버 아이피</param>
        /// <param name="remotePort">접속할 원격서버 포트</param>
        /// <param name="readByteLen">한번에 읽을 바이트 수</param>
        public void CreateTCPClientSocket(
            string remoteIpAddr, uint remotePort, string id, string pw, int readByteLen,
            ProcessPacketfuncPtr func, bool halfDuplex, string socketName)
        {

            this.procPakcetfunc = func;
            NetworkStream stream = null;
            this.remoteIpAddr = remoteIpAddr;
            this.remotePort = remotePort;
#if true
            if (halfDuplex)
            {
                //halfDuplex 송/수신
                Task.Run(() =>
                {
                    while (true)
                    {
                        if (queueSendToServer.Count > 0)
                        {
                            if (Program.commonIni.bPakcetLog)
                                Console.WriteLine("Ch={0} [{1} Send/RecvThread] QueueSize = {2}", Program.MainChannel, socketName, queueSendToServer.Count);

                            MyPacket myPacket = queueSendToServer.Dequeue();
                            Byte[] packet = myPacket.Packet.Dequeue();

                            //패킷 전송
                            SendPacket(this.remoteIpAddr, this.remotePort, socketName, stream, packet);

                            //송신 패킷이 수신 받을 패킷인 경우 수신 함수 호출
                            if (myPacket.IsRecv)
                            {
                                RecvPacket(this.remoteIpAddr, this.remotePort, myPacket.ReadLen, func, socketName, stream);
                            }
                        }
                        Thread.Sleep(1);

                    }
                });
            }
            else
#endif
            {
                //TCP PTZ 패킷 수신 스레드
                Task.Run(() =>
                {
                    while (true)
                    {
                        try
                        {
                            int bufferSize = readByteLen;

                            Byte[] recvBuffer = new Byte[bufferSize];

                            if (stream != null)
                            {
                                //if (Program.commonIni.bPakcetLog)
                                //  Console.WriteLine("Ch={0} [{1} ReceiveThread] RemoteServer={1}, RemotePort={2} waiting for packet", Program.MainChannel, socketName, remoteIpAddr, remotePort);
                                stream.ReadTimeout = 1000;

                                bool completed = false;
                                byte[] oneByte = new byte[1];
                                int curIndex = 0;

                                //Console.WriteLine("1st Try to Read 1byte");
                                Int32 bytes = stream.Read(oneByte, 0, 1);
                                if (oneByte[0] == '$')
                                {
                                    recvBuffer[curIndex++] = oneByte[0];
                                    while (true)
                                    {
                                        //Console.WriteLine("2st Try to Read 1byte");
                                        bytes = stream.Read(oneByte, 0, 1);
                                        recvBuffer[curIndex++] = oneByte[0];
                                        if (oneByte[0] == 0x0d)
                                        {
                                            completed = true;
                                            break;
                                        }
                                    }
                                }

                                if (completed == true)
                                {
                                    byte[] madePacket = new byte[curIndex];
                                    System.Array.Copy(recvBuffer, madePacket, curIndex);
                                    //Console.WriteLine("packet completed = " + Encoding.UTF8.GetString(madePacket));
                                    //수신 패킷 처리
                                    func(madePacket);

                                }
                                else
                                {
                                    //프로그램 최초실행시 '$' 없는 데이터들이 들어옴
                                    Console.WriteLine("????");
                                    Console.WriteLine(BitConverter.ToString(oneByte));
                                    Console.WriteLine(Encoding.UTF8.GetString(oneByte));
                                }
                                //if (Program.commonIni.bPakcetLog)
                                //    Console.WriteLine("Ch={0} [{1} ReceiveThread] RemoteServer={2}, RemotePort={3} Packet={4}, Length={5}",
                                //        Program.MainChannel, socketName, remoteIpAddr, remotePort, BitConverter.ToString(recvBuffer).Replace("-", " "), bytes);

                                //string str = Encoding.UTF8.GetString(recvBuffer);
                            }
                            else
                            {
                                //Console.WriteLine("채널={0} TCP 클라이언트 소켓 원격 포트={1}의 스트림을 가져오지 못함.", nCh, nRemotePort);
                                if (Program.commonIni.bPakcetLog)
                                    Console.WriteLine("Ch={0} [{1}] RemoteServer={2}, RemotePort={3} cant open tcpStream", Program.MainChannel, socketName, this.remoteIpAddr, this.remotePort);
                                //Thread.Sleep(5000);
                            }
                        }
                        catch (Exception e)
                        {
                            //System.Console.WriteLine(e.Message);
                            //접속종료??                      
                            if (Program.commonIni.bPakcetLog)
                            {
                                string log = String.Format("Ch={0} [{1} ReceiveThread] RemoteServer={2}, RemotePort={3} Exception={4}", Program.MainChannel, socketName, this.remoteIpAddr, this.remotePort, e.Message);
                                Console.WriteLine(log);

                            }
                            //Thread.Sleep(5000);

                        }

                        Thread.Sleep(1);
                    }
                });


                //TCP 송신스레드
                Task.Run(() =>
                {
                    //const int additionalSleepTime = 100;
                    const int targetLoopTime = 40;
                    Stopwatch stopwatch = new Stopwatch();
                    while (true)
                    {
                        //Stopwatch sw = new Stopwatch();
                        //sw.Start();
                        MyPacket myPacket = null;
                        stopwatch.Restart();
                        lock (lockSendToServer)
                        {
                            if (queueSendToServer.Count > 0)
                            {
                                if (Program.commonIni.bPakcetLog)
                                {
                                    Console.WriteLine("Ch={0} [{1} SendThread] QueueSize = {2}", Program.MainChannel, socketName, queueSendToServer.Count);
                                }
                                //Console.WriteLine("Ch={0} [{1} SendThread] QueueSize = {2}", Program.MainChannel, socketName, queueSendToServer.Count);
                                myPacket = queueSendToServer.Dequeue();
                                //myPacket = queueSendToServer.Last();
                                //queueSendToServer.Clear();
                            }

                        }

                        if (myPacket != null)
                        {

                            // Do some work here...

                            //System.Console.WriteLine("TCP Send RemotePort={0}", arrAuthorityServerRemoteUdpPort[nChIndex]);
                            Byte[] packet = myPacket.Packet.Dequeue();

                            try
                            {
                                DateTime now = DateTime.Now;
                                TimeSpan interval = now - previousTimestamp;
                                previousTimestamp = now;
                                //Console.WriteLine("SendPacket Elapsed time since last call: " + interval.TotalMilliseconds + "ms");
                                stream.Write(packet, 0, packet.Length);
                                string strPacket = Encoding.UTF8.GetString(packet);
                                strPacket = strPacket.Replace("\r", "");
                                //Console.WriteLine(strPacket);
                                //packet
                            }
                            catch (Exception e)
                            {
                                //System.Console.WriteLine("TCP Send Exception RemotePort={0} " + e.Message, arrAuthorityServerRemoteUdpPort[nChIndex]);
                                //System.Console.WriteLine("채널={0} TCP 클라이언트 소켓 송신 스레드 Exception 원격 포트={1}" + e.Message, nCh, nRemotePort);
                                if (Program.commonIni.bPakcetLog)
                                    Console.WriteLine("Ch={0} [{1} SendThread] RemoteServer={2}, RemotePort={3} SendFailure Exception{4}", Program.MainChannel, socketName, this.remoteIpAddr, this.remotePort, e.Message);
                            }
                            //System.Console.WriteLine("채널={0} TCP 클라이언트 소켓 원격 포트={1}에 패킷 전송함", nCh, nRemotePort);

                            if (Program.commonIni.bPakcetLog)
                            {
                                Console.WriteLine("Ch={0} [{1} SendThread] RemoteServer={2}, RemotePort={3} SendSuccess Packet={4}, Length={5}",
                                   Program.MainChannel, socketName, this.remoteIpAddr, this.remotePort, BitConverter.ToString(packet).Replace("-", " "), packet.Length);

                                //string text = Encoding.UTF8.GetString(packet);
                                //Console.WriteLine(text);
                            }
                            //Thread.Sleep(20);
                            //  stopwatch.Stop();
                            // int processingTime = (int)stopwatch.ElapsedMilliseconds;

                            //int sleepTime = targetLoopTime - processingTime;// + additionalSleepTime;
                            //if (sleepTime > 0)
                            //  {
                            //    Thread.Sleep(sleepTime);
                            //}
                            //팬틸트가 처리 할 수 있는 명령 주기가 100ms
                            //추적 좌표(x, y) 전송하면 팬1개 틸트 1개 명령이 큐에 쌓여서 200ms소모됨
                            //추적 좌표(x, y)를 100ms 주기로 전송하기 불가능함
                            //추적 좌표를 초당 200ms 보낼 수 있는게 현재로서 최선
                            Thread.Sleep(targetLoopTime - 10);
                        }

                        //Thread.Sleep(1);
                        //sw.Stop();
                        if (myPacket != null)
                        {

                        }
                        //if (sw.ElapsedMilliseconds > 0)
                        {
                            // Console.WriteLine("sendpacket cmd 시간 " + sw.ElapsedMilliseconds);
                        }
                        Thread.Sleep(1);
                    }
                });
            }



            //TCP 재 접속 스레드
#if true
            Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(5000);

                    bool bConnection = false;
                    if (tcpClient == null || stream == null)
                    {
                        bConnection = false;
                    }
                    else
                    {
                        if (tcpClient.Connected == false)
                            bConnection = false;
                        else
                            bConnection = true;

                    }
                    if (bConnection == false)
                    {
                        //System.Console.WriteLine("채널={0} TCP 클라이언트 호스트와 연결 끊킴", nCh);

                        try
                        {
                            tcpClient = new TcpClientWithTimeout(this.remoteIpAddr, this.remotePort, 3000).Connect();
                            stream = tcpClient.GetStream();
                            //System.Console.WriteLine("채널={0} TCP 클라이언트 소켓 재 접속 성공 원격 포트={1} ", nCh, nRemotePort);
                            if (Program.commonIni.bPakcetLog)
                                Console.WriteLine("Ch={0} [{1} ReConnectionThread] RemoteServer={2}, RemotePort={3} Connection Success", Program.MainChannel, socketName, this.remoteIpAddr, this.remotePort);
                        }
                        catch (Exception e)
                        {
                            //System.Console.WriteLine("채널={0} TCP 클라이언트 소켓 재 접속 실패 원격 포트={1} " + e.Message, nCh, nRemotePort);
                            if (Program.commonIni.bPakcetLog)
                                Console.WriteLine("Ch={0} [{1} ReConnectionThread] RemoteServer={2}, RemotePort={3} Connection Failure", Program.MainChannel, socketName, this.remoteIpAddr, this.remotePort);
                        }
                    }

                }

            });
#endif

            try
            {

                //서버가 다운된 상황일 경우 익셉션 발생시까지 오래 걸림 그래서 타임아웃 클래스로 사용하는게 날듯
                // arrTCPClient[nCh-1] = new TcpClient(serverIpAddr, nRemotePort);
                tcpClient = new TcpClientWithTimeout(this.remoteIpAddr, this.remotePort, 3000).Connect();
                //tcpClient.ReceiveTimeout = 100000;
                stream = tcpClient.GetStream();

                if (Program.commonIni.bPakcetLog)
                    System.Console.WriteLine("Ch={0} [{1}] RemoteServer={2}, RemotePort={3} Connection Success", Program.MainChannel, socketName, this.remoteIpAddr, this.remotePort);

            }
            catch (Exception e)
            {
                if (Program.commonIni.bPakcetLog)
                    System.Console.WriteLine("Ch={0} [{1}] RemoteServer={2}, RemotePort={3} Connection Failure", Program.MainChannel, socketName, this.remoteIpAddr, this.remotePort);
            }

        }

        private static void RecvPacket(string remoteIpAddr, uint remotePort, int readByteLen, ProcessPacketfuncPtr func, string socketName, NetworkStream stream)
        {
            try
            {
                //int bufferSize = readByteLen;

                Byte[] recvBuffer = new Byte[readByteLen];
                if (stream != null)
                {
                    if (Program.commonIni.bPakcetLog)
                    {
                        if (Program.commonIni.bRecvPakcetLog)
                            Console.WriteLine("Ch={0} [{1} RecvFunc] RemoteServer={2}, RemotePort={3} waiting for {4} len packet",
                                Program.MainChannel, socketName, remoteIpAddr, remotePort, readByteLen);
                    }

                    stream.ReadTimeout = 2000;//타임아웃동작되는지 확인됨             

                    bool completed = false;
                    byte[] oneByte = new byte[1];
                    int curIndex = 0;

                    //Console.WriteLine("1st Try to Read 1byte");
                    Int32 bytes = stream.Read(oneByte, 0, 1);
                    if (Program.commonIni.bLogToFile)
                    {
                        //파일로기록
                        Program.SendRecvPacketWriter.LogWrite(String.Format("Ch={0} [{1} RecvFunc] RemoteServer={2}, RemotePort={3}, Packet={4}, Length={5}",
                            Program.MainChannel, socketName, remoteIpAddr, remotePort, Encoding.UTF8.GetString(recvBuffer, 0, recvBuffer.Length - 1), bytes));
                    }
                    if (oneByte[0] == 0xff)
                    {
                        recvBuffer[curIndex++] = oneByte[0];
                        while (true)
                        {
                            //Console.WriteLine("2st Try to Read 1byte");
                            bytes = stream.Read(oneByte, 0, 1);
                            recvBuffer[curIndex++] = oneByte[0];
                            //if (oneByte[0] == 0x0d)
                            if (curIndex == 7)
                            {
                                completed = true;
                                break;
                            }
                        }
                    }

                    if (completed == true)
                    {
                        byte[] madePacket = new byte[curIndex];
                        System.Array.Copy(recvBuffer, madePacket, curIndex);
                        //Console.WriteLine("packet completed = " + Encoding.UTF8.GetString(madePacket));
                        //수신 패킷 처리
                        func(madePacket);

                        if (Program.commonIni.bPakcetLog)
                        {
                            //string packetName = Pelco_D.PacketToName(buffer, out int data);

                            if (Program.commonIni.bRecvPakcetLog)
                            {

                                //string msg = Encoding.UTF8.GetString(madePacket, 0, madePacket.Length - 1);
                                Console.WriteLine("Ch={0} [{1} RecvFunc] RemoteServer={2}, RemotePort={3}, Packet={4}, Length={5}",
                                  Program.MainChannel, socketName, remoteIpAddr, remotePort, BitConverter.ToString(madePacket).Replace("-", " "), madePacket.Length);
                            }

                        }

                    }
                    else
                    {
                        Console.WriteLine("????");
                    }


                    //Int32 bytes = stream.Read(buffer, 0, buffer.Length);
                    //while (bytes < buffer.Length)
                    //{
                    //    int toRead = buffer.Length - bytes;
                    //    int readLen = stream.Read(buffer, bytes, toRead);
                    //    bytes += readLen;
                    //}

                    //byte[] recvBuffer = new byte[bytes];
                    //System.Array.Copy(buffer, recvBuffer, bytes);


                    ////수신 패킷 처리
                    //if (bytes != 0)
                    //{
                    //    bool ret = func(recvBuffer);
                    //    if (ret == false)
                    //    {
                    //        //실패시 수신 버퍼 비우기
                    //        Console.WriteLine("Clear RecvBuf");
                    //        byte[] temp = new byte[512];
                    //        stream.Read(temp, 0, temp.Length);
                    //    }
                    //}
                    //string str = Encoding.UTF8.GetString(recvBuffer);
                }
                else
                {
                    if (Program.commonIni.bPakcetLog)
                    {
                        if (Program.commonIni.bRecvPakcetLog)
                            Console.WriteLine("Ch={0} [{1} RecvFunc] RemoteServer={2}, RemotePort={3} cant open tcpStream", Program.MainChannel, socketName, remoteIpAddr, remotePort);
                    }
                    Thread.Sleep(5000);

                }
            }
            catch (Exception e)
            {
                stream.Flush();
                Console.WriteLine("Flush");
                //System.Console.WriteLine(e.Message);
                //접속종료??                      
                if (Program.commonIni.bPakcetLog)
                {
                    if (Program.commonIni.bRecvPakcetLog)
                    {
                        // Creates and initializes the CultureInfo which uses the international sort.
                        CultureInfo myCIintl = new CultureInfo("en-US", false);
                        string exception = TranslateExceptionMessage(e, myCIintl);
                        string log = String.Format("Ch={0} [{1} RecvFunc] RemoteServer={2}, RemotePort={3} Exception={4}", Program.MainChannel, socketName, remoteIpAddr, remotePort, exception);
                        Console.WriteLine(log);
                        Program.ConnFailLogWriter.LogWrite(log);

                        //
                        //func(null);
                    }
                }
            }
        }

        private static void SendPacket(string remoteIpAddr, uint remotePort, string socketName, NetworkStream stream, byte[] packet)
        {
            try
            {
                //송신
                stream.Write(packet, 0, packet.Length);
                if (Program.commonIni.bPakcetLog)
                {
                    //string packetName = Pelco_D.PacketToName(packet, out int data);
                    if (Program.commonIni.bSendPakcetLog)
                    {
                        //  Console.WriteLine("Ch={0} [{1} SendFunc] RemoteServer={2}, RemotePort={3} SendSuccess Packet={4}, Length={5}",
                        // Program.MainChannel, socketName, remoteIpAddr, remotePort, Encoding.UTF8.GetString(packet, 0, packet.Length - 1), packet.Length);

                        Console.WriteLine("Ch={0} [{1} SendFunc] RemoteServer={2}, RemotePort={3} SendSuccess Packet={4}, Length={5}",
                                        Program.MainChannel, socketName, remoteIpAddr, remotePort, BitConverter.ToString(packet).Replace("-", " "), packet.Length);
                    }

                    if (Program.commonIni.bLogToFile)
                    {
                        //파일로기록
                        Program.SendRecvPacketWriter.LogWrite(String.Format("Ch={0} [{1} SendFunc] RemoteServer={2}, RemotePort={3} SendSuccess Packet={4}, Length={5}",
                       Program.MainChannel, socketName, remoteIpAddr, remotePort, Encoding.UTF8.GetString(packet, 0, packet.Length - 1), packet.Length));
                    }
                }
            }
            catch (Exception e)
            {
                if (Program.commonIni.bPakcetLog)
                {
                    if (Program.commonIni.bSendPakcetLog)
                        Console.WriteLine("Ch={0} [{1} SendFunc] RemoteServer={2}, RemotePort={3}, PacketLen={4}, SendFailure Exception={5}", Program.MainChannel, socketName, remoteIpAddr, remotePort, packet.Length, e.Message);
                }
            }
        }

        public delegate bool ProcessPacketfuncPtr(byte[] recvBuffer);

        public static string TranslateExceptionMessage(Exception E, CultureInfo targetCulture)
        {
            try
            {
                Assembly a = E.GetType().Assembly;
                ResourceManager rm = new ResourceManager(a.GetName().Name, a);
                ResourceSet rsOriginal = rm.GetResourceSet(Thread.CurrentThread.CurrentUICulture, true, true);
                ResourceSet rsTranslated = rm.GetResourceSet(targetCulture, true, true);
                foreach (DictionaryEntry item in rsOriginal)
                    if (item.Value.ToString() == E.Message.ToString())
                        return rsTranslated.GetString(item.Key.ToString(), false); // success

            }
            catch { }
            return E.Message; // failed (error or cause it's not intelligent enough to locale '{0}'-patterns
        }
    }
}



//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Globalization;
//using System.Linq;
//using System.Net.Sockets;
//using System.Reflection;
//using System.Resources;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using ByteQueue = System.Collections.Generic.Queue<byte[]>;
//namespace HawkEye_PTZInterface
//{
//    public class MyPacket
//    {
//        public ByteQueue Packet { get; set; } = new ByteQueue();
//        public bool IsRecv { get; set; } = false;
//        public int ReadLen { get; set; } = 7;
//    }

//    class TCPClientApp
//    {
//        #region TcpClient 소켓
//        private TcpClient tcpClient = null;
//        public bool IsConnected()
//        {
//            if (tcpClient != null)
//                return tcpClient.Connected;

//            return false;

//        }

//        #endregion

//        private ProcessPacketfuncPtr procPakcetfunc = null;
//        private Queue<MyPacket> queueSendToServer = new Queue<MyPacket>();
//        private readonly int MAX_QUEUE = 20;
//        public void EnqueuePacket(byte[] packet, bool isRecv = false, int readLen = 7)
//        {
//            MyPacket myPacket = new MyPacket();
//            myPacket.Packet.Enqueue(packet);
//            myPacket.IsRecv = isRecv;
//            myPacket.ReadLen = readLen;
//            if (queueSendToServer.Count > MAX_QUEUE)
//            {
//                if (procPakcetfunc != null)
//                    procPakcetfunc(null);
//            }
//            else
//                queueSendToServer.Enqueue(myPacket);
//        }
//        public void ClearQueue(byte[] packet, bool isRecv = false, int readLen = 7)
//        {
//            MyPacket myPacket = new MyPacket();
//            myPacket.Packet.Enqueue(packet);
//            myPacket.IsRecv = isRecv;
//            myPacket.ReadLen = readLen;

//            queueSendToServer.Clear();
//            queueSendToServer.Enqueue(myPacket);
//        }

//        /// <summary>
//        /// TCP 소켓 생성
//        /// </summary>
//        /// <param name="remoteIpAddr">접속할 원격서버 아이피</param>
//        /// <param name="remotePort">접속할 원격서버 포트</param>
//        /// <param name="readByteLen">한번에 읽을 바이트 수</param>
//        public void CreateTCPClientSocket(
//            string remoteIpAddr, uint remotePort, string id, string pw, int readByteLen,
//            ProcessPacketfuncPtr func, bool halfDuplex, string socketName)
//        {
//            this.procPakcetfunc = func;
//            NetworkStream stream = null;
//            try
//            {

//                //서버가 다운된 상황일 경우 익셉션 발생시까지 오래 걸림 그래서 타임아웃 클래스로 사용하는게 날듯
//                // arrTCPClient[nCh-1] = new TcpClient(serverIpAddr, nRemotePort);
//                tcpClient = new TcpClientWithTimeout(remoteIpAddr, remotePort, 3000).Connect();
//                //tcpClient.ReceiveTimeout = 100000;
//                stream = tcpClient.GetStream();

//                if (Program.commonIni.bPakcetLog)
//                    System.Console.WriteLine("Ch={0} [{1}] RemoteServer={2}, RemotePort={3} Connection Success", Program.MainChannel, socketName, remoteIpAddr, remotePort);

//            }
//            catch (Exception e)
//            {
//                if (Program.commonIni.bPakcetLog)
//                    System.Console.WriteLine("Ch={0} [{1}] RemoteServer={2}, RemotePort={3} Connection Failure", Program.MainChannel, socketName, remoteIpAddr, remotePort);
//            }

//            if (halfDuplex)
//            {
//                //halfDuplex 송/수신
//                Task.Run(() =>
//                {
//                    while (true)
//                    {
//                        if (queueSendToServer.Count > 0)
//                        {
//                            if (Program.commonIni.bPakcetLog)
//                                Console.WriteLine("Ch={0} [{1} Send/RecvThread] QueueSize = {2}", Program.MainChannel, socketName, queueSendToServer.Count);

//                            MyPacket myPacket = queueSendToServer.Dequeue();
//                            Byte[] packet = myPacket.Packet.Dequeue();

//                            //패킷 전송
//                            SendPacket(remoteIpAddr, remotePort, socketName, stream, packet);

//                            //송신 패킷이 수신 받을 패킷인 경우 수신 함수 호출
//                            if (myPacket.IsRecv)
//                            {
//                                RecvPacket(remoteIpAddr, remotePort, myPacket.ReadLen, func, socketName, stream);
//                            }
//                        }
//                        Thread.Sleep(1);

//                    }
//                });
//            }
//            else
//            {
//                //TCP PTZ 패킷 수신 스레드
//                Task.Run(() =>
//                {
//                    while (true)
//                    {
//                        try
//                        {
//                            int bufferSize = readByteLen;

//                            Byte[] recvBuffer = new Byte[bufferSize];

//                            if (stream != null)
//                            {
//                                //if (Program.commonIni.bPakcetLog)
//                                  //  Console.WriteLine("Ch={0} [{1} ReceiveThread] RemoteServer={1}, RemotePort={2} waiting for packet", Program.MainChannel, socketName, remoteIpAddr, remotePort);
//                                stream.ReadTimeout = 1000;

//                                bool completed = false;
//                                byte[] oneByte = new byte[1];
//                                int curIndex = 0;

//                                //Console.WriteLine("1st Try to Read 1byte");
//                                Int32 bytes = stream.Read(oneByte, 0, 1);
//                                if (oneByte[0] == '$')
//                                {
//                                    recvBuffer[curIndex++] = oneByte[0];
//                                    while (true)
//                                    {
//                                        //Console.WriteLine("2st Try to Read 1byte");
//                                        bytes = stream.Read(oneByte, 0, 1);
//                                        recvBuffer[curIndex++] = oneByte[0];
//                                        if (oneByte[0] == 0x0d)
//                                        {                                            
//                                            completed = true;
//                                            break;
//                                        }
//                                    }
//                                }

//                                if (completed == true)
//                                {
//                                    byte[] madePacket = new byte[curIndex];
//                                    Array.Copy(recvBuffer, madePacket, curIndex);
//                                    //Console.WriteLine("packet completed = " + Encoding.UTF8.GetString(madePacket));
//                                    //수신 패킷 처리
//                                    func(madePacket);

//                                }
//                                else
//                                {
//                                    Console.WriteLine("????");
//                                }
//                                //if (Program.commonIni.bPakcetLog)
//                                //    Console.WriteLine("Ch={0} [{1} ReceiveThread] RemoteServer={2}, RemotePort={3} Packet={4}, Length={5}",
//                                //        Program.MainChannel, socketName, remoteIpAddr, remotePort, BitConverter.ToString(recvBuffer).Replace("-", " "), bytes);

//                                //string str = Encoding.UTF8.GetString(recvBuffer);
//                            }
//                            else
//                            {
//                                //Console.WriteLine("채널={0} TCP 클라이언트 소켓 원격 포트={1}의 스트림을 가져오지 못함.", nCh, nRemotePort);
//                                if (Program.commonIni.bPakcetLog)
//                                    Console.WriteLine("Ch={0} [{1}] RemoteServer={2}, RemotePort={3} cant open tcpStream", Program.MainChannel, socketName, remoteIpAddr, remotePort);
//                                //Thread.Sleep(5000);
//                            }
//                        }
//                        catch (Exception e)
//                        {
//                            //System.Console.WriteLine(e.Message);
//                            //접속종료??                      
//                            if (Program.commonIni.bPakcetLog)
//                            {
//                                string log = String.Format("Ch={0} [{1} ReceiveThread] RemoteServer={2}, RemotePort={3} Exception={4}", Program.MainChannel, socketName, remoteIpAddr, remotePort, e.Message);
//                                Console.WriteLine(log);

//                            }
//                            //Thread.Sleep(5000);

//                        }

//                        Thread.Sleep(1);
//                    }
//                });


//                //TCP 송신스레드        
//                Task.Run(() =>
//                {
//                    while (true)
//                    {
//                        if (queueSendToServer.Count > 0)
//                        {
//                            if (Program.commonIni.bPakcetLog)
//                                Console.WriteLine("Ch={0} [{1} SendThread] QueueSize = {2}", Program.MainChannel, socketName, queueSendToServer.Count);

//                            MyPacket myPacket = queueSendToServer.Dequeue();
//                            Byte[] packet = myPacket.Packet.Dequeue();

//                            try
//                            {
//                                //System.Console.WriteLine("TCP Send RemotePort={0}", arrAuthorityServerRemoteUdpPort[nChIndex]);

//                                stream.Write(packet, 0, packet.Length);
//                                //System.Console.WriteLine("채널={0} TCP 클라이언트 소켓 원격 포트={1}에 패킷 전송함", nCh, nRemotePort);
//                                if (Program.commonIni.bPakcetLog)
//                                    Console.WriteLine("Ch={0} [{1} SendThread] RemoteServer={2}, RemotePort={3} SendSuccess Packet={4}, Length={5}",
//                                        Program.MainChannel, socketName, remoteIpAddr, remotePort, BitConverter.ToString(packet).Replace("-", " "), packet.Length);

//                                Thread.Sleep(150);
//                            }
//                            catch (Exception e)
//                            {
//                                //System.Console.WriteLine("TCP Send Exception RemotePort={0} " + e.Message, arrAuthorityServerRemoteUdpPort[nChIndex]);
//                                //System.Console.WriteLine("채널={0} TCP 클라이언트 소켓 송신 스레드 Exception 원격 포트={1}" + e.Message, nCh, nRemotePort);
//                                if (Program.commonIni.bPakcetLog)
//                                    Console.WriteLine("Ch={0} [{1} SendThread] RemoteServer={2}, RemotePort={3} SendFailure Exception{4}", Program.MainChannel, socketName, remoteIpAddr, remotePort, e.Message);
//                            }
//                            //Thread.Sleep(1);

//                        }
//                        Thread.Sleep(1);
//                    }
//                });
//            }



//            //TCP 재 접속 스레드
//#if true
//            Task.Run(() =>
//            {
//                while (true)
//                {
//                    Thread.Sleep(5000);

//                    bool bConnection = false;
//                    if (tcpClient == null || stream == null)
//                    {
//                        bConnection = false;
//                    }
//                    else
//                    {
//                        if (tcpClient.Connected == false)
//                            bConnection = false;
//                        else
//                            bConnection = true;

//                    }
//                    if (bConnection == false)
//                    {
//                        //System.Console.WriteLine("채널={0} TCP 클라이언트 호스트와 연결 끊킴", nCh);

//                        try
//                        {
//                            tcpClient = new TcpClientWithTimeout(remoteIpAddr, remotePort, 3000).Connect();
//                            stream = tcpClient.GetStream();
//                            //System.Console.WriteLine("채널={0} TCP 클라이언트 소켓 재 접속 성공 원격 포트={1} ", nCh, nRemotePort);
//                            if (Program.commonIni.bPakcetLog)
//                                Console.WriteLine("Ch={0} [{1} ReConnectionThread] RemoteServer={2}, RemotePort={3} Connection Success", Program.MainChannel, socketName, remoteIpAddr, remotePort);
//                        }
//                        catch (Exception e)
//                        {
//                            //System.Console.WriteLine("채널={0} TCP 클라이언트 소켓 재 접속 실패 원격 포트={1} " + e.Message, nCh, nRemotePort);
//                            if (Program.commonIni.bPakcetLog)
//                                Console.WriteLine("Ch={0} [{1} ReConnectionThread] RemoteServer={2}, RemotePort={3} Connection Failure", Program.MainChannel, socketName, remoteIpAddr, remotePort);
//                        }
//                    }

//                }

//            });
//#endif
//        }

//        private static void RecvPacket(string remoteIpAddr, uint remotePort, int readByteLen, ProcessPacketfuncPtr func, string socketName, NetworkStream stream)
//        {
//            try
//            {
//                //int bufferSize = readByteLen;

//                Byte[] buffer = new Byte[readByteLen];

//                if (stream != null)
//                {
//                    if (Program.commonIni.bPakcetLog)
//                    {
//                        if (Program.commonIni.bRecvPakcetLog)
//                            Console.WriteLine("Ch={0} [{1} RecvFunc] RemoteServer={2}, RemotePort={3} waiting for {4} len packet",
//                                Program.MainChannel, socketName, remoteIpAddr, remotePort, readByteLen);
//                    }

//                    stream.ReadTimeout = 2000;//타임아웃동작되는지 확인됨                    

//                    Int32 bytes = stream.Read(buffer, 0, buffer.Length);
//                    while (bytes < buffer.Length)
//                    {
//                        int toRead = buffer.Length - bytes;
//                        int readLen = stream.Read(buffer, bytes, toRead);
//                        bytes += readLen;
//                    }

//                    byte[] recvBuffer = new byte[bytes];
//                    Array.Copy(buffer, recvBuffer, bytes);
//                    if (Program.commonIni.bPakcetLog)
//                    {
//                        //string packetName = Pelco_D.PacketToName(buffer, out int data);

//                        if (Program.commonIni.bRecvPakcetLog)
//                        {
//                            string msg = Encoding.UTF8.GetString(recvBuffer, 0, recvBuffer.Length - 1);
//                            Console.WriteLine("Ch={0} [{1} RecvFunc] RemoteServer={2}, RemotePort={3}, Packet={4}, Length={5}",
//                              Program.MainChannel, socketName, remoteIpAddr, remotePort, msg, bytes);
//                        }
//                        if (Program.commonIni.bLogToFile)
//                        {
//                            //파일로기록
//                            Program.SendRecvPacketWriter.LogWrite(String.Format("Ch={0} [{1} RecvFunc] RemoteServer={2}, RemotePort={3}, Packet={4}, Length={5}",
//                                Program.MainChannel, socketName, remoteIpAddr, remotePort, Encoding.UTF8.GetString(recvBuffer, 0, recvBuffer.Length - 1), bytes));
//                        }
//                    }
//                    //수신 패킷 처리
//                    if (bytes != 0)
//                    {
//                        bool ret = func(buffer);
//                        if (ret == false)
//                        {
//                            //실패시 수신 버퍼 비우기
//                            Console.WriteLine("Clear RecvBuf");
//                            byte[] temp = new byte[512];
//                            stream.Read(temp, 0, temp.Length);
//                        }
//                    }
//                    //string str = Encoding.UTF8.GetString(recvBuffer);
//                }
//                else
//                {
//                    if (Program.commonIni.bPakcetLog)
//                    {
//                        if (Program.commonIni.bRecvPakcetLog)
//                            Console.WriteLine("Ch={0} [{1} RecvFunc] RemoteServer={2}, RemotePort={3} cant open tcpStream", Program.MainChannel, socketName, remoteIpAddr, remotePort);
//                    }
//                    Thread.Sleep(5000);

//                }
//            }
//            catch (Exception e)
//            {
//                stream.Flush();
//                Console.WriteLine("Flush");
//                //System.Console.WriteLine(e.Message);
//                //접속종료??                      
//                if (Program.commonIni.bPakcetLog)
//                {
//                    if (Program.commonIni.bRecvPakcetLog)
//                    {
//                        // Creates and initializes the CultureInfo which uses the international sort.
//                        CultureInfo myCIintl = new CultureInfo("en-US", false);
//                        string exception = TranslateExceptionMessage(e, myCIintl);
//                        string log = String.Format("Ch={0} [{1} RecvFunc] RemoteServer={2}, RemotePort={3} Exception={4}", Program.MainChannel, socketName, remoteIpAddr, remotePort, exception);
//                        Console.WriteLine(log);
//                        Program.ConnFailLogWriter.LogWrite(log);

//                        //
//                        //func(null);
//                    }
//                }
//            }
//        }

//        private static void SendPacket(string remoteIpAddr, uint remotePort, string socketName, NetworkStream stream, byte[] packet)
//        {
//            try
//            {
//                //송신
//                stream.Write(packet, 0, packet.Length);
//                if (Program.commonIni.bPakcetLog)
//                {
//                    //string packetName = Pelco_D.PacketToName(packet, out int data);
//                    if (Program.commonIni.bSendPakcetLog)
//                    {
//                        Console.WriteLine("Ch={0} [{1} SendFunc] RemoteServer={2}, RemotePort={3} SendSuccess Packet={4}, Length={5}",
//                         Program.MainChannel, socketName, remoteIpAddr, remotePort, Encoding.UTF8.GetString(packet, 0, packet.Length - 1), packet.Length);
//                    }

//                    if (Program.commonIni.bLogToFile)
//                    {
//                        //파일로기록
//                        Program.SendRecvPacketWriter.LogWrite(String.Format("Ch={0} [{1} SendFunc] RemoteServer={2}, RemotePort={3} SendSuccess Packet={4}, Length={5}",
//                       Program.MainChannel, socketName, remoteIpAddr, remotePort, Encoding.UTF8.GetString(packet, 0, packet.Length - 1), packet.Length));
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                if (Program.commonIni.bPakcetLog)
//                {
//                    if (Program.commonIni.bSendPakcetLog)
//                        Console.WriteLine("Ch={0} [{1} SendFunc] RemoteServer={2}, RemotePort={3}, PacketLen={4}, SendFailure Exception={5}", Program.MainChannel, socketName, remoteIpAddr, remotePort, packet.Length, e.Message);
//                }
//            }
//        }

//        public delegate bool ProcessPacketfuncPtr(byte[] recvBuffer);

//        public static string TranslateExceptionMessage(Exception E, CultureInfo targetCulture)
//        {
//            try
//            {
//                Assembly a = E.GetType().Assembly;
//                ResourceManager rm = new ResourceManager(a.GetName().Name, a);
//                ResourceSet rsOriginal = rm.GetResourceSet(Thread.CurrentThread.CurrentUICulture, true, true);
//                ResourceSet rsTranslated = rm.GetResourceSet(targetCulture, true, true);
//                foreach (DictionaryEntry item in rsOriginal)
//                    if (item.Value.ToString() == E.Message.ToString())
//                        return rsTranslated.GetString(item.Key.ToString(), false); // success

//            }
//            catch { }
//            return E.Message; // failed (error or cause it's not intelligent enough to locale '{0}'-patterns
//        }
//    }
//}
