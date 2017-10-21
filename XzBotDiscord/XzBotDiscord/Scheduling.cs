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

        public User(string userName,ulong userId)
        {
            UserName = userName;
            UserId = userId;
            UserPoints = 0;
        }

        public ulong GetUserId()
        {
            return UserId;
        }
    }


    class Scheduling
    {
        static Program mainProgram;
        static List<ScheduleAppt> schedulingList;
        static List<User> userList;


        public Scheduling(Program program)
        {
            mainProgram = program;
            schedulingList = new List<ScheduleAppt>();
            userList = new List<User>();

            ScheduleAppt recordUsersAppt = new ScheduleAppt(DateTime.Now, TimeSpan.FromDays(999999),TimeSpan.FromMinutes(5), "getActiveVoiceUsers");
            schedulingList.Add(recordUsersAppt);

            Thread mainThread = new Thread(Scheduling.RunScheduling);
            mainThread.Start();
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

            }
        }

        private static void GetActiveVoiceUsers()
        {
            List<SocketGuildChannel> channelList = mainProgram.GetChannelList(true);
            List<SocketGuildUser> socketUserList = new List<SocketGuildUser>();
            int exists = -1;

            for(int i=0;i < channelList.Count;i++)
            {
                for(int j=0;j < channelList[i].Users.Count;j++)
                {
                    for(int k=0;k < userList.Count;k++)
                    {
                        if(userList[k].GetUserId() == channelList[i].Users.ElementAt(j).Id)
                        {
                            exists = k;
                            break;
                        }
                    }

                    if(exists > -1)
                    {
                        userList[exists].UserPoints++;
                    }
                    else
                    {
                        if (!channelList[i].ToString().Contains("AFK"))
                        {
                            User newUser = new User(channelList[i].Users.ElementAt(j).ToString(), channelList[i].Users.ElementAt(j).Id);
                            userList.Add(newUser);
                        }
                    }
                }
            }
        }








    }
}
