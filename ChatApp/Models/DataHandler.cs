

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace ChatApp.Models
{
    internal class DataHandler
    {
        private string indexPath;

        private int chatCounter;
        private string filePath;

        public DataHandler()
        {
            filePath = "..\\..\\..\\..\\chatlogs\\";

            chatCounter = 0;

            indexPath = filePath + "chatIndex.json";
        }

        public void NewChat(string name)
        {
            // Find a filename that is not used
            while (File.Exists(filePath + chatCounter.ToString() + ".json"))
            {
                chatCounter++;
            }

            // Get and convert JSON array into history tab list
            string existingJSON = GetHistoryIndex();
            List<HistoryTab> tabs = JsonConvert.DeserializeObject<List<HistoryTab>>(existingJSON);

            //Create new history tab object and add to the array
            HistoryTab historytab = new HistoryTab(name, DateTime.Now, chatCounter.ToString());
            tabs.Add(historytab);

            //Write back the array to the JSON file
            File.WriteAllText(indexPath, JsonConvert.SerializeObject(tabs));

            
            File.AppendAllText(filePath + chatCounter.ToString() + ".json", "[]");
        }
        public void NewMessage(MessageRequest message)
        {
            string existingJSON = File.ReadAllText(filePath + chatCounter.ToString() + ".json");
            List<MessageRequest> Messages = JsonConvert.DeserializeObject<List<MessageRequest>>(existingJSON);

            
            Messages.Add(message);

            //Write back the array to the JSON file
            File.WriteAllText(filePath + chatCounter.ToString() + ".json", JsonConvert.SerializeObject(Messages));
        }

        public string GetHistoryIndex()
        {
            if(!File.Exists(indexPath))
            {
                File.AppendAllText(indexPath, "[]");
            }
           return File.ReadAllText(indexPath);
        }

        public string GetMessageFile(string index)
        {            
            return File.ReadAllText(filePath + index + ".json");
        }

    }
}
