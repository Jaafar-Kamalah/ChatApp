using System.Text.Json.Serialization;

namespace ChatApp.Models
{
    class MessageRequest
    {
        [JsonPropertyName("requesttype")]
        public string RequestType { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        public MessageRequest(string requesttype, string name, string message)
        {
            RequestType = requesttype;
            Name = name;
            Message = message;
        }
    }
}
