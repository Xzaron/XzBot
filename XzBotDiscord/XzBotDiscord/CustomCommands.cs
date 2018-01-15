using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;

namespace XzBotDiscord
{

    class HelpAttribute : Attribute
    {
        public string HelpArea { get; set; }
        public string Descr { get; set; }

    }

    public class HelpArea
    {
        string Area;
        public List<Tuple<string, string>> FunctionList;
        public HelpArea(string area, Tuple<string, string> incomingTuple)
        {
            Area = area;
            FunctionList = new List<Tuple<string, string>>();
            FunctionList.Add(incomingTuple);
        }

        public string GetArea()
        {
            return Area;
        }
    }

    class CustomCommands
    {

        public int WarChestCurrent = 0;
        bool fileExists = false;
        private Logging logging;
        string warChestFile = @"..\..\warChest.txt";

        private DiscordManager discordManager;
        ReadWriteFile readWriteFile;
        Profiles profiles;
        Streaming streaming;

        public DateTime dayStart;
        public DateTime nightStart;
        public bool isNight = false;
        public int minuteCounter = 0;

        public CustomCommands(DiscordManager program)
        {
            discordManager = program;
            logging = new Logging();
            readWriteFile = new ReadWriteFile();
            profiles = new Profiles();
            streaming = new Streaming(program);
            ReadInWarchestNumber();  

        }

        public string IncomingMessage(SocketUser user, ISocketMessageChannel channel, string message)
        {
            string returnedString = "";
            Boolean command = message.Contains("$") ? true : false;
            string[] allWords = message.Split(' ');
            string newMessage = message.Replace("$", "");
            //Calling base functions from command
            Type thisType = this.GetType();
            MethodInfo theMethod = thisType.GetMethod(allWords[0].ToLower().Replace("$", ""));
            object[] objectArray = { user, newMessage, channel };
            returnedString = (string)theMethod.Invoke(this, objectArray);

            var chnl = (SocketGuildChannel)channel;
            var GuildName = chnl.Guild.Name;

            List<string> roles = discordManager.GetRolesUserById(user.Id, GuildName);
            //Incomplete Need better way to separate commands for specific channels
            if (returnedString.Length == 0)
            {
                switch (channel.Name)
                {
                    case "warchest":
                        logging.writeLog(channel.Name + " : " + message);
                        returnedString = Warchest(roles, message, user.Username);
                        break;
                }
            }

            return returnedString;
        }

        #region General Commands

        [Help(HelpArea = "General Commands", Descr = "#gets list of roles for the server")]
        public string roles(SocketUser user, string message, ISocketMessageChannel channel)
        {
            string guildName = GetGuildName(channel);
            List<SocketRole> roleList =  discordManager.GetRolesForServer(guildName);
            string returnString = "The list of roles for the server : ";
            foreach (SocketRole role in roleList)
            {
                if(!role.Name.Contains("@everyone"))
                    returnString += role.Name + ", ";
            }
            return returnString;
        }

        [Help(HelpArea = "General Commands", Descr = "role #sets your role to specific role on the server(same as addrole)")]
        public string setrole(SocketUser user, string message, ISocketMessageChannel channel)
        {
            string guildName = GetGuildName(channel);
            string[] allWords = SplitToStringArray(message);
            List<SocketRole> roleList = discordManager.GetRolesForServer(guildName);

            string roleName = message.Replace("setrole", "");
            bool returned = false;
            foreach (SocketRole role in roleList)
            {
                if(roleName.ToString().ToLower().Trim().Equals(role.Name.ToLower()))
                {
                    roleName = role.Name;
                    returned = discordManager.SetRoleForUser(role, user, guildName,false);
                }
            }
 
            if(returned == true)
            {
                return "Role " + roleName + " has been added";
            }
            return "Role " + roleName + " has NOT been added";
        }
        [Help(HelpArea = "General Commands", Descr = "role #sets your role to specific role on the server(same as setrole)")]
        public string addrole(SocketUser user, string message, ISocketMessageChannel channel)
        {
            return setrole(user, message, channel);
        }


        [Help(HelpArea = "General Commands", Descr = "role #removes your role to specific role on the server")]
        public string removerole(SocketUser user, string message, ISocketMessageChannel channel)
        {
            string guildName = GetGuildName(channel);
            string[] allWords = SplitToStringArray(message);
            List<SocketRole> roleList = discordManager.GetRolesForServer(guildName);

            string roleName = message.Replace("removerole", "");
            bool returned = false;
            foreach (SocketRole role in roleList)
            {
                if (roleName.ToString().ToLower().Trim().Equals(role.Name.ToLower()))
                {
                    roleName = role.Name;
                    discordManager.RemoveRoleForUser(role, user, guildName, false);
                }
            }

            return "Role " + roleName + " has been removed";
        }

        #endregion

        #region Officer Commands
        [Help(HelpArea = "Officer Commands", Descr = "#Has the bot message specific channel with message")]
        public string messagechannel(SocketUser user, string message, ISocketMessageChannel channel)
        {
            List<string> roles = GetRolesByUserId(user, channel);
            string[] allWords = SplitToStringArray(message);
            if (CheckUserpermissions(roles, "Officer", user.Username))
                discordManager.MessageChannel(allWords);
            return null;
        }

        
        #endregion

        #region Admin Commands
        [Help(HelpArea = "Admin Commands", Descr = "X #Removes the last X messages from current channel")]
        public string purge(SocketUser user, string message, ISocketMessageChannel channel)
        {
            List<string> roles = GetRolesByUserId(user, channel);
            string[] allWords = SplitToStringArray(message);
            if (CheckUserpermissions(roles, "Admin", user.Username))
                discordManager.DeleteMessagesFromChannel(Int16.Parse(allWords[1]), channel);
            return null;
        }
        [Help(HelpArea = "Admin Commands", Descr = "#Has the bot private message user with message")]
        public string privatemessage(SocketUser user, string message, ISocketMessageChannel channel)
        {
            //Incomplete
            List<string> roles = GetRolesByUserId(user, channel);
            string[] allWords = SplitToStringArray(message);
            if (CheckUserpermissions(roles, "Admin", user.Username))
                discordManager.SendPMToUser("Hello", user);
            return null;
        }
        #endregion

        #region Profiles
        [Help(HelpArea = "Profile", Descr = "#Displays user profile")]
        public string profile(SocketUser user,string message, ISocketMessageChannel channel)
        {
            string fileName = profiles.CreateImage(user.Id.ToString(), user.GetAvatarUrl());
            discordManager.SendFileToChannel(fileName, channel);
            return null;
        }
        [Help(HelpArea = "Profile", Descr = "X #Sets the users background to the number X")]
        public string setbackground(SocketUser user, string message, ISocketMessageChannel channel)
        {
            string[] allWords = SplitToStringArray(message);
            profiles.SetBG(Int16.Parse(allWords[1]), user);
            return null;
        }
        [Help(HelpArea = "Profile", Descr = "#Displays image of current backgrounds")]
        public string getbackgrounds(SocketUser user, string message, ISocketMessageChannel channel)
        {
            string fileName = profiles.CreateBGList();
            discordManager.SendFileToChannel(fileName, channel);
            return null;
        }
        [Help(HelpArea = "Profile", Descr = "This Is My Bio #Sets users bio to message")]
        public string setbio(SocketUser user, string message, ISocketMessageChannel channel)
        {
            string returnString = profiles.SetBio(user, message);
            return returnString;
        }

        #endregion

        #region Streaming

        [Help(HelpArea = "Streaming", Descr = "Chanel Name #Sets your twitch channel name with the discord bot. Just use your channel name you do not need twitch.tv/ChannelName")]
        public string setstream(SocketUser user, string message, ISocketMessageChannel channel)
        {
            string[] allWords = SplitToStringArray(message);
            streaming.UpdateStreamURL(user, allWords[1]);
            return null;
        }

        #endregion

        #region BDO

        [Help(HelpArea = "BDO", Descr = "#Gets the form for node war signups")]
        public string nodewarchecklist(SocketUser user, string message, ISocketMessageChannel channel)
        {
            return "https://docs.google.com/forms/d/e/1FAIpQLSdOALhPpcH47g9wDecc_tMH4L9itTfuCoA61OoivcycdCbGzg/viewform";
        }
        [Help(HelpArea = "BDO", Descr = "8:27 AM #Sets the current BDO gametime on the bot")]
        public string gametime(SocketUser user, string message, ISocketMessageChannel channel)
        {
            string[] allWords = SplitToStringArray(message);
            if (allWords.Length > 2)
                SetGameTime(allWords[1], allWords[2]);
            return null;
        }
        [Help(HelpArea = "BDO", Descr = "#Returns how long until night starts or how much time is left in the current night")]
        public string night(SocketUser user, string message, ISocketMessageChannel channel)
        {
            return FindNight();
        }
        [Help(HelpArea = "BDO", Descr = "#Resets the bot to say night just started")]
        public string startnight(SocketUser user, string message, ISocketMessageChannel channel)
        {
            nightStart = DateTime.Now;
            isNight = true;
            return null;
        }
        [Help(HelpArea = "BDO", Descr = "#Resets the bot to say day just started")]
        public string startday(SocketUser user, string message, ISocketMessageChannel channel)
        {
            dayStart = DateTime.Now;
            isNight = false;
            return null;
        }
        private string FindNight()
        {
            string returnString = "";
            //returnString = "Night Timer may be off please use $gametime XX:XX AM or PM to reset and get the correct time if it has not been done recentl \n ";
            if (isNight == true)
            {
                DateTime currentNightEnd = nightStart.AddMinutes(40);
                DateTime now = DateTime.Now;

                TimeSpan minutesLeft = currentNightEnd - now;
                double minutes = Math.Round(minutesLeft.TotalMinutes);
                returnString += "Night will end in approx : " + minutes.ToString() + " minutes";
            }
            else
            {
                DateTime currentDayEnd = dayStart.AddMinutes(200);
                DateTime now = DateTime.Now;

                TimeSpan minutesLeft = currentDayEnd - now;
                double minutes = Math.Round(minutesLeft.TotalMinutes);

                if (minutes < 0)
                {
                    returnString += "Please run $gametime command to reset the time. $gametime 8:51 AM \n";
                }
                else if (minutes > 60)
                {
                    string minutes2 = minutesLeft.Minutes.ToString();
                    if (minutes < 10)
                    {
                        minutes2 += "0" + minutes;
                    }
                    returnString += "Night will start in approx :  " + minutesLeft.Hours + ":" + minutes2 + "";
                }
                else
                {
                    returnString += "Night will start in approx : " + minutes + " minutes";
                }
            }
            return returnString;
        }
        private void SetGameTime(string time, string ampm)
        {
            DateTime dayStartTime = new DateTime(2017, 6, 26, 7, 0, 0);
            DateTime nightStartTime = new DateTime(2017, 6, 26, 22, 0, 0);
            DateTime sentDatetime = DateTime.ParseExact("6/26/2017 " + time + " " + ampm + "", "M/dd/yyyy h:mm tt", CultureInfo.InvariantCulture);

            if (ampm.ToLower().Equals("am"))
            {
                if (sentDatetime.Hour >= 7)
                {
                    //Day
                    isNight = false;
                    TimeSpan difference = sentDatetime - dayStartTime;
                    double actualSeconds = ((difference.Hours * 60) + difference.Minutes) * (13.3333);
                    DateTime dayStartTmp = DateTime.Now - TimeSpan.FromSeconds(actualSeconds);
                    dayStart = dayStartTmp;
                }
                else
                {
                    //Night
                    isNight = true;
                    sentDatetime = DateTime.ParseExact("6/27/2017 " + time + " " + ampm + "", "M/dd/yyyy h:mm tt", CultureInfo.InvariantCulture);
                    TimeSpan difference = sentDatetime - nightStartTime;
                    double actualSeconds = ((difference.Hours * 60) + difference.Minutes) * (4.444444444);
                    DateTime nightStartTmp = DateTime.Now - TimeSpan.FromSeconds(actualSeconds);
                    nightStart = nightStartTmp;
                }
            }
            else
            {
                if (sentDatetime.Hour < 22)
                {
                    //Day
                    isNight = false;
                    TimeSpan difference = sentDatetime - dayStartTime;
                    double actualSeconds = ((difference.Hours * 60) + difference.Minutes) * (13.3333);
                    DateTime dayStartTmp = DateTime.Now - TimeSpan.FromSeconds(actualSeconds);
                    dayStart = dayStartTmp;
                }
                else
                {
                    //Night
                    isNight = true;
                    TimeSpan difference = sentDatetime - nightStartTime;
                    double actualSeconds = ((difference.Hours * 60) + difference.Minutes) * (5);
                    DateTime nightStartTmp = DateTime.Now - TimeSpan.FromSeconds(actualSeconds);
                    nightStart = nightStartTmp;
                }
            }


            int four = 4;

        }
        #endregion









        #region Help
        public string help(SocketUser user, string message, ISocketMessageChannel channel)
        {
            //Incomplete
            string tmpChannelName = "";
            string channelHelp = message.Replace("$help", "");
            string[] allWords = SplitToStringArray(message);
            List<string> roles = GetRolesByUserId(user, channel);
            channelHelp = channelHelp.Replace(" ", "");
            if (allWords.Length > 1 && allWords[1].Length > 0)
            {
                tmpChannelName = channelHelp;
            }
            else
            {
                tmpChannelName = "all";
            }

            return BuildHelpString(roles, channel, user.Username);
        }

        private string BuildHelpString(List<string> roles, ISocketMessageChannel channel,string userName)
        {
            List<HelpArea> helpAreaList = new List<HelpArea>();


            Type thisType = this.GetType();
            MethodInfo[] myArrayMethodInfo = thisType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (MethodInfo method in myArrayMethodInfo)
            {
                if (method.GetCustomAttributes(false).Length > 0)
                {
                    object[] atts = method.GetCustomAttributes(typeof(HelpAttribute), true);
                    string area = "";
                    string descrText = "";
                    if (atts[0] != null)
                    {
                        area = (atts[0] as HelpAttribute).HelpArea;
                        descrText = (atts[0] as HelpAttribute).Descr;
                    }
                    int exists = -1;
                    for (int i = 0; i < helpAreaList.Count; i++)
                    {
                        if(helpAreaList[i].GetArea().Equals(area))
                        {
                            exists = i;
                        }
                    }

                    if(exists == -1)
                    {
                        helpAreaList.Add(new HelpArea(area, new Tuple<string, string>(method.Name, descrText)));
                    }
                    else
                    {
                        helpAreaList[exists].FunctionList.Add(new Tuple<string, string>(method.Name, descrText));
                    }

                }
            }

            string returnString = "";
            //returnString += "$yellow \r\n";
            //returnString += "```";

            //Discord markdown cheatsheet https://gist.github.com/ringmatthew/9f7bbfd102003963f9be7dbcf7d40e51

            //discord embeds https://discordapp.com/developers/docs/resources/channel#embed-object     https://anidiotsguide.gitbooks.io/discord-js-bot-guide/examples/using-embeds-in-messages.html

            var eb = new EmbedBuilder() { Title = "**XzBot** Help Commands",Description = "\nUse the $command to interact with the bot. \nAnything in light grey text describes parameters that might be used in assiotiation with that command.\nAnything in dark grey and after the # is a description of what the function does. \nPlease message @Xzaron if you have any trouble'''", Color = Color.Blue };

            for (int i = 0;i < helpAreaList.Count;i++)
            {
                string fieldName = helpAreaList[i].GetArea();

                if(fieldName.Contains("Admin"))
                {
                    if (!CheckUserpermissions(roles, "Admin", userName))
                    {
                        continue;
                    }
                }

                if (fieldName.Contains("Officer"))
                {
                    if (!CheckUserpermissions(roles, "Officer", userName))
                    {
                        continue;
                    }
                }

                string message = "```bash\n";
                for (int j=0;j<helpAreaList[i].FunctionList.Count;j++)
                {
                    int additionLength = ("$" + helpAreaList[i].FunctionList[j].Item1 + " " + helpAreaList[i].FunctionList[j].Item2 + "\n").Length;

                    if (message.Length + additionLength < 1010)
                    {
                        message += "$" + helpAreaList[i].FunctionList[j].Item1 + " " + helpAreaList[i].FunctionList[j].Item2 + "\n";
                    }
                    else
                    {
                        message += "```";
                        eb.AddField(fieldName, message);
                        message = "```bash\n";
                    }

                }
                message += "```";
                eb.AddField(fieldName, message);

            }

            discordManager.EmbedToUser(eb, null, userName);


            //eb.AddField(new EmbedFieldBuilder("dflgjfjklg"));









            return returnString;
        }

        private string MemberHelpString(string channelName)
        {
            string returnString = "";

            switch (channelName)
            {
                case "warchest":
                    returnString += "Warchest Commands: \n";
                    returnString += "\t\t$warchesttotal \n";
                    returnString += "\t\t$warchest +/- Value \n";
                    returnString += "\t\t$newbot \n";
                    break;
            }

            return returnString;
        }
        private string OfficerHelpString(string channelName)
        {
            string returnString = "";

            switch (channelName)
            {
                case "warchest":
                    returnString += "Officer : \n";
                    returnString += "\t\t$setwarchest +Value \n";
                    break;
            }

            return returnString;
        }
        private string AdminHelpString(string channelName)
        {
            string returnString = "";

            switch (channelName)
            {
                case "warchest":
                    break;
                case "devroom":
                    break;
            }

            return returnString;
        }
        #endregion

        #region Warchest

        private string Warchest(List<string> roles,string message, string userName)
        {
            Boolean command = message.Contains("$") ? true : false;
            string returnString = "";

            if (command == true)
            {
                string commandMsg = "";
                int commandArea = 0;
                message = message.Replace("\n", " ");
                string[] commandString = message.Split(' ');

                for(int i=0;i < commandString.Length;i++)
                {
                    if(commandString[i].Contains("$"))
                    {
                        //message = commandString[i];
                        commandArea = i;
                    }
                }

                switch (commandString[commandArea].ToLower())
                {
                    case "$warchesttotal":
                        returnString = "Warchest Total : " + WarChestCurrent;
                        break;
                    case "$setwarchest":
                        if (CheckUserpermissions(roles, "officer", userName))
                        {
                            returnString = SetWarChestValue(message);
                        }
                        break;
                    case "$newbot":
                        returnString = "We have a new bot for the warchest! Please use the format \n $warchest +X";
                        break;
                    case "$warchest":
                        returnString =  Warchest(roles, message.Replace("$",""), userName);
                        break;
                }
            }
            else
            {

                string[] allWords = message.Split('\n');
                Boolean isParsedCorrectly = false;

                foreach (string currentString in allWords)
                {
                    Boolean warChestLine = (currentString.ToLower().Contains("warchest")) ? true : false;
                    int lastPlus = currentString.LastIndexOf("+");
                    int lastMinus = currentString.LastIndexOf("-");
                    int lastSpace = currentString.LastIndexOf(" ");

                    if (lastSpace == -1)
                        lastPlus = currentString.Length;

                    if (warChestLine == true && !currentString.ToLower().Contains("total"))
                    {
                        string numberString = "";
                        int parsedNumber = 0;
                        if (lastPlus > -1)
                        {
                            numberString = currentString.Substring(lastPlus + 1, (currentString.Length - 1) - (lastPlus));
                        }
                        else {
                            if (lastMinus > -1)
                                numberString = currentString.Substring(lastMinus + 1, (currentString.Length - 1) - (lastMinus));
                        }

                        bool result = Int32.TryParse(numberString, out parsedNumber);

                        if (parsedNumber > 0 && parsedNumber < 120)
                        {
                            if (lastPlus > -1 && lastPlus > lastMinus)
                                WarChestCurrent += parsedNumber;
                            if (lastMinus > -1 && lastMinus > lastPlus)
                                WarChestCurrent -= parsedNumber;

                            isParsedCorrectly = true;
                            //Write file out to warchest file holder
                            System.IO.File.WriteAllText(warChestFile, WarChestCurrent.ToString());
                            returnString = "Warchest Total: " + WarChestCurrent;
                        }
                        else
                        {
                            returnString = "Unable to parse message. Please use the format \n +x PieceOfGear -> +y PieceOfGear \n $Warchest +X";
                            logging.writeError(returnString);
                        }
                    }

                }

                if (isParsedCorrectly == false)
                {

                    //returnString = "Unable to parse message. Please use the format \n +x PieceOfGear -> +y PieceOfGear \n Warchest +X";
                    //logging.writeError(returnString);
                }

            }



            return returnString;
        }

        private string SetWarChestValue(string message)
        {
            string returnString = "";
            string[] allWords = message.Split(' ');

            if (allWords.Length > 1)
            {
                int parsedNumber = 0;
                bool result = Int32.TryParse(allWords[1], out parsedNumber);
                WarChestCurrent = parsedNumber;
                System.IO.File.WriteAllText(warChestFile, WarChestCurrent.ToString());
                returnString = "Warchest set to : " + WarChestCurrent;
            }


            return returnString;
        }

        private void ReadInWarchestNumber()
        {
            string WarcChestText = readWriteFile.ReadAllFromFile(warChestFile);
            int parsedNumber = 0;
            bool result = Int32.TryParse(WarcChestText, out parsedNumber);
            WarChestCurrent = parsedNumber;
        }

        #endregion

        #region Permissions

        private Boolean CheckUserpermissions(List<string> roles,string permissionlevel,string userName)
        {
            if (roles.Contains(permissionlevel))
            {
                return true;
            }
            else
            {
                logging.writeError("Invalid permissions : " + userName);
            }
       
            return false;
        }

        #endregion

        #region Class Functions
        private string[] SplitToStringArray(string message)
        {
            string[] allWords = message.Split(' ');
            return allWords;
        }

        private string GetGuildName(ISocketMessageChannel channel)
        {
            var GuildChannel = (SocketGuildChannel)channel;
            var GuildName = GuildChannel.Guild.Name;
            return GuildName;
        }

        private List<string> GetRolesByUserId(SocketUser user, ISocketMessageChannel channel)
        {
            string guidName = GetGuildName(channel);
            return discordManager.GetRolesUserById(user.Id, guidName);
        }
        #endregion

    }
}
