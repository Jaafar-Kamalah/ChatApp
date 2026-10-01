using System;
using System.Text.Json.Serialization;

namespace ChatApp.Models
{
    class HistoryTab
    {
        [JsonPropertyName("name")]
        public string Name { get; }

        [JsonPropertyName("date")]
        public DateTime Date { get; }

        [JsonPropertyName("filename")]
        public string FileName { get; }

        public HistoryTab(string name, DateTime date, string filename) //Also needs some way to store messages or an identifier for a seperate message storage?
        {
            Name = name;
            Date = date;
            FileName = filename;
        }
    }
}
