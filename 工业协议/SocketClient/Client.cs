using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace SocketClient
{
    public delegate void clientDelegate(string msg);
    public class Client
    {
        //锁
        public object threadLock = new object();
        //日志
        private log4net.ILog log = log4net.LogManager.GetLogger("ValuesController");
        //心跳线程
        private Thread heartThread = null;
        //消息事件
        public event clientDelegate clientEvent;
        //IP地址
        private IPAddress ip = null;
        //ip+端口号
        private IPEndPoint endPoint = null;
        //客户端
        private Socket socketClient = null;
        //监听服务端线程
        Thread serverThread = null;
        //重连计数
        public static int num = 1;
        public Client(string ipAddress, string port)
        {
            this.ip = IPAddress.Parse(ipAddress.Trim());
            this.endPoint = new IPEndPoint(this.ip, int.Parse(port.Trim()));
        }
        /// <summary>
        /// 开启客户端
        /// </summary>
        public void ClientStart()
        {
            try
            {
                if (serverThread != null)
                {
                    serverThread.Abort();
                }
                if (heartThread != null)
                {
                    heartThread.Abort();
                }
                socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //连接服务器
                socketClient.Connect(endPoint);
                //创建线程监听服务端发送过来的消息
                serverThread = new Thread(Receive);
                serverThread.IsBackground = true;
                serverThread.Start(socketClient);
                ShowMessage(endPoint + ":连接成功!\r\n");
                log.Info(endPoint + ":连接成功!");
                //开启心跳线程
                heartThread = new Thread(Heart);
                heartThread.IsBackground = true;
                heartThread.Start();
                log.Info("心跳线程已开启!");
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
        /// <summary>
        /// 重连
        /// </summary>
        public void ReConnected()
        {
            lock(threadLock)
            {
                while (true)
                {
                    if (num == 600)
                    {
                        break;
                    }
                    try
                    {
                        log.Info("重连:" + num + "次!");
                        ShowMessage("重连:" + num + "次!\r\n");
                        socketClient.Close();
                        socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        socketClient.Connect(endPoint);
                        ShowMessage("重连成功!\r\n");
                        num = 1;
                        break;
                    }
                    catch (Exception ex)
                    {
                        num++;
                        log.Error(ex);
                    }
                    Thread.Sleep(6000);
                }
            }
        }
        /// <summary>
        /// 发送心跳
        /// </summary>
        public void Heart()
        {
            while(true)
            {
                try
                {
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes("OK");
                    socketClient.Send(buffer);
                    log.Info("发送心跳:" + "OK");
                    Thread.Sleep(2000);
                }
                catch(Exception ex)
                {
                    ShowMessage("与服务端连接中断，发起重连!\r\n");
                    ReConnected();
                    log.Error(ex);
                }
            }
        }
        /// <summary>
        /// 客户端接收服务端发送过来的消息
        /// </summary>
        /// <param name="receive">客户端</param>
        public void Receive(object receive)
        {
            while (true)
            {
                try
                {
                    Socket socketReceive = receive as Socket;
                    while (true)
                    {
                        byte[] buffer = new byte[1024 * 1024 * 2];
                        int r = socketReceive.Receive(buffer);
                        if (r == 0)
                        {
                            break;
                        }
                        //接收的消息
                        string msg = Encoding.UTF8.GetString(buffer, 0, r);
                        log.Info("msg:" + msg);
                        switch(msg)
                        {
                            case "OK":
                                break;
                            default:
                                ShowMessage(socketReceive.RemoteEndPoint + ":" + msg + "\r\n");
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                    break;
                }
                Thread.Sleep(10);
            }
        }
        /// <summary>
        /// 发送消息到服务端
        /// </summary>
        /// <param name="msg">消息</param>
        public void PutServerMessage(string msg)
        {
            try
            {
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(msg);
                socketClient.Send(buffer);
                ShowMessage("发送到" + socketClient.RemoteEndPoint + ":" + msg + "\r\n");
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
        /// <summary>
        /// 向界面发送消息，需要委托调用
        /// </summary>
        /// <param name="msg"></param>
        public void ShowMessage(string msg)
        {
            if (clientEvent != null)
            {
                clientEvent(msg);
            }
        }
    }
}
