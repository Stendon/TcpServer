using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TcpServer
{
    /// <summary>
    /// TCP通信
    /// </summary>
    class TcpServer
    {
        private Socket          socket;
        private EndPoint        localEndPoint;
        private List<Socket>    clientSockets;

        public TcpServer()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            localEndPoint = new IPEndPoint(GetLocalAddress(), 50000);
            clientSockets = new List<Socket>();

            //绑定本地端口
            socket.Bind(localEndPoint);

            //开启监听
            socket.Listen(1024);
        }

        /// <summary>
        /// 接收客户端来连接，并开启一个Task, 次函数必须开启一个线程来一直接收客户端的连接
        /// </summary>
        public void Accept()
        {
            Console.WriteLine("服务端已经准备就绪，等待客户端连接！");
            while (true)
            {
                try
                {
                    Socket clientSocket = socket.Accept();

                    Console.WriteLine("客户端，Info=" + clientSocket.RemoteEndPoint.ToString());

                    //为了不影响客户端的连接，必须尽快处理，让服务端等待客户端的连接
                    clientSockets.Add(clientSocket);

                    //开启回话
                    //这里最好采用线程池来实现，否则新建线程耗时耗资源
                    StartChat(clientSocket);
                }
                catch(Exception e)
                {
                    Console.WriteLine("接收客户端连接失败，详细错误信息: " + e.Message);
                    break;
                }
            }
        }

        /// <summary>
        /// 开启回话
        /// </summary>
        /// <param name="clientSocket"></param>
        private void StartChat(Socket clientSocket)
        {
            Thread recvThread = new Thread(new ParameterizedThreadStart(this.Recv));
            Thread sendThread = new Thread(new ParameterizedThreadStart(this.Send));
            recvThread.IsBackground = true;
            sendThread.IsBackground = true;
            recvThread.Start(clientSocket);
            sendThread.Start(clientSocket);
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        private void Send(object clientSocket)
        {
            Socket client = clientSocket as Socket;
            if(client == null)
            {
                Console.WriteLine("开启发送数据线程失败!");
                return;
            }
            while (true)
            {
                string inputText = Console.ReadLine();
                if (inputText == "end")
                    break;
                try
                {
                    int sendBytes = client.Send(Encoding.UTF8.GetBytes(inputText));
                }
                catch (Exception e)
                {
                    Console.WriteLine("发送信息失败,详细错误信息：" + e.Message);
                }
            }
        }

        /// <summary>
        /// 接收数据
        /// </summary>
        private void Recv(object clientSocket)
        {
            Socket client = clientSocket as Socket;
            if (client == null)
            {
                Console.WriteLine("开启接收数据线程失败!");
                return;
            }
            byte[] buffer = new byte[1024];
            try
            {
                while (true)
                {
                    int readBytes = client.Receive(buffer);
                    string recvText = Encoding.UTF8.GetString(buffer, 0, readBytes);
                    Console.WriteLine("Receive data from " + client.RemoteEndPoint.ToString() + ": " + recvText);
                }
            }
            catch(SocketException se)
            {
                if (se.SocketErrorCode == SocketError.ConnectionAborted)
                    Console.WriteLine("客户端已经断开连接!");
                Console.WriteLine("接收客户端发送数据异常，详细信息：" + se.Message);
            }
        }

        /// <summary>
        /// 获取局域网的IPV4地址
        /// </summary>
        /// <returns></returns>
        private IPAddress GetLocalAddress()
        {
            string hostName = Dns.GetHostName();
            IPAddress[] ipAddr = Dns.GetHostAddresses(hostName);
            foreach (IPAddress ip in ipAddr)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip;
            }
            throw new ApplicationException("Can't find local area network ip address!");
        }
    }
}
