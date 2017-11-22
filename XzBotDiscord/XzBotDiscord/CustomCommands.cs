﻿using System;
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
    class CustomCommands
    {

        public int WarChestCurrent = 0;
        bool fileExists = false;
        private Logging logging;
        string warChestFile = @"..\..\warChest.txt";

        private DiscordManager discordManager;
        ReadWriteFile readWriteFile;
        Profiles profiles;


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

        #region BDO
        public string nodewarchecklist(SocketUser user, string message, ISocketMessageChannel channel)
        {
            return "https://docs.google.com/forms/d/e/1FAIpQLSdOALhPpcH47g9wDecc_tMH4L9itTfuCoA61OoivcycdCbGzg/viewform";
        }
        public string gametime(SocketUser user, string message, ISocketMessageChannel channel)
        {
            string[] allWords = SplitToStringArray(message);
            if (allWords.Length > 2)
                SetGameTime(allWords[1], allWords[2]);
            return null;
        }
        public string night(SocketUser user, string message, ISocketMessageChannel channel)
        {
            return FindNight();
        }
        public string startnight(SocketUser user, string message, ISocketMessageChannel channel)
        {
            nightStart = DateTime.Now;
            isNight = true;
            return null;
        }
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
        
        #region Officer Commands
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
        public string purge(SocketUser user, string message, ISocketMessageChannel channel)
        {
            List<string> roles = GetRolesByUserId(user, channel);
            string[] allWords = SplitToStringArray(message);
            if (CheckUserpermissions(roles, "Admin", user.Username))
                discordManager.DeleteMessagesFromChannel(Int16.Parse(allWords[1]), channel);
            return null;
        }
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
        public string profile(SocketUser user,string message, ISocketMessageChannel channel)
        {
            string fileName = profiles.CreateImage(user.Id.ToString(), user.GetAvatarUrl());
            discordManager.SendFileToChannel(fileName, channel);
            return null;
        }
        public string setbackground(SocketUser user, string message, ISocketMessageChannel channel)
        {
            string[] allWords = SplitToStringArray(message);
            profiles.SetBG(Int16.Parse(allWords[1]), user);
            return null;
        }
        public string getbackgrounds(SocketUser user, string message, ISocketMessageChannel channel)
        {
            string fileName = profiles.CreateBGList();
            discordManager.SendFileToChannel(fileName, channel);
            return null;
        }
        public string setbio(SocketUser user, string message, ISocketMessageChannel channel)
        {
            string returnString = profiles.SetBio(user, message);
            return returnString;
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

            return BuildHelpString(roles, tmpChannelName, user.Username);
        }

        private string BuildHelpString(List<string> roles,string channel,string userName)
        {
            string returnString = "";

            if(channel.Equals("all"))
            {
                returnString = "Help Commands for Channels \n";
                returnString += "$help warchest \n";
                returnString += "\n$startnight \n";
                returnString += "$startday \n";
                returnString += "$night \n";
                returnString += "$gametime ex.  $gametime 8:51 AM \n";
                
            }
            else
            {
                returnString = MemberHelpString(channel);

                if(CheckUserpermissions(roles, "officer", userName))
                    returnString += OfficerHelpString(channel);

                if (CheckUserpermissions(roles, "admin", userName))
                    returnString += AdminHelpString(channel);
            }

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
