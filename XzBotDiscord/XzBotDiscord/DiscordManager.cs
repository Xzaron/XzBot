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
        }

        private async Task NightTimer()
        {
            while (true)
            {
                if (init == false && client.ConnectionState == ConnectionState.Connected)
                {
                    for (int i = 0; i < client.Guilds.Count; i++)
                    {
                        if (client.Guilds.ElementAt(i).Name.Equals("Heaven and Earth"))
                        {
                            for (int j = 0; j < client.Guilds.ElementAt(i).Channels.Count; j++)
                            {
                                channelList.Add(client.Guilds.ElementAt(i).Channels.ElementAt(j).Name, client.Guilds.ElementAt(i).Channels.ElementAt(j));
                            }
                        }
                    }
                    init = true;
                }


                int sleeptimer = 1000;
                if (customCommands.isNight == true)
                {
                    //5*20
                    if ((customCommands.nightStart.AddMinutes(40) < DateTime.Now))
                    {
                        customCommands.isNight = false;
                        //customCommands.dayStart = customCommands.nightStart.AddSeconds(1);
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

                Thread.Sleep(sleeptimer);
            }

        }

        private async Task MessageReceived(SocketMessage message)
        {
            var author = message.Author;
            var channel = message.Channel;

            string returnedString = "";
            if (!author.IsBot)
                returnedString = customCommands.IncomingMessage(author, channel.ToString(), message.Content.ToString());

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

        public List<string> GetRolesUserById(ulong id)
        {
            List<SocketRole> roles = new List<SocketRole>();
            List<string> stringRoles = new List<string>();
            if (client.ConnectionState == ConnectionState.Connected)
            {
                for (int i = 0; i < client.Guilds.Count; i++)
                {
                    if (client.Guilds.ElementAt(i).Name.Equals("Heaven and Earth"))
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

        public List<SocketGuildChannel> GetChannelList(bool isVoice)
        {
            List<SocketGuildChannel> preChannelList = new List<SocketGuildChannel>();

            if (client != null && client.ConnectionState == ConnectionState.Connected)
            {
                for (int i = 0; i < client.Guilds.Count; i++)
                {
                    if (client.Guilds.ElementAt(i).Name.Equals("Heaven and Earth"))
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







    }
}