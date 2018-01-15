using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Threading;

using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace XzBotDiscord
{
    public class DiscordManager
    {
        bool init = false;
        private DiscordSocketClient client;
        Dictionary<string, SocketChannel> channelList = new Dictionary<string, SocketChannel>();

        CustomCommands customCommands;
        Scheduling schedulingClass;

        public DiscordManager(DiscordSocketClient clientIncoming)
        {
            client = clientIncoming;
            client.Log += Log;
            client.MessageReceived += MessageReceived;
            customCommands = new CustomCommands(this);
            schedulingClass = new Scheduling(this);

            Task taskA = Task.Run(() => NightTimer());
        }

        private async Task NightTimer()
        {
            while (true)
            {
                int sleeptimer = 1000;

                if (init == false && client.ConnectionState == ConnectionState.Connected)
                {
                    InitAfterLoggedIn();
                }

                CheckNightChanged();

                Thread.Sleep(sleeptimer);
            }

        }

        private void InitAfterLoggedIn()
        {
            for (int i = 0; i < client.Guilds.Count; i++)
            {
                if (client.Guilds.ElementAt(i).Name.Equals("Heaven and Earth"))
                {
                    for (int j = 0; j < client.Guilds.ElementAt(i).Channels.Count; j++)
                    {
                        if (client.Guilds.ElementAt(i).Channels.ElementAt(j).Name != null)
                        {
                            channelList.Add(client.Guilds.ElementAt(i).Channels.ElementAt(j).Name, client.Guilds.ElementAt(i).Channels.ElementAt(j));
                        }
                    }
                }
            }
            init = true;
        }

        private void CheckNightChanged()
        {
            if (customCommands.isNight == true)
            {
                //5*20
                if ((customCommands.nightStart.AddMinutes(40) < DateTime.Now))
                {
                    customCommands.isNight = false;
                    customCommands.dayStart = customCommands.nightStart.AddMinutes(40);
                }
            }
            else
            {
                //13.333*4
                if ((customCommands.dayStart.AddMinutes(200) < DateTime.Now))
                {
                    customCommands.isNight = true;
                    customCommands.nightStart = customCommands.dayStart.AddMinutes(200);
                }
            }
        }

        private async Task MessageReceived(SocketMessage message)
        {
            var author = message.Author;
            var channel = message.Channel;

            string returnedString = "";
            if (!author.IsBot)
                returnedString = customCommands.IncomingMessage(author, channel, message.Content.ToString());

            if (returnedString.Length > 0)
                await message.Channel.SendMessageAsync(returnedString);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public void MessageChannel(string[] allWords)
        {
            if (channelList.ContainsKey(allWords[1]))
            {
                string message = "";
                for (int i = 2; i < allWords.Length; i++)
                {
                    message += " " + allWords[i];
                }

                var channelName = client.GetChannel(channelList[allWords[1]].Id);
                IMessageChannel messageChannel = (IMessageChannel)channelName;
                messageChannel.SendMessageAsync(message);
            }
        }

        public void EmbedToChannel(Embed embed, ISocketMessageChannel channel)
        {
            var channelName = client.GetChannel(channelList[channel.ToString()].Id);
            IMessageChannel messageChannel = (IMessageChannel)channelName;
            messageChannel.SendMessageAsync("", false, embed);
        }

        public void EmbedToUser(Embed embed, SocketUser user = null, string userName = null)
        {
            if (user != null)
            {
                user.SendMessageAsync("", false, embed);
            }
            else
            {
                if (userName != null)
                {
                    SocketUser userToPM = GetUserByNickName(userName);
                    userToPM.SendMessageAsync("", false, embed);
                }
            }
            Thread.Sleep(200);
        }

        public void SendFileToChannel(string path, IMessageChannel channel)
        {
            if (!path.Contains("Error"))
            {
                channel.SendFileAsync(path);
            }else
            {
                channel.SendMessageAsync(path);
            }
        }
        public void SendPMToUser(string message, SocketUser user = null,string userName = null)
        {
            if (user != null)
            {
                user.SendMessageAsync(message);
            }
            else
            {
                if (userName != null)
                {
                    SocketUser userToPM = GetUserByNickName(userName);
                    userToPM.SendMessageAsync(message);
                }
            }
        }

        public List<string> GetRolesUserById(ulong id,string guildName)
        {
            List<SocketRole> roles = new List<SocketRole>();
            List<string> stringRoles = new List<string>();
            if (client.ConnectionState == ConnectionState.Connected)
            {
                for (int i = 0; i < client.Guilds.Count; i++)
                {
                    if (client.Guilds.ElementAt(i).Name.Equals(guildName))
                    {
                        foreach (var item in client.Guilds.ElementAt(i).Users)
                        {
                            if (item.Id.Equals(id))
                            {
                                roles = item.Roles.ToList();
                                break;
                            }
                        }
                    }
                }
            }

            for (int j = 0; j < roles.Count; j++)
            {
                stringRoles.Add(roles[j].Name);
            }

            return stringRoles;
        }
        public List<SocketRole> GetRolesForServer(string guildName)
        {
            List<SocketRole> roles = new List<SocketRole>();

            for (int i = 0; i < client.Guilds.Count; i++)
            {
                if (client.Guilds.ElementAt(i).Name.Equals(guildName))
                {
                    for (int j = 0; j < client.Guilds.ElementAt(i).Roles.Count; j++)
                    {
                        roles.Add(client.Guilds.ElementAt(i).Roles.ElementAt(j));
                    }
                }
            }
            return roles;
        }

        public async void RemoveRoleForUser(SocketRole role, SocketUser user, string guildName, bool is_from_bot)
        {
            if (client.ConnectionState == ConnectionState.Connected)
            {
                for (int i = 0; i < client.Guilds.Count; i++)
                {
                    if (client.Guilds.ElementAt(i).Name.Equals(guildName))
                    {
                        foreach (var item in client.Guilds.ElementAt(i).Users)
                        {
                            if (item.Id.Equals(user.Id))
                            {
                                await item.RemoveRoleAsync(role);
                            }
                        }
                    }
                }
            }
        }

        public bool SetRoleForUser(SocketRole role, SocketUser user, string guildName,bool is_from_bot)
        {
            string[] excludeRoles = {"admin","streaming","officer","veteran","elite","member","bot"};

            if (!excludeRoles.Any(role.Name.ToLower().Contains) || is_from_bot == true)
            {
                if (client.ConnectionState == ConnectionState.Connected)
                {
                    SetRole(guildName, user, role);
                    return true;
                }
            }
            return false;
        }

        private async void SetRole(string guildName, SocketUser user, SocketRole role)
        {
            for (int i = 0; i < client.Guilds.Count; i++)
            {
                if (client.Guilds.ElementAt(i).Name.Equals(guildName))
                {
                    foreach (var item in client.Guilds.ElementAt(i).Users)
                    {
                        if (item.Id.Equals(user.Id))
                        {
                            await item.AddRoleAsync(role);
                        }
                    }
                }
            }     
        }

        public SocketUser GetUserByNickName(string nickName)
        {
            if (client.ConnectionState == ConnectionState.Connected)
            {
                for (int i = 0; i < client.Guilds.Count; i++)
                {
                    if (client.Guilds.ElementAt(i).Name.Equals("Heaven and Earth"))
                    {
                        foreach (var item in client.Guilds.ElementAt(i).Users)
                        {
                            if (item.Username != null && item.Username.Equals(nickName))
                            {
                                return item;
                            }
                        }
                    }
                }
            }
            return null;
        }
        public SocketUser GetUserByID(ulong user_id)
        {
            if (client.ConnectionState == ConnectionState.Connected)
            {
                for (int i = 0; i < client.Guilds.Count; i++)
                {
                    if (client.Guilds.ElementAt(i).Name.Equals("Heaven and Earth"))
                    {
                        foreach (var item in client.Guilds.ElementAt(i).Users)
                        {
                            if (item.Username != null && item.Id.Equals(user_id))
                            {
                                return item;
                            }
                        }
                    }
                }
            }
            return null;
        }

        public List<SocketGuildChannel> GetChannelList(bool isVoice,string guildName)
        {
            List<SocketGuildChannel> preChannelList = new List<SocketGuildChannel>();

            if (client != null && client.ConnectionState == ConnectionState.Connected)
            {
                for (int i = 0; i < client.Guilds.Count; i++)
                {
                    if (client.Guilds.ElementAt(i).Name.Equals(guildName))
                    {
                        preChannelList = client.Guilds.ElementAt(i).Channels.ToList();
                    }
                }
            }

            for (int j = 0; j < preChannelList.Count; j++)
            {
                if (isVoice == true)
                {
                    if (!preChannelList[j].GetType().Name.ToString().Equals("SocketVoiceChannel"))
                    {
                        preChannelList.RemoveAt(j);
                        j--;
                    }
                }
                else
                {
                    if (preChannelList[j].GetType().Name.ToString().Equals("SocketVoiceChannel"))
                    {
                        preChannelList.RemoveAt(j);
                        j--;
                    }
                }

            }
            return preChannelList;
        }
        public ISocketMessageChannel GetChannelByName(string channelName, string guildName)
        {
            if (client != null && client.ConnectionState == ConnectionState.Connected)
            {
                for (int i = 0; i < client.Guilds.Count; i++)
                {
                    if (client.Guilds.ElementAt(i).Name.Equals(guildName))
                    {
                        for (int j = 0; j < client.Guilds.ElementAt(i).Channels.Count; j++)
                        {
                            if (client.Guilds.ElementAt(i).Channels.ElementAt(j).Name.Equals(channelName))
                                return (ISocketMessageChannel)client.Guilds.ElementAt(i).Channels.ElementAt(j);
                        }
                    }
                }
            }
            return null;
        }


        public async void DeleteMessagesFromChannel(int messages, ISocketMessageChannel channel)
        {
            if(channel != null && messages < 50)
            {
                var messagesToDelete = await channel.GetMessagesAsync(messages).Flatten();

                try
                {
                    await channel.DeleteMessagesAsync(messagesToDelete);
                }
                catch (Exception e)
                {
                }
            }
        }





    }
}