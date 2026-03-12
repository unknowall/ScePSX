using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Kaillera.Client;

namespace Kaillera
{
    public class Handler
    {
        public Client Client = new Client();
        public bool HasServers;

        public Handler()
        {
            var ret = Client.FetchServerList();
            HasServers = ret.Result;
        }

        public void OnConnected()
        {
        }

        public void OnDisconnected()
        {
        }

        public void OnConnectionFailed(string msg)
        {
        }

        public void OnUserJoined(UserInfo User)
        {
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

        public void OnChatMessage(string uname,string msg)
        {
        }

        public void OnServerMessage(string msg)
        {
        }
    }
}
