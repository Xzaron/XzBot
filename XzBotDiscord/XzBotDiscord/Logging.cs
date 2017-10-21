using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XzBotDiscord
{
    public class Logging
    {
        bool fileExists = false;
        string curFile = @"..\..\logging.txt";

        //Error Handling
        public bool writeLogs = true;
        public bool writeErrors = true;
        public bool detailedLogs = true;
        List<Error> errorList = new List<Error>();

        public class Error
        {
            public string ErrorMsg { get; set; }
            public DateTime Timestamp { get; set; }

            public Error(string errorMsg, DateTime timestamp)
            {
                ErrorMsg = errorMsg;
                Timestamp = timestamp;
            }
        }

        public Logging()
        {
            checkFileExists();
        }

        private void checkFileExists()
        {
            string curDir = @"..\..\";

            if (File.Exists(curFile))
            {
                fileExists = true;
            }
            else
            {
                if (!Directory.Exists(curDir))
                {
                    Directory.CreateDirectory(curDir);
                }

                File.Create(curFile);
                fileExists = true;
            }
        }

        private void WriteToLog(string incomingString)
        {
            if (fileExists == true)
            {
                try
                {
                    System.IO.File.AppendAllText(curFile, "\r\n" + DateTime.Now.ToString() + " " + incomingString);
                }
                catch (Exception e)
                {
                    this.writeError(e.ToString());
                }
            }
        }

        public void writeError(string error)
        {
            if (writeErrors == true)
            {
                Console.WriteLine("ERROR : " + error);
                this.WriteToLog("ERROR : " + error);
            }
            errorList.Add(new Error(error, DateTime.Now));
        }

        public void writeLog(string log)
        {
            if (writeLogs == true)
            {
                Console.WriteLine(log);
                this.WriteToLog(log);
            }
        }

        public void detailedLog(string log)
        {
            if (detailedLogs == true)
            {
                Console.WriteLine(log);
                this.WriteToLog(log);
            }
        }

        public String getLastAndRecentError()
        {
            string returnString = "";
            if (errorList.Count > 0)
            {
                Error tmpError = errorList[errorList.Count - 1];

                if (tmpError.Timestamp.Add(TimeSpan.FromMinutes(10)) > DateTime.Now)
                {
                    returnString = tmpError.Timestamp.ToShortTimeString() + " : " + tmpError.ErrorMsg;
                }
            }
            return returnString;
        }



    }
}
