using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ScePSX;

namespace Kaillera
{
    // Protocol:
    // https://kaillerareborn.github.io/resources/kailleraprotocol.txt
    public class KailleraClient : IDisposable
    {
        public struct ServerInfo
        {
            public string Name;
            public string Location;
            public string Address;
            public int Users, MaxUsers, GameCount, Version, Port;
        }

        public struct UserInfo
        {
            public string Username;
            public int UserId;
            public int Ping;
            public int ConnectionType; // 6=Bad,5=Low,4=Average,3=Good,2=Excellent,1=LAN
            public byte Status; // 0=Playing, 1=Idle
        }

        public struct GameInfo
        {
            public string GameName;
            public int GameId;
            public string EmulatorName;
            public string Owner;
            public int PlayerCount;
            public int MaxPlayers;
            public byte Status; // 0=Waiting, 1=Playing, 2=Netsync
        }

        // Server List
        // http://www.kaillera.com/raw_server_list.php
        // http://www.kaillera.com/raw_server_list2.php
        public List<ServerInfo> Servers = new List<ServerInfo>();

        private UdpClient Udp;
        private IPEndPoint remoteEndPoint;

        private const string Version = "0.83";
        private string emulatorName = "ScePSX";

        public bool connected = false;
        public string username;
        private int connectionType = 3; // Default Good

        private ushort messageNumber = 0;

        private byte[] receiveBuffer = new byte[4096];

        public List<UserInfo> serverUsers = new List<UserInfo>();
        public List<GameInfo> serverGames = new List<GameInfo>();

        private int currentGameId = -1;
        private int playerNumber = -1;
        private int totalPlayers = 0;
        private int frameDelay = 0;
        private int CurSeqNum = 0;

        // (0x13 Game Cache)
        private byte[][] gameCache = new byte[256][];
        private int cachePosition = 0;

        public event Action<string, string> OnChatMessage;
        public event Action<UserInfo> OnUserJoined;
        public event Action<int, string> OnUserQuit;
        public event Action<GameInfo> OnGameCreated;
        public event Action<int> OnGameClosed;
        public event Action<GameInfo> OnGameUpdated;
        public event Action<int> OnGameStarted;
        public event Action<byte[]> OnGameData;
        public event Action<int> OnGameCache;
        public event Action<int, string> OnUserDropped;
        public event Action<string> OnServerMessage;
        public event Action OnConnected;
        public event Action<string> OnConnectionFailed;
        public event Action OnDisconnected;

        public KailleraClient()
        {

        }

        public void Dispose()
        {
            Disconnect();
        }

        #region ServerList

        public async Task<int> PingAsync(string Address)
        {
            try
            {
                using (var ping = new System.Net.NetworkInformation.Ping())
                {
                    var reply = await ping.SendPingAsync(Address, 1000);
                    return reply.Status == System.Net.NetworkInformation.IPStatus.Success
                        ? (int)reply.RoundtripTime
                        : -1;
                }
            } catch
            {
                return -1;
            }
        }

        public async Task<bool> FetchServerList(string Url = "http://www.kaillera.com/raw_server_list.php", bool Clear = true)
        {
            try
            {
                if (Clear)
                    Servers.Clear();

                using (HttpClient client = new HttpClient())
                {
                    string listData = await client.GetStringAsync(Url);

                    string[] lines = listData.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 0; i < lines.Length; i += 2)
                    {
                        if (i + 1 >= lines.Length)
                            break;

                        string name = lines[i].Trim();
                        string dataLine = lines[i + 1].Trim();

                        //ipAddress:port;users/maxusers;gameCount;version;location
                        //ipAddress:port;users/maxusers;gameCount;location
                        string[] parts = dataLine.Split(';');
                        if (parts.Length >= 4)
                        {
                            ServerInfo info = new ServerInfo();
                            info.Name = name;

                            string[] addrPort = parts[0].Split(':');
                            info.Address = addrPort[0];
                            info.Port = int.Parse(addrPort[1]);

                            string[] users = parts[1].Split('/');
                            info.Users = int.Parse(users[0]);
                            info.MaxUsers = int.Parse(users[1]);

                            info.GameCount = int.Parse(parts[2]);

                            if (parts.Length >= 5)
                            {
                                info.Version = int.Parse(parts[3]);
                                info.Location = parts[4];
                            } else
                            {
                                info.Version = 0;
                                info.Location = parts[3];
                            }
                            Servers.Add(info);
                        }
                    }

                    return true;
                }
            } catch
            {
                return false;
            }
        }

        public List<ServerInfo> GetServerList()
        {
            return Servers;
        }

        #endregion

        #region Connect

        public async Task<bool> Connect(string host, int port, string username, int connType = 3)
        {
            if (connected)
                Disconnect();

            try
            {
                this.username = username;
                this.connectionType = connType;
                connected = false;
                CurSeqNum = -1;

                Udp = new UdpClient();
                remoteEndPoint = new IPEndPoint(IPAddress.Parse(host), port);
                Udp.Connect(remoteEndPoint);

                string hello = "HELLO" + Version + "\0";
                byte[] helloData = Encoding.ASCII.GetBytes(hello);
                await Udp.SendAsync(helloData, helloData.Length);

                var ret = await Udp.ReceiveAsync();
                string response = Encoding.ASCII.GetString(ret.Buffer).TrimEnd('\0');
                if (!response.StartsWith("HELLOD00D"))
                {
                    OnConnectionFailed?.Invoke("Not Support Server");
                    Disconnect();
                    return false;
                }
                if (response.Length > 9)
                {
                    string portStr = response.Substring(9);
                    if (int.TryParse(portStr, out int newPort))
                    {
                        var remoteAddr = remoteEndPoint.Address;
                        Udp.Close();
                        Udp = new UdpClient();
                        remoteEndPoint = new IPEndPoint(IPAddress.Parse(host), newPort);
                        Udp.Connect(remoteEndPoint);
                    }
                }

                BeginReceive();

                SendLogin(username);

                return true;
            } catch (Exception ex)
            {
                OnConnectionFailed?.Invoke(ex.Message);
                return false;
            }
        }

        public void Disconnect()
        {
            if (connected)
            {
                SendUserQuit("");
                connected = false;
            }

            Udp?.Dispose();
        }

        private void BeginReceive()
        {
            try
            {
                Udp.BeginReceive(OnDataReceived, null);
            } catch
            {
                OnDisconnected?.Invoke();
            }
        }

        private void OnDataReceived(IAsyncResult ar)
        {
            try
            {
                IPEndPoint remoteEP = null;
                byte[] data = Udp.EndReceive(ar, ref remoteEP);

                if (data != null && data.Length > 0)
                {
                    ProcessIncomingData(data);
                }

                Udp.BeginReceive(OnDataReceived, null);
            } catch(Exception ex)
            {
                OnServerMessage?.Invoke(ex.Message);
                OnDisconnected?.Invoke();
            }
        }

        private void ProcessIncomingData(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                while (ms.Position < ms.Length)
                {
                    if (ms.Position + 1 > ms.Length)
                        break;
                    byte messageCount = reader.ReadByte();

                    for (int i = 0; i < messageCount; i++)
                    {
                        // Check Head (2+2+1=5)
                        if (ms.Position + 5 > ms.Length)
                            break;

                        ushort msgNumber = reader.ReadUInt16(); // little endian
                        ushort msgLength = reader.ReadUInt16(); // little endian
                        if (msgNumber <= CurSeqNum)
                        {
                            ms.Position += msgLength;
                            continue;
                        }
                        CurSeqNum = msgNumber;

                        byte msgType = reader.ReadByte();

                        if (ms.Position + (msgLength - 1) > ms.Length)
                            break;

                        byte[] msgData = reader.ReadBytes(msgLength - 1);

                        ProcessMessage(msgType, msgData);
                    }
                }
            }
        }

        private void ProcessMessage(byte type, byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                switch (type)
                {
                    case 0x01: // User Quit
                        {
                            string username = ReadString(reader);
                            int userId = reader.ReadUInt16();
                            string message = ReadString(reader);
                            OnUserQuit?.Invoke(userId, username);
                        }
                        break;

                    case 0x02: // User Joined
                        {
                            UserInfo user = new UserInfo();
                            user.Username = ReadString(reader);
                            user.UserId = reader.ReadUInt16();
                            user.Ping = reader.ReadInt32();
                            user.ConnectionType = reader.ReadByte();
                            serverUsers.Add(user);
                            OnUserJoined?.Invoke(user);
                        }
                        break;

                    case 0x04: // Server Status
                        {
                            ReadString(reader); // Empty string
                            int userCount = reader.ReadInt32();
                            int gameCount = reader.ReadInt32();

                            serverUsers.Clear();
                            for (int i = 0; i < userCount; i++)
                            {
                                UserInfo user = new UserInfo();
                                user.Username = ReadString(reader);
                                user.Ping = reader.ReadInt32();
                                user.ConnectionType = reader.ReadByte();
                                user.UserId = reader.ReadUInt16();
                                user.Status = reader.ReadByte();
                                serverUsers.Add(user);
                            }

                            serverGames.Clear();
                            for (int i = 0; i < gameCount; i++)
                            {
                                GameInfo game = new GameInfo();
                                game.GameName = ReadString(reader);
                                game.GameId = reader.ReadInt32();
                                game.EmulatorName = ReadString(reader);
                                game.Owner = ReadString(reader);

                                string players = ReadString(reader);
                                string[] playerParts = players.Split('/');
                                game.PlayerCount = int.Parse(playerParts[0]);
                                game.MaxPlayers = int.Parse(playerParts[1]);

                                game.Status = reader.ReadByte();
                                serverGames.Add(game);
                            }

                            connected = true;
                            OnConnected?.Invoke();
                        }
                        break;

                    case 0x05: // Server to KailleraClient ACK
                        {
                            ReadString(reader); // Empty
                            reader.ReadInt32(); // 00
                            reader.ReadInt32(); // 01
                            reader.ReadInt32(); // 02
                            reader.ReadInt32(); // 03

                            // KailleraClient ACK
                            SendClientAck();
                        }
                        break;

                    case 0x07: // Global Chat
                        {
                            string username = ReadString(reader);
                            string message = ReadString(reader);
                            OnChatMessage?.Invoke(username, message);
                        }
                        break;

                    case 0x08: // Game Chat
                        {
                            string username = ReadString(reader);
                            string message = ReadString(reader);
                            OnChatMessage?.Invoke(username, message);
                        }
                        break;

                    case 0x0A: // Create Game Notification
                        {
                            string username = ReadString(reader);
                            string gameName = ReadString(reader);
                            string emuName = ReadString(reader);
                            int gameId = reader.ReadInt32();

                            GameInfo game = new GameInfo
                            {
                                GameName = gameName,
                                GameId = gameId,
                                EmulatorName = emuName,
                                Owner = username,
                                PlayerCount = 1,
                                MaxPlayers = 2,
                                Status = 0
                            };
                            serverGames.Add(game);
                            OnGameCreated?.Invoke(game);
                        }
                        break;

                    case 0x0B: // Quit Game Notification
                        {
                            string username = ReadString(reader);
                            int userId = reader.ReadUInt16();

                            serverUsers.RemoveAll(u => u.UserId == userId);

                            if (userId == GetMyUserId())
                            {
                                currentGameId = -1;
                            }
                        }
                        break;

                    case 0x0C: // Join Game Notification
                        {
                            ReadString(reader); // Empty
                            int gamePtr = reader.ReadInt32();
                            string username = ReadString(reader);
                            int ping = reader.ReadInt32();
                            int userId = reader.ReadUInt16();
                            int connType = reader.ReadByte();

                            UserInfo user = new UserInfo
                            {
                                Username = username,
                                UserId = userId,
                                Ping = ping,
                                ConnectionType = connType
                            };
                            serverUsers.Add(user);

                            if (username == this.username)
                            {
                                currentGameId = gamePtr; // eg: SLUS-01234
                            }
                        }
                        break;

                    case 0x0D: // Player Information
                        {
                            ReadString(reader); // Empty
                            int count = reader.ReadInt32();

                            serverUsers.Clear();
                            for (int i = 0; i < count; i++)
                            {
                                UserInfo user = new UserInfo();
                                user.Username = ReadString(reader);
                                user.Ping = reader.ReadInt32();
                                user.UserId = reader.ReadUInt16();
                                user.ConnectionType = reader.ReadByte();
                                serverUsers.Add(user);
                            }
                        }
                        break;

                    case 0x0E: // Update Game Status
                        {
                            ReadString(reader); // Empty
                            int gameId = reader.ReadInt32();
                            byte status = reader.ReadByte();
                            byte playerCount = reader.ReadByte();
                            byte maxPlayers = reader.ReadByte();

                            var game = serverGames.FirstOrDefault(g => g.GameId == gameId);
                            if (game.GameId != 0)
                            {
                                game.Status = status;
                                game.PlayerCount = playerCount;
                                game.MaxPlayers = maxPlayers;
                                OnGameUpdated?.Invoke(game);
                            }
                        }
                        break;

                    case 0x10: // Close Game
                        {
                            ReadString(reader); // Empty
                            int gameId = reader.ReadInt32();
                            serverGames.RemoveAll(g => g.GameId == gameId);
                            OnGameClosed?.Invoke(gameId);
                        }
                        break;

                    case 0x11: // Start Game Notification
                        {
                            ReadString(reader); // Empty
                            frameDelay = reader.ReadUInt16();
                            playerNumber = reader.ReadByte();
                            totalPlayers = reader.ReadByte();

                            OnGameStarted?.Invoke(currentGameId);

                            // Ready to Play Signal
                            SendReadyToPlay();
                        }
                        break;

                    case 0x12: // Game Data Notify
                        {
                            ReadString(reader); // Empty
                            ushort length = reader.ReadUInt16();
                            byte[] gameData = reader.ReadBytes(length);

                            TryCacheGameData(gameData);

                            OnGameData?.Invoke(gameData);
                        }
                        break;

                    case 0x13: // Game Cache Notify
                        {
                            ReadString(reader); // Empty
                            byte cachePos = reader.ReadByte();

                            OnGameCache?.Invoke(cachePos);
                        }
                        break;

                    case 0x14: // Drop Game Notification
                        {
                            string username = ReadString(reader);
                            byte playerNum = reader.ReadByte();

                            OnUserDropped?.Invoke(playerNum, username);
                        }
                        break;

                    case 0x15: // Ready to Play Signal Notification
                        {
                            ReadString(reader); // Empty
                        }
                        break;

                    case 0x16: // Ping too High Message
                        {
                            string server = ReadString(reader);
                            string message = ReadString(reader);
                            OnServerMessage?.Invoke(message);
                            Disconnect();
                        }
                        break;
                    case 0x17: // Server Information Message
                        {
                            string server = ReadString(reader);
                            string message = ReadString(reader);
                            OnServerMessage?.Invoke(message);
                        }
                        break;
                }
            }
        }

        #endregion

        #region Send

        private void SendPacket(byte[][] messages)
        {
            if (Udp == null)
                return;

            using (MemoryStream ms = new MemoryStream())
            {
                // Number
                ms.WriteByte((byte)messages.Length);

                foreach (var msg in messages)
                {
                    ms.Write(msg, 0, msg.Length);
                }

                byte[] packet = ms.ToArray();
                Udp.Send(packet, packet.Length);
            }
        }

        private byte[] BuildMessage(byte type, byte[] data)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ms.WriteByte((byte)(messageNumber & 0xFF));
                ms.WriteByte((byte)((messageNumber >> 8) & 0xFF));
                messageNumber++;

                // (type + data)
                ushort length = (ushort)(1 + (data?.Length ?? 0));
                ms.WriteByte((byte)(length & 0xFF));
                ms.WriteByte((byte)((length >> 8) & 0xFF));

                ms.WriteByte(type);

                // data
                if (data != null && data.Length > 0)
                {
                    ms.Write(data, 0, data.Length);
                }

                return ms.ToArray();
            }
        }

        public void SendLogin(string UserName = "ScePSX#user")
        {
            username = UserName;
            using (MemoryStream ms = new MemoryStream())
            {
                WriteString(ms, username);
                WriteString(ms, emulatorName);
                ms.WriteByte((byte)connectionType);

                byte[][] messages = new byte[][]
                {
                    BuildMessage(0x03, ms.ToArray())
                };

                SendPacket(messages);
            }
        }

        private void SendClientAck()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                WriteString(ms, "");
                ms.Write(BitConverter.GetBytes(0), 0, 4);
                ms.Write(BitConverter.GetBytes(1), 0, 4);
                ms.Write(BitConverter.GetBytes(2), 0, 4);
                ms.Write(BitConverter.GetBytes(3), 0, 4);

                byte[][] messages = new byte[][]
                {
                    BuildMessage(0x06, ms.ToArray())
                };

                SendPacket(messages);
            }
        }

        public void SendGlobalChat(string message)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                WriteString(ms, "");
                WriteString(ms, message);

                byte[][] messages = new byte[][]
                {
                    BuildMessage(0x07, ms.ToArray())
                };

                SendPacket(messages);
            }
        }

        public void SendGameChat(string message)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                WriteString(ms, "");
                WriteString(ms, message);

                byte[][] messages = new byte[][]
                {
                    BuildMessage(0x08, ms.ToArray())
                };

                SendPacket(messages);
            }
        }

        public void SendKeepAlive()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                WriteString(ms, "");

                byte[][] messages = new byte[][]
                {
                    BuildMessage(0x09, ms.ToArray())
                };

                SendPacket(messages);
            }
        }

        public void SendCreateGame(string gameId)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                WriteString(ms, "");
                WriteString(ms, gameId);
                WriteString(ms, "");
                ms.Write(BitConverter.GetBytes(0xFFFFFFFF), 0, 4);

                byte[][] messages = new byte[][]
                {
                    BuildMessage(0x0A, ms.ToArray())
                };

                SendPacket(messages);
            }
        }

        public void SendJoinGame(int gameId)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                WriteString(ms, "");
                ms.Write(BitConverter.GetBytes(gameId), 0, 4);
                WriteString(ms, "");
                ms.Write(BitConverter.GetBytes(0), 0, 4);
                ms.Write(BitConverter.GetBytes((ushort)0xFFFF), 0, 2);
                ms.WriteByte((byte)connectionType);

                byte[][] messages = new byte[][]
                {
                    BuildMessage(0x0C, ms.ToArray())
                };

                SendPacket(messages);
            }
        }

        public void SendQuitGame()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                WriteString(ms, "");
                ms.Write(BitConverter.GetBytes((ushort)0xFFFF), 0, 2);

                byte[][] messages = new byte[][]
                {
                    BuildMessage(0x0B, ms.ToArray())
                };

                SendPacket(messages);
            }
        }

        public void SendStartGame()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                WriteString(ms, "");
                ms.Write(BitConverter.GetBytes((ushort)0xFFFF), 0, 2);
                ms.WriteByte(0xFF);
                ms.WriteByte(0xFF);

                byte[][] messages = new byte[][]
                {
                    BuildMessage(0x11, ms.ToArray())
                };

                SendPacket(messages);
            }
        }

        public void SendReadyToPlay()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                WriteString(ms, "");

                byte[][] messages = new byte[][]
                {
                    BuildMessage(0x15, ms.ToArray())
                };

                SendPacket(messages);
            }
        }

        public void SendGameData(byte[] data)
        {
            int cachePos = FindInCache(data);
            if (cachePos >= 0)
            {
                SendGameCache((byte)cachePos);
            } else
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    WriteString(ms, "");
                    ms.Write(BitConverter.GetBytes((ushort)data.Length), 0, 2);
                    ms.Write(data, 0, data.Length);

                    byte[][] messages = new byte[][]
                    {
                        BuildMessage(0x12, ms.ToArray())
                    };

                    SendPacket(messages);
                }
            }
        }

        private void SendGameCache(byte cachePos)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                WriteString(ms, "");
                ms.WriteByte(cachePos);

                byte[][] messages = new byte[][]
                {
                    BuildMessage(0x13, ms.ToArray())
                };

                SendPacket(messages);
            }
        }

        public void SendDropGame()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                WriteString(ms, "");
                ms.WriteByte(0x00);

                byte[][] messages = new byte[][]
                {
                    BuildMessage(0x14, ms.ToArray())
                };

                SendPacket(messages);
            }
        }

        public void SendKickUser(int userId)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                WriteString(ms, "");
                ms.Write(BitConverter.GetBytes((ushort)userId), 0, 2);

                byte[][] messages = new byte[][]
                {
                    BuildMessage(0x0F, ms.ToArray())
                };

                SendPacket(messages);
            }
        }

        public void SendUserQuit(string message)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                WriteString(ms, "");
                ms.Write(BitConverter.GetBytes((ushort)0xFFFF), 0, 2);
                WriteString(ms, message);

                byte[][] messages = new byte[][]
                {
                    BuildMessage(0x01, ms.ToArray())
                };

                SendPacket(messages);
            }
        }

        #endregion

        #region Cache

        private void TryCacheGameData(byte[] data)
        {
            if (FindInCache(data) >= 0)
                return;

            if (cachePosition >= 256)
                cachePosition = 0;

            gameCache[cachePosition] = data;
            cachePosition++;
        }

        private int FindInCache(byte[] data)
        {
            for (int i = 0; i < 256; i++)
            {
                if (gameCache[i] != null && ArraysEqual(gameCache[i], data))
                    return i;
            }
            return -1;
        }

        private bool ArraysEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i])
                    return false;
            return true;
        }

        #endregion

        #region Helper

        private string ReadString(BinaryReader reader)
        {
            List<byte> bytes = new List<byte>();
            byte b;
            while ((b = reader.ReadByte()) != 0)
            {
                bytes.Add(b);
            }
            //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            //Encoding gbk = Encoding.GetEncoding("GBK");
            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        private void WriteString(MemoryStream ms, string str)
        {
            if (!string.IsNullOrEmpty(str))
            {
                byte[] data = Encoding.UTF8.GetBytes(str);
                ms.Write(data, 0, data.Length);
            }
            ms.WriteByte(0); // end
        }

        private int GetMyUserId()
        {
            var me = serverUsers.FirstOrDefault(u => u.Username == username);
            return me.UserId;
        }

        public List<UserInfo> GetRoomUsers()
        {
            return serverUsers;
        }

        public List<GameInfo> GetGames()
        {
            return serverGames;
        }

        public int GetPlayerNumber()
        {
            return playerNumber;
        }

        public int GetFrameDelay()
        {
            return frameDelay;
        }

        #endregion
    }
}
