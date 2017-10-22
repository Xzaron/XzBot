using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Discord;
using Discord.WebSocket;
using Discord.Commands;

namespace XzBotDiscord
{
    class CustomCommands
    {

        public int WarChestCurrent = 0;
        string warChestFile = @"..\..\warChest.txt";
        bool fileExists = false;
        private Logging logging;

        private Program mainProgram;


        public DateTime dayStart;
        public DateTime nightStart;
        public bool isNight = false;
        public int minuteCounter = 0;

        public string[] admins = { "Xzaron", "Defector", "Rice" };
        public string[] officers = { "Tyriel", "", "", "" };

        public CustomCommands(Program program)
        {
            mainProgram = program;
            logging = new Logging();
            checkFileExists();
        }

        public string IncomingMessage(SocketUser user,string channelName,string message)
        {
            string returnedString = "";

            returnedString = AllChannelCommands(user, message, channelName);
            List<string> roles = mainProgram.GetRolesUserById(user.Id);

            if (returnedString.Length == 0)
            {
                switch (channelName)
                {
                    case "warchest":
                        logging.writeLog(channelName + " : " + message);
                        returnedString = Warchest(roles, message, user.Username);
                        break;
                    case "devroom":
                        returnedString = Devroom(user.Username, message);
                        break;
                }
            }

            return returnedString;
        }


        #region AllChannels
        private string AllChannelCommands(SocketUser user,string message,string channel)
        {
            Boolean command = message.Contains("$") ? true : false;
            string returnString = "";

            string[] allWords = message.Split(' ');
            List<string> roles = mainProgram.GetRolesUserById(user.Id);

            if (command == true)
            {

                switch (allWords[0])
                {
                    case "$help":
                        string channelHelp = message.Replace("$help","");
                        channelHelp = channelHelp.Replace(" ", "");
                        if (allWords.Length > 1 && allWords[1].Length > 0)
                        {
                            channel = channelHelp;
                        }
                        else
                        {
                            channel = "all";
                        }

                        returnString = BuildHelpString(roles, channel,user.Username);
                        break;

                    case "$startnight":
                        nightStart = DateTime.Now;
                        isNight = true;
                        break;
                    case "$startday":
                        dayStart = DateTime.Now;
                        isNight = false;
                        break;
                    case "$night":
                        returnString =  FindNight();
                        break;
                    case "$message":
                            if (CheckUserpermissions(roles, "Officer", user.Username))
                                mainProgram.MessageChannel(allWords);
                        break;
                    case "$gametime":
                            if(allWords.Length > 2)
                                SetGameTime(allWords[1], allWords[2]);
                        break;
                    case "$nodewarchecklist":
                        returnString = "https://docs.google.com/forms/d/e/1FAIpQLSdOALhPpcH47g9wDecc_tMH4L9itTfuCoA61OoivcycdCbGzg/viewform";
                        break;
                }
            }

            return returnString;
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
                    if(minutes < 10)
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

        private void SetGameTime(string time,string ampm)
        {
            DateTime dayStartTime = new DateTime(2017, 6, 26, 7, 0, 0);
            DateTime nightStartTime = new DateTime(2017, 6, 26, 22, 0, 0);
            DateTime sentDatetime = DateTime.ParseExact("6/26/2017 "+ time + " " + ampm +"", "M/dd/yyyy h:mm tt", CultureInfo.InvariantCulture);

            if(ampm.ToLower().Equals("am"))
            {
                if(sentDatetime.Hour >= 7)
                {
                    //Day
                    isNight = false;
                    TimeSpan difference = sentDatetime - dayStartTime;
                    double actualSeconds = ((difference.Hours*60) + difference.Minutes) * (13.3333);
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



        #region Help

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

        #endregion

        #region Devroom
        private string Devroom(string userName,string message)
        {
            string returnString = "";
            return returnString;
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


        private void checkFileExists()
        {
            string curDir = @"..\..\";

            if (File.Exists(warChestFile))
            {
                fileExists = true;
                string WarcChestText = System.IO.File.ReadAllText(warChestFile);
                int parsedNumber = 0;
                bool result = Int32.TryParse(WarcChestText, out parsedNumber);
                WarChestCurrent = parsedNumber;
            }
            else
            {
                if (!Directory.Exists(curDir))
                {
                    Directory.CreateDirectory(curDir);
                }

                File.Create(warChestFile);
                fileExists = true;
            }
        }

        






    }

    




}
