using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using System.Net.Sockets;
using System.IO;
using System.Net;
using Newtonsoft.Json;

using Discord.WebSocket;




namespace XzBotDiscord
{

    public class serverReturn
    {
        public string Server { get; set; }
        public string Msg { get; set; }
        public string Error { get; set; }
    }

    public class PartialReturn
    {
        public string RefString { get; set; }
        public string StringA { get; set; }
        public string StringB { get; set; }
        public string StringC { get; set; }

        public PartialReturn(string refString, string stringA, string stringB, string stringC)
        {
            RefString = refString;
            StringA = stringA;
            StringB = stringB;
            StringC = stringC;
        }
    }

    class Profiles
    {
        int errorCount = 0;
        SQLController sqlController;
        private string currentComputer = "core";

       

public Profiles()
        {
            CheckComputer();
            sqlController = new SQLController();
        }

        public string CreateImage(string UserID,string avatarURL)
        {
            TcpClient socketForServer;
            string msg = "(DiscordAdmin)(imageCreation)(DiscordProfile)(" + UserID + ")("+ avatarURL + ")<EOF>";
            try
            {
                //socketForServer = new TcpClient("192.168.200.150", 12348);
                socketForServer = new TcpClient("192.168.200.50", 12346);
            }
            catch (Exception e)
            {
                Console.WriteLine("Couldn't Connect");
                return "Error - " + e.ToString();
                errorCount++;
                return "";
            }
            NetworkStream stream = socketForServer.GetStream();
            StreamReader sr = new StreamReader(stream);
            StreamWriter sw = new StreamWriter(stream);

            try
            {
                string output = msg;

                sw.WriteLine(output);
                Console.WriteLine("Client Message");
                sw.Flush();

                string response = sr.ReadToEnd();
                serverReturn convertObject = JsonConvert.DeserializeObject<serverReturn>(response);
                stream.Close();

                if (currentComputer.Equals("core"))
                {
                    if (convertObject.Msg.Contains("Z:"))
                        convertObject.Msg = convertObject.Msg.Replace("Z:", "E:\\Share");
                }
                else
                {
                    if (convertObject.Msg.Contains("E:"))
                        convertObject.Msg = convertObject.Msg.Replace("E:\\Share", "Z:");
                }

                return convertObject.Msg;
            }
            catch
            {
                Console.WriteLine("There was an error on the server");
            }

            stream.Close();
            return "";
        }
        public string SetBio(SocketUser user,string message)
        {
            if(message.Length < 250)
            {
                sqlController.UpdateGo("Users", "bio = '" + sqlController.SQLReplaceAPO(message) + "'", " user_id = " + user.Id);
            }
            else
            {
                return "Message must be less than 250 characters";
            }

            return null;
        }

        public string CreateBGList()
        {
            TcpClient socketForServer;
            string msg = "(DiscordAdmin)(imageCreation)(DiscordBGs)()()<EOF>";
            try
            {
                socketForServer = new TcpClient("192.168.200.150", 12348);
            }
            catch (Exception e)
            {
                Console.WriteLine("Couldn't Connect");
                return "Error - " + e.ToString();
                errorCount++;
                return "";
            }
            NetworkStream stream = socketForServer.GetStream();
            StreamReader sr = new StreamReader(stream);
            StreamWriter sw = new StreamWriter(stream);

            try
            {
                string output = msg;
                sw.WriteLine(output);
                Console.WriteLine("Client Message");
                sw.Flush();

                string response = sr.ReadToEnd();
                serverReturn convertObject = JsonConvert.DeserializeObject<serverReturn>(response);
                stream.Close();
                return convertObject.Msg;
            }
            catch
            {
                Console.WriteLine("There was an error on the server");
            }

            stream.Close();
            return "";
        }

        public void SetBG(int bgNumber, SocketUser user)
        {
            string[] files = Directory.GetFiles(@"Z:\\Discord\\Bgs");

            if(files.Length >= bgNumber - 1)
            {
                sqlController.UpdateGo("Users", "profile_bg = '" + files[bgNumber - 1] + "'", " user_id = " + user.Id);
            }
        }

        private void CheckComputer()
        {
            IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            if (ipAddress.ToString().Equals("192.168.200.50"))
            {
                currentComputer = "core";
            }
            else
            {
                if (ipAddress.ToString().Equals("192.168.200.150"))
                {
                    currentComputer = "Antharas";
                }
            }
        }

    }
}
