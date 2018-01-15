using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Sockets;
using System.IO;
using Newtonsoft.Json;

using Discord.WebSocket;

namespace XzBotDiscord
{
    public class StreamerNode
    {
        public ulong userID;
        public string streamURL;

        public StreamerNode(ulong user_id, string stream_url)
        {
            userID = user_id;
            streamURL = stream_url;
        }
    }


    class Streaming
    {
        SQLController sqlController;
        static DiscordManager discordManager;
        Twitch twitchBot;

        private List<StreamerNode> streamerList;

        public Streaming(DiscordManager discord_manager)
        {
            discordManager = discord_manager;
            sqlController = new SQLController();
            twitchBot = new Twitch();
        }

        public void UpdateStreamURL(SocketUser user, string message)
        {
            sqlController.UpdateGo("Users", "stream_url = '" + message + "'", " user_id = " + user.Id);
        }

        public void GetStreamersFromDB()
        {
            if (streamerList == null)
                streamerList = new List<StreamerNode>();

            streamerList.Clear();

            List<SqlRow> retreived = sqlController.RetreiveGo("*", "Users", "stream_url IS NOT NULL", "");
            foreach (SqlRow row in retreived)
            {
                ulong userID = ulong.Parse(row.MainTuple["user_id"]);
                string streamURL = row.MainTuple["stream_url"];
                streamerList.Add(new StreamerNode(userID, streamURL));
            }
        }

        public void CheckActiveStreamers()
        {
            foreach (StreamerNode currentNode in streamerList)
            {
                //Need to check to see if the stream is live
                bool isStreamLive = twitchBot.CheckIfStreamIsOnline(currentNode.streamURL);
                //Then we set the streamer to active if it is
                if (isStreamLive)
                {
                    SetStreamerActive(currentNode.userID);
                }
                else
                {
                    //If not then we make sure they do not have the streamer role
                    SetStreamerInactive(currentNode.userID);
                }
            }
        }

        private void SetStreamerActive(ulong streamer_id)
        {
            List<SocketRole> roleList = discordManager.GetRolesForServer("Heaven and Earth");
            SocketUser user = discordManager.GetUserByID(streamer_id);
            string roleName = "Streaming";
            bool returned = false;
            foreach (SocketRole role in roleList)
            {
                if (roleName.ToString().ToLower().Trim().Equals(role.Name.ToLower()))
                {
                    roleName = role.Name;
                    returned = discordManager.SetRoleForUser(role, user, "Heaven and Earth", true);
                }
            }
        }

        private void SetStreamerInactive(ulong streamer_id)
        {
            List<SocketRole> roleList = discordManager.GetRolesForServer("Heaven and Earth");
            SocketUser user = discordManager.GetUserByID(streamer_id);
            string roleName = "Streaming";
            bool returned = false;
            foreach (SocketRole role in roleList)
            {
                if (roleName.ToString().ToLower().Trim().Equals(role.Name.ToLower()))
                {
                    roleName = role.Name;
                    discordManager.RemoveRoleForUser(role, user, "Heaven and Earth", true);
                }
            }
        }
        
    }
}
