using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace XzBotDiscord
{
    public class ReadWriteFile
    {
        Program mainProgram;

        public ReadWriteFile(Program program)
        {
            mainProgram = program;
        }

        public void WriteToFile(bool overrideFile, string text, string fileName = @"Filename.txt")
        {

            if (overrideFile == true)
            {
                File.WriteAllText(fileName, text);
            }
            else
            {
                File.AppendAllText(fileName, text);
            }
        }


        public string ReadAllFromFile(string fileName = @"Filename.txt")
        {
            string contents = File.ReadAllText(fileName);
            return contents;
        }


        //Create a function that searches a file to see if a string exists and returns true or false
        public bool CheckForString(string word, string fileContents)
        {

            int index = fileContents.IndexOf(word);

            if (index >= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        //Create a function that search a file for a string and then gets the line that string is in and saves it to a string array.
        public List<string> FindAllLinesFromString(string word, string fileName = @"Filename.txt")
        {
            List<string> found = new List<string>();
            string[] lines = File.ReadAllLines(fileName);
            foreach (string line in lines)
            {
                if (line.Contains(word))
                {
                    found.Add(line);
                }
            }
            return found;

        }

        public string[] ReturnAllLinesAsArray(string fileName = @"Filename.txt")
        {
            try
            {
                string[] lines = File.ReadAllLines(fileName);
                return lines;
            }
            catch(Exception e)
            {

            }
            return null;
        }

        public Dictionary<string,string> CreateDictFromStringArray(string[] stringArray,char seperator = ':')
        {
            char tmpSeperator = seperator;
            Dictionary<string, string> newDict = new Dictionary<string, string>();
            for (int i = 0; i < stringArray.Length; i++)
            {
                string[] tmpArray = stringArray[i].Split(tmpSeperator);
                newDict.Add(tmpArray[0], tmpArray[1]);
            }
            return newDict;
        }

    }
}
