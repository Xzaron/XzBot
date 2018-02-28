using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.IO;
using Newtonsoft.Json;

namespace XzBotDiscord
{
    class Twitch
    {
        private string client_secret;
        private string client_id;
        ReadWriteFile readWriteFile = new ReadWriteFile();

        private string access_token = "";
        public Twitch()
        {
            LoginTwitch();
            //CheckIfStreamIsOnline("Xzaron");
        }

        private void LoginTwitch()
        {
            string[] allLines = readWriteFile.ReturnAllLinesAsArray("c:\\Users\\Public\\Documents\\DiscordSQLConnection.txt");
            Dictionary<string, string> sqlDict = readWriteFile.CreateDictFromStringArray(allLines, '=');
            client_secret = sqlDict["Twitch_client_secret"];
            client_id = sqlDict["Twitch_client_id"];

            //POST https://api.twitch.tv/kraken/oauth2/token?client_id =< your client ID>&client_secret =< your client secret>&grant_type = client_credentials&scope =< space - separated list of scopes >

            string postString = string.Format("grant_type=client_credentials");
            byte[] byteArray = Encoding.UTF8.GetBytes(postString);
            byte[] bytes = Encoding.Default.GetBytes("");
            var base64 = Convert.ToBase64String(bytes);
            string url = "https://api.twitch.tv/kraken/oauth2/token?client_id=" + client_id + "&client_secret=" + client_secret + "&grant_type=client_credentials";

            WebRequest request = WebRequest.Create(url);
            request.Method = "POST";
            using (Stream dataStream = request.GetRequestStream())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
                using (WebResponse response = request.GetResponse())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(responseStream))
                        {
                            string responseFromServer = reader.ReadToEnd();
                            dynamic stuff = JsonConvert.DeserializeObject(responseFromServer);
                            access_token = stuff.access_token;
                            //token = JsonConvert.DeserializeObject<<strong>SpotifyToken</strong>>(responseFromServer);
                        }
                    }
                }
            }
        }

        private void RefreshLog()
        {
            string url = "curl -X POST https://api.twitch.tv/kraken/oauth2/token--data-urlencode?grant_type=refresh_token&refresh_token=<yourrefreshtoken>&client_id="+client_id+"&client_secret="+client_secret+"";
            WebRequest request = WebRequest.Create(url);
            request.Method = "GET";
            request.Headers.Add("Authorization", "Bearer " + access_token);

            using (WebResponse response = request.GetResponse())
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                    }
                }
            }
        }

        public bool CheckIfStreamIsOnline(string channelName)
        {
            if (channelName.Contains("https://www."))
                channelName = channelName.Replace("https://www.","");

            if (channelName.Contains("twitch.tv/"))
                channelName = channelName.Replace("twitch.tv/", "");

            string url = "https://api.twitch.tv/kraken/streams/"+channelName+"?client_id="+ client_id + "";
            WebRequest request = WebRequest.Create(url);
            request.Method = "GET";
            request.Headers.Add("Authorization", "Bearer " + access_token);

            try
            {
                using (WebResponse response = request.GetResponse())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(responseStream))
                        {
                            string responseFromServer = reader.ReadToEnd();
                            dynamic stuff = JsonConvert.DeserializeObject(responseFromServer);

                            if (stuff["stream"] != null)
                            {
                                if (stuff["stream"].game != null)
                                {
                                    string game = stuff.stream.game.Value.ToString();
                                    return true;
                                }
                            }
                        }
                    }
                }
            }catch(Exception e)
            {
                int four = 4;
            }
            return false;
        }












    }
}
