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

        public ReadWriteFile readWriteFile;
        private DiscordManager discordManager;

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            readWriteFile = new ReadWriteFile();
            client = new DiscordSocketClient();
            services = new ServiceCollection().BuildServiceProvider();
            commands = new CommandService();

            string[] allLines = readWriteFile.ReturnAllLinesAsArray("c:\\Users\\Public\\Documents\\DiscordSQLConnection.txt");
            Dictionary<string, string> sqlDict = readWriteFile.CreateDictFromStringArray(allLines, '=');
            string token = sqlDict["Token"];// Remember to keep this private!

            discordManager = new DiscordManager(client);

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

        }


    }












}
