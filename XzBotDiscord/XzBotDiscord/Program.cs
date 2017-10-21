using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace XzBotDiscord
{
    public class Program
    {
        private CommandService commands;
        private DiscordSocketClient client;
        private IServiceProvider services;

        //general channel id
        ulong general_channel_id = 171665066754703361;
        ulong devroom_channel_id = 326457433499107338;

        SocketMessage mainMessage;
        Dictionary<string, SocketChannel> channelList = new Dictionary<string, SocketChannel>();
        bool init = false;

        CustomCommands customCommands;
        Scheduling schedulingClass;
        public SQLController sqlController;
        public ReadWriteFile readWriteFile;

        private static string token = null;

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            readWriteFile = new ReadWriteFile(this);
            customCommands = new CustomCommands(this);
            schedulingClass = new Scheduling(this);
            client = new DiscordSocketClient();
            services = new ServiceCollection().BuildServiceProvider();
            commands = new CommandService();
            
            sqlController = new SQLController(this);

            string[] allLines = readWriteFile.ReturnAllLinesAsArray("c:\\Users\\Public\\Documents\\DiscordSQLConnection.txt");
            Dictionary<string, string> sqlDict = readWriteFile.CreateDictFromStringArray(allLines, '=');
            sqlController.UpdateDBName(sqlDict["Database"]);
            sqlController.UpdateDBUserID(sqlDict["UserID"]);
            sqlController.UpdateDBPassword(sqlDict["Password"]);
            token = sqlDict["Token"];




            client.Log += Log;
            client.MessageReceived += MessageReceived;

            string tokenNew = token; // Remember to keep this private!

            await InstallCommands();

            await client.LoginAsync(TokenType.Bot, tokenNew);
            await client.StartAsync();

            Task taskA = Task.Run(() => NightTimer());

            await Task.Delay(-1);
        }


        private async Task NightTimer()
        {
             while(true)
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
                if(customCommands.isNight == true)
                {
                    //5*20
                    if((customCommands.nightStart.AddMinutes(40) < DateTime.Now ))
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

                Thread.Sleep(1000);
            }

        }

        private async Task MessageReceived(SocketMessage message)
        {
            var author = message.Author;
            var channel = message.Channel;

            
            if (mainMessage == null)
            {
                mainMessage = message;
            }


            string returnedString = "";
            if(!author.IsBot)
                returnedString = customCommands.IncomingMessage(author,channel.ToString(), message.Content.ToString());

            if(returnedString.Length > 0)
                await message.Channel.SendMessageAsync(returnedString);

        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }


        public async Task InstallCommands()
        {
            // Hook the MessageReceived Event into our Command Handler
            client.MessageReceived += HandleCommand;
            // Discover all of the commands in this assembly and load them.
            await commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public async Task HandleCommand(SocketMessage messageParam)
        {
            /*
            // Don't process the command if it was a System Message
            var message = messageParam as SocketUserMessage;
            if (message == null) return;
            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;
            // Determine if the message is a command, based on if it starts with '!' or a mention prefix
            if (!(message.HasCharPrefix('$', ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos))) return;
            // Create a Command Context
            var context = new CommandContext(client, message);
            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            var result = await commands.ExecuteAsync(context, argPos, services);
            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync(result.ErrorReason);
                */
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
