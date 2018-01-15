using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SqlClient;

using Discord;
using Discord.WebSocket;

namespace XzBotDiscord
{
    public class ScheduleAppt
    {
        private DateTime Startdate;
        private DateTime LastRunTime;
        private TimeSpan Duration;
        private string Activity;
        private TimeSpan Interval;

        public ScheduleAppt(DateTime startDate,TimeSpan duration,TimeSpan interval, string activity)
        {
            Startdate = startDate;
            LastRunTime = Startdate;
            Duration = duration;
            Interval = interval;
            Activity = activity;
        }

        public bool ShouldIRun()
        {
            if (DateTime.Now > (LastRunTime + Interval))
            {
                LastRunTime = DateTime.Now;
                return true;
            }
            return false;
        }

        public string GetActivity()
        {
            return Activity;
        }
    }

    public class User
    {
        public string UserName;
        ulong UserId;
        public int UserPoints;
        private bool DBExists = false;
        public string Nickname;

        public User(string userName, ulong userId, string nickName)
        {
            UserName = userName;
            UserId = userId;
            UserPoints = 5;
        }
        public User(string userName, ulong userId, int points, bool dbExists,string nickName)
        {
            UserName = userName;
            UserId = userId;
            UserPoints = points;
            DBExists = dbExists;
            Nickname = nickName;
        }

        public ulong GetUserId()
        {
            return UserId;
        }

        public bool DoesExistInDB()
        {
            return DBExists;
        }
        public void AddedToDB()
        {
            DBExists = true;
        }
    }


    class Scheduling
    {
        static DiscordManager discordManager;
        static SQLController sqlController;

        static List<ScheduleAppt> schedulingList;
        static List<User> userList;
        static Streaming streaming;

        public Scheduling(DiscordManager program)
        {
            discordManager = program;
            sqlController = new SQLController();
            streaming = new Streaming(program);

            schedulingList = new List<ScheduleAppt>();

            SetUserList();

            InitSchedulingServices();

            Thread mainThread = new Thread(Scheduling.RunScheduling);
            mainThread.Start();
        }

        private void InitSchedulingServices()
        {
            //Tracks a user time in voice chat and handles stuff in the db
            ScheduleAppt usersInDiscordVoice = new ScheduleAppt(DateTime.Now, TimeSpan.FromDays(999999), TimeSpan.FromMinutes(5), "getActiveVoiceUsers");      
            schedulingList.Add(usersInDiscordVoice);
            ScheduleAppt usersCurrentlyStreaming = new ScheduleAppt(DateTime.Now, TimeSpan.FromDays(999999), TimeSpan.FromMinutes(1), "getActiveStreamers");
            schedulingList.Add(usersCurrentlyStreaming);
        }

        private void SetUserList()
        {
            userList = new List<User>();
            List<SqlRow> retreived = sqlController.RetreiveGo("*", "Users", "", "");

            for (int i = 0; i < retreived.Count; i++)
            {
                ulong user_id = ulong.Parse(retreived[i].MainTuple["user_id"]);
                string user_name = retreived[i].MainTuple["user_name"];
                int coins = Int16.Parse(retreived[i].MainTuple["total_coins"]);
                string bio = retreived[i].MainTuple["bio"];
                string nickname = retreived[i].MainTuple["nickname"];
                userList.Add(new User(user_name, user_id, coins, true, nickname));
            }


        }

        private static void RunScheduling()
        {
            Thread.Sleep(5000);
            while (true)
            {
                for (int i = 0; i < schedulingList.Count; i++)
                {
                    if (schedulingList[i].ShouldIRun())
                    {
                        RunAppt(i);
                    }
                    
                }


                Thread.Sleep(1000);
            }
        }

        private static void RunAppt(int index)
        {
            switch (schedulingList[index].GetActivity())
            {
                case "getActiveVoiceUsers":
                    GetActiveVoiceUsers();
                    break;
                case "getActiveStreamers":
                    GetActiveStreamers();
                    break;
            }
        }

        private static void GetActiveStreamers()
        {
            streaming.GetStreamersFromDB();
            streaming.CheckActiveStreamers();
        }

        private static void GetActiveVoiceUsers()
        {
            UpdateActiveVoiceUsers();
            UpdateUserProfilesInDB();
        }
        private static void UpdateActiveVoiceUsers()
        {
            List<SocketGuildChannel> channelList = discordManager.GetChannelList(true,"Heaven and Earth");
            List<SocketGuildUser> socketUserList = new List<SocketGuildUser>();
            SocketGuildUser tmpUser = null;
            int exists = -1;

            for(int i=0;i < channelList.Count;i++)
            {
                exists = -1;
                for (int j=0;j < channelList[i].Users.Count;j++)
                {
                    for(int k=0;k < userList.Count;k++)
                    {
                        if(userList[k].GetUserId() == channelList[i].Users.ElementAt(j).Id)
                        {
                            tmpUser = channelList[i].Users.ElementAt(j);
                            exists = k;
                            break;
                        }
                    }

                    if(exists > -1)
                    {
                        if (!channelList[i].ToString().Contains("AFK"))
                        {
                            if (userList[exists].Nickname == null || userList[exists].Nickname.Equals(""))
                            {
                                if (tmpUser != null)
                                    userList[exists].Nickname = tmpUser.Nickname;
                            }
                            userList[exists].UserPoints += 5;
                        }
                    }
                    else
                    {
                        if (!channelList[i].ToString().Contains("AFK"))
                        {
                            User newUser = new User(channelList[i].Users.ElementAt(j).ToString(), channelList[i].Users.ElementAt(j).Id, channelList[i].Users.ElementAt(j).Nickname);
                            userList.Add(newUser);
                        }
                    }
                }
            }
        }
        private static void UpdateUserProfilesInDB()
        {
            for (int i = 0; i < userList.Count; i++)
            {
                string fieldNames = "user_id,user_name,total_coins,nickname";

                List<string> valuesList = new List<string>();
                valuesList.Add(userList[i].GetUserId().ToString());
                valuesList.Add(userList[i].UserName);
                valuesList.Add(userList[i].UserPoints.ToString());

                if (userList[i].Nickname != null)
                {
                    valuesList.Add(userList[i].Nickname.ToString());
                }
                else
                {
                    valuesList.Add("");
                }
                
                if (userList[i].DoesExistInDB())
                {
                    List<string> fieldList = new List<string>();
                    fieldList.Add("user_id");
                    fieldList.Add("user_name");
                    fieldList.Add("total_coins");
                    fieldList.Add("nickname");
                    string values = sqlController.CreateValuesForUpdate(fieldList, valuesList);
                    sqlController.UpdateGo("Users", values, "user_id = " + userList[i].GetUserId().ToString());
                }
                else
                {
                    string values = sqlController.CreateValues(valuesList);
                    userList[i].AddedToDB();
                    sqlController.InsertGo("Users", fieldNames, values);
                }
            }
        }


    }
}
