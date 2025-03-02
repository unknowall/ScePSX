using System;
using System.Net;
using System.Net.Sockets;

namespace ScePSX
{
    public interface Socket
    {
        public void Send(byte[] buffer);
        public byte[] Receive();

        public void ConnectToServer();
        public void AcceptClientConnection();

        public void BeginReceiving();
        public void Stop(IAsyncResult result);

        public bool IsConnected();

        public void Terminate();

    }

    public class Server : Socket
    {
        public IPAddress ipAddress = IPAddress.Parse("127.0.0.1"); // Localhost
        public int port = 1234;
        TcpClient client;
        NetworkStream Stream;
        TcpListener listener;
        AsyncCallback DataReceivedHandler;
        public byte[] buffer = new byte[2];
        public Server(AsyncCallback handler)
        {
            DataReceivedHandler = handler;
        }

        public void AcceptClientConnection()
        {
            listener = new TcpListener(ipAddress, port);
            Console.WriteLine("[SIO1] Server started, waiting for a connection...");
            listener.Start();
            listener.BeginAcceptTcpClient(new AsyncCallback(ClientConnected), null);
        }

        public byte[] Receive()
        {
            return buffer;
        }

        public void Send(byte[] buffer)
        {
            if (buffer.Length > 2 || Stream == null)
            {
                throw new Exception();
            }
            Stream.Write(buffer, 0, buffer.Length);
        }
        public void BeginReceiving()
        {
            if (DataReceivedHandler == null)
            {
                Console.WriteLine("[SIO1] DataReceivedHandler == null");
            }
            Stream.BeginRead(buffer, 0, 2, DataReceivedHandler, null);
        }
        public void Stop(IAsyncResult result)
        {
            Stream.EndRead(result);
        }

        public void ClientConnected(IAsyncResult result)
        {
            if (!listener.Server.IsBound)
            {
                return;
            }

            try
            {
                client = listener.EndAcceptTcpClient(result);
                Stream = client.GetStream();
                IPEndPoint remoteIpEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                Console.WriteLine("[SIO1] Client Connected: " + remoteIpEndPoint?.Address + ":" + remoteIpEndPoint?.Port);
                BeginReceiving();
            } catch (Exception e)
            {
                Console.WriteLine("[SIO1] Error: " + e.Message);
            }
        }

        public void ConnectToServer()
        {
            throw new NotSupportedException();
        }

        public bool IsConnected()
        {
            return Stream != null;
        }
        public void Terminate()
        {
            if (Stream != null)
            {
                Stream.Close();
            }

            if (client != null)
            {
                client.Close();
            }

            if (listener != null)
            {
                listener.Stop();
            }
        }
    }

    public class Client : Socket
    {
        public IPAddress ipAddress = IPAddress.Parse("127.0.0.1"); // Localhost
        public int port = 1234;
        TcpClient TCP_Client;
        NetworkStream Stream;
        public byte[] buffer = new byte[2];
        AsyncCallback DataReceivedHandler;

        public Client(AsyncCallback handler)
        {
            DataReceivedHandler = handler;
        }

        public void ConnectToServer()
        {
            TCP_Client = new TcpClient();
            try
            {
                TCP_Client.Connect(ipAddress, port);
                Stream = TCP_Client.GetStream();
                BeginReceiving();
                Console.WriteLine("[SIO1] Connected to " + ipAddress + ":" + port);
            } catch
            {
                Console.WriteLine("[SIO1] Could not connect to server");
                TCP_Client = null;
                Stream = null;
            }
        }
        public byte[] Receive()
        {
            return buffer;
        }

        public void Send(byte[] buffer)
        {
            if (buffer.Length > 2 || Stream == null)
            {
                throw new Exception();
            }
            Stream.Write(buffer, 0, buffer.Length);
        }

        public void AcceptClientConnection()
        {
            throw new NotSupportedException();
        }

        public void BeginReceiving()
        {
            if (DataReceivedHandler == null)
            {
                throw new NullReferenceException();
            }
            Stream.BeginRead(buffer, 0, 2, DataReceivedHandler, null);
        }

        public void Stop(IAsyncResult result)
        {
            Stream.EndRead(result);
        }

        public bool IsConnected()
        {
            return Stream != null;
        }
        public void Terminate()
        {
            if (Stream != null)
            {
                Stream.Close();
            }

            if (TCP_Client != null)
            {
                TCP_Client.Close();
            }
        }
    }

}
