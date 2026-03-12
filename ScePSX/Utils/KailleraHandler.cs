using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Kaillera.KailleraClient;

namespace Kaillera
{
    public class KailleraHandler
    {
        public KailleraClient Client = new KailleraClient();
        //public bool HasServers;

        public enum Events
        {
            None = 0,
            
            Connect,
            Disconnect,
            ConnectFailed,
            
            UserJoin,
            UserQuit,
            UserDroped,

            GameCreated,
            GameUpdated,
            GameStarted,
            GameClosed,

            GameCache,
            GameData,

            ChatMsg,
            SrvMsg
        }

        public event Action<Events, string> OnEvent;
        public event Action<Events, byte[]> OnKData;

        public KailleraHandler()
        {
            Client.OnConnected += OnConnected;
            Client.OnDisconnected += OnDisconnected;
            Client.OnConnectionFailed += OnConnectionFailed;

            Client.OnUserJoined += OnUserJoined;
            Client.OnUserQuit += OnUserQuit;
            Client.OnUserDropped += OnUserDropped;

            Client.OnGameCreated += OnGameCreated;
            Client.OnGameUpdated += OnGameUpdated;

            Client.OnGameStarted += OnGameStarted;
            Client.OnGameClosed += OnGameClosed;

            Client.OnGameCache += OnGameCache;
            Client.OnGameData += OnGameData;

            Client.OnChatMessage += OnChatMessage;
            Client.OnServerMessage += OnServerMessage;

            //var ret = Client.FetchServerList();
            //HasServers = ret.Result;
        }

        public void OnConnected()
        {
            OnEvent?.Invoke(Events.Connect, "");
        }

        public void OnDisconnected()
        {
            OnEvent?.Invoke(Events.Disconnect, "");
        }

        public void OnConnectionFailed(string msg)
        {
            OnEvent?.Invoke(Events.ConnectFailed, msg);
        }

        public void OnUserJoined(UserInfo User)
        {
            OnEvent?.Invoke(Events.UserJoin, User.Username);
        }

        public void OnUserQuit(int uid, string uname)
        {
        }

        public void OnUserDropped(int uid, string uname)
        {
        }

        public void OnGameCreated(GameInfo Game)
        {
        }

        public void OnGameUpdated(GameInfo Game)
        {
        }

        public void OnGameStarted(int ID)
        {
        }

        public void OnGameClosed(int ID)
        {
        }

        public void OnGameCache(int num)
        {
        }

        public void OnGameData(byte[] Data)
        {
        }

        public void OnChatMessage(string uname, string msg)
        {
            if (msg.Length < 1 || uname.Length < 1)
                return;

            OnEvent?.Invoke(Events.ChatMsg, $"> {uname} : {msg}\r\n");
        }

        public void OnServerMessage(string msg)
        {
            OnEvent?.Invoke(Events.SrvMsg, msg+"\r\n");
        }
    }
}
