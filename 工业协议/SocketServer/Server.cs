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

namespace SocketServer
{
    public delegate void serverDelegate(string msg);
    public delegate void comboxDelegate(bool cutoradd, string ipmsg);
    public class Server
    {
        private Thread timeOut = null;
        //日志
        private log4net.ILog log = log4net.LogManager.GetLogger("ValuesController");
        //客户端超时的秒数
        private int clientTimeOut = 30;
        //消息类
        class IpMessage
        {
            public string ip { get; set; }
            public string msg { get; set; }
        }
        class SocketTime
        {
            public Socket socket { get; set; }
            public DateTime dateTime { get; set; }
        }
        //线程安全消息队列
        ConcurrentQueue<IpMessage> messageQueue = new ConcurrentQueue<IpMessage>();
        //消息处理线程
        private Thread messageThread = null;
        //锁
        public object receiveLock = new object();
        //消息事件
        public event serverDelegate serverEvent;
        //ip事件
        public event comboxDelegate comboxEvent;
        //ip,socket 字典 线程安全字典
        private ConcurrentDictionary<string, SocketTime> dicSocket = new ConcurrentDictionary<string, SocketTime>();
        //客户端连接线程
        private Thread threadWatch = null;
        //监听套接字
        private Socket socketWatch = null;
        //IP地址
        private IPAddress ip = null;
        //ip+端口号
        private IPEndPoint endPoint = null;
        /// <summary>
        /// Socket服务端构造函数、赋值
        /// </summary>
        /// <param name="IpAddress">IP地址</param>
        /// <param name="Port">端口号</param>
        public Server(string ipAddress, string port)
        {
            this.ip = IPAddress.Parse(ipAddress.Trim());
            this.endPoint = new IPEndPoint(this.ip, int.Parse(port.Trim()));
        }
        /// <summary>
        /// 打开客户端的监听程序
        /// </summary>
        public void ServerStart()
        {
            try
            {
                //终结掉无用的线程
                if(threadWatch!=null)
                {
                    threadWatch.Abort();
                }
                if (messageThread != null)
                {
                    messageThread.Abort();
                }
                if (timeOut != null)
                {
                    timeOut.Abort();
                }
                // 定义一个套接字用于监听客户端发来的消息，包含三个参数（ipv4寻址协议，流式连接，tcp协议）
                socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //监听绑定的网路节点
                socketWatch.Bind(endPoint);
                //设置监听队列长度为无限
                socketWatch.Listen(0);
                threadWatch = new Thread(WatchConnecting);
                //设置为后台线程
                threadWatch.IsBackground = true;
                //启动线程
                threadWatch.Start();
                ShowMessage("成功启动监听!ip:" + endPoint + "\r\n");
                log.Info("成功启动监听!ip:" + endPoint);
                //消息处理线程
                messageThread = new Thread(MessageDel);
                messageThread.IsBackground = true;
                messageThread.Start();
                log.Info("启动消息处理线程!");
                //超时终止线程
                timeOut = new Thread(ClientTimeOut);
                timeOut.IsBackground = true;
                timeOut.Start();
                log.Info("启动超时终止线程!");
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
        /// <summary>
        /// 判断客户端是否超时，并将超时的客户端移除
        /// </summary>
        public void ClientTimeOut()
        {
            while (true)
            {
                foreach (KeyValuePair<string, SocketTime> s in dicSocket)
                {
                    TimeSpan ts = DateTime.Now - s.Value.dateTime;
                    //移除超时的客户端
                    if (ts.TotalSeconds > clientTimeOut)
                    {
                        SocketTime st = null;
                        //移除IP
                        ComboxIp(false, s.Key);
                        dicSocket.TryRemove(s.Key, out st);
                        log.Info("Remove:" + s.Key);
                    }
                }
                Thread.Sleep(10);
            }
        }
        /// <summary>
        /// 消息处理线程
        /// </summary>
        public void MessageDel()
        {
            while (true)
            {
                try
                {
                    IpMessage img = null;
                    if (messageQueue.TryDequeue(out img))
                    {
                        string msg = img.msg;
                        log.Info("msg:" + msg);
                        switch (msg)
                        {
                            //心跳消息
                            case "OK":
                                PutMessage(img.ip, "OK");
                                break;
                            default:
                                ShowMessage(img.ip + ":" + msg + "\r\n");
                                break;
                        }

                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
                Thread.Sleep(10);
            }
        }
        /// <summary>
        /// 等待客户端的连接并创建与之通讯用的Socket
        /// </summary>
        public void WatchConnecting()
        {
            while (true)
            {
                try
                {
                    //客户端接入
                    Socket socketSend = socketWatch.Accept();
                    //将ip和socket连接存入字典
                    if (dicSocket.ContainsKey(socketSend.RemoteEndPoint.ToString()))
                    {
                        dicSocket[socketSend.RemoteEndPoint.ToString()].dateTime = DateTime.Now;
                    }
                    else
                    {
                        dicSocket.TryAdd(socketSend.RemoteEndPoint.ToString(), new SocketTime()
                        {
                            socket = socketSend,
                            dateTime = DateTime.Now
                        });
                    }
                    //将ip写回Form
                    ComboxIp(true, socketSend.RemoteEndPoint.ToString());
                    //192.168.?.?:连接成功
                    ShowMessage(socketSend.RemoteEndPoint.ToString() + ":连接成功!\r\n");
                    log.Info(socketSend.RemoteEndPoint.ToString() + ":连接成功!");
                    //设定接收的超时时间，防止线程假死
                    //socketSend.ReceiveTimeout = 2000;
                    //开启一个新线程，不停地接收客户端发送过来的消息
                    Thread th = new Thread(Receive);
                    th.IsBackground = true;
                    th.Start(socketSend);
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
                Thread.Sleep(10);
            }
        }
        /// <summary>
        /// 服务器不停地接收客户端发过来的消息
        /// </summary>
        /// <param name="send"></param>
        public void Receive(object send)
        {
            Socket socketSend = send as Socket;
            while (true)
            {
                try
                {
                    byte[] buffer = new byte[1024 * 1024 * 2];
                    //接收消息的字节数
                    int r = socketSend.Receive(buffer);
                    if (r == 0)
                    {
                        break;
                    }
                    //抛入消息队列
                    messageQueue.Enqueue(new IpMessage()
                    {
                        ip = socketSend.RemoteEndPoint.ToString(),
                        msg = Encoding.UTF8.GetString(buffer, 0, r)
                    });
                    //将ip和socket连接存入字典
                    if (dicSocket.ContainsKey(socketSend.RemoteEndPoint.ToString()))
                    {
                        dicSocket[socketSend.RemoteEndPoint.ToString()].dateTime = DateTime.Now;
                    }
                    else
                    {
                        dicSocket.TryAdd(socketSend.RemoteEndPoint.ToString(), new SocketTime()
                        {
                            socket = socketSend,
                            dateTime = DateTime.Now
                        });
                        //将ip写回Form
                        ComboxIp(true, socketSend.RemoteEndPoint.ToString());
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
        /// 给指定的客户端发送消息
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="msg"></param>
        public void PutMessage(string ip, string msg)
        {
            lock (receiveLock)
            {
                try
                {
                    SocketTime socketTime = null;
                    if (dicSocket.TryGetValue(ip, out socketTime))
                    {
                        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(msg);
                        socketTime.socket.Send(buffer);
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }
            }
        }
        /// <summary>
        /// 向界面发送消息，需要委托调用
        /// </summary>
        /// <param name="msg"></param>
        public void ShowMessage(string msg)
        {
            if (serverEvent != null)
            {
                serverEvent(msg);
            }
        }
        public void ComboxIp(bool cutoradd, string ipmsg)
        {
            if (comboxEvent != null)
            {
                comboxEvent(cutoradd, ipmsg);
            }
        }
    }
}
